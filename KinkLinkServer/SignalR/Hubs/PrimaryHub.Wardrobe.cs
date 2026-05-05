using System.Diagnostics;
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
        var stopwatch = Stopwatch.StartNew();
        var friendCode = FriendCode;
        try
        {
            logger.LogTrace(
                "[SignalR] AddWardrobeItem: {FriendCode}, ItemId: {ItemId}",
                friendCode,
                request.Id
            );
            var profileId = await profilesService.GetProfileIdFromUidAsync(friendCode);
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
    public async Task<ActionResult<bool>> RemoveWardrobeItem(Guid wardrobeId)
    {
        var stopwatch = Stopwatch.StartNew();
        var friendCode = FriendCode;
        try
        {
            logger.LogTrace(
                "[SignalR] RemoveWardrobeItem: {FriendCode}, WardrobeId: {WardrobeId}",
                friendCode,
                wardrobeId
            );
            var profileId = await profilesService.GetProfileIdFromUidAsync(friendCode);
            if (profileId is not { } id)
            {
                return new ActionResult<bool>(ActionResultEc.Unknown, false);
            }

            var success = await wardrobeDataService.DeleteWardrobeItemAsync(id, wardrobeId);

            return success
                ? new ActionResult<bool>(ActionResultEc.Success, true)
                : new ActionResult<bool>(ActionResultEc.Unknown, false);
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
    public async Task<ActionResult<WardrobeDto>> GetWardrobeItem(Guid wardrobeId)
    {
        var stopwatch = Stopwatch.StartNew();
        var friendCode = FriendCode;
        try
        {
            logger.LogTrace(
                "[SignalR] GetWardrobeItem: {FriendCode}, WardrobeId: {WardrobeId}",
                friendCode,
                wardrobeId
            );
            var profileId = await profilesService.GetProfileIdFromUidAsync(friendCode);
            if (profileId is not { } id)
            {
                return new ActionResult<WardrobeDto>(ActionResultEc.Unknown, null);
            }

            var item = await wardrobeDataService.GetWardrobeItemByGuid(id, wardrobeId);

            return item != null
                ? new ActionResult<WardrobeDto>(ActionResultEc.Success, item)
                : new ActionResult<WardrobeDto>(ActionResultEc.ValueNotSet, null);
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
    public async Task<ActionResult<List<WardrobeDto>>> ListWardrobeItems()
    {
        var stopwatch = Stopwatch.StartNew();
        var friendCode = FriendCode;
        try
        {
            logger.LogTrace("[SignalR] ListWardrobeItems: {FriendCode}", friendCode);
            var profileId = await profilesService.GetProfileIdFromUidAsync(friendCode);
            if (profileId is not { } id)
            {
                return new ActionResult<List<WardrobeDto>>(ActionResultEc.Unknown, []);
            }

            var items = await wardrobeDataService.GetAllWardrobeItemsAsync(id);

            return new ActionResult<List<WardrobeDto>>(ActionResultEc.Success, items);
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
    public async Task<ActionResult<bool>> SetWardrobeStatus(WardrobeStateDto state)
    {
        var stopwatch = Stopwatch.StartNew();
        var friendCode = FriendCode;
        try
        {
            logger.LogInformation(
                "[SignalR] SetWardrobeStatus: {FriendCode}, Equipment: {EquipCount}, ModSettings: {ModCount}",
                friendCode,
                state.Equipment?.Count ?? 0,
                state.ModSettings?.Count ?? 0
            );

            var profileId = await profilesService.GetProfileIdFromUidAsync(friendCode);
            if (profileId is not { } id)
            {
                logger.LogWarning(
                    "[SignalR] SetWardrobeStatus - profile not found for {FriendCode}",
                    friendCode
                );
                return new ActionResult<bool>(ActionResultEc.Unknown, false);
            }

            var success = await wardrobeDataService.UpdateWardrobeStateAsync(id, state);

            logger.LogInformation(
                "[SignalR] SetWardrobeStatus result for {FriendCode}: {Success}",
                friendCode,
                success
            );

            return success
                ? new ActionResult<bool>(ActionResultEc.Success, true)
                : new ActionResult<bool>(ActionResultEc.Unknown, false);
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
    public async Task<ActionResult<WardrobeStateDto>> GetWardrobeStatus()
    {
        var stopwatch = Stopwatch.StartNew();
        var friendCode = FriendCode;
        try
        {
            logger.LogTrace("[SignalR] GetWardrobeStatus: {FriendCode}", friendCode);
            var profileId = await profilesService.GetProfileIdFromUidAsync(friendCode);
            if (profileId is not { } id)
            {
                return new ActionResult<WardrobeStateDto>(ActionResultEc.Unknown, null);
            }

            var state = await wardrobeDataService.GetWardrobeStateAsync(id);

            return state != null
                ? new ActionResult<WardrobeStateDto>(ActionResultEc.Success, state)
                : new ActionResult<WardrobeStateDto>(ActionResultEc.ValueNotSet, null);
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

