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

public class WardrobeViewUiController
{
    private readonly LockService _lockService;
    private readonly WardrobeManager _wardrobeManager;
    private readonly WardrobeNetworkService _wardrobeNetworkService;

    public WardrobeManager WardrobeManager => _wardrobeManager;

    public bool GlamourerApiAvailable => _wardrobeManager.GlamourerApiAvailable;

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

    public int SortColumn { get; set; } = -1; // -1 = no sort
    public bool SortAscending { get; set; } = true;

    public RelationshipPriority EditedPriority { get; set; } = RelationshipPriority.Casual;

    private List<WardrobeItem>? _filteredItems;

    // selected item per layer used by Dressup view
    private readonly Dictionary<WardrobeLayer, Guid?> _selectedForLayer = new();

    public Guid? GetSelectedForLayer(WardrobeLayer layer) =>
        _selectedForLayer.TryGetValue(layer, out var v) ? v : null;

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

            // Sort
            if (SortColumn >= 0)
            {
                items = SortColumn switch
                {
                    0 => SortAscending
                        ? items.OrderBy(i => i.Name ?? string.Empty).ToList()
                        : items.OrderByDescending(i => i.Name ?? string.Empty).ToList(),
                    1 => SortAscending
                        ? items.OrderBy(i => i.Layer).ToList()
                        : items.OrderByDescending(i => i.Layer).ToList(),
                    2 => SortAscending
                        ? items.OrderBy(i => i.Priority).ToList()
                        : items.OrderByDescending(i => i.Priority).ToList(),
                    3 => SortAscending
                        ? items.OrderBy(i => IsItemEquipped(i.Id)).ToList()
                        : items.OrderByDescending(i => IsItemEquipped(i.Id)).ToList(),
                    _ => items,
                };
            }

            return items;
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

    public WardrobeViewUiController(
        LockService lockService,
        WardrobeManager wardrobeManager,
        WardrobeNetworkService wardrobeNetworkService
    )
    {
        _lockService = lockService;
        _wardrobeManager = wardrobeManager;
        _wardrobeNetworkService = wardrobeNetworkService;
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

    public bool CanEquipToSlot(WardrobeLayer layer)
    {
        return !IsSlotLocked(layer);
    }

    public bool CanRemoveFromSlot(WardrobeLayer layer)
    {
        // Must unlock first before removing
        return !IsSlotLocked(layer);
    }

    public void SaveSlotData()
    {
        if (EditingWardrobeItem == null)
            return;

        var existingId = EditingWardrobeItem.Id != Guid.Empty ? EditingWardrobeItem.Id : Guid.NewGuid();

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
            Id = existingId,
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
        SelectedSlotLayer = EditingWardrobeItem.Layer;
        HasImportedItem = EditingWardrobeItem.Item != null;
        if (HasImportedItem && EditingWardrobeItem.Item != null)
        {
            EditedItem = EditingWardrobeItem.Item.Clone();
            EditedDye1 = EditedItem.Stain;
            EditedDye2 = EditedItem.Stain2;
        }

        // Populate selected mod settings from existing item mods
        SelectedModSettings = new Dictionary<string, ModSettings>();
        if (EditingWardrobeItem.Mods != null)
        {
            foreach (var mod in EditingWardrobeItem.Mods)
            {
                SelectedModSettings[mod.Directory] = new ModSettings(
                    mod.Settings ?? new Dictionary<string, List<string>>(),
                    mod.Priority,
                    mod.Enabled,
                    mod.ForceInherit,
                    mod.Remove
                );
            }
        }
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
        EditingWardrobeItem = item ?? new WardrobeItem();
        if (item != null)
            LoadWardrobeItemData();

        // load glamourer designs and match current item's design
        GlamourerSearchTerm = string.Empty;
        SelectedGlamourerDesignId = Guid.Empty;
        RefreshDesigns();

        // if editing existing item, try to match its design in the list
        if (item != null && item.Design.Identifier != Guid.Empty)
        {
            SelectedGlamourerDesignId = item.Design.Identifier;
        }

        // load available mods async
        _ = LoadAvailableModsAsync();
        CurrentView = SubView.Editor;
    }

    public void CloseEditor()
    {
        ResetEditorFields();
        EditingWardrobeItem = null;
        CurrentView = SubView.List;
    }

    public void CloseImport()
    {
        SelectedGlamourerDesignId = Guid.Empty;
        GlamourerSearchTerm = string.Empty;
        EditedName = string.Empty;
        EditedDescription = string.Empty;
        CurrentView = SubView.List;
    }

    public async Task<bool> SaveEditorAsync()
    {
        if (EditingWardrobeItem == null)
            return false;

        var isNewItem = EditingWardrobeItem.Id == Guid.Empty;

        // If a new design was selected in the editor, fetch and apply it
        if (SelectedGlamourerDesignId != Guid.Empty)
        {
            var design = await _wardrobeManager.GetDesignAsync(SelectedGlamourerDesignId);
            if (design != null)
            {
                design.Name = EditedName;
                design.Description = EditedDescription;
                EditingWardrobeItem.Design = design;
            }
        }

        // For new items, require a design to be set
        if (isNewItem && EditingWardrobeItem.Design.Identifier == Guid.Empty && !HasImportedItem)
            return false;

        // Update basic properties directly (preserves full Design data)
        EditingWardrobeItem.Name = EditedName;
        EditingWardrobeItem.Description = EditedDescription;
        EditingWardrobeItem.Layer = SelectedSlotLayer;
        EditingWardrobeItem.Priority = EditedPriority;

        // Apply mods from editor
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
        EditingWardrobeItem.Mods = mods;

        if (!isNewItem)
            _wardrobeManager.UpdateItem(EditingWardrobeItem.Id, EditingWardrobeItem);
        else
            _wardrobeManager.AddDesign(EditingWardrobeItem);

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
