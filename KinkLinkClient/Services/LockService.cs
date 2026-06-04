using System.Collections.Generic;
using KinkLinkCommon.Domain;

namespace KinkLinkClient.Services;

public class LockService
{
    private Dictionary<LockKind, LockInfoDto> _dicionary = new Dictionary<LockKind, LockInfoDto>();

    // Simple helper to check if a lock is currently active
    public bool IsLocked(LockKind lockId)
    {
        return _dicionary.ContainsKey(lockId);
    }

    // Used as a callback from the network to synchronize the locks.
    public void SyncLocks(List<LockInfoDto> lockinfos)
    {
        _dicionary.Clear();
        foreach (var lockinfo in lockinfos)
        {
            _dicionary[lockinfo.LockID] = lockinfo;
        }
    }

    // Set by a networking call-back handler.
    public void SyncSlot(LockInfoDto lockinfo)
    {
        _dicionary[lockinfo.LockID] = lockinfo;
    }

    public void RemoveLock(LockKind lockId)
    {
        _dicionary.Remove(lockId);
    }

    public LockInfoDto? GetLock(LockKind lockId)
    {
        return _dicionary.TryGetValue(lockId, out var lockInfo) ? lockInfo : null;
    }

    public IReadOnlyDictionary<LockKind, LockInfoDto> GetAllLocks()
    {
        return _dicionary;
    }
}
