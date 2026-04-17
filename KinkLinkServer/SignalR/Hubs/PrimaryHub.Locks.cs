using KinkLinkCommon.Domain;
using KinkLinkCommon.Domain.Network;
using Microsoft.AspNetCore.SignalR;

namespace KinkLinkServer.SignalR.Hubs;

public partial class PrimaryHub
{
    [HubMethodName(HubMethod.SyncLocks)]
    public async Task<List<LockInfoDto>> SyncLocks()
    {
        logger.LogTrace("[SignalR] SyncLocks: {FriendCode}", FriendCode);
        return await _locksHandler.GetAllLocksForUserAsync(FriendCode);
    }

    [HubMethodName(HubMethod.AddLock)]
    public async Task<ActionResult<LockInfoDto>> AddLock(LockInfoDto lockInfo)
    {
        logger.LogInformation("[SignalR] AddLock: {FriendCode}, Lockee: {Lockee}", FriendCode, lockInfo.LockeeID);
        return await _locksHandler.HandleAddLockAsync(FriendCode, lockInfo, Clients);
    }

    [HubMethodName(HubMethod.RemoveLock)]
    public async Task<ActionResult<bool>> RemoveLock(string lockId, string lockeeUid)
    {
        logger.LogInformation("[SignalR] RemoveLock: {FriendCode}, LockId: {LockId}, Lockee: {Lockee}", FriendCode, lockId, lockeeUid);
        return await _locksHandler.HandleRemoveLockAsync(FriendCode, lockId, lockeeUid, Clients);
    }
}
