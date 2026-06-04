using KinkLinkCommon.Database;
using KinkLinkCommon.Domain;
using KinkLinkCommon.Domain.Enums;
using KinkLinkCommon.Domain.Network;
using KinkLinkServer;
using KinkLinkServer.Domain;
using KinkLinkServer.Domain.Interfaces;
using KinkLinkServer.Services;
using Microsoft.AspNetCore.SignalR;

namespace KinkLinkServer.SignalR.Handlers;

public class LocksHandler(
    LockService lockService,
    PermissionsService permissionsService,
    Configuration config,
    ILogger<LocksHandler> logger
)
{
    private readonly ProfilesSql _profilesSql = new(config.DatabaseConnectionString);

    public virtual async Task<List<LockInfoDto>> GetAllLocksForUserAsync(string friendCode)
    {
        logger.LogDebug("GetAllLocksForUserAsync called for {FriendCode}", friendCode);
        var locks = await lockService.GetAllLocksForUserAsync(friendCode);
        logger.LogDebug("Returning {Count} locks for {FriendCode}", locks.Count, friendCode);
        return locks;
    }

    public async Task<List<LockInfoDto>> GetLocksForPairAsync(
        string friendCode,
        string pairFriendCode
    )
    {
        logger.LogDebug(
            "GetLocksForPairAsync called for {FriendCode} and {PairFriendCode}",
            friendCode,
            pairFriendCode
        );
        var locks = await lockService.GetLocksForPairAsync(friendCode, pairFriendCode);
        logger.LogDebug(
            "Returning {Count} locks for pair {FriendCode} <-> {PairFriendCode}",
            locks.Count,
            friendCode,
            pairFriendCode
        );
        return locks;
    }

    public async Task<ActionResult<Unit>> HandleAddLockAsync(
        string senderFriendCode,
        LockInfoDto lockInfo
    )
    {
        logger.LogInformation(
            "[LocksHandler] AddLock: Sender={Sender}, Lockee={Lockee}, LockId={LockId}",
            senderFriendCode,
            lockInfo.LockeeID,
            lockInfo.LockID
        );

        var lockeeProfile = await _profilesSql.GetProfileByUidAsync(new(lockInfo.LockeeID));
        if (lockeeProfile == null)
        {
            logger.LogWarning(
                "[LocksHandler] Lockee profile not found: {Lockee}",
                lockInfo.LockeeID
            );
            return ActionResultBuilder.Fail(ActionResultEc.TargetNotFriends);
        }

        var lockeeFriendCode = lockeeProfile.Value.Uid;
        bool isSelfLock = senderFriendCode == lockeeFriendCode;

        RelationshipPriority lockPriority;
        if (!isSelfLock)
        {
            var permissions = await permissionsService.GetPermissions(
                senderFriendCode,
                lockeeFriendCode
            );
            if (permissions == null)
            {
                logger.LogWarning(
                    "[LocksHandler] No permissions between {Sender} and {Lockee}",
                    senderFriendCode,
                    lockeeFriendCode
                );
                return ActionResultBuilder.Fail(ActionResultEc.TargetNotFriends);
            }

            var grantedBy = permissions.PermissionsGrantedBy;
            if (grantedBy == null)
            {
                logger.LogWarning(
                    "[LocksHandler] Lockee {Lockee} has not granted permissions to {Sender}",
                    lockeeFriendCode,
                    senderFriendCode
                );
                return ActionResultBuilder.Fail(ActionResultEc.TargetHasNotGrantedSenderPermissions);
            }

            var requiredPerm = GetRequiredPermissionForLock(lockInfo.LockID);
            if (requiredPerm != InteractionPerms.None && !grantedBy.Perms.HasFlag(requiredPerm))
            {
                logger.LogWarning(
                    "[LocksHandler] Sender {Sender} lacks required permission {Perm} for lock type {Type}",
                    senderFriendCode,
                    requiredPerm,
                    lockInfo.LockID
                );
                return ActionResultBuilder.Fail(ActionResultEc.TargetHasNotGrantedSenderPermissions);
            }

            var existingLock = await lockService.GetLockAsync(lockInfo.LockID, lockeeFriendCode);
            if (existingLock != null)
            {
                var senderPriority = permissions.PermissionsGrantedTo.Priority;
                if (senderPriority <= existingLock.Value.LockPriority)
                {
                    logger.LogWarning(
                        "[LocksHandler] Insufficient priority to override lock. Sender={Priority}, Existing={Priority}",
                        senderPriority,
                        existingLock.Value.LockPriority
                    );
                    return ActionResultBuilder.Fail(ActionResultEc.LockInsufficientPriority);
                }

                logger.LogInformation(
                    "[LocksHandler] Overwriting existing lock {LockId} with higher priority",
                    lockInfo.LockID
                );
            }

            lockPriority = permissions.PermissionsGrantedTo.Priority;
        }
        else
        {
            logger.LogInformation(
                "[LocksHandler] Self-lock: Sender is same as Lockee, bypassing permission check"
            );
            // Self-locks use Devotional priority so they can always be overridden by pairs
            lockPriority = RelationshipPriority.Devotional;
        }

        var lockToStore = new LockInfoDto
        {
            LockID = lockInfo.LockID,
            LockeeID = lockeeFriendCode,
            LockerID = senderFriendCode,
            LockPriority = lockPriority,
            CanSelfUnlock = lockInfo.CanSelfUnlock,
            Expires = lockInfo.Expires,
            Password = lockInfo.Password,
        };

        var result = await lockService.AddOrUpdateLockAsync(lockToStore);
        if (result == false)
        {
            logger.LogError("[LocksHandler] Failed to store lock {LockId}", lockInfo.LockID);
            return ActionResultBuilder.Fail(ActionResultEc.Unknown);
        }

        logger.LogInformation("[LocksHandler] Lock {LockId} added/updated successfully");

        return ActionResultBuilder.Ok();
    }

    public async Task<(
        ActionResult<bool> Result,
        string LockeeUid,
        string LockerFriendCode
    )> HandleRemoveLockAsync(
        string senderFriendCode,
        LockKind lockId,
        string lockeeUid,
        string? password
    )
    {
        logger.LogInformation(
            "[LocksHandler] RemoveLock: Sender={Sender}, Lockee={Lockee}, LockId={LockId}",
            senderFriendCode,
            lockeeUid,
            lockId
        );

        bool isSelfLock = senderFriendCode == lockeeUid;

        RelationshipPriority userPriority;
        if (!isSelfLock)
        {
            var permissions = await permissionsService.GetPermissions(senderFriendCode, lockeeUid);
            if (permissions == null)
            {
                logger.LogWarning(
                    "[LocksHandler] No permissions between {Sender} and {Lockee}",
                    senderFriendCode,
                    lockeeUid
                );
                return (
                    ActionResultBuilder.Fail<bool>(ActionResultEc.TargetNotFriends),
                    string.Empty,
                    string.Empty
                );
            }

            var grantedBy = permissions.PermissionsGrantedBy;
            if (grantedBy == null)
            {
                logger.LogWarning(
                    "[LocksHandler] Lockee {Lockee} has not granted permissions to {Sender}",
                    lockeeUid,
                    senderFriendCode
                );
                return (
                    ActionResultBuilder.Fail<bool>(ActionResultEc.TargetHasNotGrantedSenderPermissions),
                    string.Empty,
                    string.Empty
                );
            }

            userPriority = permissions.PermissionsGrantedTo.Priority;
        }
        else
        {
            logger.LogInformation(
                "[LocksHandler] Self-unlock: Sender is same as Lockee"
            );
            userPriority = RelationshipPriority.Devotional;
        }

        var existingLock = await lockService.GetLockAsync(lockId, lockeeUid);
        if (existingLock == null)
        {
            logger.LogWarning("[LocksHandler] Lock not found: {LockId}", lockId);
            return (
                ActionResultBuilder.Fail<bool>(ActionResultEc.LockNotFound),
                string.Empty,
                string.Empty
            );
        }

        var senderProfile = await _profilesSql.GetProfileByUidAsync(new(senderFriendCode));
        if (senderProfile == null)
        {
            logger.LogError("[LocksHandler] Sender profile not found: {Sender}", senderFriendCode);
            return (
                ActionResultBuilder.Fail<bool>(ActionResultEc.Unknown),
                string.Empty,
                string.Empty
            );
        }

        var lockeeProfile = await _profilesSql.GetProfileByUidAsync(new(lockeeUid));
        if (lockeeProfile is null)
        {
            logger.LogError("[LocksHandler] Lockee profile not found: {Lockee}", lockeeUid);
            return (
                ActionResultBuilder.Fail<bool>(ActionResultEc.TargetNotFriends),
                string.Empty,
                string.Empty
            );
        }

        var canUnlock = await lockService.CanUnlockAsync(
            password,
            senderProfile.Value.Id,
            (int)userPriority,
            lockId,
            lockeeProfile.Value.Id
        );

        if (!canUnlock)
        {
            logger.LogWarning(
                "[LocksHandler] Sender {Sender} cannot unlock lock {LockId}",
                senderFriendCode,
                lockId
            );
            return (
                ActionResultBuilder.Fail<bool>(ActionResultEc.LockInsufficientPriority),
                string.Empty,
                string.Empty
            );
        }

        var result = await lockService.RemoveLockAsync(lockId, lockeeProfile.Value.Id);
        if (!result)
        {
            logger.LogError("[LocksHandler] Failed to remove lock {LockId}", lockId);
            return (
                ActionResultBuilder.Fail<bool>(ActionResultEc.Unknown),
                string.Empty,
                string.Empty
            );
        }

        logger.LogInformation("[LocksHandler] Lock {LockId} removed successfully", lockId);

        return (ActionResultBuilder.Ok(true), lockeeUid, senderFriendCode);
    }

    public async Task<ActionResult<bool>> CheckCanModifySlotAsync(
        string senderFriendCode,
        string lockeeFriendCode,
        LockKind lockId
    )
    {
        var existingLock = await lockService.GetLockAsync(lockId, lockeeFriendCode);
        if (existingLock == null)
        {
            return ActionResultBuilder.Ok(true);
        }

        var permissions = await permissionsService.GetPermissions(
            senderFriendCode,
            lockeeFriendCode
        );
        if (permissions == null)
        {
            return ActionResultBuilder.Fail<bool>(ActionResultEc.TargetNotFriends);
        }

        var grantedBy = permissions.PermissionsGrantedBy;
        if (grantedBy == null)
        {
            return ActionResultBuilder.Fail<bool>(
                ActionResultEc.TargetHasNotGrantedSenderPermissions
            );
        }

        var senderProfile = await _profilesSql.GetProfileByUidAsync(new(senderFriendCode));
        if (senderProfile == null)
        {
            return ActionResultBuilder.Fail<bool>(ActionResultEc.Unknown);
        }

        var canUnlock = Locks.CanUnlock(
            senderFriendCode,
            existingLock.Value,
            permissions.PermissionsGrantedTo.Priority
        );

        if (!canUnlock)
        {
            return ActionResultBuilder.Fail<bool>(ActionResultEc.LockInsufficientPriority);
        }

        return ActionResultBuilder.Ok(true);
    }

    private static InteractionPerms GetRequiredPermissionForLock(LockKind lockKind)
    {
        // Based on the LockKind prefix, determine the required permission
        var name = lockKind.ToString();
        if (name.StartsWith("Wardrobe"))
            return InteractionPerms.CanLockWardrobe;
        if (name.StartsWith("Gag"))
            return InteractionPerms.CanLockGag;
        if (name.StartsWith("Garbler"))
            return InteractionPerms.CanLockGarbler;
        if (name.StartsWith("Moodles"))
            return InteractionPerms.CanLockMoodles;
        return InteractionPerms.None;
    }
}
