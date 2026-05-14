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
public class WardrobeWatcherTests : WatcherIntegrationTestBase
{
    public WardrobeWatcherTests(TestDatabaseFixture fixture) : base(fixture) { }

    [Fact]
    public async Task HandleNotificationAsync_ValidPayload_UserOnline_SendsWardrobeLibraryChanged()
    {
        await Fixture.ResetDatabaseAsync();
        var (_, profileId, uid) = await CreateTestUserWithProfileAsync(111111111111111111, "WARDROBE1");
        var connectionId = "conn-wardrobe-1";

        PresenceMock
            .Setup(p => p.TryGet(uid))
            .Returns(CreatePresence(connectionId));

        var watcher = new TestableWardrobeWatcher(
            Config, HubContextMock.Object, PresenceMock.Object, ProfilesService,
            LogFactory.CreateLogger<WardrobeWatcher>(), uid);

        await watcher.CallHandleNotificationAsync("wardrobe_changed",
            $"{{\"profile_id\":{profileId},\"action\":\"INSERT\"}}");

        ClientProxyMock.Verify(p => p.SendCoreAsync(
            HubMethod.WardrobeLibraryChanged,
            It.IsAny<object?[]>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleNotificationAsync_ValidPayload_UserOffline_DoesNotSend()
    {
        await Fixture.ResetDatabaseAsync();
        var (_, profileId, uid) = await CreateTestUserWithProfileAsync(222222222222222222, "WARDROBE2");

        PresenceMock
            .Setup(p => p.TryGet(uid))
            .Returns((Presence?)null);

        var watcher = new TestableWardrobeWatcher(
            Config, HubContextMock.Object, PresenceMock.Object, ProfilesService,
            LogFactory.CreateLogger<WardrobeWatcher>(), uid);

        await watcher.CallHandleNotificationAsync("wardrobe_changed",
            $"{{\"profile_id\":{profileId},\"action\":\"UPDATE\"}}");

        ClientProxyMock.Verify(p => p.SendCoreAsync(
            It.IsAny<string>(),
            It.IsAny<object?[]>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleNotificationAsync_InvalidPayload_DoesNotThrow()
    {
        await Fixture.ResetDatabaseAsync();

        var watcher = new TestableWardrobeWatcher(
            Config, HubContextMock.Object, PresenceMock.Object, ProfilesService,
            LogFactory.CreateLogger<WardrobeWatcher>(), null);

        var exception = await Record.ExceptionAsync(() =>
            watcher.CallHandleNotificationAsync("wardrobe_changed", "not-json"));

        Assert.Null(exception);
    }

    [Fact]
    public async Task HandleNotificationAsync_ProfileNotFound_DoesNotSend()
    {
        await Fixture.ResetDatabaseAsync();

        PresenceMock
            .Setup(p => p.TryGet(It.IsAny<string>()))
            .Returns((Presence?)null);

        var watcher = new TestableWardrobeWatcher(
            Config, HubContextMock.Object, PresenceMock.Object, ProfilesService,
            LogFactory.CreateLogger<WardrobeWatcher>(), null);

        await watcher.CallHandleNotificationAsync("wardrobe_changed",
            "{\"profile_id\":99999,\"action\":\"DELETE\"}");

        ClientProxyMock.Verify(p => p.SendCoreAsync(
            It.IsAny<string>(),
            It.IsAny<object?[]>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }
}
