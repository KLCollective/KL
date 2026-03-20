using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using KinkLinkClient;
using KinkLinkClient.Dependencies.Glamourer.Domain;
using KinkLinkClient.Dependencies.Glamourer.Services;
using KinkLinkClient.Dependencies.Penumbra.Services;
using KinkLinkClient.Domain.Configurations;
using KinkLinkClient.Utils;
using KinkLinkCommon.Dependencies.Glamourer;
using KinkLinkCommon.Dependencies.Glamourer.Components;
using KinkLinkCommon.Domain.Enums;
using KinkLinkCommon.Domain.Wardrobe;
using Newtonsoft.Json.Linq;

namespace KinkLinkClient.Services;

[Obsolete("Reserved for future use - not currently implemented")]
public enum LayerLocks
{
    InnerGag,
    Gag,
    OuterGag,
    Blindfold,
    Collar,
    Head,
    Chest,
    Hands,
    Feet,
    Legs,
    Earring,
    Necklace,
    Wrists,
    RingR,
    RingL,
    Bonus,
    SetLayer,
}

/// This represents a single equipment item.
/// Special Case: If the Equipment slot is `None` and `GlamourItem` is null it functions as just a mod settings container
public record WardrobeItem
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public GlamourerEquipmentSlot Slot { get; set; }
    public GlamourerItem? Item { get; set; }
    public List<GlamourerMod> Mods { get; set; } = [];
    public Dictionary<string, GlamourerMaterial> Materials { get; set; } = [];
    public RelationshipPriority Priority { get; set; } = RelationshipPriority.Casual;
}

public record WardrobeSet
{
    public Guid Id => Design.Identifier;
    public string Name => Design.Name;
    public string Description => Design.Description;
    public required GlamourerDesign Design { get; set; }
    public RelationshipPriority Priority { get; set; } = RelationshipPriority.Casual;
}

public class ActiveWardrobe
{
    private WardrobeSet? _baseLayer;
    private readonly Dictionary<GlamourerEquipmentSlot, WardrobeItem?> _equipment = new();
    private readonly Dictionary<Guid, WardrobeItem> _characterItems = new();
    private GlamourerShow? _hat;
    private GlamourerIsToggled? _visor;

    public WardrobeSet? BaseLayer
    {
        get => _baseLayer;
        private set => _baseLayer = value;
    }

    public GlamourerShow? Hat
    {
        get => _hat;
        private set => _hat = value;
    }

    public GlamourerIsToggled? Visor
    {
        get => _visor;
        private set => _visor = value;
    }

    public bool IsActive()
    {
        return BaseLayer != null
            || _equipment.Values.Any(v => v != null)
            || Hat != null
            || Visor != null;
    }

    public void SetBaseLayer(GlamourerDesign design, RelationshipPriority priority)
    {
        BaseLayer = new WardrobeSet { Design = design, Priority = priority };
    }

    public void ClearBaseLayer()
    {
        BaseLayer = null;
    }

    public void SetIndividual(GlamourerEquipmentSlot slot, WardrobeItem item)
    {
        _equipment[slot] = item;
    }

    public void ClearIndividual(GlamourerEquipmentSlot slot)
    {
        _equipment[slot] = null;
    }

    public void AddModItem(WardrobeItem item)
    {
        _characterItems[item.Id] = item;
    }

    public void ClearModItem(Guid id)
    {
        if (_characterItems.ContainsKey(id))
            _characterItems.Remove(id);
    }

    public IReadOnlyDictionary<Guid, WardrobeItem> GetCharacterItems() => _characterItems;

    public WardrobeItem? GetIndividual(GlamourerEquipmentSlot slot)
    {
        return _equipment.TryGetValue(slot, out var item) ? item : null;
    }

    public GlamourerDesign? GetBaseLayer() => BaseLayer?.Design ?? null;

    public GlamourerDesign GetCurrentState()
    {
        if (!IsActive())
        {
            Plugin.Log.Error("There is nothing currently set. This should not have been called");
            return new();
        }
        var final = BaseLayer?.Design.Clone() ?? new();

        foreach (var (slot, item) in _equipment)
        {
            if (item is { } glamouritem && glamouritem.Item is { } slotitem)
            {
                SetEquipmentSlot(final.Equipment, slot, slotitem);
                // TODO: Handle the material states
            }
        }

        Plugin.Log.Information(
            "Current equipment state: {EquipmentState}, FinalEquipmentState {FinalEquipmentState}",
            _equipment,
            final.Equipment
        );

        return final;
    }

    public List<GlamourerMod> GetMods()
    {
        var modlist = new List<GlamourerMod>();
        if (BaseLayer is { } baselayer)
        {
            modlist.AddRange(baselayer.Design.Mods);
        }
        foreach (var kvp in _equipment)
        {
            if (kvp.Value != null)
                modlist.AddRange(kvp.Value.Mods);
        }
        foreach (var item in _characterItems)
        {
            modlist.AddRange(item.Value.Mods);
        }
        return modlist;
    }

    private static void SetEquipmentSlot(
        GlamourerEquipment equipment,
        GlamourerEquipmentSlot slot,
        GlamourerItem item
    )
    {
        switch (slot)
        {
            case GlamourerEquipmentSlot.Head:
                equipment.Head = item;
                break;
            case GlamourerEquipmentSlot.Body:
                equipment.Body = item;
                break;
            case GlamourerEquipmentSlot.Hands:
                equipment.Hands = item;
                break;
            case GlamourerEquipmentSlot.Legs:
                equipment.Legs = item;
                break;
            case GlamourerEquipmentSlot.Feet:
                equipment.Feet = item;
                break;
            case GlamourerEquipmentSlot.Ears:
                equipment.Ears = item;
                break;
            case GlamourerEquipmentSlot.Neck:
                equipment.Neck = item;
                break;
            case GlamourerEquipmentSlot.Wrists:
                equipment.Wrists = item;
                break;
            case GlamourerEquipmentSlot.RFinger:
                equipment.RFinger = item;
                break;
            case GlamourerEquipmentSlot.LFinger:
                equipment.LFinger = item;
                break;
        }
    }
}

public record SlotStatus(string SlotName, bool HasItem, string? ItemDisplay, Guid? PieceId);

public class WardrobeService : IDisposable
{
    private readonly PenumbraService _penumbraService;
    private readonly GlamourerService _glamourerService;
    private readonly WardrobeNetworkService _wardrobeNetworkService;

    private Dictionary<string, WardrobeItem> _wardrobeItems = [];
    private Dictionary<string, WardrobeSet> _wardrobeSets = [];
    private Dictionary<string, WardrobeItem> _modItems = [];

    private Dictionary<Guid, WardrobeItem> _wardrobeItemsById = [];
    private Dictionary<Guid, WardrobeSet> _wardrobeSetsById = [];
    private Dictionary<Guid, WardrobeItem> _modItemsById = [];

    public ActiveWardrobe ActiveSet { get; private set; }

    public IReadOnlyList<WardrobeItem> WardrobePieces => [.. _wardrobeItems.Values];
    public IReadOnlyList<WardrobeSet> ImportedSets => [.. _wardrobeSets.Values];
    public IReadOnlyList<WardrobeItem> ModItems => [.. _modItems.Values];

    public WardrobeService(
        GlamourerService glamourerService,
        PenumbraService penumbraService,
        WardrobeNetworkService wardrobeNetworkService
    )
    {
        _penumbraService = penumbraService;
        _glamourerService = glamourerService;
        _wardrobeNetworkService = wardrobeNetworkService;
        ActiveSet = new();

        Plugin.Log.Information(
            "WardrobeService initialized: {ItemCount} items, {SetCount} sets, {CharacterItemCount} character items",
            _wardrobeItems.Count,
            _wardrobeSets.Count,
            _modItems.Count
        );

        _glamourerService.IpcReady += OnIpcReady;
        if (_glamourerService.ApiAvailable)
        {
            _ = RefreshGlamourerDesignsAsync();
        }
    }

    private void OnIpcReady(object? sender, EventArgs e)
    {
        Plugin.Log.Information("Glamourer IPC became ready, refreshing designs");
        _ = RefreshGlamourerDesignsAsync();
    }

    private void RebuildIdDictionaries()
    {
        _wardrobeItemsById = _wardrobeItems.Values.ToDictionary(p => p.Id);
        _wardrobeSetsById = _wardrobeSets.Values.ToDictionary(s => s.Id);
        _modItemsById = _modItems.Values.ToDictionary(m => m.Id);
    }

    private async Task SyncPieceToServerAsync(WardrobeItem item, string type)
    {
        var dto = new WardrobeDto(
            item.Id,
            item.Name,
            item.Description,
            type,
            item.Slot,
            GlamourerDesignHelper.ItemToBase64(item),
            item.Priority
        );
        await _wardrobeNetworkService.AddWardrobeItemAsync(dto);
    }

    private async Task SyncSetToServerAsync(WardrobeSet set)
    {
        GlamourerDesign design = set.Design.Clone();
        Plugin.Log.Information($"SyncSetToServerAsync {set}");
        Plugin.Log.Information($"SyncSetToServerAsync {design}");
        var dto = new WardrobeDto(
            set.Id,
            set.Name,
            set.Description,
            "set",
            GlamourerEquipmentSlot.None,
            GlamourerDesignHelper.ToBase64(design),
            set.Priority
        );
        await _wardrobeNetworkService.AddWardrobeItemAsync(dto);
    }

    public void LoadFromWardrobeDto(List<WardrobeDto> dtos)
    {
        // If there are values, clear them.
        _wardrobeItems.Clear();
        _wardrobeSets.Clear();
        _modItems.Clear();

        foreach (var dto in dtos)
        {
            if (dto.DataBase64 == null)
            {
                Plugin.Log.Warning($"[WardrobeService] received DTO {dto.Name} with empty data");
                continue;
            }
            switch (dto.Type)
            {
                case "item":

                    WardrobeItem item = GlamourerDesignHelper.FromItemBase64(dto.DataBase64);
                    if (item != null)
                        _wardrobeItems[dto.Name] = item;
                    break;

                case "set":
                    _wardrobeSets[dto.Name] = new WardrobeSet
                    {
                        Design = GlamourerDesignHelper.FromBase64(dto.DataBase64)!,
                        Priority = dto.Priority,
                    };
                    break;

                case "moditem":
                    WardrobeItem mods = GlamourerDesignHelper.FromItemBase64(dto.DataBase64);
                    if (mods != null)
                    {
                        _modItems[dto.Name] = mods;
                    }
                    break;
            }
        }

        RebuildIdDictionaries();

        Plugin.Log.Information(
            "[WardrobeService] Synced {SetCount} sets, {ItemCount} items, {ModSettings} modsettings",
            _wardrobeSets,
            _wardrobeItems,
            _modItems
        );
    }

    public void AddPiece(WardrobeItem piece)
    {
        _wardrobeItems[piece.Name] = piece;
        _wardrobeItemsById[piece.Id] = piece;
        Plugin.Log.Information(
            "Added wardrobe piece: {PieceName} (ID: {PieceId})",
            piece.Name,
            piece.Id
        );
        _ = SyncPieceToServerAsync(piece, "item");
    }

    public void UpdatePiece(WardrobeItem piece)
    {
        _wardrobeItems[piece.Name] = piece;
        _wardrobeItemsById[piece.Id] = piece;
        Plugin.Log.Information(
            "Updated wardrobe piece: {PieceName} (ID: {PieceId})",
            piece.Name,
            piece.Id
        );
        _ = SyncPieceToServerAsync(piece, "item");
    }

    public void DeletePiece(Guid id)
    {
        if (_wardrobeItemsById.TryGetValue(id, out var piece))
        {
            _wardrobeItems.Remove(piece.Name);
            _wardrobeItemsById.Remove(id);
            Plugin.Log.Information(
                "Deleted wardrobe piece: {PieceName} (ID: {PieceId})",
                piece.Name,
                id
            );
            _ = _wardrobeNetworkService.RemoveWardrobeItemAsync(id);
        }
    }

    public WardrobeItem? GetPieceById(Guid id)
    {
        return _wardrobeItemsById.TryGetValue(id, out var item) ? item : null;
    }

    public bool IsPieceInActiveSet(Guid pieceId)
    {
        var piece = GetPieceById(pieceId);
        if (piece == null)
            return false;

        var activeItem = ActiveSet.GetIndividual(piece.Slot);
        return activeItem?.Id == pieceId;
    }

    public bool IsSetActive(Guid setId)
    {
        var set = GetSetById(setId);
        if (set == null)
            return false;

        var currentBaseLayer = ActiveSet.GetBaseLayer();
        return currentBaseLayer?.Identifier == setId;
    }

    public void AddSet(GlamourerDesign set)
    {
        var wardrobeSet = new WardrobeSet { Design = set };
        _wardrobeSets[set.Name] = wardrobeSet;
        _wardrobeSetsById[set.Identifier] = wardrobeSet;
        Plugin.Log.Information(
            "Added wardrobe set: {SetName} (ID: {SetId}, Set: {Set})",
            set.Name,
            set.Identifier,
            set.ToString()
        );
        _ = SyncSetToServerAsync(wardrobeSet);
    }

    public void UpdateSet(GlamourerDesign set)
    {
        var wardrobeSet = new WardrobeSet { Design = set };
        _wardrobeSets[set.Name] = wardrobeSet;
        _wardrobeSetsById[set.Identifier] = wardrobeSet;
        Plugin.Log.Information(
            "Updated wardrobe set: {SetName} (ID: {SetId})",
            set.Name,
            set.Identifier
        );
        _ = SyncSetToServerAsync(wardrobeSet);
    }

    public void DeleteSet(Guid id)
    {
        if (_wardrobeSetsById.TryGetValue(id, out var set))
        {
            _wardrobeSets.Remove(set.Name);
            _wardrobeSetsById.Remove(id);
            Plugin.Log.Information("Deleted wardrobe set: {SetName} (ID: {SetId})", set.Name, id);
            _ = _wardrobeNetworkService.RemoveWardrobeItemAsync(id);
        }
    }

    public WardrobeSet? GetSetById(Guid id)
    {
        return _wardrobeSetsById.TryGetValue(id, out var set) ? set : null;
    }

    public WardrobeSet? GetSetByName(string name)
    {
        return _wardrobeSets.TryGetValue(name, out var set) ? set : null;
    }

    public void AddModItem(WardrobeItem item)
    {
        _modItems[item.Name] = item;
        _modItemsById[item.Id] = item;
        Plugin.Log.Information(
            "Added character item: {ItemName} (ID: {ItemId})",
            item.Name,
            item.Id
        );
        _ = SyncPieceToServerAsync(item, "moditem");
    }

    public void UpdateModItem(WardrobeItem item)
    {
        _modItems[item.Name] = item;
        _modItemsById[item.Id] = item;
        Plugin.Log.Information(
            "Updated character item: {ItemName} (ID: {ItemId})",
            item.Name,
            item.Id
        );
        _ = SyncPieceToServerAsync(item, "moditem");
    }

    public void DeleteModItem(Guid id)
    {
        if (_modItemsById.TryGetValue(id, out var item))
        {
            _modItems.Remove(item.Name);
            _modItemsById.Remove(id);
            Plugin.Log.Information(
                "Deleted character item: {ItemName} (ID: {ItemId})",
                item.Name,
                id
            );
            _ = _wardrobeNetworkService.RemoveWardrobeItemAsync(id);
        }
    }

    public WardrobeItem? GetModItemById(Guid id)
    {
        return _modItemsById.TryGetValue(id, out var item) ? item : null;
    }

    public WardrobeItem? GetCharacterItemById(Guid id) => GetModItemById(id);

    public void AddCharacterItem(WardrobeItem item) => AddModItem(item);

    public void UpdateCharacterItem(WardrobeItem item) => UpdateModItem(item);

    public void DeleteCharacterItem(Guid id) => DeleteModItem(id);

    public void ApplySetByIdSync(Guid setId)
    {
        var set = GetSetById(setId);
        if (set != null)
        {
            ActiveSet.SetBaseLayer(set.Design, set.Priority);
            Plugin.Log.Information(
                "[WardrobeService] Applied set from sync: {SetName} (ID: {SetId})",
                set.Name,
                setId
            );
        }
    }

    public void ApplyPieceSync(GlamourerEquipmentSlot slot, WardrobeItem piece)
    {
        ActiveSet.SetIndividual(slot, piece);
        Plugin.Log.Information(
            "[WardrobeService] Applied piece from sync: {PieceName} (ID: {PieceId}) to slot {Slot}",
            piece.Name,
            piece.Id,
            slot
        );
    }

    public void ApplyCharacterItemSync(WardrobeItem item)
    {
        ActiveSet.AddModItem(item);
        Plugin.Log.Information(
            "[WardrobeService] Applied character item from sync: {ItemName} (ID: {ItemId})",
            item.Name,
            item.Id
        );
    }

    private async Task SyncActiveSetToServerAsync()
    {
        Plugin.Log.Information("[WardrobeService] SyncActiveSetToServerAsync called");

        var baseLayerDesign = ActiveSet.GetBaseLayer();
        var baseLayerBase64 =
            baseLayerDesign != null ? GlamourerDesignHelper.ToBase64(baseLayerDesign) : null;
        var equipment = new Dictionary<string, WardrobeItemData>();
        var modSettings = new Dictionary<string, WardrobeItemData>();

        var slotNames = new[]
        {
            "Head",
            "Body",
            "Hands",
            "Legs",
            "Feet",
            "Ears",
            "Neck",
            "Wrists",
            "RFinger",
            "LFinger",
        };
        foreach (var slotName in slotNames)
        {
            var slot = GetSlotFromName(slotName);
            var item = ActiveSet.GetIndividual(slot);
            if (item != null && item.Item != null && item.Item.ItemId != 0)
            {
                equipment[slotName] = new WardrobeItemData(
                    item.Id,
                    item.Name,
                    item.Description,
                    item.Slot,
                    item.Item,
                    item.Mods,
                    item.Materials,
                    item.Priority
                );
            }
        }

        foreach (var item in ActiveSet.GetCharacterItems())
        {
            if (item.Value.Mods.Count > 0)
            {
                modSettings[item.Value.Mods[0].Name] = new WardrobeItemData(
                    item.Value.Id,
                    item.Value.Name,
                    item.Value.Description,
                    item.Value.Slot,
                    item.Value.Item,
                    item.Value.Mods,
                    item.Value.Materials,
                    item.Value.Priority
                );
            }
        }

        var state = new WardrobeStateDto(baseLayerBase64, equipment, modSettings);

        await _wardrobeNetworkService.SetWardrobeStatusAsync(state);
    }

    public async Task ApplyCharacterItem(WardrobeItem item) => await ApplyWardrobeItem(item);

    public async Task<List<Design>> RefreshGlamourerDesignsAsync()
    {
        if (
            _glamourerService.ApiAvailable
            && await _glamourerService.GetDesignList().ConfigureAwait(false) is { } designs
        )
        {
            Plugin.Log.Information("Retrieved {DesignCount} Glamourer designs", designs.Count);
            return designs.OrderBy(d => d.Path).ToList();
        }
        else
        {
            Plugin.Log.Warning("Failed to retrieve Glamourer designs - API not available");
            return new List<Design>();
        }
    }

    public async Task<List<(Mod, ModSettings)>> GetAvailableModsAsync()
    {
        if (!_penumbraService.ApiAvailable)
        {
            Plugin.Log.Information("Penumbra IPC is not available");
            return new();
        }
        return await _penumbraService.GetAllMods();
    }

    public async Task<GlamourerItem?> GetGlamourSlotFromPlayer(GlamourerEquipmentSlot slot)
    {
        Plugin.Log.Debug("Getting Glamourer slot {Slot} from player", slot);

        var designJson = await _glamourerService.GetDesignComponentsAsync(
            GlamourerService.PLAYER_ID
        );
        if (designJson is not JObject jObject)
        {
            Plugin.Log.Error("Design JSON is not a valid JObject");
            return null;
        }

        var glamourerDesign = GlamourerDesignHelper.FromJObject(jObject);
        if (glamourerDesign == null)
        {
            Plugin.Log.Error("Failed to convert design JSON to GlamourerDesign");
            return null;
        }

        var item = slot switch
        {
            GlamourerEquipmentSlot.Head => glamourerDesign.Equipment.Head,
            GlamourerEquipmentSlot.Body => glamourerDesign.Equipment.Body,
            GlamourerEquipmentSlot.Hands => glamourerDesign.Equipment.Hands,
            GlamourerEquipmentSlot.Legs => glamourerDesign.Equipment.Legs,
            GlamourerEquipmentSlot.Feet => glamourerDesign.Equipment.Feet,
            GlamourerEquipmentSlot.Ears => glamourerDesign.Equipment.Ears,
            GlamourerEquipmentSlot.Neck => glamourerDesign.Equipment.Neck,
            GlamourerEquipmentSlot.Wrists => glamourerDesign.Equipment.Wrists,
            GlamourerEquipmentSlot.RFinger => glamourerDesign.Equipment.RFinger,
            GlamourerEquipmentSlot.LFinger => glamourerDesign.Equipment.LFinger,
            _ => null,
        };

        Plugin.Log.Debug(
            "Retrieved slot {Slot}: ItemId={ItemId}, Apply={Apply}",
            slot,
            item?.ItemId ?? 0,
            item?.Apply ?? false
        );

        return item;
    }

    public async Task<GlamourerDesign?> GetDesignAsync(Guid designId)
    {
        Plugin.Log.Debug("Getting Glamourer design {DesignId}", designId);

        var designJson = await _glamourerService.GetDesignJObjectAsync(designId);
        if (designJson is not JObject jObject)
        {
            Plugin.Log.Error("Design JSON is not a valid JObject");
            return null;
        }
        Plugin.Log.Info($"Retrieved jObject: {jObject.ToString()}");
        var glamourerDesign = GlamourerDesignHelper.FromJObject(jObject);
        if (glamourerDesign == null)
        {
            Plugin.Log.Error("Failed to convert design JSON to GlamourerDesign");
            return null;
        }

        Plugin.Log.Debug(
            "Retrieved design {DesignName} (ID: {DesignId} Design {GlamourerDesign})",
            glamourerDesign.Name,
            designId,
            glamourerDesign
        );

        return glamourerDesign;
    }

    public async Task ApplyModsAsync(bool enabled)
    {
        foreach (var glamourerMod in ActiveSet.GetMods())
        {
            var mod = new Mod(glamourerMod.Name, glamourerMod.Directory);
            var settings = new ModSettings(
                glamourerMod.Settings,
                glamourerMod.Priority,
                glamourerMod.Enabled,
                glamourerMod.ForceInherit,
                glamourerMod.Remove
            );
            await _penumbraService.SetTemporaryModState(mod, settings, enabled);
        }
    }

    public async Task ApplySetAsync(string name)
    {
        if (!_glamourerService.ApiAvailable)
        {
            Plugin.Log.Warning("Cannot apply set: Glamourer API not available");
            return;
        }

        if (!_wardrobeSets.TryGetValue(name, out var set))
        {
            Plugin.Log.Warning("Cannot apply set: Set '{SetName}' not found in wardrobe", name);
            return;
        }

        Plugin.Log.Information(
            "Applying wardrobe set: {SetName} (ID: {SetId})",
            name,
            set.Design.Identifier
        );

        ActiveSet.SetBaseLayer(set.Design, set.Priority);

        await ApplyModsAsync(true);

        await _glamourerService.ApplyDesignAsync(ActiveSet.GetCurrentState());

        await SyncActiveSetToServerAsync();

        Plugin.Log.Information("Successfully applied wardrobe set: {SetName}", name);
    }

    public async Task ApplyDesignFromPairAsync(GlamourerDesign design, RelationshipPriority priority)
    {
        if (!_glamourerService.ApiAvailable)
        {
            Plugin.Log.Warning("Cannot apply design: Glamourer API not available");
            return;
        }

        Plugin.Log.Information(
            "Applying wardrobe design from pair: {DesignName} (ID: {DesignId})",
            design.Name,
            design.Identifier
        );

        ActiveSet.SetBaseLayer(design, priority);

        await ApplyModsAsync(true);

        await _glamourerService.ApplyDesignAsync(ActiveSet.GetCurrentState());

        await SyncActiveSetToServerAsync();

        Plugin.Log.Information("Successfully applied wardrobe design from pair: {DesignName}", design.Name);
    }

    public async Task RemoveActiveSetAsync()
    {
        if (!_glamourerService.ApiAvailable || ActiveSet == null)
        {
            return;
        }

        Plugin.Log.Information("Removing active wardrobe set");

        await ApplyModsAsync(false);
        ActiveSet.ClearBaseLayer();

        await _glamourerService.RevertToAutomation();

        await SyncActiveSetToServerAsync();

        Plugin.Log.Information("Successfully removed active wardrobe set");
    }

    public async Task ApplyPieceAsync(WardrobeItem piece)
    {
        Plugin.Log.Information(
            "Applying wardrobe piece: {PieceName} (ID: {PieceId}) to slot {Slot}",
            piece.Name,
            piece.Id,
            piece.Slot
        );

        ActiveSet.SetIndividual(piece.Slot, piece);

        await ApplyModsAsync(true);

        await _glamourerService.ApplyDesignAsync(ActiveSet.GetCurrentState());

        await SyncActiveSetToServerAsync();

        Plugin.Log.Information("Successfully applied wardrobe piece: {PieceName}", piece.Name);
    }

    public async Task ApplyWardrobeItem(WardrobeItem item)
    {
        ActiveSet.AddModItem(item);
        await ApplyModsAsync(true);
        await SyncActiveSetToServerAsync();
    }

    public async Task RemoveWardrobeItemFromActive(Guid id)
    {
        ActiveSet.ClearModItem(id);
        await ApplyModsAsync(false);
        await SyncActiveSetToServerAsync();
    }

    public async Task RemovePieceFromSlotAsync(GlamourerEquipmentSlot slot)
    {
        if (!_glamourerService.ApiAvailable || !ActiveSet.IsActive())
        {
            return;
        }

        Plugin.Log.Information("Removing piece from slot: {Slot}", slot);

        ActiveSet.ClearIndividual(slot);
        await _glamourerService.RevertToAutomation();

        await SyncActiveSetToServerAsync();

        Plugin.Log.Information("Successfully removed piece from slot: {Slot}", slot);
    }

    public List<SlotStatus> GetActiveSlotStatuses()
    {
        var statuses = new List<SlotStatus>();

        var baseLayer = ActiveSet.GetBaseLayer();
        statuses.Add(new SlotStatus("BaseSet", baseLayer != null, baseLayer?.Name, null));

        var slotNames = new[]
        {
            "Head",
            "Body",
            "Hands",
            "Legs",
            "Feet",
            "Ears",
            "Neck",
            "Wrists",
            "RFinger",
            "LFinger",
        };
        foreach (var slotName in slotNames)
        {
            var slot = GetSlotFromName(slotName);
            var item = ActiveSet.GetIndividual(slot);
            var hasItem = item != null && item.Item != null && item.Item.ItemId != 0;
            var itemDisplay = hasItem ? $"Item {item!.Item!.ItemId}" : null;
            statuses.Add(new SlotStatus(slotName, hasItem, itemDisplay, null));
        }

        return statuses;
    }

    private static GlamourerEquipmentSlot GetSlotFromName(string slotName)
    {
        return slotName switch
        {
            "Head" => GlamourerEquipmentSlot.Head,
            "Body" => GlamourerEquipmentSlot.Body,
            "Hands" => GlamourerEquipmentSlot.Hands,
            "Legs" => GlamourerEquipmentSlot.Legs,
            "Feet" => GlamourerEquipmentSlot.Feet,
            "Ears" => GlamourerEquipmentSlot.Ears,
            "Neck" => GlamourerEquipmentSlot.Neck,
            "Wrists" => GlamourerEquipmentSlot.Wrists,
            "RFinger" => GlamourerEquipmentSlot.RFinger,
            "LFinger" => GlamourerEquipmentSlot.LFinger,
            _ => GlamourerEquipmentSlot.None,
        };
    }

    public async Task ReapplyIfChanged(GlamourerDesign design)
    {
        if (!ActiveSet.IsActive())
            return;

        var currentState = ActiveSet.GetCurrentState();
        if (!WardrobeService.EquippedItemsChanged(design.Equipment, currentState.Equipment))
            return;

        Plugin.Log.Information("Detected equipment change, reapplying wardrobe");

        await _glamourerService.ApplyDesignAsync(currentState);
    }

    /// <summary>
    ///     Tests to see if any equipment marked with 'apply' are different
    /// </summary>
    private static bool EquippedItemsChanged(
        GlamourerEquipment activeset,
        GlamourerEquipment newState
    )
    {
        // Only check if the permanent transformation affects a certain item
        if (activeset.Head.Apply && activeset.Head.IsEqualTo(newState.Head) is false)
            return true;
        if (activeset.Body.Apply && activeset.Body.IsEqualTo(newState.Body) is false)
            return true;
        if (activeset.Hands.Apply && activeset.Hands.IsEqualTo(newState.Hands) is false)
            return true;
        if (activeset.Legs.Apply && activeset.Legs.IsEqualTo(newState.Legs) is false)
            return true;
        if (activeset.Feet.Apply && activeset.Feet.IsEqualTo(newState.Feet) is false)
            return true;
        if (activeset.Ears.Apply && activeset.Ears.IsEqualTo(newState.Ears) is false)
            return true;
        if (activeset.Neck.Apply && activeset.Neck.IsEqualTo(newState.Neck) is false)
            return true;
        if (activeset.Wrists.Apply && activeset.Wrists.IsEqualTo(newState.Wrists) is false)
            return true;
        if (activeset.RFinger.Apply && activeset.RFinger.IsEqualTo(newState.RFinger) is false)
            return true;
        if (activeset.LFinger.Apply && activeset.LFinger.IsEqualTo(newState.LFinger) is false)
            return true;
        return false;
    }

    public void Dispose()
    {
        _glamourerService.IpcReady -= OnIpcReady;
        // If the wardrobe is disposed all temporary mods should be unlocked
        _penumbraService.ClearAllTemporaryMods();
        GC.SuppressFinalize(this);
    }

    public async void ClearActive()
    {
        ActiveSet.ClearBaseLayer();
        ActiveSet.ClearIndividual(GlamourerEquipmentSlot.None);
        ActiveSet.ClearIndividual(GlamourerEquipmentSlot.Head);
        ActiveSet.ClearIndividual(GlamourerEquipmentSlot.Body);
        ActiveSet.ClearIndividual(GlamourerEquipmentSlot.Hands);
        ActiveSet.ClearIndividual(GlamourerEquipmentSlot.Legs);
        ActiveSet.ClearIndividual(GlamourerEquipmentSlot.Feet);
        ActiveSet.ClearIndividual(GlamourerEquipmentSlot.Ears);
        ActiveSet.ClearIndividual(GlamourerEquipmentSlot.Neck);
        ActiveSet.ClearIndividual(GlamourerEquipmentSlot.Wrists);
        ActiveSet.ClearIndividual(GlamourerEquipmentSlot.RFinger);
        ActiveSet.ClearIndividual(GlamourerEquipmentSlot.LFinger);
        ActiveSet.ClearBaseLayer();
        await _glamourerService.RevertToAutomation();
        _penumbraService.ClearAllTemporaryMods();
    }
}
