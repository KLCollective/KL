using System;
using System.Diagnostics;
using KinkLinkCommon.Domain;
using KinkLinkCommon.Domain.CharacterState;
using KinkLinkCommon.Domain.Enums;
using KinkLinkCommon.Domain.Network;
using KinkLinkCommon.Domain.Network.PairInteractions;
using KinkLinkCommon.Domain.Network.SyncPairState;
using KinkLinkCommon.Domain.Wardrobe;
using KinkLinkServer.Domain;
using KinkLinkServer.SignalR.Handlers;
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
            return await _pairInteractionsHandler.QueryPairState(FriendCode, request);
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
            return await _pairInteractionsHandler.QueryWardrobeStateAsync(FriendCode, request);
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
            return await _pairInteractionsHandler.QueryWardrobeAsync(FriendCode, request);
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
    public async Task<ActionResultEc> InteractionApplyWardrobe(
        string targetFriendCode,
        WardrobeDto dto
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

            var payload = new InteractionPayload(null, null, new List<WardrobeDto> { dto });
            var request = new ApplyInteractionRequest(targetFriendCode, PairAction.ApplyWardrobe, payload);

            var (result, _, _) = await _pairInteractionsHandler.ApplyInteraction(FriendCode, request);
            return result.Result;
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

    [HubMethodName(HubMethod.InteractionRemoveWardrobe)]
    public async Task<ActionResultEc> InteractionRemoveWardrobe(
        string targetFriendCode,
        WardrobeLayer layer
    )
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            logger.LogTrace(
                "[SignalR] InteractionRemoveWardrobe: {FriendCode} -> {Target} (Layer={Layer})",
                FriendCode,
                targetFriendCode,
                layer
            );

            if (isValidPair<ActionResultEc>(FriendCode, targetFriendCode) is { } invalid)
            {
                return invalid.Result;
            }

            // build minimal payload indicating removal of layer
            var removeItem = new WardrobeDto(Guid.Empty, string.Empty, string.Empty, layer, string.Empty, 0);
            var payload = new InteractionPayload(null, null, new List<WardrobeDto> { removeItem });
            var request = new ApplyInteractionRequest(targetFriendCode, PairAction.RemoveWardrobe, payload);

            var (result, _, _) = await _pairInteractionsHandler.ApplyInteraction(FriendCode, request);
            return result.Result;
        }
        finally
        {
            stopwatch.Stop();
            metricsService.IncrementSignalRMessage("InteractionRemoveWardrobe", true);
            metricsService.RecordSignalRMessageDuration(
                "InteractionRemoveWardrobe",
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

            var (addResult, lockee) = await _locksHandler.HandleAddLockAsync(FriendCode, lockInfo);
            return addResult.Result;
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
        LockInfoDto lockInfo
    )
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            logger.LogTrace(
                "[SignalR] InteractionRemoveLock: {FriendCode} -> {Target} (LockId={LockId})",
                FriendCode,
                targetFriendCode,
                lockInfo.LockID
            );

            if (isValidPair<ActionResultEc>(FriendCode, targetFriendCode) is { } invalid)
            {
                return invalid.Result;
            }

            var (removeResult, _, _) = await _locksHandler.HandleRemoveLockAsync(
                FriendCode,
                lockInfo.LockID,
                targetFriendCode,
                lockInfo.Password
            );
            return removeResult.Result;
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

    private async Task<object?> GetStateForTarget(string targetFriendCode)
    {
        var targetProfileId = await profilesService.GetProfileIdFromUidAsync(targetFriendCode);
        if (targetProfileId == null)
            return null;

        var locks = await _locksHandler.GetAllLocksForUserAsync(targetFriendCode);
        var wardrobeState = await wardrobeDataService.GetPairWardrobeLayersAsync(
            targetProfileId.Value
        );

        return new SyncPairStateCommand(
            targetFriendCode,
            new UserPermissions(),
            wardrobeState,
            locks
        );
    }

    private async Task<object?> GetStateForPush(string friendCode, TwoWayPermissions perm)
    {
        var friendProfileId = await profilesService.GetProfileIdFromUidAsync(friendCode);
        if (friendProfileId == null)
            return null;

        var locks = await _locksHandler.GetAllLocksForUserAsync(friendCode);
        var wardrobe = await wardrobeDataService.GetPairWardrobeLayersAsync(friendProfileId.Value);
        var wardrobeWithLocks = PairWardrobeStateDto.PopulateLockIds(wardrobe, locks, logger);

        return new SyncPairStateCommand(
            friendCode,
            perm.PermissionsGrantedTo,
            wardrobeWithLocks,
            locks
        );
    }
}
