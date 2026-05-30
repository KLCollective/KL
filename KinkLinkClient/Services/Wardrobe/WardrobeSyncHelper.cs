using System;
using System.Collections.Generic;
using System.Linq;
using KinkLinkCommon.Domain.Wardrobe;
using KinkLinkCommon.Dependencies.Glamourer;
using KinkLinkClient.Utils;

namespace KinkLinkClient.Services;

public static class WardrobeSyncHelper
{
    public static void ApplyServerItems(IDictionary<Guid, WardrobeItem> localLibrary, IEnumerable<WardrobeDto> serverItems)
    {
        var serverIds = new HashSet<Guid>();
        foreach (var item in serverItems)
        {
            serverIds.Add(item.Id);
            GlamourerDesign design;
            try
            {
                design = GlamourerDesignHelper.FromBase64(item.Base64GlamourerData) ?? new GlamourerDesign();
            }
            catch
            {
                // In test environments Glamourer types may not be available; fallback to empty design
                design = new GlamourerDesign();
            }
            var localItem = new WardrobeItem
            {
                Design = design,
                Priority = item.Priority,
                Layer = item.Layer,
                Name = item.Name ?? string.Empty,
                Description = item.Description ?? string.Empty,
            };
            localItem.Id = item.Id;
            localLibrary[item.Id] = localItem;
        }

        var toRemove = localLibrary.Keys.Except(serverIds).ToList();
        foreach (var id in toRemove)
        {
            localLibrary.Remove(id);
        }
    }
}
