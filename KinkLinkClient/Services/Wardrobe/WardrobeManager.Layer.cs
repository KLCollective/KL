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
    public void AddLayer(GlamourerDesign design, WardrobeLayer layer)
    {
        var layerItem = new WardrobeItem
        {
            Id = Guid.NewGuid(),
            Design = design,
            Layer = layer,
        };
        _wardrobeLibrary[layerItem.Id] = layerItem;
        _ = SyncItemToServer(layerItem);
    }

    public void UpdateItem(Guid id, WardrobeItem item)
    {
        // Ensure that the item's ID matches the provided ID to prevent accidental overwrites
        item.Id = id;
        _wardrobeLibrary[id] = item;
        _ = SyncItemToServer(item);
    }

    public void DeleteItem(Guid id)
    {
        if (_wardrobeLibrary.TryGetValue(id, out var layer))
        {
            _wardrobeLibrary.Remove(id);
            _ = _wardrobeNetworkService.RemoveWardrobeItemAsync(new RemoveWardrobeItemRequest(id));
        }
    }

    public WardrobeItem? GetItemById(Guid id)
    {
        return _wardrobeLibrary.TryGetValue(id, out var item) ? item : null;
    }

    public WardrobeItem? GetItemByName(string name)
    {
        return _wardrobeLibrary.Values.FirstOrDefault(s => s.Name == name);
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

    private async Task SyncItemToServer(WardrobeItem layer)
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
