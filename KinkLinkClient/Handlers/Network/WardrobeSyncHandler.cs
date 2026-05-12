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
    private readonly GlamourerService _glamourerService;
    private readonly IDisposable _syncHandler;

    public WardrobeSyncHandler(
        WardrobeManager wardrobeManager,
        NetworkService network,
        GlamourerService glamourerService
    )
    {
        _wardrobeManager = wardrobeManager;
        _glamourerService = glamourerService;

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
            await _wardrobeManager.ApplyWardrobeState(state);

            var itemCount = (state.Equipment?.Count ?? 0) + (state.ModSettings?.Count ?? 0);
            if (state.BaseLayerBase64 != null)
            {
                NotificationHelper.Info(
                    "Wardrobe Synced",
                    $"You have base set applied and {itemCount} items applied."
                );
            }
            else
            {
                NotificationHelper.Info("Wardrobe Synced", $"You have {itemCount} items applied.");
            }
            await _glamourerService.Reapply().ConfigureAwait(false);
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
