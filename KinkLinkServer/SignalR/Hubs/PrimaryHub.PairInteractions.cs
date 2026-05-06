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
            logger.LogTrace("[SignalR] QueryPairState: {FriendCode} -> {Target}", FriendCode, request.TargetFriendCode);
            if (isValidPair<QueryPairStateResponse>(FriendCode, request.TargetFriendCode) is { } result)
            {
                return result;
            }
            return await _pairInteractionsHandler.QueryPairState(FriendCode, request);
        }
        finally
        {
            stopwatch.Stop();
            metricsService.IncrementSignalRMessage("QueryPairState", true);
            metricsService.RecordSignalRMessageDuration("QueryPairState", stopwatch.ElapsedMilliseconds);
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
            logger.LogTrace("[SignalR] QueryWardrobeState: {FriendCode} -> {Target}", FriendCode, request.TargetFriendCode);
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
            metricsService.RecordSignalRMessageDuration("QueryPairWardrobeState", stopwatch.ElapsedMilliseconds);
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
            logger.LogTrace("[SignalR] QueryWardrobe: {FriendCode} -> {Target}", FriendCode, request.TargetFriendCode);
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
            metricsService.RecordSignalRMessageDuration("QueryPairWardrobe", stopwatch.ElapsedMilliseconds);
        }
    }

    [HubMethodName(HubMethod.ApplyInteraction)]
    public async Task<ActionResult<Unit>> ApplyInteraction(ApplyInteractionCommand command)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            logger.LogInformation("[SignalR] ApplyInteraction: {FriendCode} -> {Target}, Action: {Action}",
                FriendCode, command.TargetFriendCode, command.Action);
            if (isValidPair<Unit>(FriendCode, command.TargetFriendCode) is { } pairResult)
            {
                return pairResult;
            }

            var interactionResult = await _pairInteractionsHandler.ApplyInteraction(FriendCode, command);
            var result = interactionResult.Result;
            var targetFriendCode = interactionResult.TargetFriendCode;
            var action = interactionResult.Action;

            if (result.Result == ActionResultEc.Success && !string.IsNullOrEmpty(targetFriendCode))
            {
                if (PairInteractionsHandler.IsLockModificationAction(action))
                {
                    await _notificationHandler.NotifyTargetOfStateChangeAsync(
                        targetFriendCode,
                        friendCode => GetStateForTarget(friendCode),
                        Clients
                    );
                    await _notificationHandler.PushStateToAllFriendsAsync(
                        targetFriendCode,
                        friendCode => permissionsService.GetAllPermissions(friendCode),
                        (friendCode, perm) => GetStateForPush(friendCode, perm),
                        Clients
                    );
                }
                else if (action == KinkLinkCommon.Domain.Enums.Permissions.PairAction.ApplyWardrobe)
                {
                    await _notificationHandler.NotifyTargetOfStateChangeAsync(
                        targetFriendCode,
                        friendCode => GetStateForTarget(friendCode),
                        Clients
                    );
                }
            }

            return result;
        }
        finally
        {
            stopwatch.Stop();
            metricsService.IncrementSignalRMessage("ApplyInteraction", true);
            metricsService.RecordSignalRMessageDuration("ApplyInteraction", stopwatch.ElapsedMilliseconds);
        }
    }

    private async Task<object?> GetStateForTarget(string targetFriendCode)
    {
        var targetProfileId = await profilesService.GetProfileIdFromUidAsync(targetFriendCode);
        if (targetProfileId == null)
            return null;

        var locks = await _locksHandler.GetAllLocksForUserAsync(targetFriendCode);
        var wardrobeState = await wardrobeDataService.GetPairWardrobeItemsAsync(targetProfileId.Value);

        return new QueryPairStateResponse(
            targetFriendCode,
            new UserPermissions(),
            wardrobeState,
            locks
        );
    }

    private async Task<object?> GetStateForPush(
        string friendCode,
        TwoWayPermissions perm
    )
    {
        var friendProfileId = await profilesService.GetProfileIdFromUidAsync(friendCode);
        if (friendProfileId == null)
            return null;

        var locks = await _locksHandler.GetAllLocksForUserAsync(friendCode);
        var wardrobe = await wardrobeDataService.GetPairWardrobeItemsAsync(friendProfileId.Value);
        var wardrobeWithLocks = PairWardrobeStateDto.PopulateLockIds(wardrobe, locks, logger);

        return new QueryPairStateResponse(friendCode, perm.PermissionsGrantedTo, wardrobeWithLocks, locks);
    }
}
