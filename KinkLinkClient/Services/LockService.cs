using System;
using System.Collections.Generic;
using System.Linq;
using KinkLinkCommon.Domain;

namespace KinkLinkClient.Services;

public class LockService
{
    private readonly IdentityService _identity;

    // Composite key: (LockKind, lockeeUid) to avoid cache collisions
    // when lock events arrive for different users
    private Dictionary<(LockKind, string), LockInfoDto> _dictionary =
        new Dictionary<(LockKind, string), LockInfoDto>();

    public LockService(IdentityService identity)
    {
        _identity = identity;
    }

    // Lock helper: check if a lock is active for the current user
    public bool IsLocked(LockKind lockId)
    {
        var key = (lockId, _identity.FriendCode);
        return _dictionary.ContainsKey(key);
    }

    // Used as a callback from the network to synchronize the locks for the current user.
    public void SyncLocks(List<LockInfoDto> lockInfos)
    {
        _dictionary.Clear();
        var currentUid = _identity.FriendCode;
        foreach (var lockInfo in lockInfos)
        {
            // Only cache locks belonging to the current user
            if (string.IsNullOrEmpty(currentUid) || lockInfo.LockeeID == currentUid)
            {
                _dictionary[(lockInfo.LockID, lockInfo.LockeeID)] = lockInfo;
            }
            else
            {
                Plugin.Log.Debug(
                    "[LockService] SyncLocks: skipping lock for non-current user: {LockeeId}",
                    lockInfo.LockeeID
                );
            }
        }
    }

    // Called by network event handlers to sync a single lock.
    public void SyncSlot(LockInfoDto lockInfo)
    {
        var currentUid = _identity.FriendCode;
        if (!string.IsNullOrEmpty(currentUid) && lockInfo.LockeeID != currentUid)
        {
            Plugin.Log.Debug(
                "[LockService] SyncSlot: skipping lock for non-current user: {LockeeId}",
                lockInfo.LockeeID
            );
            return;
        }

        _dictionary[(lockInfo.LockID, lockInfo.LockeeID)] = lockInfo;
    }

    public void RemoveLock(LockKind lockId)
    {
        // Remove lock for current user
        var key = (lockId, _identity.FriendCode);
        _dictionary.Remove(key);
    }

    public void RemoveLock(LockKind lockId, string lockeeUid)
    {
        var key = (lockId, lockeeUid);
        _dictionary.Remove(key);
    }

    public LockInfoDto? GetLock(LockKind lockId)
    {
        var key = (lockId, _identity.FriendCode);
        return _dictionary.TryGetValue(key, out var lockInfo) ? lockInfo : null;
    }

    public LockInfoDto? GetLock(LockKind lockId, string lockeeUid)
    {
        var key = (lockId, lockeeUid);
        return _dictionary.TryGetValue(key, out var lockInfo) ? lockInfo : null;
    }

    public IReadOnlyCollection<LockInfoDto> GetAllLocks()
    {
        return _dictionary.Values.ToList().AsReadOnly();
    }
}
