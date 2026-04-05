using KinkLinkCommon.Domain;
using KinkLinkCommon.Domain.Network;
using KinkLinkCommon.Domain.Network.PairInteractions;
using KinkLinkServer.Domain;
using KinkLinkServer.Domain.Interfaces;
using KinkLinkServer.Services;
using Microsoft.AspNetCore.SignalR;

namespace KinkLinkServer.Services;

public interface INotificationService
{
    Task<bool> TryNotifyOnlineTargetAsync(
        string targetFriendCode,
        InteractionPayload? payload,
        IHubCallerClients clients
    );

    Task NotifyLockeeOfLockUpdateAsync(
        string lockeeFriendCode,
        Func<string, Task<List<LockInfoDto>>> getLocksFunc,
        IHubCallerClients clients
    );

    Task NotifyLockerOfLockUpdateAsync(
        string lockerFriendCode,
        Func<string, Task<List<LockInfoDto>>> getLocksFunc,
        IHubCallerClients clients
    );

    Task NotifyTargetOfStateChangeAsync(
        string targetFriendCode,
        Func<string, Task<object?>> getStateFunc,
        IHubCallerClients clients
    );

    Task PushStateToAllFriendsAsync(
        string friendCode,
        Func<string, Task<List<TwoWayPermissions>>> getPermissionsFunc,
        Func<string, Task<object?>> getStateFunc,
        IHubCallerClients clients
    );
}

public class NotificationService(
    IPresenceService presenceService,
    ILogger<NotificationService> logger
) : INotificationService
{
    public async Task<bool> TryNotifyOnlineTargetAsync(
        string targetFriendCode,
        InteractionPayload? payload,
        IHubCallerClients clients
    )
    {
        var target = presenceService.TryGet(targetFriendCode);
        if (target == null)
        {
            logger.LogDebug(
                "[NotificationService] Target {Target} is offline, skipping notification",
                targetFriendCode
            );
            return false;
        }

        logger.LogDebug(
            "[NotificationService] Target {Target} is online, notification sent",
            targetFriendCode
        );
        return true;
    }

    public async Task NotifyLockeeOfLockUpdateAsync(
        string lockeeFriendCode,
        Func<string, Task<List<LockInfoDto>>> getLocksFunc,
        IHubCallerClients clients
    )
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
                "[NotificationService] Failed to notify lockee {Lockee} of lock update: {Error}",
                lockeeFriendCode,
                e.Message
            );
        }
    }

    public async Task NotifyLockerOfLockUpdateAsync(
        string lockerFriendCode,
        Func<string, Task<List<LockInfoDto>>> getLocksFunc,
        IHubCallerClients clients
    )
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
                "[NotificationService] Failed to notify locker {Locker} of lock update: {Error}",
                lockerFriendCode,
                e.Message
            );
        }
    }

    public async Task NotifyTargetOfStateChangeAsync(
        string targetFriendCode,
        Func<string, Task<object?>> getStateFunc,
        IHubCallerClients clients
    )
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
                "[NotificationService] Failed to notify target {Target} of state change: {Error}",
                targetFriendCode,
                e.Message
            );
        }
    }

    public async Task PushStateToAllFriendsAsync(
        string friendCode,
        Func<string, Task<List<TwoWayPermissions>>> getPermissionsFunc,
        Func<string, Task<object?>> getStateFunc,
        IHubCallerClients clients
    )
    {
        try
        {
            var allPermissions = await getPermissionsFunc(friendCode);
            if (allPermissions.Count == 0)
                return;

            var state = await getStateFunc(friendCode);
            if (state == null)
            {
                logger.LogWarning(
                    "[NotificationService] Could not build state for {FriendCode}",
                    friendCode
                );
                return;
            }

            foreach (var perm in allPermissions)
            {
                if (presenceService.TryGet(perm.TargetUID) is { } presence)
                {
                    await clients
                        .Client(presence.ConnectionId)
                        .SendAsync(HubMethod.SyncPairState, state);
                }
            }

            logger.LogDebug("[NotificationService] Pushed {FriendCode} state to all friends", friendCode);
        }
        catch (Exception e)
        {
            logger.LogError(
                e,
                "[NotificationService] Failed to push {FriendCode} state to friends",
                friendCode
            );
        }
    }
}
