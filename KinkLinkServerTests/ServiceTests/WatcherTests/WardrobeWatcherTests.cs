using KinkLinkCommon.Domain.Network;
using KinkLinkServer.Domain;
using KinkLinkServer.Domain.Interfaces;
using KinkLinkServer.Services;
using KinkLinkServer.SignalR.Hubs;
using KinkLinkServerTests.TestInfrastructure;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Moq;

namespace KinkLinkServerTests.ServiceTests.WatcherTests;

[Collection("DatabaseCollection")]
public class WardrobeWatcherTests : WatcherTestBase
{
    public WardrobeWatcherTests(TestDatabaseFixture fixture) : base(fixture) { }

    [Fact]
    public async Task HandleNotificationAsync_ValidPayload_UserOnline_SendsWardrobeLibraryChanged()
    {
        const string uid = "WARDROBE1";
        const int profileId = 3001;
        const string connectionId = "conn-wardrobe-1";

        PresenceService.Add(uid, CreatePresence(connectionId));

        var profilesService = new KinkLinkProfilesService(Config, Metrics,
            LogFactory.CreateLogger<KinkLinkProfilesService>());

        var watcher = new TestableWardrobeWatcher(
            Config, HubContextMock.Object, PresenceService, profilesService,
            LogFactory.CreateLogger<WardrobeWatcher>(), uid);

        await watcher.CallHandleNotificationAsync("wardrobe_changed",
            $"{{\"profile_id\":{profileId},\"action\":\"INSERT\"}}");

        GetClientProxy(connectionId).Verify(p => p.SendCoreAsync(
            HubMethod.WardrobeLibraryChanged,
            It.IsAny<object?[]>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleNotificationAsync_ValidPayload_UserOffline_DoesNotSend()
    {
        const string uid = "WARDROBE2";
        const int profileId = 3002;

        // User is offline (no presence added)

        var profilesService = new KinkLinkProfilesService(Config, Metrics,
            LogFactory.CreateLogger<KinkLinkProfilesService>());

        var watcher = new TestableWardrobeWatcher(
            Config, HubContextMock.Object, PresenceService, profilesService,
            LogFactory.CreateLogger<WardrobeWatcher>(), uid);

        await watcher.CallHandleNotificationAsync("wardrobe_changed",
            $"{{\"profile_id\":{profileId},\"action\":\"UPDATE\"}}");

        Assert.Empty(ClientProxyMocks);
    }

    [Fact]
    public async Task HandleNotificationAsync_InvalidPayload_DoesNotThrow()
    {
        var profilesService = new KinkLinkProfilesService(Config, Metrics,
            LogFactory.CreateLogger<KinkLinkProfilesService>());

        var watcher = new TestableWardrobeWatcher(
            Config, HubContextMock.Object, PresenceService, profilesService,
            LogFactory.CreateLogger<WardrobeWatcher>(), null);

        var exception = await Record.ExceptionAsync(() =>
            watcher.CallHandleNotificationAsync("wardrobe_changed", "not-json"));

        Assert.Null(exception);
    }

    [Fact]
    public async Task HandleNotificationAsync_ProfileNotFound_DoesNotSend()
    {
        var profilesService = new KinkLinkProfilesService(Config, Metrics,
            LogFactory.CreateLogger<KinkLinkProfilesService>());

        var watcher = new TestableWardrobeWatcher(
            Config, HubContextMock.Object, PresenceService, profilesService,
            LogFactory.CreateLogger<WardrobeWatcher>(), null);

        await watcher.CallHandleNotificationAsync("wardrobe_changed",
            "{\"profile_id\":99999,\"action\":\"DELETE\"}");

        Assert.Empty(ClientProxyMocks);
    }
}
