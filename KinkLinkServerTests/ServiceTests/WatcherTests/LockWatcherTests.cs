using KinkLinkCommon.Domain;
using KinkLinkCommon.Domain.Network;
using KinkLinkCommon.Domain.Network.Locks;
using KinkLinkCommon.Domain.Wardrobe;
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

        GetClientProxy("conn-1").Verify(p => p.SendCoreAsync(
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

        GetClientProxy("conn-lockee").Verify(p => p.SendCoreAsync(
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

        Assert.Empty(ClientProxyMocks);
    }

    [Fact]
    public async Task HandleNotificationAsync_UserOnlineWithFriend_SendsSyncPairState()
    {
        const string uid = "LOCK5";
        const int profileId = 2005;

        PresenceMock
            .Setup(p => p.TryGet(uid))
            .Returns(CreatePresence("conn-5"));

        PresenceMock
            .Setup(p => p.TryGet("FRIEND5"))
            .Returns(CreatePresence("conn-friend-5"));

        var locksHandlerMock = CreateLocksHandlerMock();
        locksHandlerMock.Setup(l => l.GetAllLocksForUserAsync(uid))
            .Returns(Task.FromResult(new List<LockInfoDto>()));

        var permissionsMock = CreatePermissionsServiceMock();
        permissionsMock.Setup(p => p.GetAllPermissions(uid))
            .ReturnsAsync(new List<TwoWayPermissions>
            {
                new(uid, "FRIEND5", new UserPermissions(), null)
            });

        var wardrobeDataMock = CreateWardrobeDataServiceMock();
        wardrobeDataMock.Setup(w => w.GetPairWardrobeItemsAsync(profileId))
            .ReturnsAsync(new PairWardrobeStateDto(null, null));

        var logger = LogFactory.CreateLogger<LockWatcher>();
        var watcher = new TestableLockWatcher(
            Config, HubContextMock.Object, PresenceMock.Object,
            CreateProfilesServiceMock().Object,
            locksHandlerMock.Object, permissionsMock.Object,
            wardrobeDataMock.Object, logger, uid);

        await watcher.CallHandleNotificationAsync("lock_changed",
            $"{{\"lockee_id\":{profileId},\"locker_id\":{profileId}}}");

        // SyncLocks should still be sent to the lockee
        GetClientProxy("conn-5").Verify(p => p.SendCoreAsync(
            HubMethod.SyncLocks,
            It.Is<object?[]>(a => a[0] is SyncLocksResponse),
            It.IsAny<CancellationToken>()), Times.Once);

        // SyncPairState should be sent to the online friend
        GetClientProxy("conn-friend-5").Verify(p => p.SendCoreAsync(
            HubMethod.SyncPairState,
            It.IsAny<object?[]>(),
            It.IsAny<CancellationToken>()), Times.Once);
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

        Assert.Empty(ClientProxyMocks);
    }
}
