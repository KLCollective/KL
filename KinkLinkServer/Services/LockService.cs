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

        return rows.Select(row => new LockInfoDto
            {
                LockID = row.LockId,
                LockeeID = row.LockeeId,
                LockerID = row.LockerId,
                LockPriority = (RelationshipPriority)row.LockPriority,
                CanSelfUnlock = row.CanSelfUnlock,
                Expires = row.Expires,
                Password = row.Password,
            })
            .ToList();
    }

    public async Task<LockInfoDto?> GetLockAsync(string lockId, string lockeeUid)
    {
        var profile = await _profilesSql.GetProfileByUidAsync(new(lockeeUid));
        if (profile is null)
        {
            return null;
        }

        var row = await _locksSql.GetLockByIdAsync(new(lockId, profile.Value.Id));
        if (row is null)
        {
            return null;
        }

        return new LockInfoDto
        {
            LockID = row.Value.LockId,
            LockeeID = row.Value.LockeeId,
            LockerID = row.Value.LockerId,
            LockPriority = (RelationshipPriority)row.Value.LockPriority,
            CanSelfUnlock = row.Value.CanSelfUnlock,
            Expires = row.Value.Expires,
            Password = row.Value.Password,
        };
    }

    public async Task<LockInfoDto?> AddOrUpdateLockAsync(LockInfoDto lockInfo)
    {
        var row = await _locksSql.AddOrUpdateLockAsync(
            new(
                lockInfo.LockID,
                lockInfo.LockeeID,
                lockInfo.LockerID,
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
            return null;
        }

        return new LockInfoDto
        {
            LockID = row.Value.LockId,
            LockeeID = row.Value.LockeeId,
            LockerID = row.Value.LockerId,
            LockPriority = (RelationshipPriority)row.Value.LockPriority,
            CanSelfUnlock = row.Value.CanSelfUnlock,
            Expires = row.Value.Expires,
            Password = row.Value.Password,
        };
    }

    public async Task<bool> RemoveLockAsync(string lockId, string lockeeUid)
    {
        var profile = await _profilesSql.GetProfileByUidAsync(new(lockeeUid));
        if (profile is null)
        {
            return false;
        }

        var result = await _locksSql.RemoveLockAsync(new(lockId, profile.Value.Id));
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

        return rows.Select(row => new LockInfoDto
            {
                LockID = row.LockId,
                LockeeID = row.LockeeId,
                LockerID = row.LockerId,
                LockPriority = (RelationshipPriority)row.LockPriority,
                CanSelfUnlock = row.CanSelfUnlock,
                Expires = row.Expires,
                Password = row.Password,
            })
            .ToList();
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

    public async Task<bool> IsSlotLockedAsync(int profileId, string slotName)
    {
        try
        {
            if (await _locksSql.IsLockedAsync(new(slotName, profileId)) is { } result)
            {
                return result.IsLocked;
            }
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error checking if slot is locked for profileId: {ProfileId}, slotName: {SlotName} with {Message}",
                profileId,
                slotName,
                ex.Message
            );
            return false;
        }
    }

    public async Task<bool> CanLockeeUnlock(int profileId, string slotName)
    {
        try
        {
            if (await _locksSql.CanLockeeUnlockAsync(new(slotName, profileId)) is { } result)
            {
                return result.CanUnlock;
            }
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error checking if slot is locked for profileId: {ProfileId}, slotName: {SlotName} with {Message}",
                profileId,
                slotName,
                ex.Message
            );
            return false;
        }
    }

    public async Task<bool> CanLockerUnlock(
        int profileId,
        RelationshipPriority priority,
        string slotName
    )
    {
        try
        {
            if (await _locksSql.CanLockeeUnlockAsync(new(slotName, profileId)) is { } result)
            {
                return result.CanUnlock;
            }
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error checking if slot is locked for profileId: {ProfileId}, slotName: {SlotName} with {Message}",
                profileId,
                slotName,
                ex.Message
            );
            return false;
        }
    }
}
