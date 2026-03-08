using KinkLinkCommon.Domain.Network;
using KinkLinkServer.Domain;
using KinkLinkServer.Domain.Interfaces;
using KinkLinkServer.SignalR.Handlers;
using KinkLinkServer.Services;
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

    // Managers
    OnlineStatusUpdateHandler onlineStatusUpdateHandler,

    // Handlers
    AddFriendHandler addFriendHandler,
    ChatHandler chatHandler,
    CustomizePlusHandler customizePlusHandler,
    EmoteHandler emoteHandler,
    GetAccountDataHandler getAccountDataHandler,
    HonorificHandler honorificHandler,
    MoodlesHandler moodlesHandler,
    RemoveFriendHandler removeFriendHandler,
    SpeakHandler speakHandler,
    UpdateFriendHandler updateFriendHandler,

    // Logger
    ILogger<PrimaryHub> logger) : Hub
{
    /// <summary>
    ///     Friend Code obtained from authenticated token claims
    /// </summary>
    private string FriendCode => Context.User?.FindFirst(AuthClaimTypes.Uid)?.Value ?? throw new Exception("FriendCode not present in claims");

    /// <summary>
    ///     Handles when a client connects to hub
    /// </summary>
    public override async Task OnConnectedAsync()
    {
        metricsService.IncrementSignalRConnection("connect");
        await onlineStatusUpdateHandler.Handle(FriendCode, true, Clients);
        await base.OnConnectedAsync();
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
        Both = Console | Disk
    }
}
