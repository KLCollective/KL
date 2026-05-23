using System;
using System.Collections.Generic;
using System.Linq;
using KinkLinkClient.Utils;
using KinkLinkCommon.Dependencies.Glamourer;
using KinkLinkCommon.Domain.Enums;
using KinkLinkCommon.Domain.Wardrobe;

namespace KinkLinkClient.Services;

public class ActiveWardrobe
{
    private Dictionary<WardrobeLayer, WardrobeItem> _layers = new();

    public void Clear() => _layers.Clear();

    public bool IsActive() => _layers.Count > 0;

    public bool HasLayer(WardrobeLayer layer) => _layers.ContainsKey(layer);

    public bool HasItem(Guid id) => _layers.Values.Any(item => item.Id == id);

    public GlamourerDesign GetCurrentState()
    {
        if (!IsActive())
        {
            Plugin.Log.Error("There is nothing currently set. This should not have been called");
            return new();
        }
        GlamourerDesign merged = _layers[WardrobeLayer.BaseLayer].Design;
        // Iterate through the `WardrobeLayer` from `BaseLayer` to `Mods` and merge the glamourer set

        foreach (var (layer, item) in _layers)
        {
            merged.Merge(item.Design, layer);
        }

        return merged;
    }

    public List<GlamourerMod> GetMods()
    {
        var modlist = new List<GlamourerMod>();
        foreach (var kvp in _layers)
        {
            if (kvp.Value != null)
                modlist.AddRange(kvp.Value.Design.Mods);
        }
        return modlist;
    }

    public void OverwriteWith(WardrobeStateDto dto)
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
                    // TODO: Implement this on the server side.
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
