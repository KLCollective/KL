using KinkLinkCommon.Domain;
using KinkLinkCommon.Domain.Network;
using KinkLinkCommon.Domain.Network.PairInteractions;
using KinkLinkCommon.Domain.Network.SyncPairState;
using KinkLinkCommon.Domain.Wardrobe;
using KinkLinkServer.Domain;
using KinkLinkServer.Domain.Interfaces;
using KinkLinkServer.Services;
using KinkLinkServer.SignalR.Handlers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace KinkLinkServer.SignalR.Hubs;

[Authorize]
public partial class PrimaryHub(
    // Services
    IRequestLoggingService requestLoggingService,
    IMetricsService metricsService,
    KinkLinkProfilesService profilesService,
    WardrobeDataService wardrobeDataService,
    PermissionsService permissionsService,
    IPresenceService presenceService,
    // Managers
    OnlineStatusUpdateHandler onlineStatusUpdateHandler,
    // Handlers
    AddFriendHandler addFriendHandler,
    ChatHandler chatHandler,
    CustomizePlusHandler customizePlusHandler,
    EmoteHandler emoteHandler,
    GetAccountDataHandler getAccountDataHandler,
    HonorificHandler honorificHandler,
    LocksHandler locksHandler,
    MoodlesHandler moodlesHandler,
    PairInteractionsHandler pairInteractionsHandler,
    RemoveFriendHandler removeFriendHandler,
    SpeakHandler speakHandler,
    UpdateFriendHandler updateFriendHandler,
    // Logger
    ILogger<PrimaryHub> logger
) : Hub
{
    private readonly PairInteractionsHandler _pairInteractionsHandler = pairInteractionsHandler;
    private readonly LocksHandler _locksHandler = locksHandler;

    /// <summary>
    ///     Friend Code obtained from authenticated token claims
    /// </summary>
    private string FriendCode =>
        Context.User?.FindFirst(AuthClaimTypes.Uid)?.Value
        ?? throw new Exception("FriendCode not present in claims");

    /// <summary>
    ///     Handles when a client connects to hub
    /// </summary>
    public override async Task OnConnectedAsync()
    {
        metricsService.IncrementSignalRConnection("connect");
        await onlineStatusUpdateHandler.Handle(FriendCode, true, Clients);

        await base.OnConnectedAsync();
    }

    [HubMethodName(HubMethod.RequestInitialState)]
    public async Task<ActionResult<List<QueryPairStateResponse>>> RequestInitialState()
    {
        // Push to friends
        await PushClientStateToFriendsAsync();
        // REturn the complete initial state for us _including_ out friends status
        return await PushInitialStateToClientAsync();
    }

    /// <summary>
    ///     Handles when a client disconnects from the hub
    /// </summary>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        metricsService.IncrementSignalRConnection("disconnect");
        await onlineStatusUpdateHandler.Handle(FriendCode, false, Clients);
        await base.OnDisconnectedAsync(exception);
    }

    // Pushes initial state to User (such as when initially logged in)
    private async Task<ActionResult<List<QueryPairStateResponse>>> PushInitialStateToClientAsync()
    {
        List<QueryPairStateResponse> friendsState = new List<QueryPairStateResponse>();
        try
        {
            var allPairsWithPerms = await permissionsService.GetAllPermissions(FriendCode);
            foreach (var perm in allPairsWithPerms)
            {
                var profileId = await profilesService.GetIdFromUidAsync(perm.TargetUID);
                // If invalid no need to try to query, it'll fail anyways.
                if (profileId == null)
                    continue;

                var locks = await locksHandler.GetAllLocksForUserAsync(perm.TargetUID);
                logger.LogInformation(
                    "[PushInitialState] Target={Target}, Locks count={LockCount}",
                    perm.TargetUID,
                    locks.Count
                );
                foreach (var l in locks)
                {
                    logger.LogInformation(
                        "[PushInitialState] Lock: LockID={LockId}, LockeeID={LockeeId}, LockerID={LockerId}",
                        l.LockID,
                        l.LockeeID,
                        l.LockerID
                    );
                }
                var wardrobe = await wardrobeDataService.GetPairWardrobeItemsAsync(profileId.Value);
                var wardrobeWithLocks = PairWardrobeStateDto.PopulateLockIds(
                    wardrobe,
                    locks,
                    logger
                );

                friendsState.Add(
                    new QueryPairStateResponse(
                        perm.TargetUID,
                        perm.PermissionsGrantedTo,
                        wardrobeWithLocks,
                        locks
                    )
                );
            }
            logger.LogDebug("Pushed initial state to {FriendCode}", FriendCode);
            return ActionResultBuilder.Ok(friendsState);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to push initial state to {FriendCode}", FriendCode);
            return ActionResultBuilder.Fail<List<QueryPairStateResponse>>(
                KinkLinkCommon.Domain.Enums.ActionResultEc.Unknown
            );
        }
    }

    private async Task PushClientStateToFriendsAsync()
    {
        try
        {
            var myProfileId = await profilesService.GetIdFromUidAsync(FriendCode);
            if (myProfileId == null)
                return;

            var myLocks = await locksHandler.GetAllLocksForUserAsync(FriendCode);
            var myWardrobe = await wardrobeDataService.GetPairWardrobeItemsAsync(myProfileId.Value);
            var wardrobeWithLocks = PairWardrobeStateDto.PopulateLockIds(
                myWardrobe,
                myLocks,
                logger
            );

            var allPermissions = await permissionsService.GetAllPermissions(FriendCode);

            foreach (var perm in allPermissions)
            {
                if (presenceService.TryGet(perm.TargetUID) is { } presence)
                {
                    await Clients
                        .Client(presence.ConnectionId)
                        .SendAsync(
                            HubMethod.SyncPairState,
                            new QueryPairStateResponse(
                                FriendCode,
                                perm.PermissionsGrantedTo,
                                wardrobeWithLocks,
                                myLocks
                            )
                        );
                }
            }
            logger.LogDebug("Pushed {FriendCode} state to all friends", FriendCode);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to push {FriendCode} state to friends", FriendCode);
        }
    }

    /// <summary>
    ///     Special logging instruction for either console or file
    /// </summary>
    private void LogWithBehavior(string message, LogMode mode)
    {
        if ((mode & LogMode.Console) == LogMode.Console)
            logger.LogInformation("{Message}", message);

        if ((mode & LogMode.Disk) == LogMode.Disk)
            requestLoggingService.Log(message);
    }

    [Flags]
    private enum LogMode
    {
        Console = 1 << 0,
        Disk = 1 << 1,
        Both = Console | Disk,
    }
}
