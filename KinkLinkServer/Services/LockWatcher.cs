using KinkLinkCommon.Domain.Network;
using KinkLinkCommon.Domain.Network.Locks;
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

        // Resolve lockee UID once, reuse for lockee SyncLocks and friend push
        var lockeeUid = await GetUidByProfileIdAsync(evt.LockeeId);

        // Push SyncLocks to lockee
        await PushSyncLocksToUserAsync(evt.LockeeId, lockeeUid);

        // Push SyncLocks to locker (if different from lockee)
        if (evt.LockerId != evt.LockeeId)
        {
            var lockerUid = await GetUidByProfileIdAsync(evt.LockerId);
            await PushSyncLocksToUserAsync(evt.LockerId, lockerUid);
        }

        // Push SyncPairState to lockee's friends
        if (lockeeUid != null)
            await FriendStatePusher.PushPairStateToFriendsAsync(
                lockeeUid, evt.LockeeId,
                _permissionsService, _locksHandler, _wardrobeData,
                HubContext, PresenceService, _typedLogger);
    }

    private async Task PushSyncLocksToUserAsync(int profileId, string? knownUid = null)
    {
        var uid = knownUid ?? await GetUidByProfileIdAsync(profileId);
        if (uid == null)
            return;

        var presence = PresenceService.TryGet(uid);
        if (presence == null)
            return;

        var locks = await _locksHandler.GetAllLocksForUserAsync(uid);
        try
        {
            await HubContext.Clients
                .Client(presence.ConnectionId)
                .SendAsync(HubMethod.SyncLocks, new SyncLocksResponse(locks));
        }
        catch (Exception ex)
        {
            _typedLogger.LogWarning(ex,
                "[LockWatcher] Failed to push SyncLocks to profile {ProfileId}",
                profileId);
        }
    }
}
