using System.Diagnostics;
using KinkLinkCommon.Domain;
using KinkLinkCommon.Domain.Enums;
using KinkLinkCommon.Domain.Enums.Permissions;
using KinkLinkCommon.Domain.Network;
using KinkLinkCommon.Domain.Network.PairInteractions;
using KinkLinkCommon.Domain.Network.SyncPairState;
using KinkLinkCommon.Domain.Wardrobe;
using KinkLinkServer.Domain;
using Microsoft.AspNetCore.SignalR;

namespace KinkLinkServer.SignalR.Hubs;

public partial class PrimaryHub
{
    private ActionResult<T>? isValidPair<T>(string sender, string target)
    {
        if (string.IsNullOrWhiteSpace(target))
        {
            return ActionResultBuilder.Fail<T>(ActionResultEc.ClientBadData);
        }

        if (sender == target)
        {
            return ActionResultBuilder.Fail<T>(ActionResultEc.ClientBadData);
        }

        return null;
    }

    [HubMethodName(HubMethod.QueryPairState)]
    public async Task<ActionResult<QueryPairStateResponse>> QueryPairState(
        QueryPairStateRequest request
    )
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            logger.LogTrace(
                "[SignalR] QueryPairState: {FriendCode} -> {Target}",
                FriendCode,
                request.TargetFriendCode
            );
            if (
                isValidPair<QueryPairStateResponse>(FriendCode, request.TargetFriendCode) is
                { } result
            )
            {
                return result;
            }
            return await pairInteractionsHandler.QueryPairState(FriendCode, request);
        }
        finally
        {
            stopwatch.Stop();
            metricsService.IncrementSignalRMessage("QueryPairState", true);
            metricsService.RecordSignalRMessageDuration(
                "QueryPairState",
                stopwatch.ElapsedMilliseconds
            );
        }
    }

    [HubMethodName(HubMethod.QueryPairWardrobeState)]
    public async Task<ActionResult<QueryPairWardrobeStateResponse>> QueryWardrobeState(
        QueryPairWardrobeStateRequest request
    )
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            logger.LogTrace(
                "[SignalR] QueryWardrobeState: {FriendCode} -> {Target}",
                FriendCode,
                request.TargetFriendCode
            );
            if (
                isValidPair<QueryPairWardrobeStateResponse>(FriendCode, request.TargetFriendCode) is
                { } result
            )
            {
                return result;
            }
            return await pairInteractionsHandler.QueryWardrobeStateAsync(FriendCode, request);
        }
        finally
        {
            stopwatch.Stop();
            metricsService.IncrementSignalRMessage("QueryPairWardrobeState", true);
            metricsService.RecordSignalRMessageDuration(
                "QueryPairWardrobeState",
                stopwatch.ElapsedMilliseconds
            );
        }
    }

    [HubMethodName(HubMethod.QueryPairWardrobe)]
    public async Task<ActionResult<QueryPairWardrobeResponse>> QueryWardrobe(
        QueryPairWardrobeRequest request
    )
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            logger.LogTrace(
                "[SignalR] QueryWardrobe: {FriendCode} -> {Target}",
                FriendCode,
                request.TargetFriendCode
            );
            if (
                isValidPair<QueryPairWardrobeResponse>(FriendCode, request.TargetFriendCode) is
                { } result
            )
            {
                return result;
            }
            return await pairInteractionsHandler.QueryPairWardrobeAsync(FriendCode, request);
        }
        finally
        {
            stopwatch.Stop();
            metricsService.IncrementSignalRMessage("QueryPairWardrobe", true);
            metricsService.RecordSignalRMessageDuration(
                "QueryPairWardrobe",
                stopwatch.ElapsedMilliseconds
            );
        }
    }

    [HubMethodName(HubMethod.InteractionApplyWardrobe)]
    public async Task<ActionResultEc> InteractionUpdateWardrobeLayer(
        string targetFriendCode,
        WardrobeLayer layer,
        Guid? id
    )
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            logger.LogTrace(
                "[SignalR] InteractionApplyWardrobe: {FriendCode} -> {Target}",
                FriendCode,
                targetFriendCode
            );

            if (isValidPair<ActionResultEc>(FriendCode, targetFriendCode) is { } invalid)
            {
                return invalid.Result;
            }

            var r = await pairInteractionsHandler.UpdateWardrobeStateAsync(
                FriendCode,
                targetFriendCode,
                layer,
                id
            );
            return r.Result;
        }
        finally
        {
            stopwatch.Stop();
            metricsService.IncrementSignalRMessage("InteractionApplyWardrobe", true);
            metricsService.RecordSignalRMessageDuration(
                "InteractionApplyWardrobe",
                stopwatch.ElapsedMilliseconds
            );
        }
    }

    [HubMethodName(HubMethod.InteractionApplyLock)]
    public async Task<ActionResultEc> InteractionApplyLock(
        string targetFriendCode,
        LockInfoDto lockInfo
    )
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            logger.LogTrace(
                "[SignalR] InteractionApplyLock: {FriendCode} -> {Target} (LockId={LockId})",
                FriendCode,
                targetFriendCode,
                lockInfo.LockID
            );

            if (isValidPair<ActionResultEc>(FriendCode, targetFriendCode) is { } invalid)
            {
                return invalid.Result;
            }

            var r = await locksHandler.HandleAddLockAsync(FriendCode, lockInfo);
            return r.Result;
        }
        finally
        {
            stopwatch.Stop();
            metricsService.IncrementSignalRMessage("InteractionApplyLock", true);
            metricsService.RecordSignalRMessageDuration(
                "InteractionApplyLock",
                stopwatch.ElapsedMilliseconds
            );
        }
    }

    [HubMethodName(HubMethod.InteractionRemoveLock)]
    public async Task<ActionResultEc> InteractionRemoveLock(
        string targetFriendCode,
        string lockId,
        string? password
    )
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            logger.LogTrace(
                "[SignalR] InteractionRemoveLock: {FriendCode} -> {Target} (LockId={LockId})",
                FriendCode,
                targetFriendCode,
                lockId
            );

            if (isValidPair<ActionResultEc>(FriendCode, targetFriendCode) is { } invalid)
            {
                return ActionResultEc.TargetNotFriends;
            }

            var removeResult = await locksHandler.HandleRemoveLockAsync(
                FriendCode,
                lockId,
                targetFriendCode,
                password
            );
            if (removeResult.Result.Value)
            {
                return ActionResultEc.Success;
            }
            else
            {
                return ActionResultEc.Unknown;
            }
        }
        finally
        {
            stopwatch.Stop();
            metricsService.IncrementSignalRMessage("InteractionRemoveLock", true);
            metricsService.RecordSignalRMessageDuration(
                "InteractionRemoveLock",
                stopwatch.ElapsedMilliseconds
            );
        }
    }
}
