using KinkLinkCommon.Domain.Network;
using KinkLinkCommon.Domain.Network.PairInteractions;
using KinkLinkServer.Domain.Interfaces;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace KinkLinkServer.SignalR.Hubs;

public partial class NotificationHandler(
    IPresenceService presenceService,
    ILogger<NotificationHandler> logger)
{
    public async Task<bool> TryNotifyOnlineTargetAsync(
        string targetFriendCode,
        InteractionPayload? payload,
        IHubCallerClients clients)
    {
        var target = presenceService.TryGet(targetFriendCode);
        if (target == null)
        {
            logger.LogDebug(
                "[NotificationHandler] Target {Target} is offline, skipping notification",
                targetFriendCode);
            return false;
        }

        logger.LogDebug(
            "[NotificationHandler] Target {Target} is online, notification sent",
            targetFriendCode);
        return true;
    }
}
