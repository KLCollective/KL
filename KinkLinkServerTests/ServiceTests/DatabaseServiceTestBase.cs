using KinkLinkCommon.Database;
using KinkLinkCommon.Security;
using KinkLinkServer.Domain;
using KinkLinkServer.Services;
using KinkLinkServerTests.Database;
using KinkLinkServerTests.TestInfrastructure;
using Microsoft.Extensions.Logging;

namespace KinkLinkServerTests.ServiceTests;

[Collection("DatabaseCollection")]
public class DatabaseServiceTestBase
{
    protected readonly TestDatabaseFixture Fixture;
    protected readonly AuthService AuthService;
    protected readonly PermissionsService PermissionsService;
    protected readonly KinkLinkServerTests.Database.TestHarnessSql TestHarness;

    protected DatabaseServiceTestBase(TestDatabaseFixture fixture)
    {
        Fixture = fixture;

        var config = new Configuration(
            fixture.ConnectionString,
            "test_signing_key_that_is_long_enough_for_hs256",
            "http://localhost:5006"
        );

        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var authLogger = loggerFactory.CreateLogger<AuthService>();
        var permissionsLogger = loggerFactory.CreateLogger<PermissionsService>();
        var pairsLogger = loggerFactory.CreateLogger<PairsService>();

        var authSql = new AuthSql(fixture.ConnectionString);
        var pairsService = new PairsService(config, pairsLogger);
        var profilesService = new KinkLinkProfilesService(config);

        AuthService = new AuthService(config, authLogger);
        PermissionsService = new PermissionsService(config, permissionsLogger, pairsService, profilesService);
        TestHarness = new KinkLinkServerTests.Database.TestHarnessSql(fixture.ConnectionString);
    }

    protected async Task<(int UserId, int ProfileId, string Uid)> CreateTestUserWithProfileAsync(
        long discordId,
        string uid,
        string? secretHash = null
    )
    {
        var userId = await TestHarness.InsertTestUserAsync(discordId, secretHash ?? "test_hash");
        Assert.NotNull(userId);

        var profileId = await TestHarness.InsertTestProfileAsync(userId.Value, uid);
        Assert.NotNull(profileId);

        return (userId.Value, profileId.Value, uid);
    }

    protected async Task<(
        int ProfileId1,
        int ProfileId2,
        string Uid1,
        string Uid2
    )> CreatePairedProfilesAsync()
    {
        var (userId1, profileId1, uid1) = await CreateTestUserWithProfileAsync(
            111111111111111111,
            "TESTUSER1"
        );
        var (userId2, profileId2, uid2) = await CreateTestUserWithProfileAsync(
            222222222222222222,
            "TESTUSER2"
        );

        await TestHarness.InsertTestPairAsync(
            new InsertTestPairParams { Id = profileId1, PairId = profileId2 }
        );

        return (profileId1, profileId2, uid1, uid2);
    }
}
