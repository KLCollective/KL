using System;
using System.Threading.Tasks;
using KinkLinkClient.Dependencies.Glamourer.Services;
using KinkLinkClient.Services;
using KinkLinkClient.Utils;
using KinkLinkCommon.Domain.Network;
using KinkLinkCommon.Domain.Wardrobe;
using Microsoft.AspNetCore.SignalR.Client;

namespace KinkLinkClient.Handlers.Network;

public class WardrobeSyncHandler : IDisposable
{
    private readonly WardrobeManager _wardrobeManager;
    private readonly IDisposable _syncHandler;

    public WardrobeSyncHandler(WardrobeManager wardrobeManager, NetworkService network)
    {
        _wardrobeManager = wardrobeManager;

        _syncHandler = network.Connection.On<WardrobeStateDto>(
            HubMethod.SyncWardrobeState,
            HandleSyncWardrobeState
        );
    }

    private async Task HandleSyncWardrobeState(WardrobeStateDto state)
    {
        try
        {
            Plugin.Log.Information(
                "[WardrobeSyncHandler] Received wardrobe state sync from server"
            );

            // Delegate to wardrobe manager which encapsulates Glamourer/Penumbra interaction
            await _wardrobeManager.HandleServerWardrobeStateAsync(state).ConfigureAwait(false);
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
