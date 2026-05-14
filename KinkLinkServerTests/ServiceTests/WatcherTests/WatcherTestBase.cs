using KinkLinkCommon.Database;
using KinkLinkServer.Domain;
using KinkLinkServer.Domain.Interfaces;
using KinkLinkServer.Services;
using KinkLinkServer.SignalR.Hubs;
using KinkLinkServerTests.TestInfrastructure;
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
    protected readonly Mock<ISingleClientProxy> ClientProxyMock;
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

        ClientProxyMock = new Mock<ISingleClientProxy>(MockBehavior.Strict);
        ClientProxyMock
            .Setup(p => p.SendCoreAsync(
                It.IsAny<string>(),
                It.IsAny<object?[]>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        HubClientsMock = new Mock<IHubClients>(MockBehavior.Strict);
        HubClientsMock
            .Setup(c => c.Client(It.IsAny<string>()))
            .Returns(ClientProxyMock.Object);

        HubContextMock = new Mock<IHubContext<PrimaryHub>>(MockBehavior.Strict);
        HubContextMock
            .Setup(h => h.Clients)
            .Returns(HubClientsMock.Object);

        PresenceMock = new Mock<IPresenceService>(MockBehavior.Strict);

        var profilesLogger = LogFactory.CreateLogger<KinkLinkProfilesService>();
        ProfilesService = new KinkLinkProfilesService(Config, Metrics, profilesLogger);
    }

    protected Presence CreatePresence(string connectionId = "test-conn-id")
        => new(connectionId, "TestCharacter", "TestWorld");
}
