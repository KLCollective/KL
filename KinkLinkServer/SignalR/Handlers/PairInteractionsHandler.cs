using KinkLinkCommon.Domain;
using KinkLinkCommon.Domain.Enums;
using KinkLinkCommon.Domain.Enums.Permissions;
using KinkLinkCommon.Domain.Network;
using KinkLinkCommon.Domain.Network.PairInteractions;
using KinkLinkCommon.Domain.Wardrobe;
using KinkLinkServer.Services;

namespace KinkLinkServer.SignalR.Handlers;

public class PairInteractionsHandler(
    PermissionsService permissionsService,
    WardrobeDataService wardrobeDataService,
    IActiveWardrobeStateService activeWardrobeStateService,
    KinkLinkProfilesService profilesService,
    LocksHandler locksHandler,
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
        var wardrobe = await activeWardrobeStateService.GetPairWardrobeStateAsync(
            targetProfileId.Value
        );
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
        var wardrobeWithLocks = PairWardrobeStateDto.PopulateLockIds(wardrobe, locks);

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

    private static string SlotNameFromLayer(WardrobeLayer layer) =>
        layer switch
        {
            WardrobeLayer.Outfit => "Outfit",
            WardrobeLayer.Head => "Head",
            WardrobeLayer.Chest => "Body",
            WardrobeLayer.Hands => "Hands",
            WardrobeLayer.Legs => "Legs",
            WardrobeLayer.Feet => "Feet",
            WardrobeLayer.Ears => "Ears",
            WardrobeLayer.Neck => "Neck",
            WardrobeLayer.Wrists => "Wrists",
            WardrobeLayer.RFinger => "RFinger",
            WardrobeLayer.LFinger => "LFinger",
            WardrobeLayer.Mods => "Mods",
            _ => layer.ToString(),
        };

    private async Task<ActionResult<Unit>> HandleUnlockAsync(
        string senderFriendCode,
        string targetFriendCode,
        LockInfoDto lockInfo
    )
    {
        logger.LogInformation(
            "[PairInteractionsHandler] HandleUnlockAsync: Sender={Sender}, Target={Target}",
            senderFriendCode,
            targetFriendCode
        );

        logger.LogInformation(
            "[PairInteractionsHandler] Attempting to unlock: LockId={LockId}",
            lockInfo.LockID
        );

        var removeResult = await locksHandler.HandleRemoveLockAsync(
            senderFriendCode,
            lockInfo.LockID,
            targetFriendCode,
            // TODO add passwords to the payload and plumb it in.
            null
        );

        if (removeResult.Result.Result == ActionResultEc.Success)
        {
            logger.LogInformation(
                "[PairInteractionsHandler] Successfully unlocked {LockId}",
                lockInfo.LockID
            );
        }
        else
        {
            logger.LogWarning(
                "[PairInteractionsHandler] Failed to unlock {LockId}: {Error}",
                lockInfo.LockID,
                removeResult.Result.Result
            );
        }
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

        var wardrobeState = await activeWardrobeStateService.GetPairWardrobeStateAsync(
            targetProfileId.Value
        );

        return new ActionResult<QueryPairWardrobeStateResponse>(
            ActionResultEc.Success,
            new QueryPairWardrobeStateResponse(
                request.TargetFriendCode,
                hasWardrobe,
                hasWardrobe ? wardrobeState : null
            )
        );
    }

    public async Task<ActionResult<QueryPairWardrobeResponse>> QueryPairWardrobeAsync(
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
            .Select(item => new LightWardrobeItemDto(
                item.Id,
                item.Name,
                item.Description,
                (WardrobeLayer)item.Layer,
                item.Priority,
                null
            ))
            .ToList();

        return ActionResultBuilder.Ok<QueryPairWardrobeResponse>(
            new(request.TargetFriendCode, filteredItems)
        );
    }

    public async Task<ActionResult<ActionResultEc>> UpdateWardrobeStateAsync(
        string senderFriendCode,
        string targetFriendCode,
        WardrobeLayer layer,
        Guid? id
    )
    {
        if (string.IsNullOrWhiteSpace(targetFriendCode))
        {
            return ActionResultBuilder.Fail<ActionResultEc>(ActionResultEc.ClientBadData);
        }

        if (senderFriendCode == targetFriendCode)
        {
            return ActionResultBuilder.Fail<ActionResultEc>(ActionResultEc.ClientBadData);
        }

        var permissions = await permissionsService.GetPermissions(senderFriendCode, targetFriendCode);
        if (permissions == null)
        {
            return ActionResultBuilder.Fail<ActionResultEc>(ActionResultEc.TargetNotFriends);
        }

        var grantedBy = permissions.PermissionsGrantedBy;
        if (grantedBy == null || !grantedBy.Perms.HasFlag(InteractionPerms.CanApplyWardrobe))
        {
            return ActionResultBuilder.Fail<ActionResultEc>(
                ActionResultEc.TargetHasNotGrantedSenderPermissions
            );
        }

        var targetProfileId = await profilesService.GetProfileIdFromUidAsync(targetFriendCode);
        if (targetProfileId == null)
        {
            return ActionResultBuilder.Fail<ActionResultEc>(ActionResultEc.TargetNotFriends);
        }

        // If id specified, ensure item exists and priority allowed
        if (id is { } wardrobeId)
        {
            var item = await wardrobeDataService.GetWardrobeItemByGuid(
                targetProfileId.Value,
                wardrobeId
            );
            if (item == null)
            {
                logger.LogWarning(
                    "[PairInteractionsHandler] Wardrobe item not found: {WardrobeId} for {Target}",
                    wardrobeId,
                    targetFriendCode
                );
                return ActionResultBuilder.Fail<ActionResultEc>(ActionResultEc.ClientBadData);
            }

            if ((int)item.Priority > (int)grantedBy.Priority)
            {
                logger.LogWarning(
                    "[PairInteractionsHandler] Sender {Sender} insufficient priority to apply wardrobe {WardrobeId} (itemPriority={ItemPriority} grantedPriority={GrantedPriority})",
                    senderFriendCode,
                    wardrobeId,
                    item.Priority,
                    grantedBy.Priority
                );
                return ActionResultBuilder.Fail<ActionResultEc>(
                    ActionResultEc.LockInsufficientPriority
                );
            }

            // Ensure the item's layer matches the target layer being applied to
            if (item.Layer != layer)
            {
                logger.LogWarning(
                    "[PairInteractionsHandler] Item {WardrobeId} layer mismatch: itemLayer={ItemLayer} targetLayer={TargetLayer}",
                    wardrobeId,
                    item.Layer,
                    layer
                );
                return ActionResultBuilder.Fail<ActionResultEc>(ActionResultEc.ClientBadData);
            }
        }

        var updateResult = await activeWardrobeStateService.UpdateWardrobeStateAsync(
            targetProfileId.Value,
            layer,
            id
        );

        if (!updateResult)
        {
            return ActionResultBuilder.Fail<ActionResultEc>(ActionResultEc.Unknown);
        }

        logger.LogInformation(
            "[PairInteractionsHandler] Updated wardrobe state: Target={Target} Layer={Layer} Id={Id}",
            targetFriendCode,
            layer,
            id
        );

        return ActionResultBuilder.Ok(ActionResultEc.Success);
    }
}
