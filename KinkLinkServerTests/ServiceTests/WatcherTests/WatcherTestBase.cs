using KinkLinkCommon.Database;
using KinkLinkServer.Domain;
using KinkLinkServer.Domain.Interfaces;
using KinkLinkServer.Services;
using KinkLinkServer.SignalR.Handlers;
using KinkLinkServer.SignalR.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Moq;

namespace KinkLinkServerTests.ServiceTests.WatcherTests;

public class WatcherTestBase
{
    protected readonly Configuration Config;
    protected readonly ILoggerFactory LogFactory;
    protected readonly Mock<IHubContext<PrimaryHub>> HubContextMock;
    protected readonly Mock<IHubClients> HubClientsMock;
    protected readonly Dictionary<string, Mock<ISingleClientProxy>> ClientProxyMocks = new();
    protected readonly Mock<IPresenceService> PresenceMock;
    protected readonly KinkLinkProfilesService ProfilesService;
    protected readonly MetricsService Metrics;

    protected WatcherTestBase()
    {
        Config = new Configuration(
            "Host=localhost;Database=nonexistent",
            "test_signing_key_that_is_long_enough_for_hs256",
            "http://localhost:5006"
        );

        LogFactory = Microsoft.Extensions.Logging.LoggerFactory.Create(builder => { });
        Metrics = new MetricsService();

        HubClientsMock = new Mock<IHubClients>(MockBehavior.Strict);
        HubClientsMock
            .Setup(c => c.Client(It.IsAny<string>()))
            .Returns((string connId) => GetOrCreateClientProxy(connId).Object);

        HubContextMock = new Mock<IHubContext<PrimaryHub>>(MockBehavior.Strict);
        HubContextMock
            .Setup(h => h.Clients)
            .Returns(HubClientsMock.Object);

        PresenceMock = new Mock<IPresenceService>(MockBehavior.Strict);

        var profilesLogger = LogFactory.CreateLogger<KinkLinkProfilesService>();
        ProfilesService = new KinkLinkProfilesService(Config, Metrics, profilesLogger);
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

    protected Mock<LockService> CreateLockServiceMock()
        => new(Config, LogFactory.CreateLogger<LockService>());

    protected Mock<PairsService> CreatePairsServiceMock()
        => new(Config, LogFactory.CreateLogger<PairsService>(), Metrics);

    protected Mock<KinkLinkProfilesService> CreateProfilesServiceMock()
        => new(Config, Metrics, LogFactory.CreateLogger<KinkLinkProfilesService>());

    protected Mock<PermissionsService> CreatePermissionsServiceMock()
    {
        var pairsMock = CreatePairsServiceMock();
        var profilesMock = CreateProfilesServiceMock();
        return new(Config, LogFactory.CreateLogger<PermissionsService>(),
            pairsMock.Object, profilesMock.Object);
    }

    protected Mock<WardrobeDataService> CreateWardrobeDataServiceMock()
    {
        var lockServiceMock = CreateLockServiceMock();
        return new(Config, LogFactory.CreateLogger<WardrobeDataService>(),
            Metrics, lockServiceMock.Object);
    }

    protected Mock<LocksHandler> CreateLocksHandlerMock(
        Mock<LockService>? lockServiceMock = null,
        Mock<PermissionsService>? permissionsMock = null,
        Mock<WardrobeDataService>? wardrobeDataMock = null)
    {
        lockServiceMock ??= CreateLockServiceMock();
        permissionsMock ??= CreatePermissionsServiceMock();
        wardrobeDataMock ??= CreateWardrobeDataServiceMock();
        return new(lockServiceMock.Object, permissionsMock.Object,
            Config, LogFactory.CreateLogger<LocksHandler>());
    }
}
