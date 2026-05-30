using KinkLinkCommon.Domain;
using KinkLinkCommon.Domain.Enums;
using KinkLinkCommon.Domain.Network;
using KinkLinkCommon.Domain.Network.AddFriend;
using KinkLinkCommon.Domain.Network.SyncOnlineStatus;
using KinkLinkCommon.Domain.Network.SyncPairState;
using KinkLinkCommon.Domain.Wardrobe;
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
    KinkLinkProfilesService profilesService,
    LocksHandler locksHandler,
    IActiveWardrobeStateService activeWardrobeStateService,
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
        logger.LogInformation(
            "AddFriend request: {From} -> {To}",
            userUID,
            request.TargetFriendCode
        );

        // Adding a pair/friend is tracked by creating the relevant permissions in the database.
        var result = await permissionsService.CreatePermissions(userUID, request.TargetFriendCode);

        logger.LogDebug(
            "AddFriend result: {From} -> {To} = {Result}",
            userUID,
            request.TargetFriendCode,
            result
        );

        // Map the result
        var code = result switch
        {
            DBPairResult.Success => PairRequestResult.Success,
            DBPairResult.PairCreated => PairRequestResult.Success,
            DBPairResult.OnesidedPairExists => PairRequestResult.Pending,
            DBPairResult.Paired => PairRequestResult.AlreadyFriends,
            DBPairResult.PairUIDDoesNotExist => PairRequestResult.NoSuchFriendCode,
            _ => PairRequestResult.Unknown,
        };

        if (code is not PairRequestResult.Success)
        {
            return code is PairRequestResult.Pending
                ? new AddFriendResponse(code, FriendOnlineStatus.Pending)
                : new AddFriendResponse(code, FriendOnlineStatus.Offline);
        }

        var requesterPerms = await permissionsService.GetPermissions(
            userUID,
            request.TargetFriendCode
        );
        var targetPerms = await permissionsService.GetPermissions(
            request.TargetFriendCode,
            userUID
        );

        var requesterPermissions = requesterPerms?.PermissionsGrantedTo ?? new UserPermissions();
        var targetPermissions = targetPerms?.PermissionsGrantedTo ?? new UserPermissions();

        var targetOnline = presenceService.TryGet(request.TargetFriendCode) is not null;
        var targetStatus = targetOnline ? FriendOnlineStatus.Online : FriendOnlineStatus.Offline;

        try
        {
            var syncToCaller = new SyncOnlineStatusCommand(
                request.TargetFriendCode,
                targetStatus,
                targetPermissions
            );
            await clients.Caller.SendAsync(HubMethod.SyncOnlineStatus, syncToCaller);
        }
        catch (Exception e)
        {
            logger.LogError(
                "Syncing online status to requester {Caller} -> {Target} failed, {Error}",
                userUID,
                request.TargetFriendCode,
                e
            );
        }

        if (presenceService.TryGet(request.TargetFriendCode) is not { } target)
            return new AddFriendResponse(code, FriendOnlineStatus.Offline);

        try
        {
            var syncToTarget = new SyncOnlineStatusCommand(
                userUID,
                FriendOnlineStatus.Online,
                requesterPermissions
            );
            await clients
                .Client(target.ConnectionId)
                .SendAsync(HubMethod.SyncOnlineStatus, syncToTarget);
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

        try
        {
            var myProfileId = await profilesService.GetProfileIdFromUidAsync(userUID);
            if (myProfileId != null)
            {
                var myLocks = await locksHandler.GetAllLocksForUserAsync(userUID);
                var myWardrobe = await activeWardrobeStateService.GetPairWardrobeStateAsync(
                    myProfileId.Value
                );
                var wardrobeWithLocks = PairWardrobeStateDto.PopulateLockIds(myWardrobe, myLocks);

                var pairState = new SyncPairStateCommand(
                    userUID,
                    requesterPermissions,
                    wardrobeWithLocks,
                    myLocks
                );
                await clients
                    .Client(target.ConnectionId)
                    .SendAsync(HubMethod.SyncPairState, pairState);
            }
        }
        catch (Exception e)
        {
            logger.LogError(
                "Syncing pair state {Sender} -> {Target} failed, {Error}",
                userUID,
                request.TargetFriendCode,
                e
            );
        }

        logger.LogDebug(
            "AddFriend response: {From} -> {To} = {Code}, onlineStatus: {Status}",
            userUID,
            request.TargetFriendCode,
            code,
            FriendOnlineStatus.Online
        );
        return new AddFriendResponse(code, FriendOnlineStatus.Online);
    }
}
