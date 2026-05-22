using System;
using System.Collections.Generic;
using System.Linq;
using KinkLinkCommon.Dependencies.Glamourer;
using KinkLinkCommon.Domain.Wardrobe;

namespace KinkLinkClient.Services;

public class ActiveWardrobe
{
    private readonly Dictionary<WardrobeLayer, WardrobeItem> _layers = new();

    public bool IsActive()
    {
        return _layers.Count > 0;
    }

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
}
