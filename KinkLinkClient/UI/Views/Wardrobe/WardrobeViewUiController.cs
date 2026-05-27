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
using KinkLinkCommon.Domain.Wardrobe;

namespace KinkLinkClient.UI.Views.Wardrobe;

public enum SubView
{
    List,
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

public class WardrobeViewUiController
{
    private readonly LockService _lockService;
    private readonly WardrobeManager _wardrobeManager;

    public WardrobeManager WardrobeManager => _wardrobeManager;

    public SubView CurrentView { get; set; } = SubView.List;

    public Guid? SelectedItem { get; set; }

    public Guid? HoveredItemId { get; set; }

    public string ModFilter { get; set; } = string.Empty;

    public WardrobeLayer EditingLayer { get; set; }
    public WardrobeItem? EditingWardrobeItem { get; set; }

    public string EditedName { get; set; } = string.Empty;
    public string EditedDescription { get; set; } = string.Empty;

    public WardrobeLayer SelectedSlotLayer { get; set; } = WardrobeLayer.CustomLayer1;
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
            var sets = _wardrobeManager.ImportedDesigns.ToList();
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

    public WardrobeViewUiController(LockService lockService, WardrobeManager wardrobeManager)
    {
        _lockService = lockService;
        _wardrobeManager = wardrobeManager;
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

        var slot = WardrobeSlotHelper.GetSlotFromName(SelectedSlotLayer);

        var mods = new List<GlamourerMod>();
        foreach (var (dirName, settings) in SelectedModSettings)
        {
            var mod = AvailableMods.FirstOrDefault(m => m.Item1.DirectoryName == dirName);
            if (mod.Item1 != null)
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
        SelectedSlotLayer = "Head";
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

    public void DeletePiece(Guid id)
    {
        _wardrobeManager.DeleteItem(id);
        if (SelectedPieceId == id)
            SelectedPieceId = null;
    }

    public bool IsPieceEquipped(Guid pieceId)
    {
        return _wardrobeManager.IsPieceInActiveSet(pieceId);
    }

    public bool IsSetEquipped(Guid setId)
    {
        return _wardrobeManager.IsSetActive(setId);
    }

    public void DeleteSet(Guid id)
    {
        _wardrobeManager.DeleteSet(id);
        if (SelectedItem == id)
            SelectedItem = null;
    }

    public async Task ApplySetAsync(string name)
    {
        await _wardrobeManager.ApplySetAsync(name);
    }

    public async Task RemoveActiveSetAsync()
    {
        await _wardrobeManager.RemoveActiveSetAsync();
    }

    public async Task ApplyPieceAsync(ClientWardrobeItem piece)
    {
        await _wardrobeManager.ApplyPieceAsync(piece);
    }

    public async Task RemoveSlotItemAsync(string slotName)
    {
        if (slotName == "BaseSet")
        {
            await _wardrobeManager.RemoveActiveSetAsync();
        }
        else
        {
            var layer = WardrobeSlotHelper.GetLayerFromName(slotName);
            await _wardrobeManager.RemovePieceFromSlotAsync(layer);
        }
    }

    public List<SlotStatus> GetActiveSlotStatuses() => _wardrobeManager.GetActiveSlotStatuses();

    public async Task ImportFromPlayerAsync()
    {
        var slot = WardrobeSlotHelper.GetSlotFromName(ImportSlotName);
        var item = await _wardrobeManager.GetGlamourSlotFromPlayer(slot);
        if (item != null)
        {
            EditedItem = item;
            EditedDye1 = item.Stain;
            EditedDye2 = item.Stain2;
            SelectedSlotLayer = ImportSlotName;
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
