using KinkLinkCommon.Database;
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

namespace KinkLinkServerTests.ServiceTests.WatcherTests;

public class WatcherTestBase : DatabaseServiceTestBase
{
    protected readonly Configuration Config;
    protected readonly ILoggerFactory LogFactory;
    protected readonly Mock<IHubContext<PrimaryHub>> HubContextMock;
    protected readonly Mock<IHubClients> HubClientsMock;
    protected readonly Dictionary<string, Mock<ISingleClientProxy>> ClientProxyMocks = new();
    protected readonly PresenceService PresenceService;
    protected readonly LockService LockService;
    protected readonly LocksHandler LocksHandler;
    protected readonly ActiveWardrobeStateService ActiveWardrobeService;
    protected readonly MetricsService Metrics;

    protected WatcherTestBase(TestDatabaseFixture fixture)
        : base(fixture)
    {
        Config = new Configuration(
            Fixture.ConnectionString,
            "test_signing_key_that_is_long_enough_for_hs256",
            "http://localhost:5006"
        );

        LogFactory = LoggerFactory.Create(builder => { });
        Metrics = new MetricsService();

        HubClientsMock = new Mock<IHubClients>(MockBehavior.Strict);
        HubClientsMock
            .Setup(c => c.Client(It.IsAny<string>()))
            .Returns((string connId) => GetOrCreateClientProxy(connId).Object);

        HubContextMock = new Mock<IHubContext<PrimaryHub>>(MockBehavior.Strict);
        HubContextMock
            .Setup(h => h.Clients)
            .Returns(HubClientsMock.Object);

        PresenceService = new PresenceService(LogFactory.CreateLogger<PresenceService>());

        var lockLogger = LogFactory.CreateLogger<LockService>();
        LockService = new LockService(Config, lockLogger);

        LocksHandler = new LocksHandler(LockService, PermissionsService, Config,
            LogFactory.CreateLogger<LocksHandler>());

        var sharedWardrobeSql = new WardrobeSql(Config.DatabaseConnectionString);
        ActiveWardrobeService = new ActiveWardrobeStateService(sharedWardrobeSql,
            LogFactory.CreateLogger<ActiveWardrobeStateService>(), Metrics, LockService);
    }

    private Mock<ISingleClientProxy> GetOrCreateClientProxy(string connectionId)
    {
        if (!ClientProxyMocks.TryGetValue(connectionId, out var mock))
        {
            mock = new Mock<ISingleClientProxy>(MockBehavior.Strict);
            mock
                .Setup(p => p.SendCoreAsync(
                    It.IsAny<string>(),
                    It.IsAny<object?[]>(),
                    It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            ClientProxyMocks[connectionId] = mock;
        }
        return mock;
    }

    protected Mock<ISingleClientProxy> GetClientProxy(string connectionId) =>
        ClientProxyMocks[connectionId];

    protected Presence CreatePresence(string connectionId = "test-conn-id")
        => new(connectionId, "TestCharacter", "TestWorld");
}
