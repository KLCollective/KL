using KinkLinkCommon.Domain;
using KinkLinkCommon.Domain.CharacterState;
using KinkLinkCommon.Domain.Enums;
using KinkLinkCommon.Domain.Enums.Permissions;
using KinkLinkCommon.Domain.Network;
using KinkLinkCommon.Domain.Network.PairInteractions;
using KinkLinkCommon.Domain.Network.SyncPairState;
using KinkLinkCommon.Domain.Wardrobe;
using KinkLinkCommon.Util;
using KinkLinkServer.Domain;
using KinkLinkServer.Domain.Interfaces;
using KinkLinkServer.Managers;
using KinkLinkServer.Services;
using KinkLinkServer.SignalR.Handlers.Interactions;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace KinkLinkServer.SignalR.Handlers;

public class PairInteractionsHandler(
    PermissionsService permissionsService,
    WardrobeDataService wardrobeDataService,
    KinkLinkProfilesService profilesService,
    IPresenceService presenceService,
    IPairInteractionHandlerFactory handlerFactory,
    LocksHandler locksHandler,
    INotificationService notificationService,
    ILogger<PairInteractionsHandler> logger
)
{
    public async Task<ActionResult<QueryPairStateResponse>> QueryPairState(
        string senderFriendCode,
        QueryPairStateRequest request
    )
    {
        if (string.IsNullOrWhiteSpace(request.TargetFriendCode))
        {
            return ActionResultBuilder.Fail<QueryPairStateResponse>(ActionResultEc.ClientBadData);
        }

        if (senderFriendCode == request.TargetFriendCode)
        {
            return ActionResultBuilder.Fail<QueryPairStateResponse>(ActionResultEc.ClientBadData);
        }

        var permissions = await permissionsService.GetPermissions(
            senderFriendCode,
            request.TargetFriendCode
        );
        if (permissions == null)
        {
            return ActionResultBuilder.Fail<QueryPairStateResponse>(
                ActionResultEc.TargetNotFriends
            );
        }

        var grantedBy = permissions.PermissionsGrantedBy;
        if (grantedBy == null)
        {
            return ActionResultBuilder.Fail<QueryPairStateResponse>(
                ActionResultEc.TargetHasNotGrantedSenderPermissions
            );
        }

        var hasGag = grantedBy.Perms.HasFlag(InteractionPerms.CanApplyGag);
        var hasGarbler = grantedBy.Perms.HasFlag(InteractionPerms.CanEnableGarbler);
        var hasWardrobe = grantedBy.Perms.HasFlag(InteractionPerms.CanApplyWardrobe);
        // TODO: Reimplement when moodles is done
        // var hasMoodle = grantedBy.Perms.HasFlag(InteractionPerms.CanApplyOwnMoodles);
        var targetProfileId = await profilesService.GetProfileIdFromUidAsync(
            request.TargetFriendCode
        );
        if (!targetProfileId.HasValue)
        {
            return ActionResultBuilder.Fail<QueryPairStateResponse>(ActionResultEc.ClientBadData);
        }
        var wardrobe = await wardrobeDataService.GetPairWardrobeItemsAsync(targetProfileId.Value);
        var locks = await locksHandler.GetAllLocksForUserAsync(request.TargetFriendCode);
        logger.LogInformation(
            "[QueryPairState] Target={Target}, Locks count={LockCount}",
            request.TargetFriendCode,
            locks.Count
        );
        foreach (var l in locks)
        {
            logger.LogInformation(
                "[QueryPairState] Lock: LockID={LockId}, LockeeID={LockeeId}, LockerID={LockerId}",
                l.LockID,
                l.LockeeID,
                l.LockerID
            );
        }
        var wardrobeWithLocks = PairWardrobeStateDto.PopulateLockIds(wardrobe, locks, logger);

        return new ActionResult<QueryPairStateResponse>(
            ActionResultEc.Success,
            new QueryPairStateResponse(
                request.TargetFriendCode,
                permissions.PermissionsGrantedTo,
                wardrobeWithLocks,
                locks
            )
        );
    }

    private static readonly string[] WardrobeSlots =
    [
        "Head",
        "Body",
        "Hands",
        "Legs",
        "Feet",
        "Ears",
        "Neck",
        "Wrists",
        "RFinger",
        "LFinger",
    ];

    public async Task<ActionResult<Unit>> ApplyInteraction(
        string senderFriendCode,
        ApplyInteractionCommand command,
        IHubCallerClients clients
    )
    {
        logger.LogInformation(
            "[PairInteractionsHandler] ApplyInteraction: Sender={Sender}, Target={Target}, Action={Action}",
            senderFriendCode,
            command.TargetFriendCode,
            command.Action
        );

        if (command.Action == PairAction.ApplyWardrobe && command.Payload?.WardrobeItems != null)
        {
            logger.LogInformation(
                "[PairInteractionsHandler] ApplyWardrobe: {Count} items in payload",
                command.Payload.WardrobeItems.Count
            );
        }

        if (presenceService.TryGet(senderFriendCode) is not { } sender)
        {
            logger.LogWarning(
                "[PairInteractionsHandler] Sender {Sender} not online",
                senderFriendCode
            );
            return ActionResultBuilder.Fail<Unit>(ActionResultEc.TargetOffline);
        }

        var permissions = await permissionsService.GetPermissions(
            senderFriendCode,
            command.TargetFriendCode
        );
        if (permissions == null)
        {
            logger.LogWarning(
                "[PairInteractionsHandler] No permissions between {Sender} and {Target}",
                senderFriendCode,
                command.TargetFriendCode
            );
            return ActionResultBuilder.Fail<Unit>(ActionResultEc.TargetNotFriends);
        }

        var grantedBy = permissions.PermissionsGrantedBy;
        if (grantedBy == null)
        {
            logger.LogWarning(
                "[PairInteractionsHandler] Target {Target} has not granted permissions to {Sender}",
                command.TargetFriendCode,
                senderFriendCode
            );
            return ActionResultBuilder.Fail<Unit>(
                ActionResultEc.TargetHasNotGrantedSenderPermissions
            );
        }

        logger.LogInformation(
            "[PairInteractionsHandler] Permissions check: GrantedBy={Perms}, Required={Required}",
            grantedBy.Perms,
            command.Action.ToInteractionPerm()
        );

        if (!grantedBy.Perms.HasFlag(command.Action.ToInteractionPerm()))
        {
            logger.LogWarning(
                "[PairInteractionsHandler] Action {Action} not permitted for {Sender}. Has={HasPerms}",
                command.Action,
                senderFriendCode,
                grantedBy.Perms
            );
            return ActionResultBuilder.Fail<Unit>(
                ActionResultEc.TargetHasNotGrantedSenderPermissions
            );
        }

        var targetFriendCode = command.TargetFriendCode;
        var context = new InteractionContext(senderFriendCode, targetFriendCode, permissions);

        ActionResult<Unit> result;

        if (command.Action == PairAction.UnlockWardrobe)
        {
            result = await HandleUnlockAsync(context, command.Payload, clients);
        }
        else
        {
            var handler = handlerFactory.GetHandler(command.Action);
            if (handler == null)
            {
                logger.LogWarning(
                    "[PairInteractionsHandler] No handler found for action {Action}",
                    command.Action
                );
                return ActionResultBuilder.Fail<Unit>(ActionResultEc.Unknown);
            }

            result = await handler.HandleAsync(context, command.Payload);

            if (result.Result != ActionResultEc.Success)
            {
                logger.LogWarning(
                    "[PairInteractionsHandler] Handler returned error {Error}",
                    result.Result
                );
                return result;
            }

            logger.LogInformation("[PairInteractionsHandler] Handler completed successfully");
        }

        if (IsLockModificationAction(command.Action))
        {
            // await notificationService.NotifyLockeeOfLockUpdateAsync(
            //     senderFriendCode,
            //     friendCode => locksHandler.GetLocksForPairAsync(friendCode, targetFriendCode),
            //     clients
            // );
            // await notificationService.NotifyLockeeOfLockUpdateAsync(
            //     targetFriendCode,
            //     friendCode => locksHandler.GetLocksForPairAsync(friendCode, senderFriendCode),
            //     clients
            // );
            await NotifyTargetOfStateChangeAsync(targetFriendCode, clients);
            await PushStateToFriendsAsync(targetFriendCode, clients);
        }
        else if (command.Action == PairAction.ApplyWardrobe)
        {
            await NotifyTargetOfStateChangeAsync(targetFriendCode, clients);
        }

        return ActionResultBuilder.Ok(Unit.Empty);
    }

    private async Task<ActionResult<Unit>> HandleUnlockAsync(
        InteractionContext context,
        InteractionPayload? payload,
        IHubCallerClients clients
    )
    {
        logger.LogInformation(
            "[PairInteractionsHandler] HandleUnlockAsync: Sender={Sender}, Target={Target}, HasPayload={HasPayload}",
            context.SenderFriendCode,
            context.TargetFriendCode,
            payload != null
        );

        var slotToLockIdMap = new Dictionary<string, string>
        {
            ["set"] = "wardrobe-baseset",
            ["Head"] = "wardrobe-head",
            ["Body"] = "wardrobe-body",
            ["Hands"] = "wardrobe-hands",
            ["Legs"] = "wardrobe-legs",
            ["Feet"] = "wardrobe-feet",
            ["Ears"] = "wardrobe-ears",
            ["Neck"] = "wardrobe-neck",
            ["Wrists"] = "wardrobe-wrists",
            ["RFinger"] = "wardrobe-rfinger",
            ["LFinger"] = "wardrobe-lfinger",
        };

        var allLockIds = slotToLockIdMap.Values.ToList();

        List<string> lockIdsToProcess;
        if (payload?.WardrobeItems != null && payload.WardrobeItems.Count > 0)
        {
            logger.LogInformation(
                "[PairInteractionsHandler] Unlock payload has {Count} items",
                payload.WardrobeItems.Count
            );

            lockIdsToProcess = payload
                .WardrobeItems.Where(item => item.Type == "set" || item.Type == "item")
                .Select(item =>
                {
                    var slotKey = item.Type == "set" ? "set" : item.Slot.ToString();
                    logger.LogDebug(
                        "[PairInteractionsHandler] Processing item: Type={Type}, Slot={Slot}, SlotKey={SlotKey}",
                        item.Type,
                        item.Slot,
                        slotKey
                    );
                    return slotToLockIdMap.TryGetValue(slotKey, out var lockId) ? lockId : null;
                })
                .Where(lockId => lockId != null)
                .Cast<string>()
                .ToList();

            logger.LogInformation(
                "[PairInteractionsHandler] Mapped {Count} lock IDs from payload",
                lockIdsToProcess.Count
            );

            if (lockIdsToProcess.Count == 0)
            {
                logger.LogInformation(
                    "[PairInteractionsHandler] No lock IDs from payload, processing all"
                );
                lockIdsToProcess = allLockIds;
            }
        }
        else
        {
            logger.LogInformation("[PairInteractionsHandler] No payload, processing all lock IDs");
            lockIdsToProcess = allLockIds;
        }

        var successCount = 0;
        var failCount = 0;

        foreach (var lockId in lockIdsToProcess)
        {
            logger.LogInformation(
                "[PairInteractionsHandler] Attempting to unlock: LockId={LockId}",
                lockId
            );

            var removeResult = await locksHandler.HandleRemoveLockAsync(
                context.SenderFriendCode,
                lockId,
                context.TargetFriendCode,
                // TODO add passwords to the payload and plumb it in.
                null,
                clients
            );

            if (removeResult.Result == ActionResultEc.Success)
            {
                successCount++;
                logger.LogInformation(
                    "[PairInteractionsHandler] Successfully unlocked {LockId}",
                    lockId
                );
            }
            else
            {
                failCount++;
                logger.LogWarning(
                    "[PairInteractionsHandler] Failed to unlock {LockId}: {Error}",
                    lockId,
                    removeResult.Result
                );
            }
        }

        logger.LogInformation(
            "[PairInteractionsHandler] Unlock completed: {Success} succeeded, {Fail} failed for {Target}",
            successCount,
            failCount,
            context.TargetFriendCode
        );

        return ActionResultBuilder.Ok(Unit.Empty);
    }

    public async Task<ActionResult<QueryPairWardrobeStateResponse>> QueryWardrobeStateAsync(
        string senderFriendCode,
        QueryPairWardrobeStateRequest request
    )
    {
        if (string.IsNullOrWhiteSpace(request.TargetFriendCode))
        {
            return ActionResultBuilder.Fail<QueryPairWardrobeStateResponse>(
                ActionResultEc.ClientBadData
            );
        }

        if (senderFriendCode == request.TargetFriendCode)
        {
            return ActionResultBuilder.Fail<QueryPairWardrobeStateResponse>(
                ActionResultEc.ClientBadData
            );
        }

        var permissions = await permissionsService.GetPermissions(
            senderFriendCode,
            request.TargetFriendCode
        );
        if (permissions == null)
        {
            return ActionResultBuilder.Fail<QueryPairWardrobeStateResponse>(
                ActionResultEc.TargetNotFriends
            );
        }

        var grantedBy = permissions.PermissionsGrantedBy;
        if (grantedBy == null)
        {
            return ActionResultBuilder.Fail<QueryPairWardrobeStateResponse>(
                ActionResultEc.TargetHasNotGrantedSenderPermissions
            );
        }

        var hasWardrobe = grantedBy.Perms.HasFlag(InteractionPerms.CanApplyWardrobe);

        var targetProfileId = await profilesService.GetProfileIdFromUidAsync(
            request.TargetFriendCode
        );
        if (targetProfileId == null)
        {
            return ActionResultBuilder.Fail<QueryPairWardrobeStateResponse>(
                ActionResultEc.TargetNotFriends
            );
        }

        var wardrobeState = await wardrobeDataService.GetWardrobeStateAsync(targetProfileId.Value);

        return new ActionResult<QueryPairWardrobeStateResponse>(
            ActionResultEc.Success,
            new QueryPairWardrobeStateResponse(
                request.TargetFriendCode,
                hasWardrobe,
                hasWardrobe ? wardrobeState : null
            )
        );
    }

    public async Task<ActionResult<QueryPairWardrobeResponse>> QueryWardrobeAsync(
        string senderFriendCode,
        QueryPairWardrobeRequest request
    )
    {
        if (string.IsNullOrWhiteSpace(request.TargetFriendCode))
        {
            return ActionResultBuilder.Fail<QueryPairWardrobeResponse>(
                ActionResultEc.ClientBadData
            );
        }

        if (senderFriendCode == request.TargetFriendCode)
        {
            return ActionResultBuilder.Fail<QueryPairWardrobeResponse>(
                ActionResultEc.ClientBadData
            );
        }

        var permissions = await permissionsService.GetPermissions(
            senderFriendCode,
            request.TargetFriendCode
        );
        if (permissions == null)
        {
            return ActionResultBuilder.Fail<QueryPairWardrobeResponse>(
                ActionResultEc.TargetNotFriends
            );
        }

        var grantedBy = permissions.PermissionsGrantedBy;
        if (grantedBy == null || !grantedBy.Perms.HasFlag(InteractionPerms.CanApplyWardrobe))
        {
            return ActionResultBuilder.Fail<QueryPairWardrobeResponse>(
                ActionResultEc.TargetHasNotGrantedSenderPermissions
            );
        }

        var targetProfileId = await profilesService.GetProfileIdFromUidAsync(
            request.TargetFriendCode
        );
        if (targetProfileId == null)
        {
            return ActionResultBuilder.Fail<QueryPairWardrobeResponse>(
                ActionResultEc.TargetNotFriends
            );
        }

        var allItems = await wardrobeDataService.GetAllWardrobeItemsAsync(targetProfileId.Value);

        var filteredItems = allItems
            .Where(item => item.Priority <= grantedBy.Priority)
            .Select(item => new PairWardrobeItemDto(
                item.Id,
                item.Name,
                item.Description,
                item.Slot,
                item.Priority,
                item.LockId
            ))
            .ToList();

        return ActionResultBuilder.Ok<QueryPairWardrobeResponse>(
            new(request.TargetFriendCode, filteredItems)
        );
    }

    private static bool IsLockModificationAction(PairAction action) =>
        action is PairAction.LockWardrobe or PairAction.UnlockWardrobe;

    private async Task NotifyTargetOfStateChangeAsync(
        string targetFriendCode,
        IHubCallerClients clients
    )
    {
        if (presenceService.TryGet(targetFriendCode) is not { } presence)
            return;

        try
        {
            var targetProfileId = await profilesService.GetProfileIdFromUidAsync(targetFriendCode);
            if (targetProfileId == null)
                return;

            var locks = await locksHandler.GetAllLocksForUserAsync(targetFriendCode);
            var wardrobeState = await wardrobeDataService.GetWardrobeStateAsync(
                targetProfileId.Value
            );

            await clients
                .Client(presence.ConnectionId)
                .SendAsync(
                    HubMethod.SyncPairState,
                    new { Locks = locks, WardrobeState = wardrobeState }
                );
        }
        catch (Exception e)
        {
            logger.LogWarning(
                "[PairInteractionsHandler] Failed to notify target {Target} of state change: {Error}",
                targetFriendCode,
                e.Message
            );
        }
    }

    private async Task PushStateToFriendsAsync(string friendCode, IHubCallerClients clients)
    {
        try
        {
            var allPermissions = await permissionsService.GetAllPermissions(friendCode);
            if (allPermissions.Count == 0)
                return;

            var friendProfileId = await profilesService.GetProfileIdFromUidAsync(friendCode);
            if (friendProfileId == null)
                return;

            var locks = await locksHandler.GetAllLocksForUserAsync(friendCode);
            var wardrobeState = await wardrobeDataService.GetPairWardrobeItemsAsync(
                friendProfileId.Value
            );

            var wardrobeWithLocks = PairWardrobeStateDto.PopulateLockIds(
                wardrobeState,
                locks,
                logger
            );

            foreach (var perm in allPermissions)
            {
                if (presenceService.TryGet(perm.TargetUID) is { } presence)
                {
                    await clients
                        .Client(presence.ConnectionId)
                        .SendAsync(
                            HubMethod.SyncPairState,
                            new QueryPairStateResponse(
                                friendCode,
                                perm.PermissionsGrantedTo,
                                wardrobeWithLocks,
                                locks
                            )
                        );
                }
            }

            logger.LogDebug(
                "[PairInteractionsHandler] Pushed {FriendCode} state to all friends",
                friendCode
            );
        }
        catch (Exception e)
        {
            logger.LogError(
                e,
                "[PairInteractionsHandler] Failed to push {FriendCode} state to friends",
                friendCode
            );
        }
    }
}
