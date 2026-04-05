using System;
using System.Collections.Generic;
using KinkLinkClient.Services;
using KinkLinkCommon.Domain;
using KinkLinkCommon.Domain.Network;
using Microsoft.AspNetCore.SignalR.Client;

namespace KinkLinkClient.Handlers.Network;

public class LockSyncHandler : IDisposable
{
    private readonly LockService _lockService;
    private readonly IDisposable _syncHandler;

    public LockSyncHandler(LockService lockService, NetworkService network)
    {
        _lockService = lockService;

        _syncHandler = network.Connection.On<List<LockInfoDto>>(
            HubMethod.SyncLocks,
            HandleSyncLocks
        );
    }

    private void HandleSyncLocks(List<LockInfoDto> locks)
    {
        Plugin.Log.Information("[LockSyncHandler] Received {Count} locks from server", locks.Count);
        _lockService.SyncLocks(locks);
    }

    public void Dispose()
    {
        _syncHandler.Dispose();
        GC.SuppressFinalize(this);
    }
}
