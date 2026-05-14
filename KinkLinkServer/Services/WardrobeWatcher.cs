using KinkLinkCommon.Domain.Network;
using KinkLinkServer.Domain;
using KinkLinkServer.Domain.Interfaces;
using KinkLinkServer.SignalR.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace KinkLinkServer.Services;

public class WardrobeWatcher : DatabaseWatcherBase
{
    protected override string ChannelName => "wardrobe_changed";

    public WardrobeWatcher(
        Configuration config,
        IHubContext<PrimaryHub> hubContext,
        IPresenceService presenceService,
        KinkLinkProfilesService profilesService,
        ILogger<WardrobeWatcher> logger)
        : base(config, hubContext, presenceService, profilesService, logger)
    {
    }

    protected override async Task HandleNotificationAsync(string? channel, string payload)
    {
        var evt = DeserializePayload<ProfileChangeEvent>(payload);
        if (evt == null)
            return;

        var uid = await GetUidByProfileIdAsync(evt.ProfileId);
        if (uid == null)
            return;

        var presence = PresenceService.TryGet(uid);
        if (presence == null)
            return;

        await HubContext.Clients
            .Client(presence.ConnectionId)
            .SendAsync(HubMethod.WardrobeLibraryChanged);
    }
}
