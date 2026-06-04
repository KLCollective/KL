using KinkLinkCommon.Database;
using KinkLinkCommon.Domain;
using KinkLinkCommon.Domain.Enums;
using KinkLinkServer.Domain;
using Microsoft.Extensions.Logging;

namespace KinkLinkServer.Services;

public class LockService
{
    private readonly ILogger<LockService> _logger;
    private readonly LocksSql _locksSql;
    private readonly ProfilesSql _profilesSql;

    public LockService(Configuration config, ILogger<LockService> logger)
    {
        _logger = logger;
        _locksSql = new LocksSql(config.DatabaseConnectionString);
        _profilesSql = new ProfilesSql(config.DatabaseConnectionString);
    }

    public async Task<List<LockInfoDto>> GetAllLocksForUserAsync(string lockeeUid)
    {
        var profile = await _profilesSql.GetProfileByUidAsync(new(lockeeUid));
        if (profile is null)
        {
            return new List<LockInfoDto>();
        }

        var rows = await _locksSql.GetLocksForLockeeAsync(new(profile.Value.Id));

        var result = new List<LockInfoDto>();
        foreach (var row in rows)
        {
            var lockerProfile = await _profilesSql.GetProfileByIdAsync(new(row.LockerId));
            result.Add(new LockInfoDto
            {
                LockID = (LockKind)row.LockId,
                LockeeID = profile.Value.Uid,
                LockerID = lockerProfile?.Uid ?? row.LockerId.ToString(),
                LockPriority = (RelationshipPriority)row.LockPriority,
                CanSelfUnlock = row.CanSelfUnlock,
                Expires = row.Expires,
                Password = row.Password,
            });
        }

        return result;
    }

    public async Task<LockInfoDto?> GetLockAsync(LockKind lockId, string lockeeUid)
    {
        var profile = await _profilesSql.GetProfileByUidAsync(new(lockeeUid));
        if (profile is null)
        {
            return null;
        }

        var row = await _locksSql.GetLockByIdAsync(new((int)lockId, profile.Value.Id));
        if (row is null)
        {
            return null;
        }

        var lockerProfile = await _profilesSql.GetProfileByIdAsync(new(row.Value.LockerId));
        return new LockInfoDto
        {
            LockID = (LockKind)row.Value.LockId,
            LockeeID = profile.Value.Uid,
            LockerID = lockerProfile?.Uid ?? row.Value.LockerId.ToString(),
            LockPriority = (RelationshipPriority)row.Value.LockPriority,
            CanSelfUnlock = row.Value.CanSelfUnlock,
            Expires = row.Value.Expires,
            Password = row.Value.Password,
        };
    }

    public async Task<bool> AddOrUpdateLockAsync(LockInfoDto lockInfo)
    {
        var lockeeProfile = await _profilesSql.GetProfileByUidAsync(new(lockInfo.LockeeID));
        var lockerProfile = await _profilesSql.GetProfileByUidAsync(new(lockInfo.LockerID));

        if (lockeeProfile is null || lockerProfile is null)
        {
            _logger.LogError(
                "AddOrUpdateLockAsync: profile lookup failed for lockee={LockeeId} locker={LockerId}",
                lockInfo.LockeeID,
                lockInfo.LockerID
            );
            return false;
        }

        var row = await _locksSql.AddOrUpdateLockAsync(
            new(
                (int)lockInfo.LockID,
                lockeeProfile.Value.Id,
                lockerProfile.Value.Id,
                (int)lockInfo.LockPriority,
                lockInfo.CanSelfUnlock,
                lockInfo.Expires,
                lockInfo.Password
            )
        );

        if (row is null)
        {
            _logger.LogError(
                "AddOrUpdateLockAsync: failed to add/update lock for lockId: {LockId}",
                lockInfo.LockID
            );
            return false;
        }

        return true;
    }

    public async Task<bool> RemoveLockAsync(LockKind lockId, int lockeeId)
    {
        var result = await _locksSql.RemoveLockAsync(new((int)lockId, lockeeId));
        if (result is null)
        {
            return false;
        }

        return true;
    }

    public async Task<int> RemoveAllLocksForUserAsync(string uid)
    {
        var profile = await _profilesSql.GetProfileByUidAsync(new(uid));
        if (profile is null)
        {
            return 0;
        }

        var result = await _locksSql.RemoveAllLocksForUserAsync(new(profile.Value.Id));
        return result.Count;
    }

    public async Task<List<LockInfoDto>> GetLocksForPairAsync(
        string friendCodeUid,
        string pairFriendCodeUid
    )
    {
        _logger.LogDebug(
            "GetLocksForPairAsync called with friendCodeUid: {FriendCodeUid}, pairFriendCodeUid: {PairFriendCodeUid}",
            friendCodeUid,
            pairFriendCodeUid
        );

        var profile = await _profilesSql.GetProfileByUidAsync(new(friendCodeUid));
        if (profile is null)
        {
            _logger.LogWarning(
                "GetLocksForPairAsync: profile not found for uid: {FriendCodeUid}",
                friendCodeUid
            );
            return new List<LockInfoDto>();
        }

        var pairProfile = await _profilesSql.GetProfileByUidAsync(new(pairFriendCodeUid));
        if (pairProfile is null)
        {
            _logger.LogWarning(
                "GetLocksForPairAsync: pair profile not found for uid: {PairFriendCodeUid}",
                pairFriendCodeUid
            );
            return new List<LockInfoDto>();
        }

        var rows = await _locksSql.GetLocksForPairAsync(
            new(profile.Value.Id, pairProfile.Value.Id)
        );
        _logger.LogDebug(
            "GetLocksForPairAsync returned {Count} locks for pair {FriendCodeUid} <-> {PairFriendCodeUid}",
            rows.Count,
            friendCodeUid,
            pairFriendCodeUid
        );

        var result = new List<LockInfoDto>();
        foreach (var row in rows)
        {
            var lockeeProfile = await _profilesSql.GetProfileByIdAsync(new(row.LockeeId));
            var lockerProfile = await _profilesSql.GetProfileByIdAsync(new(row.LockerId));
            result.Add(new LockInfoDto
            {
                LockID = (LockKind)row.LockId,
                LockeeID = lockeeProfile?.Uid ?? row.LockeeId.ToString(),
                LockerID = lockerProfile?.Uid ?? row.LockerId.ToString(),
                LockPriority = (RelationshipPriority)row.LockPriority,
                CanSelfUnlock = row.CanSelfUnlock,
                Expires = row.Expires,
                Password = row.Password,
            });
        }

        return result;
    }

    public async Task<int> PurgeExpiredLocksAsync()
    {
        var result = await _locksSql.PurgeExpiredLocksAsync();
        return result.Count;
    }

    public async Task<bool> HasExpiredLocksAsync()
    {
        var result = await _locksSql.HasExpiredLocksAsync();
        return result?.HasExpired ?? false;
    }

    public async Task<bool> IsSlotLockedAsync(int profileId, LockKind lockKind)
    {
        try
        {
            if (await _locksSql.IsLockedAsync(new((int)lockKind, profileId)) is { } result)
            {
                return result.IsLocked;
            }
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error checking if slot is locked for profileId: {ProfileId}, lockKind: {LockKind} with {Message}",
                profileId,
                lockKind,
                ex.Message
            );
            return false;
        }
    }

    public async Task<bool> CanUnlockAsync(
        string? password,
        int unlocker,
        int userpriority,
        LockKind lockKind,
        int lockee
    )
    {
        try
        {
            if (
                await _locksSql.CanUnlockByLockIdAsync(
                    new(password, unlocker, userpriority, (int)lockKind, lockee)
                ) is
                { } result
            )
            {
                return result.CanUnlock;
            }
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error checking if slot is locked for lockKind: {LockKind}, unlocker: {Unlocker}, lockee: {Lockee}, userpriority: {UserPriority}, password: {Password} with {Message}",
                lockKind,
                lockee,
                unlocker,
                userpriority,
                password,
                ex.Message
            );
            return false;
        }
    }
}
