using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KinkLinkClient.Dependencies.Glamourer.Domain;
using KinkLinkClient.Dependencies.Penumbra.Services;
using KinkLinkClient.Services;
using KinkLinkCommon.Dependencies.Glamourer;
using KinkLinkCommon.Dependencies.Glamourer.Components;
using KinkLinkCommon.Domain;
using KinkLinkCommon.Domain.Enums;
using ClientWardrobeItem = KinkLinkClient.Services.WardrobeItem;

namespace KinkLinkClient.UI.Views.Wardrobe;

public enum SubView
{
    List,
    Import,
    Editor,
    Active,
}

public enum ListTab
{
    IndividualItems,
    Sets,
}

public enum PairAccessFilter
{
    All,
    Casual,
    Serious,
    Devotional,
}

public class WardrobeViewUiController
{
    private readonly LockService _lockService;
    private readonly WardrobeService _wardrobeService;

    public WardrobeService WardrobeService => _wardrobeService;

    public SubView CurrentView { get; set; } = SubView.List;
    public ListTab CurrentTab { get; set; } = ListTab.IndividualItems;

    public Guid? SelectedPieceId { get; set; }
    public Guid? SelectedSetId { get; set; }

    public Guid? HoveredPieceId { get; set; }
    public Guid? HoveredSetId { get; set; }

    public string ModFilter { get; set; } = string.Empty;

    public ClientWardrobeItem? EditingPiece { get; set; }
    public WardrobeSet? EditingSet { get; set; }

    public string EditedName { get; set; } = string.Empty;
    public string EditedDescription { get; set; } = string.Empty;

    public string SelectedSlotName { get; set; } = "Head";
    public GlamourerItem EditedItem { get; set; } = new();
    public uint EditedDye1 { get; set; }
    public uint EditedDye2 { get; set; }

    public bool HasImportedItem { get; set; }

    public bool IsNewItem => EditingPiece?.Id == Guid.Empty;

    public string ImportSlotName { get; set; } = "Head";

    public List<(Mod, ModSettings)> AvailableMods { get; private set; } = [];
    public Dictionary<string, ModSettings> SelectedModSettings { get; set; } = [];

    public List<Design>? GlamourerDesigns { get; private set; }
    private List<Design>? _filteredGlamourerDesigns;

    public string GlamourerSearchTerm { get; set; } = string.Empty;
    public Guid SelectedGlamourerDesignId { get; set; } = Guid.Empty;

    public string SearchFilter { get; set; } = string.Empty;
    public PairAccessFilter PairAccessFilter { get; set; } = PairAccessFilter.All;

    public RelationshipPriority EditedPriority { get; set; } = RelationshipPriority.Casual;

    private List<ClientWardrobeItem>? _filteredItems;
    private List<GlamourerDesign>? _filteredSets;

    public List<ClientWardrobeItem>? FilteredItems
    {
        get
        {
            var items = _wardrobeService.WardrobePieces.ToList();
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

    public List<WardrobeSet>? FilteredSets
    {
        get
        {
            var sets = _wardrobeService.ImportedSets.ToList();
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
    public static string[] AllSlotNames =>
        ["Head", "Body", "Hands", "Legs", "Feet", "Ears", "Neck", "Wrists", "RFinger", "LFinger"];

    public static string GetSlotDisplayName(string slotName)
    {
        return slotName switch
        {
            "Head" => "Head",
            "Body" => "Body",
            "Hands" => "Hands",
            "Legs" => "Legs",
            "Feet" => "Feet",
            "Ears" => "Earrings",
            "Neck" => "Necklace",
            "Wrists" => "Bracelet",
            "RFinger" => "Right Ring",
            "LFinger" => "Left Ring",
            _ => slotName,
        };
    }

    public static GlamourerEquipmentSlot GetSlotFromName(string slotName)
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

    public WardrobeViewUiController(LockService lockService, WardrobeService wardrobeService)
    {
        _lockService = lockService;
        _wardrobeService = wardrobeService;
    }

    public string GetWardrobeLockId(string slotName)
    {
        return $"wardrobe-{slotName.ToLowerInvariant()}";
    }

    public bool IsSlotLocked(string slotName)
    {
        var lockId = GetWardrobeLockId(slotName);
        return _lockService.IsLocked(lockId);
    }

    public LockInfoDto? GetSlotLock(string slotName)
    {
        var lockId = GetWardrobeLockId(slotName);
        return _lockService.GetLock(lockId);
    }

    public bool CanEquipToSlot(string slotName)
    {
        return !IsSlotLocked(slotName);
    }

    public bool CanRemoveFromSlot(string slotName)
    {
        if (!IsSlotLocked(slotName))
            return true;

        var lockInfo = GetSlotLock(slotName);
        return lockInfo?.CanSelfUnlock ?? false;
    }

    public void SaveSlotData()
    {
        if (EditingPiece == null)
            return;

        var slot = GetSlotFromName(SelectedSlotName);
        EditingPiece.Id = Guid.NewGuid();
        EditingPiece.Name = EditedName;
        EditingPiece.Description = EditedDescription;
        EditingPiece.Slot = slot;
        EditingPiece.Item = HasImportedItem ? EditedItem : null;
        EditingPiece.Priority = EditedPriority;

        EditingPiece.Mods = [];
        foreach (var (dirName, settings) in SelectedModSettings)
        {
            var mod = AvailableMods.FirstOrDefault(m => m.Item1.DirectoryName == dirName);
            if (mod.Item1 != null)
            {
                EditingPiece.Mods.Add(
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
    }

    public void LoadSlotData()
    {
        if (EditingPiece == null)
            return;

        EditedName = EditingPiece.Name;
        EditedDescription = EditingPiece.Description;
        SelectedSlotName = EditingPiece.Slot.ToString();
        EditedPriority = EditingPiece.Priority;
        LoadModsFromPiece();
    }

    public void LoadSetData()
    {
        if (EditingSet == null)
            return;

        EditedName = EditingSet.Name;
        EditedDescription = EditingSet.Description;
        EditedPriority = EditingSet.Priority;
    }

    public void SaveSetData()
    {
        if (EditingSet == null)
            return;

        EditingSet.Design.Name = EditedName;
        EditingSet.Design.Description = EditedDescription;
        EditingSet.Priority = EditedPriority;
    }

    public void ResetEditorFields()
    {
        EditedName = string.Empty;
        EditedDescription = string.Empty;
        SelectedSlotName = "Head";
        EditedItem = new GlamourerItem();
        EditedDye1 = 0;
        EditedDye2 = 0;
        HasImportedItem = false;
        ImportSlotName = "Head";
        AvailableMods = [];
        SelectedModSettings = [];
        EditedPriority = RelationshipPriority.Casual;
    }

    public ClientWardrobeItem? GetSelectedPiece() =>
        SelectedPieceId.HasValue ? _wardrobeService.GetPieceById(SelectedPieceId.Value) : null;

    public WardrobeSet? GetSelectedSet() =>
        SelectedSetId.HasValue ? _wardrobeService.GetSetById(SelectedSetId.Value) : null;

    public void OpenEditor(ClientWardrobeItem? piece = null)
    {
        var isNew = piece == null;
        EditingPiece =
            piece
            ?? new ClientWardrobeItem
            {
                Id = Guid.Empty,
                Name = "New Piece",
                Description = string.Empty,
                Slot = GlamourerEquipmentSlot.Head,
                Item = new GlamourerItem
                {
                    Apply = true,
                    ApplyCrest = false,
                    ApplyStain = true,
                    Crest = false,
                    ItemId = 0,
                    Stain = 0,
                    Stain2 = 0,
                },
            };
        EditingSet = null;
        HasImportedItem = false;
        if (isNew)
            LoadSlotData();
        else
            LoadSlotData();
        CurrentView = SubView.Editor;
    }

    public void OpenSetEditor(WardrobeSet? set = null)
    {
        EditingSet = set;
        EditingPiece = null;
        if (set != null)
            LoadSetData();
        CurrentView = SubView.Editor;
    }

    public void CloseEditor()
    {
        ResetEditorFields();
        EditingPiece = null;
        EditingSet = null;
        CurrentView = SubView.List;
    }

    public async Task<bool> SaveEditorAsync()
    {
        if (EditingPiece != null)
        {
            if (IsNewItem && !HasImportedItem)
                return false;

            SaveSlotData();
            _wardrobeService.AddPiece(EditingPiece, null);
        }
        else if (EditingSet != null)
        {
            SaveSetData();
            _wardrobeService.UpdateSet(EditingSet.Design, null);
        }

        CloseEditor();
        return true;
    }

    public void DeletePiece(Guid id)
    {
        _wardrobeService.DeletePiece(id);
        if (SelectedPieceId == id)
            SelectedPieceId = null;
    }

    public bool IsPieceEquipped(Guid pieceId)
    {
        return _wardrobeService.IsPieceInActiveSet(pieceId);
    }

    public bool IsSetEquipped(Guid setId)
    {
        return _wardrobeService.IsSetActive(setId);
    }

    public void DeleteSet(Guid id)
    {
        _wardrobeService.DeleteSet(id);
        if (SelectedSetId == id)
            SelectedSetId = null;
    }

    public async Task ApplySetAsync(string name)
    {
        await _wardrobeService.ApplySetAsync(name);
    }

    public async Task RemoveActiveSetAsync()
    {
        await _wardrobeService.RemoveActiveSetAsync();
    }

    public async Task ApplyPieceAsync(ClientWardrobeItem piece)
    {
        await _wardrobeService.ApplyPieceAsync(piece);
    }

    public async Task RemoveSlotItemAsync(string slotName)
    {
        if (slotName == "BaseSet")
        {
            await _wardrobeService.RemoveActiveSetAsync();
        }
        else
        {
            var slot = GetSlotFromName(slotName);
            await _wardrobeService.RemovePieceFromSlotAsync(slot);
        }
    }

    public List<SlotStatus> GetActiveSlotStatuses() => _wardrobeService.GetActiveSlotStatuses();

    public async Task ImportFromPlayerAsync()
    {
        var slot = GetSlotFromName(ImportSlotName);
        var item = await _wardrobeService.GetGlamourSlotFromPlayer(slot);
        if (item != null)
        {
            EditedItem = item;
            EditedDye1 = item.Stain;
            EditedDye2 = item.Stain2;
            SelectedSlotName = ImportSlotName;
            HasImportedItem = true;
        }
    }

    public async Task LoadAvailableModsAsync()
    {
        AvailableMods = await _wardrobeService.GetAvailableModsAsync();
    }

    public void LoadModsFromPiece()
    {
        SelectedModSettings = [];
        if (EditingPiece?.Mods == null)
            return;

        foreach (var glamMod in EditingPiece.Mods)
        {
            var settings = new ModSettings(
                glamMod.Settings,
                glamMod.Priority,
                glamMod.Enabled,
                glamMod.ForceInherit,
                glamMod.Remove
            );
            SelectedModSettings[glamMod.Directory] = settings;
        }
    }

    public void UpdateModSelection(string modDirectoryName, bool enabled, int priority)
    {
        if (enabled)
        {
            if (!SelectedModSettings.ContainsKey(modDirectoryName))
            {
                SelectedModSettings[modDirectoryName] = new ModSettings([], priority, true);
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
            SelectedModSettings[modDirectoryName] = new ModSettings([], 0, true);
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

        _filteredGlamourerDesigns =
        [
            .. GlamourerDesigns.Where(d =>
                d.Path.Contains(GlamourerSearchTerm, StringComparison.OrdinalIgnoreCase)
            ),
        ];
    }

    public async void RefreshDesigns()
    {
        SelectedGlamourerDesignId = Guid.Empty;
        GlamourerDesigns = await _wardrobeService.RefreshGlamourerDesignsAsync();
        FilterDesigns();
    }
}
