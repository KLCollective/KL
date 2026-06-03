using KinkLinkCommon.Domain;
using KinkLinkCommon.Domain.Enums;
using KinkLinkCommon.Domain.Network;
using KinkLinkCommon.Domain.Network.AddFriend;
using KinkLinkCommon.Domain.Network.SyncOnlineStatus;
using KinkLinkCommon.Database;
using KinkLinkCommon.Domain.Network.SyncPairState;
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
public class AddFriendHandlerTests : DatabaseServiceTestBase
{
    private readonly AddFriendHandler _handler;
    private readonly Configuration _config;
    private readonly PresenceService _presenceService;
    private readonly Mock<IHubCallerClients> _hubClientsMock;
    private readonly Mock<ISingleClientProxy> _callerProxyMock;
    private readonly Mock<ISingleClientProxy> _targetProxyMock;
    private readonly LocksHandler _locksHandler;
    private readonly LockService _lockService;

    public AddFriendHandlerTests(TestDatabaseFixture fixture)
        : base(fixture)
    {
        _config = new Configuration(
            Fixture.ConnectionString,
            "test_signing_key_that_is_long_enough_for_hs256",
            "http://localhost:5006"
        );
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());

        _presenceService = new PresenceService(loggerFactory.CreateLogger<PresenceService>());

        _hubClientsMock = new Mock<IHubCallerClients>(MockBehavior.Strict);
        _callerProxyMock = new Mock<ISingleClientProxy>(MockBehavior.Strict);
        _targetProxyMock = new Mock<ISingleClientProxy>(MockBehavior.Strict);

        _hubClientsMock
            .Setup(c => c.Caller)
            .Returns(_callerProxyMock.Object);
        _hubClientsMock
            .Setup(c => c.Others)
            .Returns(new Mock<ISingleClientProxy>(MockBehavior.Strict).Object);
        _hubClientsMock
            .Setup(c => c.OthersInGroup(It.IsAny<string>()))
            .Returns(new Mock<IClientProxy>(MockBehavior.Strict).Object);
        _hubClientsMock
            .Setup(c => c.Client(It.IsAny<string>()))
            .Returns((string connId) => _targetProxyMock.Object);
        // Locks handler with real DB
        var lockLogger = loggerFactory.CreateLogger<LockService>();
        _lockService = new LockService(_config, lockLogger);
        _locksHandler = new LocksHandler(_lockService, PermissionsService, _config,
            loggerFactory.CreateLogger<LocksHandler>());

        var profilesLogger = loggerFactory.CreateLogger<KinkLinkProfilesService>();
        var profilesService = new KinkLinkProfilesService(_config, new MetricsService(), profilesLogger);

        var sharedWardrobeSql = new WardrobeSql(_config.DatabaseConnectionString);
        var activeWardrobeService = new ActiveWardrobeStateService(sharedWardrobeSql,
            loggerFactory.CreateLogger<ActiveWardrobeStateService>(),
            new MetricsService(), _lockService);

        _handler = new AddFriendHandler(
            _presenceService,
            PermissionsService,
            profilesService,
            _locksHandler,
            activeWardrobeService,
            loggerFactory.CreateLogger<AddFriendHandler>()
        );

        SetupDefaultSendAsync(_callerProxyMock);
        SetupDefaultSendAsync(_targetProxyMock);
    }

    private static void SetupDefaultSendAsync(Mock<ISingleClientProxy> proxy)
    {
        proxy
            .Setup(p => p.SendCoreAsync(
                It.IsAny<string>(),
                It.IsAny<object?[]>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    [Fact]
    public async Task Handle_ValidRequest_TargetOnline_CreatesPairAndSendsSync()
    {
        await Fixture.ResetDatabaseAsync();

        var (_, _, requesterUid) = await CreateTestUserWithProfileAsync(
            111111111111111080, "ADDFR1");
        var (_, targetProfileId, targetUid) = await CreateTestUserWithProfileAsync(
            222222222222222080, "ADDFR2");

        // Target is online
        _presenceService.Add(targetUid, new Presence("conn-target", "TargetChar", "TargetWorld"));

        var request = new AddFriendRequest(targetUid);
        var result = await _handler.Handle(requesterUid, request, _hubClientsMock.Object);

        Assert.Equal(PairRequestResult.Success, result.Result);
        Assert.Equal(FriendOnlineStatus.Online, result.Status);

        // Verify pair exists in DB (one-way from requester to target)
        var pairsLogger = LoggerFactory.Create(builder => { }).CreateLogger<PairsService>();
        var pairsService = new PairsService(_config, pairsLogger, new MetricsService());
        var pairResult = await pairsService.GetPairByProfileIdsAsync(requesterUid, targetUid);
        Assert.NotNull(pairResult);

        // Verify SyncOnlineStatus was sent to caller
        _callerProxyMock.Verify(p => p.SendCoreAsync(
            HubMethod.SyncOnlineStatus,
            It.Is<object?[]>(a => a[0] is SyncOnlineStatusCommand),
            It.IsAny<CancellationToken>()), Times.Once);

        // Verify SyncOnlineStatus sent to target
        _targetProxyMock.Verify(p => p.SendCoreAsync(
            HubMethod.SyncOnlineStatus,
            It.Is<object?[]>(a => a[0] is SyncOnlineStatusCommand),
            It.IsAny<CancellationToken>()), Times.Once);

        // Verify SyncPairState sent to target
        _targetProxyMock.Verify(p => p.SendCoreAsync(
            HubMethod.SyncPairState,
            It.Is<object?[]>(a => a[0] is SyncPairStateCommand),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ValidRequest_TargetOffline_ReturnsOffline()
    {
        await Fixture.ResetDatabaseAsync();

        var (_, _, requesterUid) = await CreateTestUserWithProfileAsync(
            111111111111111081, "ADDFR3");
        var (_, _, targetUid) = await CreateTestUserWithProfileAsync(
            222222222222222081, "ADDFR4");

        // Target is offline (no presence added)

        var request = new AddFriendRequest(targetUid);
        var result = await _handler.Handle(requesterUid, request, _hubClientsMock.Object);

        Assert.Equal(PairRequestResult.Success, result.Result);
        Assert.Equal(FriendOnlineStatus.Offline, result.Status);

        // SyncOnlineStatus to caller (with Offline status)
        _callerProxyMock.Verify(p => p.SendCoreAsync(
            HubMethod.SyncOnlineStatus,
            It.IsAny<object?[]>(),
            It.IsAny<CancellationToken>()), Times.Once);

        // No SyncPairState or SyncOnlineStatus to target since they're offline
        _targetProxyMock.Verify(p => p.SendCoreAsync(
            It.IsAny<string>(),
            It.IsAny<object?[]>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_SelfAdd_ReturnsNoSuchFriendCode()
    {
        await Fixture.ResetDatabaseAsync();

        var (_, _, uid) = await CreateTestUserWithProfileAsync(
            111111111111111082, "ADDFR5");

        var request = new AddFriendRequest(uid);
        var result = await _handler.Handle(uid, request, _hubClientsMock.Object);

        Assert.Equal(PairRequestResult.NoSuchFriendCode, result.Result);
    }

    [Fact]
    public async Task Handle_NonExistentRequester_ReturnsNoSuchFriendCode()
    {
        await Fixture.ResetDatabaseAsync();

        var (_, _, targetUid) = await CreateTestUserWithProfileAsync(
            222222222222222082, "ADDFR6");

        var request = new AddFriendRequest(targetUid);
        var result = await _handler.Handle("NONEXISTENT", request, _hubClientsMock.Object);

        Assert.Equal(PairRequestResult.NoSuchFriendCode, result.Result);
    }

    [Fact]
    public async Task Handle_NonExistentTarget_ReturnsNoSuchFriendCode()
    {
        await Fixture.ResetDatabaseAsync();

        var (_, _, requesterUid) = await CreateTestUserWithProfileAsync(
            111111111111111083, "ADDFR7");

        var request = new AddFriendRequest("NONEXISTENT");
        var result = await _handler.Handle(requesterUid, request, _hubClientsMock.Object);

        Assert.Equal(PairRequestResult.NoSuchFriendCode, result.Result);
    }

    [Fact]
    public async Task Handle_AlreadyBidirectionalPair_ReturnsAlreadyFriends()
    {
        await Fixture.ResetDatabaseAsync();

        var (_, profileId1, uid1) = await CreateTestUserWithProfileAsync(
            111111111111111084, "ADDFR8");
        var (_, profileId2, uid2) = await CreateTestUserWithProfileAsync(
            222222222222222084, "ADDFR9");

        // Create bidirectional pair
        await TestHarness.InsertTestPairAsync(new InsertTestPairParams
        {
            Id = profileId1,
            PairId = profileId2,
        });
        await TestHarness.InsertTestPairAsync(new InsertTestPairParams
        {
            Id = profileId2,
            PairId = profileId1,
        });

        _presenceService.Add(uid2, new Presence("conn", "Char", "World"));

        var request = new AddFriendRequest(uid2);
        var result = await _handler.Handle(uid1, request, _hubClientsMock.Object);

        Assert.Equal(PairRequestResult.AlreadyFriends, result.Result);
        Assert.Equal(FriendOnlineStatus.Offline, result.Status);

        _targetProxyMock.Verify(p => p.SendCoreAsync(
            HubMethod.SyncPairState,
            It.IsAny<object?[]>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ExistingOneSidedPair_ReturnsPending()
    {
        await Fixture.ResetDatabaseAsync();

        var (_, profileId1, uid1) = await CreateTestUserWithProfileAsync(
            111111111111111085, "ADDFR10");
        var (_, profileId2, uid2) = await CreateTestUserWithProfileAsync(
            222222222222222085, "ADDFR11");

        // One-sided pair: uid2 -> uid1 exists
        await TestHarness.InsertTestPairAsync(new InsertTestPairParams
        {
            Id = profileId2,
            PairId = profileId1,
        });

        // uid2 is offline
        // (no presence added for uid2)

        var request = new AddFriendRequest(uid2);
        var result = await _handler.Handle(uid1, request, _hubClientsMock.Object);

        // uid1->uid2 doesn't exist, uid2->uid1 exists,
        // so it creates uid1->uid2, making it bidirectional, returns Success
        Assert.Equal(PairRequestResult.Success, result.Result);
        Assert.Equal(FriendOnlineStatus.Offline, result.Status);
    }
}
