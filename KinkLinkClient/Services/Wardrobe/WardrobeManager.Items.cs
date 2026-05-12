using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using KinkLinkClient.Utils;
using KinkLinkCommon.Domain.Network.Wardrobe;
using KinkLinkCommon.Domain.Wardrobe;

namespace KinkLinkClient.Services;

public partial class WardrobeManager
{
    public void LoadFromWardrobeDto(List<WardrobeDto> dtos)
    {
        _items.Clear();
        _sets.Clear();
        _modItems.Clear();

        foreach (var dto in dtos)
        {
            if (dto.DataBase64 == null)
            {
                Plugin.Log.Warning($"[WardrobeManager] received DTO {dto.Name} with empty data");
                continue;
            }
            switch (dto.Type)
            {
                case "item":
                    var item = GlamourerDesignHelper.FromItemBase64(dto.DataBase64);
                    if (item != null)
                        _items[item.Id] = item;
                    break;

                case "set":
                    var design = GlamourerDesignHelper.FromBase64(dto.DataBase64);
                    if (design != null)
                    {
                        var set = new WardrobeSet { Design = design, Priority = dto.Priority };
                        _sets[set.Id] = set;
                    }
                    break;

                case "moditem":
                    var mods = GlamourerDesignHelper.FromItemBase64(dto.DataBase64);
                    if (mods != null)
                        _modItems[mods.Id] = mods;
                    break;
            }
        }
    }

    public void AddPiece(WardrobeItem piece, string? lockId)
    {
        _items[piece.Id] = piece;
        _ = SyncPieceToServerAsync(piece, "item", lockId);
    }

    public void UpdatePiece(WardrobeItem piece, string? lockId)
    {
        _items[piece.Id] = piece;
        _ = SyncPieceToServerAsync(piece, "item", lockId);
    }

    public void DeletePiece(Guid id)
    {
        if (_items.TryGetValue(id, out var piece))
        {
            _items.Remove(id);
            _ = _wardrobeNetworkService.RemoveWardrobeItemAsync(new RemoveWardrobeItemRequest(id));
        }
    }

    public WardrobeItem? GetPieceById(Guid id)
    {
        return _items.TryGetValue(id, out var item) ? item : null;
    }

    public void AddModItem(WardrobeItem item, string? lockId)
    {
        _modItems[item.Id] = item;
        _ = SyncPieceToServerAsync(item, "moditem", lockId);
    }

    public void UpdateModItem(WardrobeItem item, string? lockId)
    {
        _modItems[item.Id] = item;
        _ = SyncPieceToServerAsync(item, "moditem", lockId);
    }

    public void DeleteModItem(Guid id)
    {
        if (_modItems.TryGetValue(id, out var item))
        {
            _modItems.Remove(id);
            _ = _wardrobeNetworkService.RemoveWardrobeItemAsync(new RemoveWardrobeItemRequest(id));
        }
    }

    public WardrobeItem? GetModItemById(Guid id)
    {
        return _modItems.TryGetValue(id, out var item) ? item : null;
    }

    public WardrobeItem? GetCharacterItemById(Guid id) => GetModItemById(id);

    public void AddCharacterItem(WardrobeItem item, string? lockId) => AddModItem(item, lockId);

    public void UpdateCharacterItem(WardrobeItem item, string? lockId) => UpdateModItem(item, lockId);

    public void DeleteCharacterItem(Guid id) => DeleteModItem(id);

    private async Task SyncPieceToServerAsync(WardrobeItem item, string type, string? lockId)
    {
        var dto = new WardrobeDto(
            item.Id,
            item.Name,
            item.Description,
            type,
            item.Slot,
            GlamourerDesignHelper.ItemToBase64(item),
            item.Priority,
            lockId
        );
        await _wardrobeNetworkService.AddWardrobeItemAsync(new AddWardrobeItemRequest(dto));
    }
}
