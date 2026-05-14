using System.Diagnostics;
using KinkLinkCommon.Domain.Enums;
using KinkLinkCommon.Domain.Network;
using KinkLinkCommon.Domain.Network.Wardrobe;
using KinkLinkCommon.Domain.Wardrobe;
using KinkLinkServer.Services;
using Microsoft.AspNetCore.SignalR;

namespace KinkLinkServer.SignalR.Hubs;

public partial class PrimaryHub
{
    [HubMethodName(HubMethod.AddWardrobeItem)]
    public async Task<ActionResult<AddWardrobeItemResponse>> AddWardrobeItem(AddWardrobeItemRequest request)
    {
        var stopwatch = Stopwatch.StartNew();
        var friendCode = FriendCode;
        try
        {
            logger.LogTrace(
                "[SignalR] AddWardrobeItem: {FriendCode}, ItemId: {ItemId}",
                friendCode,
                request.Item.Id
            );
            var profileId = await profilesService.GetProfileIdFromUidAsync(friendCode);
            if (profileId is not { } id)
            {
                return new ActionResult<AddWardrobeItemResponse>(ActionResultEc.Unknown, null);
            }

            var success = await wardrobeDataService.CreateOrUpdateWardrobeItemsByNameAsync(
                id,
                request.Item.Id,
                request.Item
            );

            return success
                ? new ActionResult<AddWardrobeItemResponse>(ActionResultEc.Success, new AddWardrobeItemResponse(request.Item))
                : new ActionResult<AddWardrobeItemResponse>(ActionResultEc.Unknown, null);
        }
        finally
        {
            stopwatch.Stop();
            metricsService.IncrementSignalRMessage("AddWardrobeItem", true);
            metricsService.RecordSignalRMessageDuration(
                "AddWardrobeItem",
                stopwatch.ElapsedMilliseconds
            );
        }
    }

    [HubMethodName(HubMethod.RemoveWardrobeItem)]
    public async Task<ActionResult<RemoveWardrobeItemResponse>> RemoveWardrobeItem(RemoveWardrobeItemRequest request)
    {
        var stopwatch = Stopwatch.StartNew();
        var friendCode = FriendCode;
        try
        {
            logger.LogTrace(
                "[SignalR] RemoveWardrobeItem: {FriendCode}, WardrobeId: {WardrobeId}",
                friendCode,
                request.WardrobeId
            );
            var profileId = await profilesService.GetProfileIdFromUidAsync(friendCode);
            if (profileId is not { } id)
            {
                return new ActionResult<RemoveWardrobeItemResponse>(ActionResultEc.Unknown, null);
            }

            var success = await wardrobeDataService.DeleteWardrobeItemAsync(id, request.WardrobeId);

            return success
                ? new ActionResult<RemoveWardrobeItemResponse>(ActionResultEc.Success, new RemoveWardrobeItemResponse(true))
                : new ActionResult<RemoveWardrobeItemResponse>(ActionResultEc.Unknown, null);
        }
        finally
        {
            stopwatch.Stop();
            metricsService.IncrementSignalRMessage("RemoveWardrobeItem", true);
            metricsService.RecordSignalRMessageDuration(
                "RemoveWardrobeItem",
                stopwatch.ElapsedMilliseconds
            );
        }
    }

    [HubMethodName(HubMethod.GetWardrobeItem)]
    public async Task<ActionResult<GetWardrobeItemResponse>> GetWardrobeItem(GetWardrobeItemRequest request)
    {
        var stopwatch = Stopwatch.StartNew();
        var friendCode = FriendCode;
        try
        {
            logger.LogTrace(
                "[SignalR] GetWardrobeItem: {FriendCode}, WardrobeId: {WardrobeId}",
                friendCode,
                request.WardrobeId
            );
            var profileId = await profilesService.GetProfileIdFromUidAsync(friendCode);
            if (profileId is not { } id)
            {
                return new ActionResult<GetWardrobeItemResponse>(ActionResultEc.Unknown, null);
            }

            var item = await wardrobeDataService.GetWardrobeItemByGuid(id, request.WardrobeId);

            return item != null
                ? new ActionResult<GetWardrobeItemResponse>(ActionResultEc.Success, new GetWardrobeItemResponse(item))
                : new ActionResult<GetWardrobeItemResponse>(ActionResultEc.ValueNotSet, null);
        }
        finally
        {
            stopwatch.Stop();
            metricsService.IncrementSignalRMessage("GetWardrobeItem", true);
            metricsService.RecordSignalRMessageDuration(
                "GetWardrobeItem",
                stopwatch.ElapsedMilliseconds
            );
        }
    }

    [HubMethodName(HubMethod.ListWardrobeItems)]
    public async Task<ActionResult<ListWardrobeItemsResponse>> ListWardrobeItems()
    {
        var stopwatch = Stopwatch.StartNew();
        var friendCode = FriendCode;
        try
        {
            logger.LogTrace("[SignalR] ListWardrobeItems: {FriendCode}", friendCode);
            var profileId = await profilesService.GetProfileIdFromUidAsync(friendCode);
            if (profileId is not { } id)
            {
                return new ActionResult<ListWardrobeItemsResponse>(ActionResultEc.Unknown, null);
            }

            var items = await wardrobeDataService.GetAllWardrobeItemsAsync(id);

            return new ActionResult<ListWardrobeItemsResponse>(ActionResultEc.Success, new ListWardrobeItemsResponse(items));
        }
        finally
        {
            stopwatch.Stop();
            metricsService.IncrementSignalRMessage("ListWardrobeItems", true);
            metricsService.RecordSignalRMessageDuration(
                "ListWardrobeItems",
                stopwatch.ElapsedMilliseconds
            );
        }
    }

    [HubMethodName(HubMethod.SetWardrobeStatus)]
    public async Task<ActionResult<SetWardrobeStatusResponse>> SetWardrobeStatus(SetWardrobeStatusRequest request)
    {
        var stopwatch = Stopwatch.StartNew();
        var friendCode = FriendCode;
        try
        {
            logger.LogInformation(
                "[SignalR] SetWardrobeStatus: {FriendCode}, Equipment: {EquipCount}, ModSettings: {ModCount}",
                friendCode,
                request.State.Equipment?.Count ?? 0,
                request.State.ModSettings?.Count ?? 0
            );

            var profileId = await profilesService.GetProfileIdFromUidAsync(friendCode);
            if (profileId is not { } id)
            {
                logger.LogWarning(
                    "[SignalR] SetWardrobeStatus - profile not found for {FriendCode}",
                    friendCode
                );
                return new ActionResult<SetWardrobeStatusResponse>(ActionResultEc.Unknown, null);
            }

            var success = await wardrobeDataService.UpdateWardrobeStateAsync(id, request.State);

            logger.LogInformation(
                "[SignalR] SetWardrobeStatus result for {FriendCode}: {Success}",
                friendCode,
                success
            );

            if (success)
            {
                return new ActionResult<SetWardrobeStatusResponse>(ActionResultEc.Success, new SetWardrobeStatusResponse(true));
            }

            return new ActionResult<SetWardrobeStatusResponse>(ActionResultEc.Unknown, null);
        }
        finally
        {
            stopwatch.Stop();
            metricsService.IncrementSignalRMessage("SetWardrobeStatus", true);
            metricsService.RecordSignalRMessageDuration(
                "SetWardrobeStatus",
                stopwatch.ElapsedMilliseconds
            );
        }
    }

    [HubMethodName(HubMethod.GetWardrobeStatus)]
    public async Task<ActionResult<GetWardrobeStatusResponse>> GetWardrobeStatus()
    {
        var stopwatch = Stopwatch.StartNew();
        var friendCode = FriendCode;
        try
        {
            logger.LogTrace("[SignalR] GetWardrobeStatus: {FriendCode}", friendCode);
            var profileId = await profilesService.GetProfileIdFromUidAsync(friendCode);
            if (profileId is not { } id)
            {
                return new ActionResult<GetWardrobeStatusResponse>(ActionResultEc.Unknown, null);
            }

            var state = await wardrobeDataService.GetWardrobeStateAsync(id);

            return state != null
                ? new ActionResult<GetWardrobeStatusResponse>(ActionResultEc.Success, new GetWardrobeStatusResponse(state))
                : new ActionResult<GetWardrobeStatusResponse>(ActionResultEc.ValueNotSet, null);
        }
        finally
        {
            stopwatch.Stop();
            metricsService.IncrementSignalRMessage("GetWardrobeStatus", true);
            metricsService.RecordSignalRMessageDuration(
                "GetWardrobeStatus",
                stopwatch.ElapsedMilliseconds
            );
        }
    }
}

