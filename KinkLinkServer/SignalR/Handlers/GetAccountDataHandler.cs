using KinkLinkCommon;
using KinkLinkCommon.Domain;
using KinkLinkCommon.Domain.Enums;
using KinkLinkCommon.Domain.Network.GetAccountData;
using KinkLinkCommon.Domain.Network.PairInteractions;
using KinkLinkCommon.Domain.Wardrobe;
using KinkLinkServer.Domain.Interfaces;
using KinkLinkServer.Services;
using Microsoft.Extensions.Logging;

namespace KinkLinkServer.SignalR.Handlers;

/// <summary>
///     Handles the logic for fulfilling a <see cref="GetAccountDataRequest"/>
/// </summary>
public class GetAccountDataHandler(
    PermissionsService permissionsService,
    IPresenceService presenceService,
    KinkLinkProfilesService profilesService,
    LocksHandler locksHandler,
    WardrobeDataService wardrobeDataService,
    ILogger<GetAccountDataHandler> logger
)
{
    /// <summary>
    ///     Handles the request
    /// </summary>
    public async Task<GetAccountDataResponse> Handle(
        string friendCode,
        string connectionId,
        GetAccountDataRequest request
    )
    {
        var presence = new Presence(connectionId, request.CharacterName, request.CharacterWorld);
        presenceService.Add(friendCode, presence);

        var relationshipResults = new List<FriendRelationship>();
        var permissions = await permissionsService.GetAllPermissions(friendCode);
        Dictionary<string, QueryPairStateResponse> pairStates =
            new Dictionary<string, QueryPairStateResponse>();
        foreach (var permission in permissions)
        {
            var online =
                permission.PermissionsGrantedBy is null ? FriendOnlineStatus.Pending
                : presenceService.TryGet(permission.TargetUID) is null ? FriendOnlineStatus.Offline
                : FriendOnlineStatus.Online;

            relationshipResults.Add(
                new FriendRelationship(
                    permission.TargetUID,
                    online,
                    permission.PermissionsGrantedTo,
                    permission.PermissionsGrantedBy
                )
            );

            var targetProfileId = await profilesService.GetIdFromUidAsync(permission.TargetUID);
            var locks = await locksHandler.GetAllLocksForUserAsync(permission.TargetUID);
            logger.LogInformation(
                "[GetAccountDataHandler] Target={Target}, Locks count={LockCount}",
                permission.TargetUID,
                locks.Count
            );
            var wardrobe = await wardrobeDataService.GetPairWardrobeItemsAsync(
                targetProfileId.Value
            );
            var wardrobeWithLocks = PairWardrobeStateDto.PopulateLockIds(wardrobe, locks, logger);
            pairStates[permission.TargetUID] = new QueryPairStateResponse(
                permission.TargetUID,
                permission.PermissionsGrantedTo,
                wardrobeWithLocks,
                locks
            );
        }

        return new GetAccountDataResponse(
            GetAccountDataEc.Success,
            friendCode,
            relationshipResults,
            pairStates
        );
    }
}
