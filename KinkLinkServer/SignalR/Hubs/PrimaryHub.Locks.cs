using System.Diagnostics;
using KinkLinkCommon.Domain;
using KinkLinkCommon.Domain.Network;
using Microsoft.AspNetCore.SignalR;

namespace KinkLinkServer.SignalR.Hubs;

public partial class PrimaryHub
{
    [HubMethodName(HubMethod.SyncLocks)]
    public async Task<List<LockInfoDto>> SyncLocks()
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            logger.LogTrace("[SignalR] SyncLocks: {FriendCode}", FriendCode);
            return await _locksHandler.GetAllLocksForUserAsync(FriendCode);
        }
        finally
        {
            stopwatch.Stop();
            metricsService.IncrementSignalRMessage("SyncLocks", true);
            metricsService.RecordSignalRMessageDuration("SyncLocks", stopwatch.ElapsedMilliseconds);
        }
    }

    [HubMethodName(HubMethod.AddLock)]
    public async Task<ActionResult<LockInfoDto>> AddLock(LockInfoDto lockInfo)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            logger.LogInformation(
                "[SignalR] AddLock: {FriendCode}, Lockee: {Lockee}",
                FriendCode,
                lockInfo.LockeeID
            );
            return await _locksHandler.HandleAddLockAsync(FriendCode, lockInfo, Clients);
        }
        finally
        {
            stopwatch.Stop();
            metricsService.IncrementSignalRMessage("AddLock", true);
            metricsService.RecordSignalRMessageDuration("AddLock", stopwatch.ElapsedMilliseconds);
        }
    }

    [HubMethodName(HubMethod.RemoveLock)]
    public async Task<ActionResult<bool>> RemoveLock(string lockId, string lockeeUid)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            logger.LogInformation(
                "[SignalR] RemoveLock: {FriendCode}, LockId: {LockId}, Lockee: {Lockee}",
                FriendCode,
                lockId,
                lockeeUid
            );
            return await _locksHandler.HandleRemoveLockAsync(
                FriendCode,
                lockId,
                lockeeUid,
                // TODO: For when passwords are supported, plumb it here
                null,
                Clients
            );
        }
        finally
        {
            stopwatch.Stop();
            metricsService.IncrementSignalRMessage("RemoveLock", true);
            metricsService.RecordSignalRMessageDuration(
                "RemoveLock",
                stopwatch.ElapsedMilliseconds
            );
        }
    }
}
