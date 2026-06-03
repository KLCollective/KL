using System;
using System.Collections.Generic;
using System.Linq;
using KinkLinkClient.Utils;
using KinkLinkCommon.Dependencies.Glamourer;
using KinkLinkCommon.Dependencies.Glamourer.Components;
using KinkLinkCommon.Domain.Enums;
using KinkLinkCommon.Domain.Wardrobe;

namespace KinkLinkClient.Services;

public class ActiveWardrobe
{
    private Dictionary<WardrobeLayer, WardrobeItem> _layers = new();

    public void Clear() => _layers.Clear();

    public bool IsActive() => _layers.Count > 0;

    public bool HasLayer(WardrobeLayer layer) => _layers.ContainsKey(layer);

    public void RemoveLayer(WardrobeLayer layer) => _layers.Remove(layer);

    public IReadOnlyDictionary<WardrobeLayer, WardrobeItem> Layers => _layers;

    public bool HasItem(Guid id) => _layers.Values.Any(item => item.Id == id);

    public GlamourerDesign GetCurrentState()
    {
        if (!IsActive())
        {
            Plugin.Log.Error("There is nothing currently set. This should not have been called");
            return new();
        }

        // Seed metadata from the first active design to ensure valid FileVersion,
        // Identifier, etc. — otherwise a freshly-created GlamourerDesign has
        // FileVersion=0 which Glamourer rejects with InvalidState.
        var firstDesign = _layers.Values.FirstOrDefault(i => i?.Design != null)?.Design;

        GlamourerDesign merged;
        if (firstDesign != null)
        {
            merged = new GlamourerDesign
            {
                FileVersion = firstDesign.FileVersion,
                Identifier = firstDesign.Identifier,
                CreationDate = firstDesign.CreationDate,
                LastEdit = firstDesign.LastEdit,
                Name = firstDesign.Name,
                Description = firstDesign.Description,
                ForcedRedraw = firstDesign.ForcedRedraw,
                ResetAdvancedDyes = firstDesign.ResetAdvancedDyes,
                ResetTemporarySettings = firstDesign.ResetTemporarySettings,
                Color = firstDesign.Color,
                QuickDesign = firstDesign.QuickDesign,
                WriteProtected = firstDesign.WriteProtected,
                Tags = (string[])firstDesign.Tags.Clone(),
                Bonus = firstDesign.Bonus.Clone(),
                Materials = new Dictionary<string, GlamourerMaterial>(firstDesign.Materials),
            };
        }
        else
        {
            merged = new GlamourerDesign();
        }

        // Iterate layers in deterministic order and merge on top of base
        var orderedLayers = Enum.GetValues(typeof(WardrobeLayer))
            .Cast<WardrobeLayer>()
            .OrderBy(x => (int)x);
        foreach (var layer in orderedLayers)
        {
            if (_layers.TryGetValue(layer, out var item) && item != null)
            {
                merged = merged.Merge(item.Design, layer);
            }
        }

        return merged;
    }

    public List<GlamourerMod> GetMods()
    {
        var modlist = new Dictionary<string, GlamourerMod>();
        foreach (var kvp in _layers)
        {
            if (kvp.Value == null)
                continue;
            // Rather than extending, we loop _specifically_ to ensure that we don't get double mod applications.
            // In this case the first instance of the mod application in the list
            foreach (var mod in kvp.Value.Design.Mods)
            {
                modlist[mod.Directory] = mod;
            }
        }
        return modlist.Values.ToList();
    }

    // Compatibility helpers for older UI
    public GlamourerDesign? GetBaseLayer()
    {
        return _layers.TryGetValue(WardrobeLayer.Outfit, out var item) ? item.Design : null;
    }

    public WardrobeItem? GetIndividual(
        KinkLinkCommon.Dependencies.Glamourer.GlamourerEquipmentSlot slot
    )
    {
        var layer = slot switch
        {
            KinkLinkCommon.Dependencies.Glamourer.GlamourerEquipmentSlot.Head => WardrobeLayer.Head,
            KinkLinkCommon.Dependencies.Glamourer.GlamourerEquipmentSlot.Body =>
                WardrobeLayer.Chest,
            KinkLinkCommon.Dependencies.Glamourer.GlamourerEquipmentSlot.Hands =>
                WardrobeLayer.Hands,
            KinkLinkCommon.Dependencies.Glamourer.GlamourerEquipmentSlot.Legs => WardrobeLayer.Legs,
            KinkLinkCommon.Dependencies.Glamourer.GlamourerEquipmentSlot.Feet => WardrobeLayer.Feet,
            KinkLinkCommon.Dependencies.Glamourer.GlamourerEquipmentSlot.Ears => WardrobeLayer.Ears,
            KinkLinkCommon.Dependencies.Glamourer.GlamourerEquipmentSlot.Neck => WardrobeLayer.Neck,
            KinkLinkCommon.Dependencies.Glamourer.GlamourerEquipmentSlot.Wrists =>
                WardrobeLayer.Wrists,
            KinkLinkCommon.Dependencies.Glamourer.GlamourerEquipmentSlot.RFinger =>
                WardrobeLayer.RFinger,
            KinkLinkCommon.Dependencies.Glamourer.GlamourerEquipmentSlot.LFinger =>
                WardrobeLayer.LFinger,
            _ => WardrobeLayer.Outfit,
        };

        return _layers.TryGetValue(layer, out var item) ? item : null;
    }

    public WardrobeItem? GetIndividual(WardrobeLayer layer)
    {
        return _layers.TryGetValue(layer, out var item) ? item : null;
    }

    public void OverwriteWith(
        WardrobeStateDto dto,
        IReadOnlyDictionary<Guid, WardrobeItem>? library = null
    )
    {
        if (dto == null)
        {
            Plugin.Log.Error("Wardrobe Dto that was received was null");
            return;
        }

        try
        {
            var newLayers = new Dictionary<WardrobeLayer, WardrobeItem>();
            foreach (var kvp in dto.Layers)
            {
                var layer = kvp.Key;
                var base64 = kvp.Value;
                var design = GlamourerDesignHelper.FromBase64(base64) ?? new GlamourerDesign();
                var item = new WardrobeItem
                {
                    Design = design,
                    Layer = layer,
                    // Server doesn't propagate priority yet
                    Priority = RelationshipPriority.Casual,
                };

                newLayers[layer] = item;
            }

            _layers = newLayers;
        }
        catch (Exception e)
        {
            Plugin.Log.Error($"Failed to overwrite active wardrobe: {e}");
        }
    }
}
