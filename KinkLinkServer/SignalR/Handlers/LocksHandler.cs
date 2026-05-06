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
    IPresenceService presenceService,
    WardrobeDataService wardrobeDataService,
    KinkLinkProfilesService profilesService,
    Configuration config,
    ILogger<LocksHandler> logger
)
{
    private readonly ProfilesSql _profilesSql = new(config.DatabaseConnectionString);

    public async Task<List<LockInfoDto>> GetAllLocksForUserAsync(string friendCode)
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

    public async Task<(
        ActionResult<LockInfoDto> Result,
        string LockeeFriendCode
    )> HandleAddLockAsync(string senderFriendCode, LockInfoDto lockInfo)
    {
        logger.LogInformation(
            "[LocksHandler] AddLock: Sender={Sender}, Lockee={Lockee}, LockId={LockId}",
            senderFriendCode,
            lockInfo.LockeeID,
            lockInfo.LockID
        );

        var lockeeProfile = await _profilesSql.GetProfileByUidAsync(
            new(lockInfo.LockeeID.ToString())
        );
        if (lockeeProfile == null)
        {
            logger.LogWarning(
                "[LocksHandler] Lockee profile not found: {Lockee}",
                lockInfo.LockeeID
            );
            return (
                ActionResultBuilder.Fail<LockInfoDto>(ActionResultEc.TargetNotFriends),
                string.Empty
            );
        }

        var lockeeFriendCode = lockeeProfile.Value.Uid;

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
            return (
                ActionResultBuilder.Fail<LockInfoDto>(ActionResultEc.TargetNotFriends),
                string.Empty
            );
        }

        var grantedBy = permissions.PermissionsGrantedBy;
        if (grantedBy == null)
        {
            logger.LogWarning(
                "[LocksHandler] Lockee {Lockee} has not granted permissions to {Sender}",
                lockeeFriendCode,
                senderFriendCode
            );
            return (
                ActionResultBuilder.Fail<LockInfoDto>(
                    ActionResultEc.TargetHasNotGrantedSenderPermissions
                ),
                string.Empty
            );
        }

        var lockType = GetLockType(lockInfo.LockID);
        var requiredPerm = GetRequiredPermissionForLock(lockType);
        if (requiredPerm != InteractionPerms.None && !grantedBy.Perms.HasFlag(requiredPerm))
        {
            logger.LogWarning(
                "[LocksHandler] Sender {Sender} lacks required permission {Perm} for lock type {Type}",
                senderFriendCode,
                requiredPerm,
                lockType
            );
            return (
                ActionResultBuilder.Fail<LockInfoDto>(
                    ActionResultEc.TargetHasNotGrantedSenderPermissions
                ),
                string.Empty
            );
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
                return (
                    ActionResultBuilder.Fail<LockInfoDto>(ActionResultEc.LockInsufficientPriority),
                    string.Empty
                );
            }

            logger.LogInformation(
                "[LocksHandler] Overwriting existing lock {LockId} with higher priority",
                lockInfo.LockID
            );
        }

        var senderProfile = await _profilesSql.GetProfileByUidAsync(new(senderFriendCode));
        if (senderProfile == null)
        {
            logger.LogError("[LocksHandler] Sender profile not found: {Sender}", senderFriendCode);
            return (ActionResultBuilder.Fail<LockInfoDto>(ActionResultEc.Unknown), string.Empty);
        }

        var lockToStore = new LockInfoDto
        {
            LockID = lockInfo.LockID,
            LockeeID = lockInfo.LockeeID,
            LockerID = senderProfile.Value.Id,
            LockPriority = permissions.PermissionsGrantedTo.Priority,
            CanSelfUnlock = lockInfo.CanSelfUnlock,
            Expires = lockInfo.Expires,
            Password = lockInfo.Password,
        };

        var result = await lockService.AddOrUpdateLockAsync(lockToStore);
        if (result == null)
        {
            logger.LogError("[LocksHandler] Failed to store lock {LockId}", lockInfo.LockID);
            return (ActionResultBuilder.Fail<LockInfoDto>(ActionResultEc.Unknown), string.Empty);
        }

        logger.LogInformation(
            "[LocksHandler] Lock {LockId} added/updated successfully",
            lockInfo.LockID
        );

        return (ActionResultBuilder.Ok(result.Value), lockeeFriendCode);
    }

    public async Task<(
        ActionResult<bool> Result,
        string LockeeUid,
        string LockerFriendCode
    )> HandleRemoveLockAsync(
        string senderFriendCode,
        string lockId,
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
            (int)permissions.PermissionsGrantedTo.Priority,
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
        string lockId
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
            senderProfile.Value.Id,
            existingLock.Value,
            permissions.PermissionsGrantedTo.Priority
        );

        if (!canUnlock)
        {
            return ActionResultBuilder.Fail<bool>(ActionResultEc.LockInsufficientPriority);
        }

        return ActionResultBuilder.Ok(true);
    }

    private static string GetLockType(string lockId)
    {
        var parts = lockId.Split('-', 2);
        return parts.Length >= 1 ? parts[0] : lockId;
    }

    private static InteractionPerms GetRequiredPermissionForLock(string lockType)
    {
        return lockType.ToLowerInvariant() switch
        {
            "wardrobe" => InteractionPerms.CanLockWardrobe,
            "gag" => InteractionPerms.CanLockGag,
            "garbler" => InteractionPerms.CanLockGarbler,
            "garblerchannels" => InteractionPerms.CanLockGarblerChannels,
            "moodles" => InteractionPerms.CanLockMoodles,
            _ => InteractionPerms.None,
        };
    }
}
