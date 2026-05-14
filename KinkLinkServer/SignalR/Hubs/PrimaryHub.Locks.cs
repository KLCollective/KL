using System.Diagnostics;
using KinkLinkCommon.Domain;
using KinkLinkCommon.Domain.Enums;
using KinkLinkCommon.Domain.Network;
using KinkLinkCommon.Domain.Network.Locks;
using Microsoft.AspNetCore.SignalR;

namespace KinkLinkServer.SignalR.Hubs;

public partial class PrimaryHub
{
    [HubMethodName(HubMethod.SyncLocks)]
    public async Task<ActionResult<SyncLocksResponse>> SyncLocks()
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            logger.LogTrace("[SignalR] SyncLocks: {FriendCode}", FriendCode);
            var locks = await _locksHandler.GetAllLocksForUserAsync(FriendCode);
            return new ActionResult<SyncLocksResponse>(
                ActionResultEc.Success,
                new SyncLocksResponse(locks)
            );
        }
        finally
        {
            stopwatch.Stop();
            metricsService.IncrementSignalRMessage("SyncLocks", true);
            metricsService.RecordSignalRMessageDuration("SyncLocks", stopwatch.ElapsedMilliseconds);
        }
    }

    [HubMethodName(HubMethod.AddLock)]
    public async Task<ActionResult<AddLockResponse>> AddLock(AddLockRequest request)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            logger.LogInformation(
                "[SignalR] AddLock: {FriendCode}, Lockee: {Lockee}",
                FriendCode,
                request.LockInfo.LockeeID
            );
            var (result, lockeeFriendCode) = await _locksHandler.HandleAddLockAsync(
                FriendCode,
                request.LockInfo
            );

            var innerResult = result.Result;
            return new ActionResult<AddLockResponse>(
                innerResult,
                innerResult == ActionResultEc.Success ? new AddLockResponse(result.Value) : null
            );
        }
        finally
        {
            stopwatch.Stop();
            metricsService.IncrementSignalRMessage("AddLock", true);
            metricsService.RecordSignalRMessageDuration("AddLock", stopwatch.ElapsedMilliseconds);
        }
    }

    [HubMethodName(HubMethod.RemoveLock)]
    public async Task<ActionResult<RemoveLockResponse>> RemoveLock(RemoveLockRequest request)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            logger.LogInformation(
                "[SignalR] RemoveLock: {FriendCode}, LockId: {LockId}, Lockee: {Lockee}",
                FriendCode,
                request.LockId,
                request.LockeeUid
            );
            var removeResult = await _locksHandler.HandleRemoveLockAsync(
                FriendCode,
                request.LockId,
                request.LockeeUid,
                // TODO: For when passwords are supported, plumb it here
                null
            );
            var result = removeResult.Result;

            var success = result.Result == ActionResultEc.Success;
            return new ActionResult<RemoveLockResponse>(
                result.Result,
                success ? new RemoveLockResponse(true) : null
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
