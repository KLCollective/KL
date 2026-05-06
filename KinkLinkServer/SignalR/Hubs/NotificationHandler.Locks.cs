using KinkLinkCommon.Domain;
using KinkLinkCommon.Domain.Network;
using KinkLinkServer.Domain.Interfaces;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace KinkLinkServer.SignalR.Hubs;

public partial class NotificationHandler
{
    public async Task NotifyLockeeOfLockUpdateAsync(
        string lockeeFriendCode,
        Func<string, Task<List<LockInfoDto>>> getLocksFunc,
        IHubCallerClients clients)
    {
        if (presenceService.TryGet(lockeeFriendCode) is not { } presence)
            return;

        try
        {
            var locks = await getLocksFunc(lockeeFriendCode);
            await clients.Client(presence.ConnectionId).SendAsync(HubMethod.SyncLocks, locks);
        }
        catch (Exception e)
        {
            logger.LogWarning(
                "[NotificationHandler] Failed to notify lockee {Lockee} of lock update: {Error}",
                lockeeFriendCode,
                e.Message);
        }
    }

    public async Task NotifyLockerOfLockUpdateAsync(
        string lockerFriendCode,
        Func<string, Task<List<LockInfoDto>>> getLocksFunc,
        IHubCallerClients clients)
    {
        if (presenceService.TryGet(lockerFriendCode) is not { } presence)
            return;

        try
        {
            var locks = await getLocksFunc(lockerFriendCode);
            await clients.Client(presence.ConnectionId).SendAsync(HubMethod.SyncLocks, locks);
        }
        catch (Exception e)
        {
            logger.LogWarning(
                "[NotificationHandler] Failed to notify locker {Locker} of lock update: {Error}",
                lockerFriendCode,
                e.Message);
        }
    }
}
