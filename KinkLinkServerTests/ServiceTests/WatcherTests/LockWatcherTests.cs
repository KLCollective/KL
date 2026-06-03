using KinkLinkCommon.Domain;
using KinkLinkCommon.Domain.Enums;
using KinkLinkCommon.Domain.Network;
using KinkLinkCommon.Domain.Network.Locks;
using KinkLinkServer.Domain;
using KinkLinkServer.Domain.Interfaces;
using KinkLinkServer.Services;
using KinkLinkServer.SignalR.Handlers;
using KinkLinkServer.SignalR.Hubs;
using KinkLinkServerTests.Database;
using KinkLinkServerTests.TestInfrastructure;
using Microsoft.Extensions.Logging;
using Moq;

namespace KinkLinkServerTests.ServiceTests.WatcherTests;

[Collection("DatabaseCollection")]
public class LockWatcherTests : WatcherTestBase
{
    public LockWatcherTests(TestDatabaseFixture fixture) : base(fixture) { }

    [Fact]
    public async Task HandleNotificationAsync_LockeeEqualsLocker_SendsSyncLocksOnce()
    {
        await Fixture.ResetDatabaseAsync();

        const string uid = "LOCK1";

        var (_, dbProfileId, _) = await CreateTestUserWithProfileAsync(
            111111111111111001, uid);

        PresenceService.Add(uid, CreatePresence("conn-1"));

        var profilesService = new KinkLinkProfilesService(Config, Metrics,
            LogFactory.CreateLogger<KinkLinkProfilesService>());

        var logger = LogFactory.CreateLogger<LockWatcher>();
        var watcher = new TestableLockWatcher(
            Config,
            HubContextMock.Object,
            PresenceService,
            profilesService,
            LocksHandler,
            PermissionsService,
            ActiveWardrobeService,
            logger,
            uid
        );

        await watcher.CallHandleNotificationAsync(
            "lock_changed",
            $"{{\"lockee_id\":{dbProfileId},\"locker_id\":{dbProfileId}}}"
        );

        GetClientProxy("conn-1")
            .Verify(
                p =>
                    p.SendCoreAsync(
                        HubMethod.SyncLocks,
                        It.Is<object?[]>(a => a[0] is SyncLocksResponse),
                        It.IsAny<CancellationToken>()
                    ),
                Times.Once
            );
    }

    [Fact]
    public async Task HandleNotificationAsync_LockeeDifferentFromLocker_SendsSyncLocksTwice()
    {
        await Fixture.ResetDatabaseAsync();

        const string lockeeUid = "LOCKEE2";
        const string lockerUid = "LOCKER2";

        var (_, dbLockeeId, _) = await CreateTestUserWithProfileAsync(
            111111111111111002, lockeeUid);
        var (_, dbLockerId, _) = await CreateTestUserWithProfileAsync(
            222222222222222002, lockerUid);

        PresenceService.Add(lockeeUid, CreatePresence("conn-lockee"));
        PresenceService.Add(lockerUid, CreatePresence("conn-locker"));

        var profilesService = new KinkLinkProfilesService(Config, Metrics,
            LogFactory.CreateLogger<KinkLinkProfilesService>());

        var logger = LogFactory.CreateLogger<LockWatcher>();
        var watcher = new TestableLockWatcher(
            Config,
            HubContextMock.Object,
            PresenceService,
            profilesService,
            LocksHandler,
            PermissionsService,
            ActiveWardrobeService,
            logger,
            lockeeUid
        );

        await watcher.CallHandleNotificationAsync(
            "lock_changed",
            $"{{\"lockee_id\":{dbLockeeId},\"locker_id\":{dbLockerId}}}"
        );

        GetClientProxy("conn-lockee")
            .Verify(
                p =>
                    p.SendCoreAsync(
                        HubMethod.SyncLocks,
                        It.Is<object?[]>(a => a[0] is SyncLocksResponse),
                        It.IsAny<CancellationToken>()
                    ),
                Times.Exactly(2)
            );
    }

    [Fact]
    public async Task HandleNotificationAsync_InvalidPayload_DoesNotThrow()
    {
        var profilesService = new KinkLinkProfilesService(Config, Metrics,
            LogFactory.CreateLogger<KinkLinkProfilesService>());

        var watcher = new TestableLockWatcher(
            Config,
            HubContextMock.Object,
            PresenceService,
            profilesService,
            LocksHandler,
            PermissionsService,
            ActiveWardrobeService,
            LogFactory.CreateLogger<LockWatcher>(),
            null
        );

        var exception = await Record.ExceptionAsync(() =>
            watcher.CallHandleNotificationAsync("lock_changed", "bad-json")
        );

        Assert.Null(exception);
    }

    [Fact]
    public async Task HandleNotificationAsync_UserOffline_DoesNotSendSyncLocks()
    {
        await Fixture.ResetDatabaseAsync();

        const string uid = "LOCK4";

        var (_, dbProfileId, _) = await CreateTestUserWithProfileAsync(
            111111111111111004, uid);

        // User is offline (no presence added)

        var profilesService = new KinkLinkProfilesService(Config, Metrics,
            LogFactory.CreateLogger<KinkLinkProfilesService>());

        var logger = LogFactory.CreateLogger<LockWatcher>();
        var watcher = new TestableLockWatcher(
            Config,
            HubContextMock.Object,
            PresenceService,
            profilesService,
            LocksHandler,
            PermissionsService,
            ActiveWardrobeService,
            logger,
            uid
        );

        await watcher.CallHandleNotificationAsync(
            "lock_changed",
            $"{{\"lockee_id\":{dbProfileId},\"locker_id\":{dbProfileId}}}"
        );

        Assert.Empty(ClientProxyMocks);
    }

    [Fact]
    public async Task HandleNotificationAsync_UserOnlineWithFriend_SendsSyncPairState()
    {
        await Fixture.ResetDatabaseAsync();

        const string uid = "LOCK5";
        const string friendUid = "FRIEND5";

        var (_, dbProfileId, _) = await CreateTestUserWithProfileAsync(
            111111111111111005, uid);
        var (_, dbFriendId, _) = await CreateTestUserWithProfileAsync(
            222222222222222005, friendUid);

        await TestHarness.InsertTestPairAsync(new InsertTestPairParams
        {
            Id = dbProfileId,
            PairId = dbFriendId,
            Priority = (int)RelationshipPriority.Casual,
        });
        await TestHarness.InsertTestPairAsync(new InsertTestPairParams
        {
            Id = dbFriendId,
            PairId = dbProfileId,
            Priority = (int)RelationshipPriority.Casual,
        });

        PresenceService.Add(uid, CreatePresence("conn-5"));
        PresenceService.Add(friendUid, CreatePresence("conn-friend-5"));

        var profilesService = new KinkLinkProfilesService(Config, Metrics,
            LogFactory.CreateLogger<KinkLinkProfilesService>());

        var logger = LogFactory.CreateLogger<LockWatcher>();
        var watcher = new TestableLockWatcher(
            Config,
            HubContextMock.Object,
            PresenceService,
            profilesService,
            LocksHandler,
            PermissionsService,
            ActiveWardrobeService,
            logger,
            uid
        );

        await watcher.CallHandleNotificationAsync(
            "lock_changed",
            $"{{\"lockee_id\":{dbProfileId},\"locker_id\":{dbProfileId}}}"
        );

        GetClientProxy("conn-5")
            .Verify(
                p =>
                    p.SendCoreAsync(
                        HubMethod.SyncLocks,
                        It.Is<object?[]>(a => a[0] is SyncLocksResponse),
                        It.IsAny<CancellationToken>()
                    ),
                Times.Once
            );

        GetClientProxy("conn-friend-5")
            .Verify(
                p =>
                    p.SendCoreAsync(
                        HubMethod.SyncPairState,
                        It.IsAny<object?[]>(),
                        It.IsAny<CancellationToken>()
                    ),
                Times.Once
            );
    }

    [Fact]
    public async Task HandleNotificationAsync_ProfileNotFound_DoesNotSend()
    {
        var profilesService = new KinkLinkProfilesService(Config, Metrics,
            LogFactory.CreateLogger<KinkLinkProfilesService>());

        var watcher = new TestableLockWatcher(
            Config,
            HubContextMock.Object,
            PresenceService,
            profilesService,
            LocksHandler,
            PermissionsService,
            ActiveWardrobeService,
            LogFactory.CreateLogger<LockWatcher>(),
            null
        );

        await watcher.CallHandleNotificationAsync(
            "lock_changed",
            "{\"lockee_id\":99999,\"locker_id\":99999}"
        );

        Assert.Empty(ClientProxyMocks);
    }
}
