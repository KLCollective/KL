using System;
using System.Linq;
using System.Threading.Tasks;
using KinkLinkClient.Utils;
using KinkLinkCommon.Dependencies.Glamourer;
using KinkLinkCommon.Dependencies.Glamourer.Components;
using KinkLinkCommon.Domain.Network.Wardrobe;
using KinkLinkCommon.Domain.Wardrobe;

namespace KinkLinkClient.Services;

public partial class WardrobeManager
{
    public void AddLayer(GlamourerDesign design, string? lockId)
    {
        var layer = new WardrobeItem { Design = design };
        _layers[layer.Id] = layer;
        _ = SyncLayerToServerAsync(layer);
    }

    public void UpdateItem(WardrobeItem item)
    {
        var layer = new WardrobeItem { Design = item.Design };
        _layers[layer.Id] = layer;
        _ = SyncLayerToServerAsync(layer);
    }

    public void DeleteItem(Guid id)
    {
        if (_layers.TryGetValue(id, out var layer))
        {
            _layers.Remove(id);
            _ = _wardrobeNetworkService.RemoveWardrobeItemAsync(new RemoveWardrobeItemRequest(id));
        }
    }

    public WardrobeItem? GetItemById(Guid id)
    {
        return _layers.TryGetValue(id, out var layer) ? layer : null;
    }

    public WardrobeItem? GetItemByName(string name)
    {
        return _layers.Values.FirstOrDefault(s => s.Name == name);
    }

    public bool IsItemActive(Guid pieceId)
    {
        var piece = GetItemById(pieceId);
        if (piece == null)
            return false;

        return ActiveSet.HasLayer(piece.Layer) && ActiveSet.HasItem(piece.Id);
    }

    public bool IsLayerActive(WardrobeLayer layer)
    {
        return ActiveSet.HasLayer(layer);
    }

    private async Task SyncLayerToServerAsync(WardrobeItem layer)
    {
        var design = layer.Design.Clone();
        var dto = new WardrobeDto(
            layer.Id,
            layer.Name,
            layer.Description,
            layer.Layer,
            GlamourerDesignHelper.ToBase64(design),
            layer.Priority
        );
        await _wardrobeNetworkService.AddWardrobeItemAsync(new AddWardrobeItemRequest(dto));
    }
}
