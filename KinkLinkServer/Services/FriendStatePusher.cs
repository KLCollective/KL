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

public static class FriendStatePusher
{
    public static async Task PushPairStateToFriendsAsync<T>(
        string uid,
        int profileId,
        PermissionsService permissionsService,
        LocksHandler locksHandler,
        WardrobeDataService wardrobeData,
        IHubContext<PrimaryHub> hubContext,
        IPresenceService presenceService,
        ILogger<T> logger)
    {
        var allPermissions = await permissionsService.GetAllPermissions(uid);
        if (allPermissions.Count == 0)
            return;

        var hasOnlineFriend = false;
        foreach (var perm in allPermissions)
        {
            if (presenceService.TryGet(perm.TargetUID) is not null)
            {
                hasOnlineFriend = true;
                break;
            }
        }
        if (!hasOnlineFriend)
            return;

        var locks = await locksHandler.GetAllLocksForUserAsync(uid);
        var wardrobe = await wardrobeData.GetPairWardrobeItemsAsync(profileId);
        var wardrobeWithLocks = PairWardrobeStateDto.PopulateLockIds(wardrobe, locks, logger);

        foreach (var perm in allPermissions)
        {
            if (presenceService.TryGet(perm.TargetUID) is not { } presence)
                continue;

            try
            {
                await hubContext.Clients
                    .Client(presence.ConnectionId)
                    .SendAsync(
                        HubMethod.SyncPairState,
                        new SyncPairStateCommand(uid, perm.PermissionsGrantedTo, wardrobeWithLocks, locks)
                    );
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex,
                    "[FriendStatePusher] Failed to push pair state to {Target} for {Uid}",
                    perm.TargetUID, uid);
            }
        }
    }
}
