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
    public void AddSet(GlamourerDesign design, string? lockId)
    {
        var set = new WardrobeSet { Design = design };
        _sets[set.Id] = set;
        _ = SyncSetToServerAsync(set, lockId);
    }

    public void UpdateSet(GlamourerDesign design, string? lockId)
    {
        var set = new WardrobeSet { Design = design };
        _sets[set.Id] = set;
        _ = SyncSetToServerAsync(set, lockId);
    }

    public void DeleteSet(Guid id)
    {
        if (_sets.TryGetValue(id, out var set))
        {
            _sets.Remove(id);
            _ = _wardrobeNetworkService.RemoveWardrobeItemAsync(new RemoveWardrobeItemRequest(id));
        }
    }

    public WardrobeSet? GetSetById(Guid id)
    {
        return _sets.TryGetValue(id, out var set) ? set : null;
    }

    public WardrobeSet? GetSetByName(string name)
    {
        return _sets.Values.FirstOrDefault(s => s.Name == name);
    }

    public bool IsPieceInActiveSet(Guid pieceId)
    {
        var piece = GetPieceById(pieceId);
        if (piece == null)
            return false;

        var activeItem = ActiveSet.GetIndividual(piece.Slot);
        return activeItem?.Id == pieceId;
    }

    public bool IsSetActive(Guid setId)
    {
        var set = GetSetById(setId);
        if (set == null)
            return false;

        var currentBaseLayer = ActiveSet.GetBaseLayer();
        return currentBaseLayer?.Identifier == setId;
    }

    public void ApplySetByIdSync(Guid setId)
    {
        var set = GetSetById(setId);
        if (set != null)
        {
            ActiveSet.SetBaseLayer(set.Design, set.Priority);
        }
    }

    private async Task SyncSetToServerAsync(WardrobeSet set, string? lockId)
    {
        var design = set.Design.Clone();
        var dto = new WardrobeDto(
            set.Id,
            set.Name,
            set.Description,
            "set",
            GlamourerEquipmentSlot.None,
            GlamourerDesignHelper.ToBase64(design),
            set.Priority,
            lockId
        );
        await _wardrobeNetworkService.AddWardrobeItemAsync(new AddWardrobeItemRequest(dto));
    }
}
