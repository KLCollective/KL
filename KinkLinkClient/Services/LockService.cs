using System.Collections.Generic;
using KinkLinkCommon.Domain;

namespace KinkLinkClient.Services;

public class LockService
{
    private Dictionary<string, LockInfoDto> _dicionary = new Dictionary<string, LockInfoDto>();

    // Simple helper to check if a lock is currently active
    public bool IsLocked(string lockId)
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

    public void RemoveLock(string lockId)
    {
        _dicionary.Remove(lockId);
    }

    public LockInfoDto? GetLock(string lockId)
    {
        return _dicionary.TryGetValue(lockId, out var lockInfo) ? lockInfo : null;
    }

    public IReadOnlyDictionary<string, LockInfoDto> GetAllLocks()
    {
        return _dicionary;
    }
}
