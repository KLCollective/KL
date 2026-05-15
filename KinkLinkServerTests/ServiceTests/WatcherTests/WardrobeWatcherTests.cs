using KinkLinkCommon.Domain.Network;
using KinkLinkServer.Domain;
using KinkLinkServer.Domain.Interfaces;
using KinkLinkServer.Services;
using KinkLinkServer.SignalR.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Moq;

namespace KinkLinkServerTests.ServiceTests.WatcherTests;

public class WardrobeWatcherTests : WatcherTestBase
{
    [Fact]
    public async Task HandleNotificationAsync_ValidPayload_UserOnline_SendsWardrobeLibraryChanged()
    {
        const string uid = "WARDROBE1";
        const int profileId = 3001;
        const string connectionId = "conn-wardrobe-1";

        PresenceMock
            .Setup(p => p.TryGet(uid))
            .Returns(CreatePresence(connectionId));

        var watcher = new TestableWardrobeWatcher(
            Config, HubContextMock.Object, PresenceMock.Object, ProfilesService,
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

        PresenceMock
            .Setup(p => p.TryGet(uid))
            .Returns((Presence?)null);

        var watcher = new TestableWardrobeWatcher(
            Config, HubContextMock.Object, PresenceMock.Object, ProfilesService,
            LogFactory.CreateLogger<WardrobeWatcher>(), uid);

        await watcher.CallHandleNotificationAsync("wardrobe_changed",
            $"{{\"profile_id\":{profileId},\"action\":\"UPDATE\"}}");

        Assert.Empty(ClientProxyMocks);
    }

    [Fact]
    public async Task HandleNotificationAsync_InvalidPayload_DoesNotThrow()
    {
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
        var watcher = new TestableWardrobeWatcher(
            Config, HubContextMock.Object, PresenceMock.Object, ProfilesService,
            LogFactory.CreateLogger<WardrobeWatcher>(), null);

        await watcher.CallHandleNotificationAsync("wardrobe_changed",
            "{\"profile_id\":99999,\"action\":\"DELETE\"}");

        Assert.Empty(ClientProxyMocks);
    }
}
