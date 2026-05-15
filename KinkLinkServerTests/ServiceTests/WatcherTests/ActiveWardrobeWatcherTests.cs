using KinkLinkCommon.Domain;
using KinkLinkCommon.Domain.Network;
using KinkLinkCommon.Domain.Wardrobe;
using KinkLinkServer.Domain;
using KinkLinkServer.Domain.Interfaces;
using KinkLinkServer.Services;
using KinkLinkServer.SignalR.Handlers;
using KinkLinkServer.SignalR.Hubs;
using Microsoft.Extensions.Logging;
using Moq;

namespace KinkLinkServerTests.ServiceTests.WatcherTests;

public class ActiveWardrobeWatcherTests : WatcherTestBase
{
    [Fact]
    public async Task HandleNotificationAsync_UserOnlineWithStateAndFriend_SendsStateAndPairState()
    {
        const string uid = "ACTIVE1";
        const int profileId = 1001;

        PresenceMock
            .Setup(p => p.TryGet(uid))
            .Returns(CreatePresence("conn-1"));
        PresenceMock
            .Setup(p => p.TryGet("FRIEND1"))
            .Returns(CreatePresence("conn-friend-1"));

        var state = new WardrobeStateDto(null, null, null);

        var wardrobeDataMock = CreateWardrobeDataServiceMock();
        wardrobeDataMock.Setup(w => w.GetWardrobeStateAsync(profileId)).ReturnsAsync(state);
        wardrobeDataMock.Setup(w => w.GetPairWardrobeItemsAsync(profileId))
            .ReturnsAsync(new PairWardrobeStateDto(null, null));

        var locksHandlerMock = CreateLocksHandlerMock(
            wardrobeDataMock: wardrobeDataMock);
        locksHandlerMock.Setup(l => l.GetAllLocksForUserAsync(uid))
            .Returns(Task.FromResult(new List<LockInfoDto>()));

        var permissionsMock = CreatePermissionsServiceMock();
        permissionsMock.Setup(p => p.GetAllPermissions(uid))
            .ReturnsAsync(new List<TwoWayPermissions>
            {
                new(uid, "FRIEND1", new UserPermissions(), null)
            });

        var logger = LogFactory.CreateLogger<ActiveWardrobeWatcher>();
        var watcher = new TestableActiveWardrobeWatcher(
            Config, HubContextMock.Object, PresenceMock.Object,
            CreateProfilesServiceMock().Object,
            wardrobeDataMock.Object, locksHandlerMock.Object,
            permissionsMock.Object, logger, uid);

        await watcher.CallHandleNotificationAsync("activewardrobe_changed",
            $"{{\"profile_id\":{profileId}}}");

        GetClientProxy("conn-1").Verify(p => p.SendCoreAsync(
            HubMethod.SyncWardrobeState,
            It.Is<object?[]>(a => a[0] is WardrobeStateDto && (WardrobeStateDto)(a[0]!) == state),
            It.IsAny<CancellationToken>()), Times.Once);

        GetClientProxy("conn-friend-1").Verify(p => p.SendCoreAsync(
            HubMethod.SyncPairState,
            It.IsAny<object?[]>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleNotificationAsync_UserOnlineNoWardrobeState_DoesNotSendSyncWardrobeState()
    {
        const string uid = "ACTIVE2";
        const int profileId = 1002;

        PresenceMock
            .Setup(p => p.TryGet(uid))
            .Returns(CreatePresence("conn-2"));

        var wardrobeDataMock = CreateWardrobeDataServiceMock();
        wardrobeDataMock.Setup(w => w.GetWardrobeStateAsync(profileId)).ReturnsAsync((WardrobeStateDto?)null);
        wardrobeDataMock.Setup(w => w.GetPairWardrobeItemsAsync(profileId))
            .ReturnsAsync(new PairWardrobeStateDto(null, null));

        var locksHandlerMock = CreateLocksHandlerMock(wardrobeDataMock: wardrobeDataMock);
        locksHandlerMock.Setup(l => l.GetAllLocksForUserAsync(uid))
            .Returns(Task.FromResult(new List<LockInfoDto>()));

        var permissionsMock = CreatePermissionsServiceMock();
        permissionsMock.Setup(p => p.GetAllPermissions(uid))
            .ReturnsAsync(new List<TwoWayPermissions>
            {
                new(uid, "FRIEND2", new UserPermissions(), null)
            });

        PresenceMock
            .Setup(p => p.TryGet("FRIEND2"))
            .Returns(CreatePresence("conn-friend-2"));

        var logger = LogFactory.CreateLogger<ActiveWardrobeWatcher>();
        var watcher = new TestableActiveWardrobeWatcher(
            Config, HubContextMock.Object, PresenceMock.Object,
            CreateProfilesServiceMock().Object,
            wardrobeDataMock.Object, locksHandlerMock.Object,
            permissionsMock.Object, logger, uid);

        await watcher.CallHandleNotificationAsync("activewardrobe_changed",
            $"{{\"profile_id\":{profileId}}}");

        // No SyncWardrobeState was sent (no proxy created for conn-2)
        Assert.False(ClientProxyMocks.ContainsKey("conn-2"));

        GetClientProxy("conn-friend-2").Verify(p => p.SendCoreAsync(
            HubMethod.SyncPairState,
            It.IsAny<object?[]>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleNotificationAsync_UserOffline_SendsOnlyPairState()
    {
        const string uid = "ACTIVE3";
        const int profileId = 1003;

        PresenceMock
            .Setup(p => p.TryGet(uid))
            .Returns((Presence?)null);

        var wardrobeDataMock = CreateWardrobeDataServiceMock();
        wardrobeDataMock.Setup(w => w.GetPairWardrobeItemsAsync(profileId))
            .ReturnsAsync(new PairWardrobeStateDto(null, null));

        var locksHandlerMock = CreateLocksHandlerMock(wardrobeDataMock: wardrobeDataMock);
        locksHandlerMock.Setup(l => l.GetAllLocksForUserAsync(uid))
            .Returns(Task.FromResult(new List<LockInfoDto>()));

        var permissionsMock = CreatePermissionsServiceMock();
        permissionsMock.Setup(p => p.GetAllPermissions(uid))
            .ReturnsAsync(new List<TwoWayPermissions>
            {
                new(uid, "FRIEND3", new UserPermissions(), null)
            });

        PresenceMock
            .Setup(p => p.TryGet("FRIEND3"))
            .Returns(CreatePresence("conn-friend-3"));

        var logger = LogFactory.CreateLogger<ActiveWardrobeWatcher>();
        var watcher = new TestableActiveWardrobeWatcher(
            Config, HubContextMock.Object, PresenceMock.Object,
            CreateProfilesServiceMock().Object,
            wardrobeDataMock.Object, locksHandlerMock.Object,
            permissionsMock.Object, logger, uid);

        await watcher.CallHandleNotificationAsync("activewardrobe_changed",
            $"{{\"profile_id\":{profileId}}}");

        // No proxy for user (offline, no Client() call expected)
        Assert.False(ClientProxyMocks.ContainsKey("conn-3"));

        GetClientProxy("conn-friend-3").Verify(p => p.SendCoreAsync(
            HubMethod.SyncPairState,
            It.IsAny<object?[]>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleNotificationAsync_InvalidPayload_DoesNotThrow()
    {
        var wardrobeDataMock = CreateWardrobeDataServiceMock();
        var locksHandlerMock = CreateLocksHandlerMock(wardrobeDataMock: wardrobeDataMock);
        var permissionsMock = CreatePermissionsServiceMock();

        var logger = LogFactory.CreateLogger<ActiveWardrobeWatcher>();
        var watcher = new TestableActiveWardrobeWatcher(
            Config, HubContextMock.Object, PresenceMock.Object,
            CreateProfilesServiceMock().Object,
            wardrobeDataMock.Object, locksHandlerMock.Object,
            permissionsMock.Object, logger, null);

        var exception = await Record.ExceptionAsync(() =>
            watcher.CallHandleNotificationAsync("activewardrobe_changed", "bad-json"));

        Assert.Null(exception);
    }

    [Fact]
    public async Task HandleNotificationAsync_ProfileNotFound_DoesNotSend()
    {
        var wardrobeDataMock = CreateWardrobeDataServiceMock();
        var locksHandlerMock = CreateLocksHandlerMock(wardrobeDataMock: wardrobeDataMock);
        var permissionsMock = CreatePermissionsServiceMock();

        var logger = LogFactory.CreateLogger<ActiveWardrobeWatcher>();
        var watcher = new TestableActiveWardrobeWatcher(
            Config, HubContextMock.Object, PresenceMock.Object,
            CreateProfilesServiceMock().Object,
            wardrobeDataMock.Object, locksHandlerMock.Object,
            permissionsMock.Object, logger, null);

        await watcher.CallHandleNotificationAsync("activewardrobe_changed",
            "{\"profile_id\":99999}");

        Assert.Empty(ClientProxyMocks);
    }

    [Fact]
    public async Task HandleNotificationAsync_UserOnlineNoPermissions_SendsOnlyWardrobeState()
    {
        const string uid = "ACTIVE4";
        const int profileId = 1004;

        PresenceMock
            .Setup(p => p.TryGet(uid))
            .Returns(CreatePresence("conn-4"));

        var state = new WardrobeStateDto(null, null, null);

        var wardrobeDataMock = CreateWardrobeDataServiceMock();
        wardrobeDataMock.Setup(w => w.GetWardrobeStateAsync(profileId)).ReturnsAsync(state);

        var locksHandlerMock = CreateLocksHandlerMock(wardrobeDataMock: wardrobeDataMock);
        locksHandlerMock.Setup(l => l.GetAllLocksForUserAsync(uid))
            .Returns(Task.FromResult(new List<LockInfoDto>()));

        var permissionsMock = CreatePermissionsServiceMock();
        permissionsMock.Setup(p => p.GetAllPermissions(uid))
            .ReturnsAsync(new List<TwoWayPermissions>());

        var logger = LogFactory.CreateLogger<ActiveWardrobeWatcher>();
        var watcher = new TestableActiveWardrobeWatcher(
            Config, HubContextMock.Object, PresenceMock.Object,
            CreateProfilesServiceMock().Object,
            wardrobeDataMock.Object, locksHandlerMock.Object,
            permissionsMock.Object, logger, uid);

        await watcher.CallHandleNotificationAsync("activewardrobe_changed",
            $"{{\"profile_id\":{profileId}}}");

        GetClientProxy("conn-4").Verify(p => p.SendCoreAsync(
            HubMethod.SyncWardrobeState,
            It.Is<object?[]>(a => a[0] is WardrobeStateDto && (WardrobeStateDto)(a[0]!) == state),
            It.IsAny<CancellationToken>()), Times.Once);

        // Only one proxy created — no friends, so no SyncPairState to any connection
        Assert.Single(ClientProxyMocks);
    }
}
