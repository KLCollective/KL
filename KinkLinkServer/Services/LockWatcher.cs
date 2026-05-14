using KinkLinkCommon.Domain;
using KinkLinkCommon.Domain.Network;
using KinkLinkCommon.Domain.Network.Locks;
using KinkLinkCommon.Domain.Network.SyncPairState;
using KinkLinkCommon.Domain.Wardrobe;
using KinkLinkServer.Domain;
using KinkLinkServer.Domain.Interfaces;
using KinkLinkServer.SignalR.Handlers;
using KinkLinkServer.SignalR.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace KinkLinkServer.Services;

public class LockWatcher : DatabaseWatcherBase
{
    private readonly LocksHandler _locksHandler;
    private readonly PermissionsService _permissionsService;
    private readonly WardrobeDataService _wardrobeData;
    private readonly ILogger<LockWatcher> _typedLogger;

    protected override string ChannelName => "lock_changed";

    public LockWatcher(
        Configuration config,
        IHubContext<PrimaryHub> hubContext,
        IPresenceService presenceService,
        KinkLinkProfilesService profilesService,
        LocksHandler locksHandler,
        PermissionsService permissionsService,
        WardrobeDataService wardrobeData,
        ILogger<LockWatcher> logger)
        : base(config, hubContext, presenceService, profilesService, logger)
    {
        _locksHandler = locksHandler;
        _permissionsService = permissionsService;
        _wardrobeData = wardrobeData;
        _typedLogger = logger;
    }

    protected override async Task HandleNotificationAsync(string? channel, string payload)
    {
        var evt = DeserializePayload<LockChangeEvent>(payload);
        if (evt == null)
            return;

        // Push SyncLocks to lockee
        await PushSyncLocksToUserAsync(evt.LockeeId);

        // Push SyncLocks to locker (if different from lockee)
        if (evt.LockerId != evt.LockeeId)
            await PushSyncLocksToUserAsync(evt.LockerId);

        // Push SyncPairState to lockee's friends
        await PushPairStateToFriendsAsync(evt.LockeeId);
    }

    private async Task PushSyncLocksToUserAsync(int profileId)
    {
        var uid = await GetUidByProfileIdAsync(profileId);
        if (uid == null)
            return;

        var presence = PresenceService.TryGet(uid);
        if (presence == null)
            return;

        var locks = await _locksHandler.GetAllLocksForUserAsync(uid);
        await HubContext.Clients
            .Client(presence.ConnectionId)
            .SendAsync(HubMethod.SyncLocks, new SyncLocksResponse(locks));
    }

    private async Task PushPairStateToFriendsAsync(int lockeeProfileId)
    {
        var uid = await GetUidByProfileIdAsync(lockeeProfileId);
        if (uid == null)
            return;

        var allPermissions = await _permissionsService.GetAllPermissions(uid);
        if (allPermissions.Count == 0)
            return;

        var locks = await _locksHandler.GetAllLocksForUserAsync(uid);
        var wardrobe = await _wardrobeData.GetPairWardrobeItemsAsync(lockeeProfileId);
        var wardrobeWithLocks = PairWardrobeStateDto.PopulateLockIds<LockWatcher>(wardrobe, locks, _typedLogger);

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
