using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KinkLinkClient.Dependencies.Glamourer.Domain;
using KinkLinkClient.Dependencies.Penumbra.Services;
using KinkLinkClient.Services;
using KinkLinkClient.Utils;
using KinkLinkCommon.Dependencies.Glamourer;
using KinkLinkCommon.Dependencies.Glamourer.Components;
using KinkLinkCommon.Domain;
using KinkLinkCommon.Domain.Enums;
using KinkLinkCommon.Domain.Wardrobe;

namespace KinkLinkClient.UI.Views.DressUp;

public enum SubView
{
    List,
    Active,
    Personal,
    Import,
    Editor,
}

public enum PairAccessFilter
{
    All,
    Casual,
    Serious,
    Devotional,
}

public class DressupViewUiController
{
    private readonly LockService _lockService;
    private readonly WardrobeManager _wardrobeManager;
    private readonly WardrobeNetworkService _wardrobeNetworkService;
    private readonly ClientCharacterStateService _characterState;
    private readonly IdentityService _identity;

    public WardrobeManager WardrobeManager => _wardrobeManager;

    public SubView CurrentView { get; set; } = SubView.List;

    public Guid? SelectedItem { get; set; }

    public Guid? HoveredItemId { get; set; }

    public string ModFilter { get; set; } = string.Empty;

    public WardrobeLayer EditingLayer { get; set; }
    public WardrobeItem? EditingWardrobeItem { get; set; }

    public string EditedName { get; set; } = string.Empty;
    public string EditedDescription { get; set; } = string.Empty;

    public WardrobeLayer SelectedSlotLayer { get; set; } = WardrobeLayer.Outfit;
    public GlamourerItem EditedItem { get; set; } = new();
    public uint EditedDye1 { get; set; }
    public uint EditedDye2 { get; set; }

    public bool HasImportedItem { get; set; }

    public bool IsNewItem => EditingWardrobeItem?.Id == Guid.Empty;

    public string ImportSlotName { get; set; } = "Head";

    public List<(Mod, ModSettings)> AvailableMods { get; private set; } =
        new List<(Mod, ModSettings)>();
    public Dictionary<string, ModSettings> SelectedModSettings { get; set; } =
        new Dictionary<string, ModSettings>();

    public List<Design>? GlamourerDesigns { get; private set; }
    private List<Design>? _filteredGlamourerDesigns;

    public string GlamourerSearchTerm { get; set; } = string.Empty;
    public Guid SelectedGlamourerDesignId { get; set; } = Guid.Empty;

    public string SearchFilter { get; set; } = string.Empty;
    public PairAccessFilter PairAccessFilter { get; set; } = PairAccessFilter.All;

    public RelationshipPriority EditedPriority { get; set; } = RelationshipPriority.Casual;

    private List<WardrobeItem>? _filteredItems;

    // selected item per layer used by Dressup view
    private readonly Dictionary<WardrobeLayer, Guid?> _selectedForLayer = new();

    public Guid? GetSelectedForLayer(WardrobeLayer layer)
    {
        // Only return pending selection from the combo box.
        // Active-set items are read directly via status in the UI — never
        // cross-reference them through the library by ID since server-pushed
        // items carry freshly-generated GUIDs that won't match.
        return _selectedForLayer.TryGetValue(layer, out var v) ? v : null;
    }

    public void SetSelectedForLayer(WardrobeLayer layer, Guid? id)
    {
        if (id.HasValue)
            _selectedForLayer[layer] = id;
        else
            _selectedForLayer.Remove(layer);
    }

    public async Task ApplyItemToLayerAsync(WardrobeLayer layer, Guid itemId)
    {
        await _wardrobeManager.ApplyWardrobeLayerToActive(layer, itemId);
    }

    public List<WardrobeItem>? FilteredItems
    {
        get
        {
            var items = _wardrobeManager.WardrobeLibrary.ToList();
            if (!string.IsNullOrEmpty(SearchFilter))
            {
                items = items
                    .Where(i =>
                        i.Name.Contains(SearchFilter, StringComparison.OrdinalIgnoreCase)
                        || i.Description.Contains(SearchFilter, StringComparison.OrdinalIgnoreCase)
                    )
                    .ToList();
            }

            if (PairAccessFilter != PairAccessFilter.All)
            {
                var priority = PairAccessFilter switch
                {
                    PairAccessFilter.Casual => RelationshipPriority.Casual,
                    PairAccessFilter.Serious => RelationshipPriority.Serious,
                    PairAccessFilter.Devotional => RelationshipPriority.Devotional,
                    _ => RelationshipPriority.Casual,
                };
                items = items.Where(i => i.Priority == priority).ToList();
            }

            return items;
        }
    }

    public List<WardrobeItem>? FilteredSets
    {
        get
        {
            var sets = _wardrobeManager
                .WardrobeLibrary.Where(i =>
                    i.Slot == KinkLinkCommon.Dependencies.Glamourer.GlamourerEquipmentSlot.None
                )
                .ToList();
            if (!string.IsNullOrEmpty(SearchFilter))
            {
                sets = sets.Where(s =>
                        s.Name.Contains(SearchFilter, StringComparison.OrdinalIgnoreCase)
                        || s.Description.Contains(SearchFilter, StringComparison.OrdinalIgnoreCase)
                    )
                    .ToList();
            }

            if (PairAccessFilter != PairAccessFilter.All)
            {
                var priority = PairAccessFilter switch
                {
                    PairAccessFilter.Casual => RelationshipPriority.Casual,
                    PairAccessFilter.Serious => RelationshipPriority.Serious,
                    PairAccessFilter.Devotional => RelationshipPriority.Devotional,
                    _ => RelationshipPriority.Casual,
                };
                sets = sets.Where(s => s.Priority == priority).ToList();
            }

            return sets;
        }
    }

    public List<Design>? FilteredGlamourerDesigns =>
        string.IsNullOrEmpty(GlamourerSearchTerm) ? GlamourerDesigns : _filteredGlamourerDesigns;
    public static WardrobeLayer[] AllLayers => Enum.GetValues<WardrobeLayer>();

    public static string GetSlotDisplayName(WardrobeLayer layer)
    {
        return layer switch
        {
            WardrobeLayer.Head => "Head",
            WardrobeLayer.Chest => "Chest",
            WardrobeLayer.Hands => "Hands",
            WardrobeLayer.Legs => "Legs",
            WardrobeLayer.Feet => "Feet",
            WardrobeLayer.Ears => "Earrings",
            WardrobeLayer.Neck => "Necklace",
            WardrobeLayer.Wrists => "Bracelet",
            WardrobeLayer.RFinger => "Right Ring",
            WardrobeLayer.LFinger => "Left Ring",
            _ => layer.ToString(),
        };
    }

    // compatibility: accept string slotName and map to layer
    public static string GetSlotDisplayName(string slotName)
    {
        var layer = WardrobeSlotHelper.GetLayerFromName(slotName);
        return GetSlotDisplayName(layer);
    }

    public DressupViewUiController(
        LockService lockService,
        WardrobeManager wardrobeManager,
        WardrobeNetworkService wardrobeNetworkService,
        ClientCharacterStateService characterState,
        IdentityService identity
    )
    {
        _lockService = lockService;
        _wardrobeManager = wardrobeManager;
        _wardrobeNetworkService = wardrobeNetworkService;
        _characterState = characterState;
        _identity = identity;
    }

    public LockKind GetWardrobeLockId(WardrobeLayer layer)
    {
        return LockKindExtensions.From(layer);
    }

    public bool IsSlotLocked(WardrobeLayer layer)
    {
        var lockId = GetWardrobeLockId(layer);
        return _lockService.IsLocked(lockId);
    }

    public LockInfoDto? GetSlotLock(WardrobeLayer layer)
    {
        var lockId = GetWardrobeLockId(layer);
        return _lockService.GetLock(lockId);
    }

    public void SaveSlotData()
    {
        if (EditingWardrobeItem == null)
            return;

        var slotName = WardrobeSlotHelper.GetNameFromSlot(SelectedSlotLayer);
        var slot = WardrobeSlotHelper.GetSlotFromName(slotName);

        var mods = new List<GlamourerMod>();
        foreach (var (dirName, settings) in SelectedModSettings)
        {
            var mod = AvailableMods.FirstOrDefault(m => m.Item1.DirectoryName == dirName);
            if (!string.IsNullOrEmpty(mod.Item1.DirectoryName))
            {
                mods.Add(
                    new GlamourerMod(
                        mod.Item1.Name,
                        dirName,
                        settings.Enabled,
                        settings.Priority,
                        settings.Settings,
                        settings.ForceInherit,
                        settings.Remove
                    )
                );
            }
        }

        EditingWardrobeItem = new WardrobeItem
        {
            Id = Guid.NewGuid(),
            Name = EditedName,
            Description = EditedDescription,
            Slot = slot,
            Item = HasImportedItem ? EditedItem : null,
            Priority = EditedPriority,
            Mods = mods,
        };
    }

    public void LoadWardrobeItemData()
    {
        if (EditingWardrobeItem == null)
            return;

        EditedName = EditingWardrobeItem.Name;
        EditedDescription = EditingWardrobeItem.Description;
        EditedPriority = EditingWardrobeItem.Priority;
    }

    public void SaveSetData()
    {
        if (EditingWardrobeItem == null)
            return;

        EditingWardrobeItem.Design.Name = EditedName;
        EditingWardrobeItem.Design.Description = EditedDescription;
        EditingWardrobeItem.Priority = EditedPriority;
    }

    public void ResetEditorFields()
    {
        EditedName = string.Empty;
        EditedDescription = string.Empty;
        SelectedSlotLayer = WardrobeLayer.Outfit;
        EditedItem = new GlamourerItem();
        EditedDye1 = 0;
        EditedDye2 = 0;
        AvailableMods = new List<(Mod, ModSettings)>();
        SelectedModSettings = new Dictionary<string, ModSettings>();
        EditedPriority = RelationshipPriority.Casual;
    }

    public WardrobeItem? GetSelectedItem() =>
        SelectedItem.HasValue ? _wardrobeManager.GetItemById(SelectedItem.Value) : null;

    public void OpenItemEditor(WardrobeItem? item = null)
    {
        EditingWardrobeItem = item;
        if (item != null)
            LoadWardrobeItemData();
        CurrentView = SubView.Editor;
    }

    public void CloseEditor()
    {
        ResetEditorFields();
        EditingWardrobeItem = null;
        CurrentView = SubView.List;
    }

    public async Task<bool> SaveEditorAsync()
    {
        if (EditingWardrobeItem != null)
        {
            if (IsNewItem && !HasImportedItem)
                return false;

            SaveSlotData();
            _wardrobeManager.AddDesign(EditingWardrobeItem);
        }

        CloseEditor();
        return true;
    }

    public void DeleteItem(Guid id)
    {
        _wardrobeManager.DeleteItem(id);
        if (SelectedItem == id)
            SelectedItem = null;
    }

    public bool IsItemEquipped(Guid pieceId)
    {
        return _wardrobeManager.IsItemActive(pieceId);
    }

    public async Task ApplySetAsync(string name)
    {
        var item = _wardrobeManager.WardrobeLibrary.FirstOrDefault(i =>
            i.Name == name || i.Design?.Name == name
        );
        if (item != null)
        {
            await _wardrobeNetworkService.SetActiveWardrobeLayerAsync(item.Layer, item);
        }
    }

    public async Task RemoveActiveItemAsync(WardrobeLayer layer)
    {
        await _wardrobeManager.RemovePieceFromSlotAsync(layer);
        // Clear pending selection since it's no longer active
        _selectedForLayer.Remove(layer);
    }

    public bool CanRemoveFromSlot(WardrobeLayer layer)
    {
        if (!IsSlotLocked(layer))
            return true;

        var lockInfo = GetSlotLock(layer);
        return lockInfo?.CanSelfUnlock ?? false;
    }

    public bool CanUnlockSlot(WardrobeLayer layer)
    {
        if (!IsSlotLocked(layer))
            return false;

        var lockInfo = GetSlotLock(layer);
        if (lockInfo == null)
            return false;

        // Can unlock if: self-lock, can self-unlock, or locker is self
        return lockInfo.Value.CanSelfUnlock
            || lockInfo.Value.LockerID == _identity.FriendCode
            || lockInfo.Value.LockeeID == _identity.FriendCode;
    }

    public async Task LockSlotAsync(WardrobeLayer layer)
    {
        var uid = _identity.FriendCode;
        if (string.IsNullOrEmpty(uid))
        {
            Plugin.Log.Warning("[DressupView] Cannot lock slot {Layer}: no UID", layer);
            NotificationHelper.Error("Lock Failed", "Not logged in. Profile UID is missing.");
            return;
        }

        var lockId = LockKindExtensions.From(layer);
        var lockInfo = new LockInfoDto
        {
            LockID = lockId,
            LockeeID = uid,
            CanSelfUnlock = true,
            Expires = null,
            Password = null,
            LockPriority = RelationshipPriority.Casual,
        };

        var result = await _characterState.AddSelfLockAsync(lockInfo);
        if (result == null)
        {
            Plugin.Log.Error("[DressupView] Lock slot {Layer}: null response", layer);
            NotificationHelper.Error("Lock Failed", "No response from server.");
            return;
        }

        if (result.Result == ActionResultEc.Success)
        {
            Plugin.Log.Information("[DressupView] Locked slot {Layer}", layer);
        }
        else
        {
            Plugin.Log.Error(
                "[DressupView] Failed to lock slot {Layer}: {Result}",
                layer,
                result.Result
            );
            NotificationHelper.Error("Lock Failed", $"Could not lock {layer}: {result.Result}");
        }
    }

    public async Task UnlockSlotAsync(WardrobeLayer layer)
    {
        var uid = _identity.FriendCode;
        if (string.IsNullOrEmpty(uid))
        {
            Plugin.Log.Warning("[DressupView] Cannot unlock slot {Layer}: no UID", layer);
            NotificationHelper.Error("Unlock Failed", "Not logged in. Profile UID is missing.");
            return;
        }

        var lockId = LockKindExtensions.From(layer);
        var result = await _characterState.RemoveSelfLockAsync(lockId, uid);
        if (result == null)
        {
            Plugin.Log.Error("[DressupView] Unlock slot {Layer}: null response", layer);
            NotificationHelper.Error("Unlock Failed", "No response from server.");
            return;
        }

        if (result.Result == ActionResultEc.Success)
        {
            Plugin.Log.Information("[DressupView] Unlocked slot {Layer}", layer);
        }
        else
        {
            Plugin.Log.Error(
                "[DressupView] Failed to unlock slot {Layer}: {Result}",
                layer,
                result.Result
            );
            NotificationHelper.Error("Unlock Failed", $"Could not unlock {layer}: {result.Result}");
        }
    }

    public List<SlotStatus> GetActiveSlotStatuses()
    {
        var result = new List<SlotStatus>();
        foreach (WardrobeLayer layer in Enum.GetValues(typeof(WardrobeLayer)))
        {
            var slotName = WardrobeSlotHelper.GetNameFromSlot(layer);
            var hasItem = _wardrobeManager.ActiveSet.HasLayer(layer);
            string? display = null;
            Guid? pieceId = null;
            if (
                hasItem
                && _wardrobeManager.ActiveSet.Layers.TryGetValue(layer, out var item)
                && item != null
            )
            {
                display = item.Name ?? "Active";
                pieceId = item.Id;
            }
            result.Add(new SlotStatus(slotName, hasItem, display, pieceId));
        }
        return result;
    }

    public async Task ImportFromPlayerAsync()
    {
        var slot = WardrobeSlotHelper.GetSlotFromName(ImportSlotName);
        var item = await _wardrobeManager.GetGlamourSlotFromPlayer(slot);
        if (item != null)
        {
            EditedItem = item;
            EditedDye1 = item.Stain;
            EditedDye2 = item.Stain2;
            SelectedSlotLayer = WardrobeSlotHelper.GetLayerFromName(ImportSlotName);
            HasImportedItem = true;
        }
    }

    public async Task LoadAvailableModsAsync()
    {
        AvailableMods = await _wardrobeManager.GetAvailableModsAsync();
    }

    public void UpdateModSelection(string modDirectoryName, bool enabled, int priority)
    {
        if (enabled)
        {
            if (!SelectedModSettings.ContainsKey(modDirectoryName))
            {
                SelectedModSettings[modDirectoryName] = new ModSettings(
                    new Dictionary<string, List<string>>(),
                    priority,
                    true
                );
            }
            else
            {
                var existing = SelectedModSettings[modDirectoryName];
                SelectedModSettings[modDirectoryName] = new ModSettings(
                    existing.Settings,
                    priority,
                    true,
                    existing.ForceInherit,
                    existing.Remove
                );
            }
        }
        else
        {
            SelectedModSettings.Remove(modDirectoryName);
        }
    }

    public void UpdateModSettings(string modDirectoryName, ModSettings settings)
    {
        SelectedModSettings[modDirectoryName] = settings;
    }

    public int GetSelectedModCount() => SelectedModSettings.Count;

    public bool IsModSelected(string modDirectoryName) =>
        SelectedModSettings.ContainsKey(modDirectoryName);

    public int GetModPriority(string modDirectoryName)
    {
        return SelectedModSettings.TryGetValue(modDirectoryName, out var settings)
            ? settings.Priority
            : 0;
    }

    public ModSettings? GetModSettings(string modDirectoryName)
    {
        return SelectedModSettings.TryGetValue(modDirectoryName, out var settings)
            ? settings
            : null;
    }

    public void AddMod(string modDirectoryName)
    {
        if (!SelectedModSettings.ContainsKey(modDirectoryName))
        {
            SelectedModSettings[modDirectoryName] = new ModSettings(
                new Dictionary<string, List<string>>(),
                0,
                true
            );
        }
    }

    public void RemoveMod(string modDirectoryName)
    {
        SelectedModSettings.Remove(modDirectoryName);
    }

    public string? GetModName(string modDirectoryName)
    {
        var mod = AvailableMods.FirstOrDefault(m => m.Item1.DirectoryName == modDirectoryName);
        return string.IsNullOrEmpty(mod.Item1.Name) ? null : mod.Item1.Name;
    }

    public void FilterDesigns()
    {
        if (GlamourerDesigns == null)
        {
            _filteredGlamourerDesigns = null;
            return;
        }

        if (string.IsNullOrEmpty(GlamourerSearchTerm))
        {
            _filteredGlamourerDesigns = null;
            return;
        }

        _filteredGlamourerDesigns = GlamourerDesigns
            .Where(d => d.Path.Contains(GlamourerSearchTerm, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    public async void RefreshDesigns()
    {
        SelectedGlamourerDesignId = Guid.Empty;
        GlamourerDesigns = await _wardrobeManager.RefreshGlamourerDesignsAsync();
        FilterDesigns();
    }
}
