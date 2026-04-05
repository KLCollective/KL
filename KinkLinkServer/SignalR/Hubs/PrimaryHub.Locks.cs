using KinkLinkCommon.Domain;
using KinkLinkCommon.Domain.Network;
using Microsoft.AspNetCore.SignalR;

namespace KinkLinkServer.SignalR.Hubs;

public partial class PrimaryHub
{
    [HubMethodName(HubMethod.SyncLocks)]
    public async Task<List<LockInfoDto>> SyncLocks()
    {
        return await _locksHandler.GetAllLocksForUserAsync(FriendCode);
    }

    [HubMethodName(HubMethod.AddLock)]
    public async Task<ActionResult<LockInfoDto>> AddLock(LockInfoDto lockInfo)
    {
        return await _locksHandler.HandleAddLockAsync(FriendCode, lockInfo, Clients);
    }

    [HubMethodName(HubMethod.RemoveLock)]
    public async Task<ActionResult<bool>> RemoveLock(string lockId, string lockeeUid)
    {
        return await _locksHandler.HandleRemoveLockAsync(FriendCode, lockId, lockeeUid, Clients);
    }
}
