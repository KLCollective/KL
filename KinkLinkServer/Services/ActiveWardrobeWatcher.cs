using KinkLinkCommon.Domain;
using KinkLinkCommon.Domain.Network;
using KinkLinkCommon.Domain.Network.SyncPairState;
using KinkLinkCommon.Domain.Wardrobe;
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
        await PushPairStateToFriendsAsync(uid, evt.ProfileId);
    }

    private async Task PushPairStateToFriendsAsync(string uid, int profileId)
    {
        var allPermissions = await _permissionsService.GetAllPermissions(uid);
        if (allPermissions.Count == 0)
            return;

        var locks = await _locksHandler.GetAllLocksForUserAsync(uid);
        var wardrobe = await _wardrobeData.GetPairWardrobeItemsAsync(profileId);
        var wardrobeWithLocks = PairWardrobeStateDto.PopulateLockIds<ActiveWardrobeWatcher>(wardrobe, locks, _typedLogger);

        foreach (var perm in allPermissions)
        {
            if (PresenceService.TryGet(perm.TargetUID) is not { } presence)
                continue;

            await HubContext.Clients
                .Client(presence.ConnectionId)
                .SendAsync(
                    HubMethod.SyncPairState,
                    new SyncPairStateCommand(uid, perm.PermissionsGrantedTo, wardrobeWithLocks, locks)
                );
        }
    }
}
