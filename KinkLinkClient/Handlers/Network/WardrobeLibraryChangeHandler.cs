using System;
using System.Threading.Tasks;
using KinkLinkClient.Services;
using KinkLinkCommon.Domain.Network;
using Microsoft.AspNetCore.SignalR.Client;

namespace KinkLinkClient.Handlers.Network;

public class WardrobeLibraryChangeHandler : IDisposable
{
    private readonly WardrobeManager _wardrobeManager;
    private readonly IDisposable _handler;

    public WardrobeLibraryChangeHandler(WardrobeManager wardrobeManager, NetworkService network)
    {
        _wardrobeManager = wardrobeManager;
        _handler = network.Connection.On(HubMethod.WardrobeLibraryChanged, HandleLibraryChanged);
    }

    private async Task HandleLibraryChanged()
    {
        Plugin.Log.Information("[WardrobeLibraryChangeHandler] Wardrobe library changed on server, refreshing");
        await _wardrobeManager.SyncFromServerAsync().ConfigureAwait(false);
    }

    public void Dispose()
    {
        _handler.Dispose();
    }
}
