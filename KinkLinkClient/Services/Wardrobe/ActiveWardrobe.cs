using System;
using System.Collections.Generic;
using System.Linq;
using KinkLinkCommon.Dependencies.Glamourer;
using KinkLinkCommon.Dependencies.Glamourer.Components;
using KinkLinkCommon.Domain.Enums;

namespace KinkLinkClient.Services;

public class ActiveWardrobe
{
    private WardrobeSet? _baseLayer;
    private readonly Dictionary<GlamourerEquipmentSlot, WardrobeItem?> _equipment = new();
    private readonly Dictionary<Guid, WardrobeItem> _characterItems = new();
    public WardrobeSet? BaseLayer
    {
        get => _baseLayer;
        private set => _baseLayer = value;
    }

    public bool IsActive()
    {
        return BaseLayer != null
            || _equipment.Values.Any(v => v != null);
    }

    public void SetBaseLayer(GlamourerDesign design, RelationshipPriority priority)
    {
        Plugin.Log.Verbose("[ActiveWardrobe] SetBaseLayer id={Id} name={Name} priority={Priority}", design.Identifier, design.Name, priority);
        BaseLayer = new WardrobeSet { Design = design, Priority = priority };
    }

    public void ClearBaseLayer()
    {
        Plugin.Log.Verbose("[ActiveWardrobe] ClearBaseLayer");
        BaseLayer = null;
    }

    public void SetIndividual(GlamourerEquipmentSlot slot, WardrobeItem item)
    {
        Plugin.Log.Verbose("[ActiveWardrobe] SetIndividual slot={Slot} id={Id} name={Name}", slot, item.Id, item.Name);
        _equipment[slot] = item;
    }

    public void ClearIndividual(GlamourerEquipmentSlot slot)
    {
        Plugin.Log.Verbose("[ActiveWardrobe] ClearIndividual slot={Slot}", slot);
        _equipment[slot] = null;
    }

    public void AddModItem(WardrobeItem item)
    {
        Plugin.Log.Verbose("[ActiveWardrobe] AddModItem id={Id} name={Name} mods={ModCount}", item.Id, item.Name, item.Mods?.Count ?? 0);
        _characterItems[item.Id] = item;
    }

    public void ClearModItem(Guid id)
    {
        if (_characterItems.ContainsKey(id))
        {
            Plugin.Log.Verbose("[ActiveWardrobe] ClearModItem id={Id}", id);
            _characterItems.Remove(id);
        }
    }

    public void ClearAllModItems()
    {
        _characterItems.Clear();
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
                WardrobeSlotHelper.SetEquipmentSlot(final.Equipment, slot, slotitem);
            }
        }

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
}
