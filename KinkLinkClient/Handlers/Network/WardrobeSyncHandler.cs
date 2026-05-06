using System;
using KinkLinkClient.Services;
using KinkLinkCommon.Domain.Network;
using KinkLinkCommon.Domain.Wardrobe;
using Microsoft.AspNetCore.SignalR.Client;

namespace KinkLinkClient.Handlers.Network;

public class WardrobeSyncHandler : IDisposable
{
    private readonly WardrobeService _wardrobeService;
    private readonly IDisposable _syncHandler;

    public WardrobeSyncHandler(WardrobeService wardrobeService, NetworkService network)
    {
        _wardrobeService = wardrobeService;

        _syncHandler = network.Connection.On<WardrobeStateDto>(
            HubMethod.SyncWardrobeState,
            HandleSyncWardrobeState
        );
    }

    private void HandleSyncWardrobeState(WardrobeStateDto state)
    {
        try
        {
            Plugin.Log.Information("[WardrobeSyncHandler] Received wardrobe state sync from server");
            _wardrobeService.ApplyWardrobeState(state);
        }
        catch (Exception ex)
        {
            Plugin.Log.Error(ex, "[WardrobeSyncHandler] Failed to handle SyncWardrobeState");
        }
    }

    public void Dispose()
    {
        _syncHandler.Dispose();
        GC.SuppressFinalize(this);
    }
}
