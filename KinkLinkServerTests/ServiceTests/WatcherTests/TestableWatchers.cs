using KinkLinkServer.Domain;
using KinkLinkServer.Domain.Interfaces;
using KinkLinkServer.Services;
using KinkLinkServer.SignalR.Handlers;
using KinkLinkServer.SignalR.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace KinkLinkServerTests.ServiceTests.WatcherTests;

public class TestableWardrobeWatcher : WardrobeWatcher
{
    private readonly string? _mockUid;

    public TestableWardrobeWatcher(
        Configuration config,
        IHubContext<PrimaryHub> hubContext,
        IPresenceService presenceService,
        KinkLinkProfilesService profilesService,
        ILogger<WardrobeWatcher> logger,
        string? mockUid = null)
        : base(config, hubContext, presenceService, profilesService, logger)
    {
        _mockUid = mockUid;
    }

    public Task CallHandleNotificationAsync(string channel, string payload)
        => HandleNotificationAsync(channel, payload);

    protected override Task<string?> GetUidByProfileIdAsync(int profileId)
        => Task.FromResult(_mockUid);
}

public class TestableActiveWardrobeWatcher : ActiveWardrobeWatcher
{
    private readonly string? _mockUid;

    public TestableActiveWardrobeWatcher(
        Configuration config,
        IHubContext<PrimaryHub> hubContext,
        IPresenceService presenceService,
        KinkLinkProfilesService profilesService,
        WardrobeDataService wardrobeData,
        LocksHandler locksHandler,
        PermissionsService permissionsService,
        ILogger<ActiveWardrobeWatcher> logger,
        string? mockUid = null)
        : base(config, hubContext, presenceService, profilesService, wardrobeData, locksHandler, permissionsService, logger)
    {
        _mockUid = mockUid;
    }

    public Task CallHandleNotificationAsync(string channel, string payload)
        => HandleNotificationAsync(channel, payload);

    protected override Task<string?> GetUidByProfileIdAsync(int profileId)
        => Task.FromResult(_mockUid);
}

public class TestableLockWatcher : LockWatcher
{
    private readonly string? _mockUid;

    public TestableLockWatcher(
        Configuration config,
        IHubContext<PrimaryHub> hubContext,
        IPresenceService presenceService,
        KinkLinkProfilesService profilesService,
        LocksHandler locksHandler,
        PermissionsService permissionsService,
        WardrobeDataService wardrobeData,
        ILogger<LockWatcher> logger,
        string? mockUid = null)
        : base(config, hubContext, presenceService, profilesService, locksHandler, permissionsService, wardrobeData, logger)
    {
        _mockUid = mockUid;
    }

    public Task CallHandleNotificationAsync(string channel, string payload)
        => HandleNotificationAsync(channel, payload);

    protected override Task<string?> GetUidByProfileIdAsync(int profileId)
        => Task.FromResult(_mockUid);
}
