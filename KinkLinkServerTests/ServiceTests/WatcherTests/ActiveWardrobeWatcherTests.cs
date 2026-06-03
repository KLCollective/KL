using KinkLinkCommon.Domain;
using KinkLinkCommon.Domain.Enums;
using KinkLinkCommon.Domain.Network;
using KinkLinkCommon.Domain.Wardrobe;
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
public class ActiveWardrobeWatcherTests : WatcherTestBase
{
    public ActiveWardrobeWatcherTests(TestDatabaseFixture fixture) : base(fixture) { }

    [Fact]
    public async Task HandleNotificationAsync_UserOnlineWithStateAndFriend_SendsStateAndPairState()
    {
        await Fixture.ResetDatabaseAsync();

        const string uid = "ACTIVE1";
        const string friendUid = "FRIEND1";

        // Set up profiles and bidirectional pair in DB
        var (_, dbProfileId, _) = await CreateTestUserWithProfileAsync(
            111111111111111001, uid);
        var (_, dbFriendId, _) = await CreateTestUserWithProfileAsync(
            222222222222222001, friendUid);

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

        // Add presence for both
        PresenceService.Add(uid, CreatePresence("conn-1"));
        PresenceService.Add(friendUid, CreatePresence("conn-friend-1"));

        // Insert active wardrobe state
        var glamourerBase64 = Convert.ToBase64String(
            System.Text.Encoding.UTF8.GetBytes("{\"Name\":\"Test\"}"));
        await TestHarness.InsertTestActiveWardrobeAsync(
            new InsertTestActiveWardrobeParams
            {
                ProfileId = dbProfileId,
                Glamourerset = glamourerBase64,
            });

        var profilesService = new KinkLinkProfilesService(Config, Metrics,
            LogFactory.CreateLogger<KinkLinkProfilesService>());

        var logger = LogFactory.CreateLogger<ActiveWardrobeWatcher>();
        var watcher = new TestableActiveWardrobeWatcher(
            Config,
            HubContextMock.Object,
            PresenceService,
            profilesService,
            ActiveWardrobeService,
            LocksHandler,
            PermissionsService,
            logger,
            uid
        );

        await watcher.CallHandleNotificationAsync(
            "active_wardrobe_changed",
            $"{{\"profile_id\":{dbProfileId}}}"
        );

        GetClientProxy("conn-1")
            .Verify(
                p =>
                    p.SendCoreAsync(
                        HubMethod.SyncWardrobeState,
                        It.Is<object?[]>(a => a[0] is WardrobeStateDto),
                        It.IsAny<CancellationToken>()
                    ),
                Times.Once
            );

        GetClientProxy("conn-friend-1")
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
    public async Task HandleNotificationAsync_UserOnlineNoWardrobeState_DoesNotSendSyncWardrobeState()
    {
        await Fixture.ResetDatabaseAsync();

        const string uid = "ACTIVE2";
        const string friendUid = "FRIEND2";

        var (_, dbProfileId, _) = await CreateTestUserWithProfileAsync(
            111111111111111002, uid);
        var (_, dbFriendId, _) = await CreateTestUserWithProfileAsync(
            222222222222222002, friendUid);

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

        // User is online but has NO active wardrobe state (no insert)
        PresenceService.Add(uid, CreatePresence("conn-2"));
        PresenceService.Add(friendUid, CreatePresence("conn-friend-2"));

        var profilesService = new KinkLinkProfilesService(Config, Metrics,
            LogFactory.CreateLogger<KinkLinkProfilesService>());

        var logger = LogFactory.CreateLogger<ActiveWardrobeWatcher>();
        var watcher = new TestableActiveWardrobeWatcher(
            Config,
            HubContextMock.Object,
            PresenceService,
            profilesService,
            ActiveWardrobeService,
            LocksHandler,
            PermissionsService,
            logger,
            uid
        );

        await watcher.CallHandleNotificationAsync(
            "active_wardrobe_changed",
            $"{{\"profile_id\":{dbProfileId}}}"
        );

        // No SyncWardrobeState was sent (no proxy created for conn-2)
        Assert.False(ClientProxyMocks.ContainsKey("conn-2"));

        GetClientProxy("conn-friend-2")
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
    public async Task HandleNotificationAsync_UserOffline_SendsOnlyPairState()
    {
        await Fixture.ResetDatabaseAsync();

        const string uid = "ACTIVE3";
        const string friendUid = "FRIEND3";

        var (_, dbProfileId, _) = await CreateTestUserWithProfileAsync(
            111111111111111003, uid);
        var (_, dbFriendId, _) = await CreateTestUserWithProfileAsync(
            222222222222222003, friendUid);

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

        // User is offline (no presence added)
        PresenceService.Add(friendUid, CreatePresence("conn-friend-3"));

        var profilesService = new KinkLinkProfilesService(Config, Metrics,
            LogFactory.CreateLogger<KinkLinkProfilesService>());

        var logger = LogFactory.CreateLogger<ActiveWardrobeWatcher>();
        var watcher = new TestableActiveWardrobeWatcher(
            Config,
            HubContextMock.Object,
            PresenceService,
            profilesService,
            ActiveWardrobeService,
            LocksHandler,
            PermissionsService,
            logger,
            uid
        );

        await watcher.CallHandleNotificationAsync(
            "active_wardrobe_changed",
            $"{{\"profile_id\":{dbProfileId}}}"
        );

        // No proxy for user (offline)
        Assert.False(ClientProxyMocks.ContainsKey("conn-3"));

        GetClientProxy("conn-friend-3")
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
    public async Task HandleNotificationAsync_InvalidPayload_DoesNotThrow()
    {
        var profilesService = new KinkLinkProfilesService(Config, Metrics,
            LogFactory.CreateLogger<KinkLinkProfilesService>());

        var watcher = new TestableActiveWardrobeWatcher(
            Config,
            HubContextMock.Object,
            PresenceService,
            profilesService,
            ActiveWardrobeService,
            LocksHandler,
            PermissionsService,
            LogFactory.CreateLogger<ActiveWardrobeWatcher>(),
            null
        );

        var exception = await Record.ExceptionAsync(() =>
            watcher.CallHandleNotificationAsync("active_wardrobe_changed", "bad-json")
        );

        Assert.Null(exception);
    }

    [Fact]
    public async Task HandleNotificationAsync_ProfileNotFound_DoesNotSend()
    {
        var profilesService = new KinkLinkProfilesService(Config, Metrics,
            LogFactory.CreateLogger<KinkLinkProfilesService>());

        var watcher = new TestableActiveWardrobeWatcher(
            Config,
            HubContextMock.Object,
            PresenceService,
            profilesService,
            ActiveWardrobeService,
            LocksHandler,
            PermissionsService,
            LogFactory.CreateLogger<ActiveWardrobeWatcher>(),
            null
        );

        await watcher.CallHandleNotificationAsync(
            "active_wardrobe_changed",
            "{\"profile_id\":99999}"
        );

        Assert.Empty(ClientProxyMocks);
    }

    [Fact]
    public async Task HandleNotificationAsync_UserOnlineNoPermissions_SendsOnlyWardrobeState()
    {
        await Fixture.ResetDatabaseAsync();

        const string uid = "ACTIVE4";

        var (_, dbProfileId, _) = await CreateTestUserWithProfileAsync(
            111111111111111004, uid);

        // User is online
        PresenceService.Add(uid, CreatePresence("conn-4"));

        // Insert active wardrobe state so GetWardrobeStateAsync returns data
        var glamourerBase64 = Convert.ToBase64String(
            System.Text.Encoding.UTF8.GetBytes("{\"Name\":\"Test\"}"));
        await TestHarness.InsertTestActiveWardrobeAsync(
            new InsertTestActiveWardrobeParams
            {
                ProfileId = dbProfileId,
                Glamourerset = glamourerBase64,
            });

        var profilesService = new KinkLinkProfilesService(Config, Metrics,
            LogFactory.CreateLogger<KinkLinkProfilesService>());

        var logger = LogFactory.CreateLogger<ActiveWardrobeWatcher>();
        var watcher = new TestableActiveWardrobeWatcher(
            Config,
            HubContextMock.Object,
            PresenceService,
            profilesService,
            ActiveWardrobeService,
            LocksHandler,
            PermissionsService,
            logger,
            uid
        );

        await watcher.CallHandleNotificationAsync(
            "active_wardrobe_changed",
            $"{{\"profile_id\":{dbProfileId}}}"
        );

        GetClientProxy("conn-4")
            .Verify(
                p =>
                    p.SendCoreAsync(
                        HubMethod.SyncWardrobeState,
                        It.Is<object?[]>(a => a[0] is WardrobeStateDto),
                        It.IsAny<CancellationToken>()
                    ),
                Times.Once
            );

        // Only one proxy created — no friends, so no SyncPairState
        Assert.Single(ClientProxyMocks);
    }
}
