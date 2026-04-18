using System.Diagnostics;
using KinkLinkCommon;
using KinkLinkCommon.Domain;
using KinkLinkCommon.Domain.Enums;
using KinkLinkCommon.Domain.Network;
using KinkLinkCommon.Domain.Network.PairInteractions;
using KinkLinkCommon.Domain.Network.Profile;
using KinkLinkCommon.Domain.Network.ProfileConfig;
using KinkLinkCommon.Domain.Network.SyncPairState;
using KinkLinkCommon.Domain.Wardrobe;
using KinkLinkServer.Domain;
using KinkLinkServer.Domain.Interfaces;
using KinkLinkServer.Services;
using KinkLinkServer.SignalR.Handlers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace KinkLinkServer.SignalR.Hubs;

[Authorize]
public partial class PrimaryHub(
    // Services
    IRequestLoggingService requestLoggingService,
    IMetricsService metricsService,
    KinkLinkProfilesService profilesService,
    KinkLinkProfileConfigService profileConfigService,
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
    private static int _activeConnections;

    /// <summary>
    ///     Friend Code obtained from authenticated token claims
    /// </summary>
    private string FriendCode =>
        Context.User?.FindFirst(AuthClaimTypes.Uid)?.Value
        ?? throw new Exception("FriendCode not present in claims");

    /// <summary>
    ///     Handles when a client connects to the hub
    /// </summary>
    public override async Task OnConnectedAsync()
    {
        Interlocked.Increment(ref _activeConnections);
        metricsService.SetActiveConnections(_activeConnections);
        logger.LogInformation("[SignalR] Client connected: {FriendCode}", FriendCode);
        metricsService.IncrementSignalRConnection("connect");
        await onlineStatusUpdateHandler.Handle(FriendCode, true, Clients);

        await base.OnConnectedAsync();
    }

    [HubMethodName(HubMethod.RequestInitialState)]
    public async Task<ActionResult<List<QueryPairStateResponse>>> RequestInitialState()
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            logger.LogInformation("[SignalR] RequestInitialState: {FriendCode}", FriendCode);
            // Push to friends
            await PushClientStateToFriendsAsync();
            // REturn the complete initial state for us _including_ out friends status
            return await PushInitialStateToClientAsync();
        }
        finally
        {
            stopwatch.Stop();
            metricsService.IncrementSignalRMessage("RequestInitialState", true);
            metricsService.RecordSignalRMessageDuration(
                "RequestInitialState",
                stopwatch.ElapsedMilliseconds
            );
        }
    }

    /// <summary>
    ///     Handles when a client disconnects from the hub
    /// </summary>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        Interlocked.Decrement(ref _activeConnections);
        metricsService.SetActiveConnections(_activeConnections);
        logger.LogInformation(
            "[SignalR] Client disconnected: {FriendCode}, Exception: {Exception}",
            FriendCode,
            exception?.Message
        );
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

    [HubMethodName(HubMethod.GetProfile)]
    public async Task<ActionResult<KinkLinkProfile>> GetProfile(string uid)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            logger.LogTrace("[SignalR] GetProfile: {FriendCode} -> {Uid}", FriendCode, uid);
            if (!await profilesService.ExistsAsync(FriendCode))
                return ActionResultBuilder.Fail<KinkLinkProfile>(ActionResultEc.Unknown);

            var profile = await profilesService.GetProfileByUidAsync(uid);
            return ActionResultBuilder.Ok(profile!);
        }
        finally
        {
            stopwatch.Stop();
            metricsService.IncrementSignalRMessage("GetProfile", true);
            metricsService.RecordSignalRMessageDuration(
                "GetProfile",
                stopwatch.ElapsedMilliseconds
            );
        }
    }

    [HubMethodName(HubMethod.UpdateProfile)]
    public async Task<ActionResult<KinkLinkProfile>> UpdateProfile(UpdateProfileRequest request)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            logger.LogTrace("[SignalR] UpdateProfile: {FriendCode}", FriendCode);
            if (!await profilesService.ExistsAsync(FriendCode))
                return ActionResultBuilder.Fail<KinkLinkProfile>(ActionResultEc.Unknown);

            var profile = await profilesService.UpdateDetailsByUidAsync(
                FriendCode,
                request.Title,
                request.Alias ?? string.Empty,
                request.ChatRole ?? string.Empty,
                request.Description ?? string.Empty
            );

            if (profile is null)
                return ActionResultBuilder.Fail<KinkLinkProfile>(ActionResultEc.Unknown);

            return ActionResultBuilder.Ok(profile);
        }
        finally
        {
            stopwatch.Stop();
            metricsService.IncrementSignalRMessage("UpdateProfile", true);
            metricsService.RecordSignalRMessageDuration(
                "UpdateProfile",
                stopwatch.ElapsedMilliseconds
            );
        }
    }

    [HubMethodName(HubMethod.GetProfileConfig)]
    public async Task<ActionResult<KinkLinkProfileConfig>> GetProfileConfig()
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            logger.LogTrace("[SignalR] GetProfileConfig: {FriendCode}", FriendCode);
            if (!await profilesService.ExistsAsync(FriendCode))
                return ActionResultBuilder.Fail<KinkLinkProfileConfig>(ActionResultEc.Unknown);

            var config = await profileConfigService.GetProfileConfigByUidAsync(FriendCode);
            return ActionResultBuilder.Ok(
                config ?? new KinkLinkProfileConfig(false, false, false, false)
            );
        }
        finally
        {
            stopwatch.Stop();
            metricsService.IncrementSignalRMessage("GetProfileConfig", true);
            metricsService.RecordSignalRMessageDuration(
                "GetProfileConfig",
                stopwatch.ElapsedMilliseconds
            );
        }
    }

    [HubMethodName(HubMethod.UpdateProfileConfig)]
    public async Task<ActionResult<KinkLinkProfileConfig>> UpdateProfileConfig(
        UpdateProfileConfigRequest request
    )
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            logger.LogTrace("[SignalR] UpdateProfileConfig: {FriendCode}", FriendCode);
            if (!await profilesService.ExistsAsync(request.Uid))
                return ActionResultBuilder.Fail<KinkLinkProfileConfig>(ActionResultEc.Unknown);

            var config = await profileConfigService.UpdateProfileConfigAsync(
                request.Uid,
                request.EnableGlamours,
                request.EnableGarbler,
                request.EnableGarblerChannels,
                request.EnableMoodles
            );

            if (config is null)
                return ActionResultBuilder.Fail<KinkLinkProfileConfig>(ActionResultEc.Unknown);

            return ActionResultBuilder.Ok(config);
        }
        finally
        {
            stopwatch.Stop();
            metricsService.IncrementSignalRMessage("UpdateProfileConfig", true);
            metricsService.RecordSignalRMessageDuration(
                "UpdateProfileConfig",
                stopwatch.ElapsedMilliseconds
            );
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
