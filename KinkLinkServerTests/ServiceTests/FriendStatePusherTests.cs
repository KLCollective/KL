using KinkLinkCommon.Domain;
using KinkLinkCommon.Domain.Enums;
using KinkLinkCommon.Domain.Network;
using KinkLinkCommon.Domain.Network.SyncPairState;
using KinkLinkCommon.Database;
using KinkLinkCommon.Domain.Wardrobe;
using KinkLinkServer.Domain;
using KinkLinkServer.Domain.Interfaces;
using KinkLinkServer.Services;
using KinkLinkServer.SignalR.Handlers;
using KinkLinkServer.SignalR.Hubs;
using KinkLinkServerTests.Database;
using KinkLinkServerTests.TestInfrastructure;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Moq;

namespace KinkLinkServerTests.ServiceTests;

[Collection("DatabaseCollection")]
public class FriendStatePusherTests : DatabaseServiceTestBase
{
    private readonly Mock<IHubContext<PrimaryHub>> _hubContextMock;
    private readonly Mock<IHubClients> _hubClientsMock;
    private readonly Mock<ISingleClientProxy> _clientProxyMock;
    private readonly LocksHandler _locksHandler;
    private readonly ActiveWardrobeStateService _wardrobeService;
    private readonly LockService _lockService;
    private readonly PresenceService _presenceService;
    private readonly Configuration _config;
    private readonly ILoggerFactory _logFactory;

    public FriendStatePusherTests(TestDatabaseFixture fixture)
        : base(fixture)
    {
        _logFactory = LoggerFactory.Create(builder => { });
        _config = new Configuration(
            Fixture.ConnectionString,
            "test_signing_key_that_is_long_enough_for_hs256",
            "http://localhost:5006"
        );

        _clientProxyMock = new Mock<ISingleClientProxy>(MockBehavior.Strict);
        _clientProxyMock
            .Setup(p => p.SendCoreAsync(
                It.IsAny<string>(),
                It.IsAny<object?[]>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _hubClientsMock = new Mock<IHubClients>(MockBehavior.Strict);

        _hubContextMock = new Mock<IHubContext<PrimaryHub>>(MockBehavior.Strict);
        _hubContextMock
            .Setup(h => h.Clients)
            .Returns(_hubClientsMock.Object);

        _presenceService = new PresenceService(_logFactory.CreateLogger<PresenceService>());

        var lockLogger = _logFactory.CreateLogger<LockService>();
        _lockService = new LockService(_config, lockLogger);

        _locksHandler = new LocksHandler(_lockService, PermissionsService, _config,
            _logFactory.CreateLogger<LocksHandler>());

        var sharedWardrobeSql = new WardrobeSql(_config.DatabaseConnectionString);
        _wardrobeService = new ActiveWardrobeStateService(sharedWardrobeSql,
            _logFactory.CreateLogger<ActiveWardrobeStateService>(),
            new MetricsService(), _lockService);
    }

    [Fact]
    public async Task PushPairStateToFriendsAsync_NoPermissions_ReturnsEarly()
    {
        await FriendStatePusher.PushPairStateToFriendsAsync<string>(
            "TESTUID",
            1,
            PermissionsService,
            _locksHandler,
            _wardrobeService,
            _hubContextMock.Object,
            _presenceService,
            _logFactory.CreateLogger<string>());

        _hubContextMock.Verify(h => h.Clients, Times.Never);
    }

    [Fact]
    public async Task PushPairStateToFriendsAsync_NoOnlineFriends_ReturnsEarly()
    {
        await Fixture.ResetDatabaseAsync();

        var (_, profileId1, uid1) = await CreateTestUserWithProfileAsync(
            111111111111111001, "PUSHER1");
        var (_, profileId2, uid2) = await CreateTestUserWithProfileAsync(
            222222222222222001, "PUSHER2");

        await TestHarness.InsertTestPairAsync(new InsertTestPairParams
        {
            Id = profileId1,
            PairId = profileId2,
            Priority = (int)RelationshipPriority.Casual,
        });
        await TestHarness.InsertTestPairAsync(new InsertTestPairParams
        {
            Id = profileId2,
            PairId = profileId1,
            Priority = (int)RelationshipPriority.Casual,
        });

        await FriendStatePusher.PushPairStateToFriendsAsync<string>(
            uid1,
            profileId1,
            PermissionsService,
            _locksHandler,
            _wardrobeService,
            _hubContextMock.Object,
            _presenceService,
            _logFactory.CreateLogger<string>());

        _hubContextMock.Verify(h => h.Clients, Times.Never);
    }

    [Fact]
    public async Task PushPairStateToFriendsAsync_OneOnlineFriend_PushesState()
    {
        await Fixture.ResetDatabaseAsync();

        var (_, profileId1, uid1) = await CreateTestUserWithProfileAsync(
            111111111111111002, "PUSHER3");
        var (_, profileId2, uid2) = await CreateTestUserWithProfileAsync(
            222222222222222002, "PUSHER4");

        await TestHarness.InsertTestPairAsync(new InsertTestPairParams
        {
            Id = profileId1,
            PairId = profileId2,
            Priority = (int)RelationshipPriority.Serious,
            Interaction = (long)InteractionPerms.CanApplyGag,
        });
        await TestHarness.InsertTestPairAsync(new InsertTestPairParams
        {
            Id = profileId2,
            PairId = profileId1,
            Priority = (int)RelationshipPriority.Serious,
        });

        _presenceService.Add(uid2, new Presence("conn-friend", "FriendChar", "FriendWorld"));

        await _lockService.AddOrUpdateLockAsync(new LockInfoDto
        {
            LockID = "wardrobe-hat",
            LockeeID = profileId1,
            LockerID = profileId2,
            LockPriority = RelationshipPriority.Casual,
        });

        _hubClientsMock
            .Setup(c => c.Client("conn-friend"))
            .Returns(_clientProxyMock.Object);

        await FriendStatePusher.PushPairStateToFriendsAsync<string>(
            uid1,
            profileId1,
            PermissionsService,
            _locksHandler,
            _wardrobeService,
            _hubContextMock.Object,
            _presenceService,
            _logFactory.CreateLogger<string>());

        _clientProxyMock.Verify(p => p.SendCoreAsync(
            HubMethod.SyncPairState,
            It.Is<object?[]>(args =>
                args[0] != null &&
                args[0].GetType() == typeof(SyncPairStateCommand) &&
                ((SyncPairStateCommand)args[0]!).TargetFriendCode == uid1 &&
                ((SyncPairStateCommand)args[0]!).LockStates.Count == 1),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task PushPairStateToFriendsAsync_MultipleOnlineFriends_PushesToEach()
    {
        await Fixture.ResetDatabaseAsync();

        var (_, profileId1, uid1) = await CreateTestUserWithProfileAsync(
            111111111111111003, "PUSHER5");
        var (_, friend1Id, friend1Uid) = await CreateTestUserWithProfileAsync(
            222222222222222003, "PUSHER6");
        var (_, friend2Id, friend2Uid) = await CreateTestUserWithProfileAsync(
            333333333333333003, "PUSHER7");

        foreach (var (friendId, friendUid) in new[] { (friend1Id, friend1Uid), (friend2Id, friend2Uid) })
        {
            await TestHarness.InsertTestPairAsync(new InsertTestPairParams
            {
                Id = profileId1,
                PairId = friendId,
                Priority = (int)RelationshipPriority.Casual,
            });
            await TestHarness.InsertTestPairAsync(new InsertTestPairParams
            {
                Id = friendId,
                PairId = profileId1,
                Priority = (int)RelationshipPriority.Casual,
            });
        }

        _presenceService.Add(friend1Uid, new Presence("conn-f1", "F1", "W1"));
        _presenceService.Add(friend2Uid, new Presence("conn-f2", "F2", "W2"));

        var proxyMock1 = new Mock<ISingleClientProxy>(MockBehavior.Strict);
        proxyMock1
            .Setup(p => p.SendCoreAsync(
                It.IsAny<string>(), It.IsAny<object?[]>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var proxyMock2 = new Mock<ISingleClientProxy>(MockBehavior.Strict);
        proxyMock2
            .Setup(p => p.SendCoreAsync(
                It.IsAny<string>(), It.IsAny<object?[]>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _hubClientsMock
            .Setup(c => c.Client("conn-f1"))
            .Returns(proxyMock1.Object);
        _hubClientsMock
            .Setup(c => c.Client("conn-f2"))
            .Returns(proxyMock2.Object);

        await FriendStatePusher.PushPairStateToFriendsAsync<string>(
            uid1,
            profileId1,
            PermissionsService,
            _locksHandler,
            _wardrobeService,
            _hubContextMock.Object,
            _presenceService,
            _logFactory.CreateLogger<string>());

        proxyMock1.Verify(p => p.SendCoreAsync(
            HubMethod.SyncPairState, It.IsAny<object?[]>(), It.IsAny<CancellationToken>()),
            Times.Once);
        proxyMock2.Verify(p => p.SendCoreAsync(
            HubMethod.SyncPairState, It.IsAny<object?[]>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task PushPairStateToFriendsAsync_MixedOnlineOffline_SkipsOffline()
    {
        await Fixture.ResetDatabaseAsync();

        var (_, profileId1, uid1) = await CreateTestUserWithProfileAsync(
            111111111111111004, "PUSHER8");
        var (_, onlineId, onlineUid) = await CreateTestUserWithProfileAsync(
            222222222222222004, "PUSHER9");
        var (_, offlineId, offlineUid) = await CreateTestUserWithProfileAsync(
            333333333333333004, "PUSHER10");

        foreach (var (friendId, friendUid) in new[] { (onlineId, onlineUid), (offlineId, offlineUid) })
        {
            await TestHarness.InsertTestPairAsync(new InsertTestPairParams
            {
                Id = profileId1,
                PairId = friendId,
                Priority = (int)RelationshipPriority.Casual,
            });
            await TestHarness.InsertTestPairAsync(new InsertTestPairParams
            {
                Id = friendId,
                PairId = profileId1,
                Priority = (int)RelationshipPriority.Casual,
            });
        }

        _presenceService.Add(onlineUid, new Presence("conn-online", "Online", "World"));

        var proxyMock = new Mock<ISingleClientProxy>(MockBehavior.Strict);
        proxyMock
            .Setup(p => p.SendCoreAsync(
                It.IsAny<string>(), It.IsAny<object?[]>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _hubClientsMock
            .Setup(c => c.Client("conn-online"))
            .Returns(proxyMock.Object);
        _hubClientsMock
            .Setup(c => c.Client(It.Is<string>(s => s != "conn-online")))
            .Returns(new Mock<ISingleClientProxy>(MockBehavior.Strict).Object);

        await FriendStatePusher.PushPairStateToFriendsAsync<string>(
            uid1,
            profileId1,
            PermissionsService,
            _locksHandler,
            _wardrobeService,
            _hubContextMock.Object,
            _presenceService,
            _logFactory.CreateLogger<string>());

        proxyMock.Verify(p => p.SendCoreAsync(
            HubMethod.SyncPairState, It.IsAny<object?[]>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task PushPairStateToFriendsAsync_NoLocks_StillPushesState()
    {
        await Fixture.ResetDatabaseAsync();

        var (_, profileId1, uid1) = await CreateTestUserWithProfileAsync(
            111111111111111005, "PUSHER11");
        var (_, profileId2, uid2) = await CreateTestUserWithProfileAsync(
            222222222222222005, "PUSHER12");

        await TestHarness.InsertTestPairAsync(new InsertTestPairParams
        {
            Id = profileId1,
            PairId = profileId2,
            Priority = (int)RelationshipPriority.Devotional,
        });
        await TestHarness.InsertTestPairAsync(new InsertTestPairParams
        {
            Id = profileId2,
            PairId = profileId1,
            Priority = (int)RelationshipPriority.Devotional,
        });

        _presenceService.Add(uid2, new Presence("conn-f1", "F1", "W1"));

        _hubClientsMock
            .Setup(c => c.Client("conn-f1"))
            .Returns(_clientProxyMock.Object);

        await FriendStatePusher.PushPairStateToFriendsAsync<string>(
            uid1,
            profileId1,
            PermissionsService,
            _locksHandler,
            _wardrobeService,
            _hubContextMock.Object,
            _presenceService,
            _logFactory.CreateLogger<string>());

        _clientProxyMock.Verify(p => p.SendCoreAsync(
            HubMethod.SyncPairState,
            It.Is<object?[]>(args =>
                args[0] != null &&
                args[0].GetType() == typeof(SyncPairStateCommand) &&
                ((SyncPairStateCommand)args[0]!).TargetFriendCode == uid1 &&
                ((SyncPairStateCommand)args[0]!).LockStates.Count == 0),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
