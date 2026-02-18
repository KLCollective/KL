using KinkLinkCommon.Domain;
using KinkLinkCommon.Domain.Enums;
using KinkLinkCommon.Domain.Network;
using KinkLinkCommon.Domain.Network.AddFriend;
using KinkLinkCommon.Domain.Network.SyncOnlineStatus;
using KinkLinkServer.Domain.Interfaces;
using KinkLinkServer.Services;
using Microsoft.AspNetCore.SignalR;

namespace KinkLinkServer.SignalR.Handlers;

/// <summary>
///     Handles the logic for fulfilling a <see cref="AddFriendRequest"/>
/// </summary>
public class AddFriendHandler(
    IPresenceService presenceService,
    PermissionsService permissionsService,
    ILogger<AddFriendHandler> logger
)
{
    /// <summary>
    ///     Handles the request
    /// </summary>
    public async Task<AddFriendResponse> Handle(
        string userUID,
        AddFriendRequest request,
        IHubCallerClients clients
    )
    {
        // Adding a pair/friend is tracked by creating the relevant permissions in the database.
        var result = await permissionsService.CreatePermissions(userUID, request.TargetFriendCode);

        // Map the result
        var code = result switch
        {
            DBPairResult.Success => PairRequestResult.Success,
            DBPairResult.OnesidedPairExists => PairRequestResult.Pending,
            DBPairResult.Paired => PairRequestResult.AlreadyFriends,
            DBPairResult.PairUIDDoesNotExist => PairRequestResult.NoSuchFriendCode,
            _ => PairRequestResult.Unknown,
        };

        // Only update other person if it is a success
        if (code is not PairRequestResult.Success)
        {
            return code is PairRequestResult.Pending
                ? new AddFriendResponse(code, FriendOnlineStatus.Pending)
                : new AddFriendResponse(code, FriendOnlineStatus.Offline);
        }

        // Only update if they are online
        if (presenceService.TryGet(request.TargetFriendCode) is not { } target)
            return new AddFriendResponse(code, FriendOnlineStatus.Offline);

        try
        {
            // Try to send an update to that client that we've accepted the friend request
            var sync = new SyncOnlineStatusCommand(
                userUID,
                FriendOnlineStatus.Online,
                new UserPermissions()
            );
            await clients.Client(target.ConnectionId).SendAsync(HubMethod.SyncOnlineStatus, sync);
        }
        catch (Exception e)
        {
            logger.LogError(
                "Syncing online status {Sender} -> {Target} failed, {Error}",
                userUID,
                request.TargetFriendCode,
                e
            );
        }

        return new AddFriendResponse(code, FriendOnlineStatus.Online);
    }
}
