using KinkLinkCommon.Domain;
using KinkLinkCommon.Domain.Network;
using KinkLinkCommon.Domain.Network.Locks;
using KinkLinkServer.Domain;
using KinkLinkServer.Domain.Interfaces;
using KinkLinkServer.Services;
using KinkLinkServer.SignalR.Handlers;
using KinkLinkServer.SignalR.Hubs;
using Microsoft.Extensions.Logging;
using Moq;

namespace KinkLinkServerTests.ServiceTests.WatcherTests;

public class LockWatcherTests : WatcherTestBase
{
    private Mock<LockService> CreateLockServiceMock()
        => new(Config, LogFactory.CreateLogger<LockService>());

    private Mock<PairsService> CreatePairsServiceMock()
        => new(Config, LogFactory.CreateLogger<PairsService>(), Metrics);

    private Mock<KinkLinkProfilesService> CreateProfilesServiceMock()
        => new(Config, Metrics, LogFactory.CreateLogger<KinkLinkProfilesService>());

    private Mock<PermissionsService> CreatePermissionsServiceMock()
    {
        var pairsMock = CreatePairsServiceMock();
        var profilesMock = CreateProfilesServiceMock();
        return new(Config, LogFactory.CreateLogger<PermissionsService>(),
            pairsMock.Object, profilesMock.Object);
    }

    private Mock<WardrobeDataService> CreateWardrobeDataServiceMock()
    {
        var lockServiceMock = CreateLockServiceMock();
        return new(Config, LogFactory.CreateLogger<WardrobeDataService>(),
            Metrics, lockServiceMock.Object);
    }

    private Mock<LocksHandler> CreateLocksHandlerMock(
        Mock<LockService>? lockServiceMock = null,
        Mock<PermissionsService>? permissionsMock = null,
        Mock<WardrobeDataService>? wardrobeDataMock = null)
    {
        lockServiceMock ??= CreateLockServiceMock();
        permissionsMock ??= CreatePermissionsServiceMock();
        wardrobeDataMock ??= CreateWardrobeDataServiceMock();
        return new(lockServiceMock.Object, permissionsMock.Object,
            Mock.Of<IPresenceService>(), wardrobeDataMock.Object,
            CreateProfilesServiceMock().Object, Config,
            LogFactory.CreateLogger<LocksHandler>());
    }

    [Fact]
    public async Task HandleNotificationAsync_LockeeEqualsLocker_SendsSyncLocksOnce()
    {
        const string uid = "LOCK1";
        const int profileId = 2001;

        PresenceMock
            .Setup(p => p.TryGet(uid))
            .Returns(CreatePresence("conn-1"));

        var locksHandlerMock = CreateLocksHandlerMock();
        locksHandlerMock.Setup(l => l.GetAllLocksForUserAsync(uid))
            .Returns(Task.FromResult(new List<LockInfoDto>()));

        var permissionsMock = CreatePermissionsServiceMock();
        permissionsMock.Setup(p => p.GetAllPermissions(uid))
            .ReturnsAsync(new List<TwoWayPermissions>());

        var wardrobeDataMock = CreateWardrobeDataServiceMock();

        var logger = LogFactory.CreateLogger<LockWatcher>();
        var watcher = new TestableLockWatcher(
            Config, HubContextMock.Object, PresenceMock.Object,
            CreateProfilesServiceMock().Object,
            locksHandlerMock.Object, permissionsMock.Object,
            wardrobeDataMock.Object, logger, uid);

        await watcher.CallHandleNotificationAsync("lock_changed",
            $"{{\"lockee_id\":{profileId},\"locker_id\":{profileId}}}");

        ClientProxyMock.Verify(p => p.SendCoreAsync(
            HubMethod.SyncLocks,
            It.Is<object?[]>(a => a[0] is SyncLocksResponse),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleNotificationAsync_LockeeDifferentFromLocker_SendsSyncLocksTwice()
    {
        const string uid = "LOCKEE2";
        const int lockeeProfileId = 2002;
        const int lockerProfileId = 2003;

        PresenceMock
            .Setup(p => p.TryGet(uid))
            .Returns(CreatePresence("conn-lockee"));

        var locksHandlerMock = CreateLocksHandlerMock();
        locksHandlerMock.Setup(l => l.GetAllLocksForUserAsync(uid))
            .Returns(Task.FromResult(new List<LockInfoDto>()));

        var permissionsMock = CreatePermissionsServiceMock();
        permissionsMock.Setup(p => p.GetAllPermissions(uid))
            .ReturnsAsync(new List<TwoWayPermissions>());

        var wardrobeDataMock = CreateWardrobeDataServiceMock();

        var logger = LogFactory.CreateLogger<LockWatcher>();
        var watcher = new TestableLockWatcher(
            Config, HubContextMock.Object, PresenceMock.Object,
            CreateProfilesServiceMock().Object,
            locksHandlerMock.Object, permissionsMock.Object,
            wardrobeDataMock.Object, logger, uid);

        await watcher.CallHandleNotificationAsync("lock_changed",
            $"{{\"lockee_id\":{lockeeProfileId},\"locker_id\":{lockerProfileId}}}");

        ClientProxyMock.Verify(p => p.SendCoreAsync(
            HubMethod.SyncLocks,
            It.Is<object?[]>(a => a[0] is SyncLocksResponse),
            It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task HandleNotificationAsync_InvalidPayload_DoesNotThrow()
    {
        var locksHandlerMock = CreateLocksHandlerMock();
        var permissionsMock = CreatePermissionsServiceMock();
        var wardrobeDataMock = CreateWardrobeDataServiceMock();

        var logger = LogFactory.CreateLogger<LockWatcher>();
        var watcher = new TestableLockWatcher(
            Config, HubContextMock.Object, PresenceMock.Object,
            CreateProfilesServiceMock().Object,
            locksHandlerMock.Object, permissionsMock.Object,
            wardrobeDataMock.Object, logger, null);

        var exception = await Record.ExceptionAsync(() =>
            watcher.CallHandleNotificationAsync("lock_changed", "bad-json"));

        Assert.Null(exception);
    }

    [Fact]
    public async Task HandleNotificationAsync_UserOffline_DoesNotSendSyncLocks()
    {
        const string uid = "LOCK4";
        const int profileId = 2004;

        PresenceMock
            .Setup(p => p.TryGet(uid))
            .Returns((Presence?)null);

        var locksHandlerMock = CreateLocksHandlerMock();
        var permissionsMock = CreatePermissionsServiceMock();
        permissionsMock
            .Setup(p => p.GetAllPermissions(It.IsAny<string>()))
            .ReturnsAsync(new List<TwoWayPermissions>());
        var wardrobeDataMock = CreateWardrobeDataServiceMock();

        var logger = LogFactory.CreateLogger<LockWatcher>();
        var watcher = new TestableLockWatcher(
            Config, HubContextMock.Object, PresenceMock.Object,
            CreateProfilesServiceMock().Object,
            locksHandlerMock.Object, permissionsMock.Object,
            wardrobeDataMock.Object, logger, uid);

        await watcher.CallHandleNotificationAsync("lock_changed",
            $"{{\"lockee_id\":{profileId},\"locker_id\":{profileId}}}");

        ClientProxyMock.Verify(p => p.SendCoreAsync(
            It.IsAny<string>(),
            It.IsAny<object?[]>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleNotificationAsync_ProfileNotFound_DoesNotSend()
    {
        var locksHandlerMock = CreateLocksHandlerMock();
        var permissionsMock = CreatePermissionsServiceMock();
        var wardrobeDataMock = CreateWardrobeDataServiceMock();

        var logger = LogFactory.CreateLogger<LockWatcher>();
        var watcher = new TestableLockWatcher(
            Config, HubContextMock.Object, PresenceMock.Object,
            CreateProfilesServiceMock().Object,
            locksHandlerMock.Object, permissionsMock.Object,
            wardrobeDataMock.Object, logger, null);

        await watcher.CallHandleNotificationAsync("lock_changed",
            "{\"lockee_id\":99999,\"locker_id\":99999}");

        ClientProxyMock.Verify(p => p.SendCoreAsync(
            It.IsAny<string>(),
            It.IsAny<object?[]>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }
}
