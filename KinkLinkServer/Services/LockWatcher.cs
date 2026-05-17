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
        _typedLogger.LogDebug("[LockWatcher] Notification received on {Channel}: {Payload}", channel, payload);
        var evt = DeserializePayload<LockChangeEvent>(payload);
        if (evt == null)
        {
            _typedLogger.LogInformation("[LockWatcher] Ignoring notification on {Channel}: failed to deserialize payload", channel);
            return;
        }

        // Resolve lockee UID once, reuse for lockee SyncLocks and friend push
        var lockeeUid = await GetUidByProfileIdAsync(evt.LockeeId);
        if (lockeeUid == null)
        {
            _typedLogger.LogDebug("[LockWatcher] No UID found for lockee profile {ProfileId}", evt.LockeeId);
        }

        // Push SyncLocks to lockee
        _typedLogger.LogDebug("[LockWatcher] Pushing SyncLocks to lockee profile {ProfileId}", evt.LockeeId);
        await PushSyncLocksToUserAsync(evt.LockeeId, lockeeUid);

        // Push SyncLocks to locker (if different from lockee)
        _typedLogger.LogDebug("[LockWatcher] Checking whether locker differs from lockee for profile {ProfileId} (locker={LockerId})", evt.LockeeId, evt.LockerId);
        if (evt.LockerId != evt.LockeeId)
        {
            var lockerUid = await GetUidByProfileIdAsync(evt.LockerId);
            await PushSyncLocksToUserAsync(evt.LockerId, lockerUid);
        }

        // Push SyncPairState to lockee's friends
        if (lockeeUid != null)
        {
            _typedLogger.LogDebug("[LockWatcher] Pushing pair state to friends for uid {Uid}, profile {ProfileId}", lockeeUid, evt.LockeeId);
            await FriendStatePusher.PushPairStateToFriendsAsync(
                lockeeUid, evt.LockeeId,
                _permissionsService, _locksHandler, _wardrobeData,
                HubContext, PresenceService, _typedLogger);
            _typedLogger.LogDebug("[LockWatcher] Finished pushing pair state to friends for profile {ProfileId}", evt.LockeeId);
        }

        _typedLogger.LogInformation("[LockWatcher] Processed lock_changed for lockee {LockeeId} locker {LockerId}", evt.LockeeId, evt.LockerId);
    }

    private async Task PushSyncLocksToUserAsync(int profileId, string? knownUid = null)
    {
        var uid = knownUid ?? await GetUidByProfileIdAsync(profileId);
        if (uid == null)
        {
            _typedLogger.LogInformation("[LockWatcher] Not sending SyncLocks for profile {ProfileId}: UID not found", profileId);
            return;
        }

        var presence = PresenceService.TryGet(uid);
        if (presence == null)
        {
            _typedLogger.LogInformation("[LockWatcher] Not sending SyncLocks for profile {ProfileId} (uid={Uid}): user not present/online", profileId, uid);
            return;
        }

        var locks = await _locksHandler.GetAllLocksForUserAsync(uid);
        try
        {
            await HubContext.Clients
                .Client(presence.ConnectionId)
                .SendAsync(HubMethod.SyncLocks, new SyncLocksResponse(locks));
            _typedLogger.LogInformation("[LockWatcher] Sent SyncLocks to profile {ProfileId} (uid={Uid})", profileId, uid);
        }
        catch (Exception ex)
        {
            _typedLogger.LogWarning(ex,
                "[LockWatcher] Failed to push SyncLocks to profile {ProfileId}",
                profileId);
        }
    }
}
