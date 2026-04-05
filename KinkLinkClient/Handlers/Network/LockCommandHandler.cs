using System;
using KinkLinkClient.Services;
using KinkLinkCommon.Domain;
using KinkLinkCommon.Domain.Network;
using Microsoft.AspNetCore.SignalR.Client;

namespace KinkLinkClient.Handlers.Network;

public class LockCommandHandler : IDisposable
{
    private readonly LockService _lockService;
    private readonly IDisposable _addLockHandler;
    private readonly IDisposable _removeLockHandler;

    public event Action<LockInfoDto>? OnLockAdded;
    public event Action<string>? OnLockRemoved;

    public LockCommandHandler(LockService lockService, NetworkService network)
    {
        _lockService = lockService;

        _addLockHandler = network.Connection.On<LockInfoDto>(HubMethod.AddLock, HandleLockAdded);
        _removeLockHandler = network.Connection.On<string>(HubMethod.RemoveLock, HandleLockRemoved);
    }

    private void HandleLockAdded(LockInfoDto lockInfo)
    {
        Plugin.Log.Information(
            "[LockCommandHandler] Lock added: {LockId} by locker {LockerId}",
            lockInfo.LockID,
            lockInfo.LockerID
        );
        _lockService.SyncSlot(lockInfo);
        OnLockAdded?.Invoke(lockInfo);
    }

    private void HandleLockRemoved(string lockId)
    {
        Plugin.Log.Information("[LockCommandHandler] Lock removed: {LockId}", lockId);
        _lockService.RemoveLock(lockId);
        OnLockRemoved?.Invoke(lockId);
    }

    public void Dispose()
    {
        _addLockHandler.Dispose();
        _removeLockHandler.Dispose();
        GC.SuppressFinalize(this);
    }
}
