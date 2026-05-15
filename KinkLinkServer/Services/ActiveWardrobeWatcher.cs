using KinkLinkCommon.Domain.Network;
using KinkLinkServer.Domain;
using KinkLinkServer.Domain.Interfaces;
using KinkLinkServer.SignalR.Handlers;
using KinkLinkServer.SignalR.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace KinkLinkServer.Services;

public class ActiveWardrobeWatcher : DatabaseWatcherBase
{
    private readonly WardrobeDataService _wardrobeData;
    private readonly LocksHandler _locksHandler;
    private readonly PermissionsService _permissionsService;
    private readonly ILogger<ActiveWardrobeWatcher> _typedLogger;

    protected override string ChannelName => "activewardrobe_changed";

    public ActiveWardrobeWatcher(
        Configuration config,
        IHubContext<PrimaryHub> hubContext,
        IPresenceService presenceService,
        KinkLinkProfilesService profilesService,
        WardrobeDataService wardrobeData,
        LocksHandler locksHandler,
        PermissionsService permissionsService,
        ILogger<ActiveWardrobeWatcher> logger)
        : base(config, hubContext, presenceService, profilesService, logger)
    {
        _wardrobeData = wardrobeData;
        _locksHandler = locksHandler;
        _permissionsService = permissionsService;
        _typedLogger = logger;
    }

    protected override async Task HandleNotificationAsync(string? channel, string payload)
    {
        var evt = DeserializePayload<ProfileChangeEvent>(payload);
        if (evt == null)
            return;

        var uid = await GetUidByProfileIdAsync(evt.ProfileId);
        if (uid == null)
            return;

        // Push SyncWardrobeState to the owner
        var presence = PresenceService.TryGet(uid);
        if (presence != null)
        {
            var state = await _wardrobeData.GetWardrobeStateAsync(evt.ProfileId);
            if (state != null)
            {
                await HubContext.Clients
                    .Client(presence.ConnectionId)
                    .SendAsync(HubMethod.SyncWardrobeState, state);
            }
        }

        // Push SyncPairState to all online friends
        await FriendStatePusher.PushPairStateToFriendsAsync(
            uid, evt.ProfileId,
            _permissionsService, _locksHandler, _wardrobeData,
            HubContext, PresenceService, _typedLogger);
    }
}
