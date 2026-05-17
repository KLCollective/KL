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
        _typedLogger.LogDebug("[ActiveWardrobeWatcher] Notification received on {Channel}: {Payload}", channel, payload);

        var evt = DeserializePayload<ProfileChangeEvent>(payload);
        if (evt == null)
        {
            _typedLogger.LogInformation("[ActiveWardrobeWatcher] Ignoring notification on {Channel}: failed to deserialize payload", channel);
            return;
        }

        var uid = await GetUidByProfileIdAsync(evt.ProfileId);
        if (uid == null)
        {
            _typedLogger.LogInformation("[ActiveWardrobeWatcher] Ignoring notification: no UID found for profile {ProfileId}", evt.ProfileId);
            return;
        }

        var ownerSent = false;

        // Push SyncWardrobeState to the owner
        var presence = PresenceService.TryGet(uid);
        if (presence != null)
        {
            var state = await _wardrobeData.GetWardrobeStateAsync(evt.ProfileId);
            if (state != null)
            {
                _typedLogger.LogDebug("[ActiveWardrobeWatcher] Sending SyncWardrobeState to owner (uid={Uid}, conn={Conn}) for profile {ProfileId}", uid, presence.ConnectionId, evt.ProfileId);
                await HubContext.Clients
                    .Client(presence.ConnectionId)
                    .SendAsync(HubMethod.SyncWardrobeState, state);
                _typedLogger.LogDebug("[ActiveWardrobeWatcher] Sent SyncWardrobeState to owner for profile {ProfileId}", evt.ProfileId);
                ownerSent = true;
            }
            else
            {
                _typedLogger.LogDebug("[ActiveWardrobeWatcher] No wardrobe state available for profile {ProfileId}", evt.ProfileId);
            }
        }
        else
        {
            _typedLogger.LogDebug("[ActiveWardrobeWatcher] Owner not present/online for uid {Uid}", uid);
        }

        // Push SyncPairState to all online friends
        _typedLogger.LogDebug("[ActiveWardrobeWatcher] Pushing pair state to friends for uid {Uid}, profile {ProfileId}", uid, evt.ProfileId);
        await FriendStatePusher.PushPairStateToFriendsAsync(
            uid, evt.ProfileId,
            _permissionsService, _locksHandler, _wardrobeData,
            HubContext, PresenceService, _typedLogger);
        _typedLogger.LogDebug("[ActiveWardrobeWatcher] Finished pushing pair state to friends for profile {ProfileId}", evt.ProfileId);

        // Final information-level event for tracing
        _typedLogger.LogInformation("[ActiveWardrobeWatcher] Processed activewardrobe_changed for profile {ProfileId} (uid={Uid}) ownerSent={OwnerSent}", evt.ProfileId, uid, ownerSent);
    }
}
