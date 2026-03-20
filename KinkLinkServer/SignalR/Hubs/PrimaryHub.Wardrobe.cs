using KinkLinkCommon.Domain.Enums;
using KinkLinkCommon.Domain.Network;
using KinkLinkCommon.Domain.Wardrobe;
using KinkLinkServer.Services;
using Microsoft.AspNetCore.SignalR;

namespace KinkLinkServer.SignalR.Hubs;

public partial class PrimaryHub
{
    [HubMethodName(HubMethod.AddWardrobeItem)]
    public async Task<ActionResult<WardrobeDto>> AddWardrobeItem(WardrobeDto request)
    {
        var friendCode = FriendCode;
        var profileId = await profilesService.GetIdFromUidAsync(friendCode);
        if (profileId is not { } id)
        {
            return new ActionResult<WardrobeDto>(ActionResultEc.Unknown, null);
        }

        var success = await wardrobeDataService.CreateOrUpdateWardrobeItemsByNameAsync(
            id,
            request.Id,
            request
        );

        return success
            ? new ActionResult<WardrobeDto>(ActionResultEc.Success, request)
            : new ActionResult<WardrobeDto>(ActionResultEc.Unknown, null);
    }

    [HubMethodName(HubMethod.RemoveWardrobeItem)]
    public async Task<ActionResult<bool>> RemoveWardrobeItem(Guid wardrobeId)
    {
        var friendCode = FriendCode;
        var profileId = await profilesService.GetIdFromUidAsync(friendCode);
        if (profileId is not { } id)
        {
            return new ActionResult<bool>(ActionResultEc.Unknown, false);
        }

        var success = await wardrobeDataService.DeleteWardrobeItemAsync(id, wardrobeId);

        return success
            ? new ActionResult<bool>(ActionResultEc.Success, true)
            : new ActionResult<bool>(ActionResultEc.Unknown, false);
    }

    [HubMethodName(HubMethod.GetWardrobeItem)]
    public async Task<ActionResult<WardrobeDto>> GetWardrobeItem(Guid wardrobeId)
    {
        var friendCode = FriendCode;
        var profileId = await profilesService.GetIdFromUidAsync(friendCode);
        if (profileId is not { } id)
        {
            return new ActionResult<WardrobeDto>(ActionResultEc.Unknown, null);
        }

        var item = await wardrobeDataService.GetWardrobeItemByGuid(id, wardrobeId);

        return item != null
            ? new ActionResult<WardrobeDto>(ActionResultEc.Success, item)
            : new ActionResult<WardrobeDto>(ActionResultEc.ValueNotSet, null);
    }

    [HubMethodName(HubMethod.ListWardrobeItems)]
    public async Task<ActionResult<List<WardrobeDto>>> ListWardrobeItems()
    {
        var friendCode = FriendCode;
        var profileId = await profilesService.GetIdFromUidAsync(friendCode);
        if (profileId is not { } id)
        {
            return new ActionResult<List<WardrobeDto>>(ActionResultEc.Unknown, []);
        }

        var items = await wardrobeDataService.GetAllWardrobeItemsAsync(id);

        return new ActionResult<List<WardrobeDto>>(ActionResultEc.Success, items);
    }

    [HubMethodName(HubMethod.SetWardrobeStatus)]
    public async Task<ActionResult<bool>> SetWardrobeStatus(WardrobeStateDto state)
    {
        var friendCode = FriendCode;
        logger.LogInformation("[PrimaryHub] SetWardrobeStatus called for {FriendCode}", friendCode);

        var profileId = await profilesService.GetIdFromUidAsync(friendCode);
        if (profileId is not { } id)
        {
            logger.LogWarning("[PrimaryHub] SetWardrobeStatus - profile not found for {FriendCode}", friendCode);
            return new ActionResult<bool>(ActionResultEc.Unknown, false);
        }

        logger.LogInformation("[PrimaryHub] SetWardrobeStatus for {FriendCode}, profileId: {ProfileId}, Equipment: {EquipCount}, ModSettings: {ModCount}",
            friendCode, id, state.Equipment?.Count ?? 0, state.ModSettings?.Count ?? 0);

        var success = await wardrobeDataService.UpdateWardrobeStateAsync(id, state);

        logger.LogInformation("[PrimaryHub] SetWardrobeStatus result for {FriendCode}: {Success}", friendCode, success);

        return success
            ? new ActionResult<bool>(ActionResultEc.Success, true)
            : new ActionResult<bool>(ActionResultEc.Unknown, false);
    }

    [HubMethodName(HubMethod.GetWardrobeStatus)]
    public async Task<ActionResult<WardrobeStateDto>> GetWardrobeStatus()
    {
        var friendCode = FriendCode;
        var profileId = await profilesService.GetIdFromUidAsync(friendCode);
        if (profileId is not { } id)
        {
            return new ActionResult<WardrobeStateDto>(ActionResultEc.Unknown, null);
        }

        var state = await wardrobeDataService.GetWardrobeStateAsync(id);

        return state != null
            ? new ActionResult<WardrobeStateDto>(ActionResultEc.Success, state)
            : new ActionResult<WardrobeStateDto>(ActionResultEc.ValueNotSet, null);
    }
}
