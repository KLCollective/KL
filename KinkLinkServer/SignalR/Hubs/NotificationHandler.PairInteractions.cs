using KinkLinkCommon.Domain;
using KinkLinkCommon.Domain.Network;
using KinkLinkCommon.Domain.Network.SyncPairState;
using KinkLinkServer.Domain;
using KinkLinkServer.Domain.Interfaces;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace KinkLinkServer.SignalR.Hubs;

public partial class NotificationHandler
{
    public async Task NotifyTargetOfStateChangeAsync(
        string targetFriendCode,
        Func<string, Task<object?>> getStateFunc,
        IHubCallerClients clients)
    {
        if (presenceService.TryGet(targetFriendCode) is not { } presence)
            return;

        try
        {
            var state = await getStateFunc(targetFriendCode);
            if (state != null)
            {
                await clients
                    .Client(presence.ConnectionId)
                    .SendAsync(HubMethod.SyncPairState, state);
            }
        }
        catch (Exception e)
        {
            logger.LogWarning(
                "[NotificationHandler] Failed to notify target {Target} of state change: {Error}",
                targetFriendCode,
                e.Message);
        }
    }

    public async Task PushStateToAllFriendsAsync(
        string friendCode,
        Func<string, Task<List<TwoWayPermissions>>> getPermissionsFunc,
        Func<string, TwoWayPermissions, Task<object?>> getStateFunc,
        IHubCallerClients clients)
    {
        try
        {
            var allPermissions = await getPermissionsFunc(friendCode);
            if (allPermissions.Count == 0)
                return;

            foreach (var perm in allPermissions)
            {
                if (presenceService.TryGet(perm.TargetUID) is not { } presence)
                    continue;

                var state = await getStateFunc(friendCode, perm);
                if (state == null)
                    continue;

                await clients
                    .Client(presence.ConnectionId)
                    .SendAsync(HubMethod.SyncPairState, state);
            }

            logger.LogDebug("[NotificationHandler] Pushed {FriendCode} state to all friends", friendCode);
        }
        catch (Exception e)
        {
            logger.LogError(
                e,
                "[NotificationHandler] Failed to push {FriendCode} state to friends",
                friendCode);
        }
    }
}
