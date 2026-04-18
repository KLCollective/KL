using KinkLinkCommon.Domain.Enums;
using KinkLinkCommon.Domain.Network;
using KinkLinkCommon.Domain.Network.SyncOnlineStatus;
using KinkLinkServer.Domain.Interfaces;
using KinkLinkServer.Services;
using Microsoft.AspNetCore.SignalR;

namespace KinkLinkServer.SignalR.Handlers;

/// <summary>
///     Processes clients connecting and disconnecting from the server
/// </summary>
public class OnlineStatusUpdateHandler(
    PermissionsService permissionsService,
    IPresenceService presenceService,
    ILogger<OnlineStatusUpdateHandler> logger
)
{
    /// <summary>
    ///     Handle the event, removing or adding from the current client list, and updating all the user's friends who are online
    /// </summary>
    public async Task Handle(string friendCode, bool online, IHubCallerClients clients)
    {
        logger.LogDebug("OnlineStatusUpdate: {FriendCode} -> {Online}", friendCode, online);

        if (online is false)
            presenceService.Remove(friendCode);

        var permissions = await permissionsService.GetAllPermissions(friendCode);
        foreach (var permission in permissions)
        {
            // Ignore pending friends
            if (permission.PermissionsGrantedBy is null)
                continue;

            // Only evaluate online friends
            if (presenceService.TryGet(permission.TargetUID) is not { } target)
                continue;

            try
            {
                if (online)
                {
                    var request = new SyncOnlineStatusCommand(
                        friendCode,
                        FriendOnlineStatus.Online,
                        permission.PermissionsGrantedTo
                    );
                    await clients
                        .Client(target.ConnectionId)
                        .SendAsync(HubMethod.SyncOnlineStatus, request);
                }
                else
                {
                    var request = new SyncOnlineStatusCommand(
                        friendCode,
                        FriendOnlineStatus.Offline,
                        null
                    );
                    await clients
                        .Client(target.ConnectionId)
                        .SendAsync(HubMethod.SyncOnlineStatus, request);
                }
            }
            catch (Exception e)
            {
                logger.LogError(
                    "Syncing online status {Sender} -> {Target} failed, {Error}",
                    friendCode,
                    permission.TargetUID,
                    e
                );
            }
        }
    }
}
