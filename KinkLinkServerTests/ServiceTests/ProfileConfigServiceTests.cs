using KinkLinkServer.Domain;
using KinkLinkServer.Services;
using KinkLinkServerTests.Database;
using KinkLinkServerTests.TestInfrastructure;
using Microsoft.Extensions.Logging;

namespace KinkLinkServerTests.ServiceTests;

[Collection("DatabaseCollection")]
public class ProfileConfigServiceTests : DatabaseServiceTestBase
{
    private readonly KinkLinkProfileConfigService _profileConfigService;

    public ProfileConfigServiceTests(TestDatabaseFixture fixture)
        : base(fixture)
    {
        var config = new Configuration(
            Fixture.ConnectionString,
            "test_signing_key_that_is_long_enough_for_hs256",
            "http://localhost:5006"
        );

        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var logger = loggerFactory.CreateLogger<KinkLinkProfileConfigService>();

        _profileConfigService = new KinkLinkProfileConfigService(config, logger);
    }

    #region GetProfileConfigByUidAsync Tests

    [Fact]
    public async Task GetProfileConfigByUidAsync_ConfigExists_ReturnsConfig()
    {
        await Fixture.ResetDatabaseAsync();

        var (_, profileId, uid) = await CreateTestUserWithProfileAsync(
            111111111111111300,
            "CFGTEST1"
        );

        await TestHarness.InsertTestProfileConfigAsync(
            new InsertTestProfileConfigParams
            {
                Id = profileId,
                EnableGlamours = true,
                EnableGarbler = false,
                EnableGarblerChannels = true,
                EnableMoodles = false,
            }
        );

        var result = await _profileConfigService.GetProfileConfigByUidAsync("CFGTEST1");

        Assert.NotNull(result);
        Assert.True(result.EnableGlamours);
        Assert.False(result.EnableGarbler);
        Assert.True(result.EnableGarblerChannels);
        Assert.False(result.EnableMoodles);
    }

    [Fact]
    public async Task GetProfileConfigByUidAsync_NullDbValues_MapsToFalse()
    {
        await Fixture.ResetDatabaseAsync();

        var (_, profileId, uid) = await CreateTestUserWithProfileAsync(
            111111111111111301,
            "CFGTEST2"
        );

        // Insert with explicit values to test null handling
        await using var conn = new Npgsql.NpgsqlConnection(Fixture.ConnectionString);
        await conn.OpenAsync();
        await using var cmd = new Npgsql.NpgsqlCommand(
            "INSERT INTO ProfileConfig (id, enable_glamours, enable_garbler, enable_garbler_channels, enable_moodles) " +
            "VALUES (@id, NULL, NULL, NULL, NULL) ON CONFLICT (id) DO UPDATE SET " +
            "enable_glamours = NULL, enable_garbler = NULL, enable_garbler_channels = NULL, enable_moodles = NULL",
            conn
        );
        cmd.Parameters.AddWithValue("id", profileId);
        await cmd.ExecuteNonQueryAsync();

        var result = await _profileConfigService.GetProfileConfigByUidAsync("CFGTEST2");

        Assert.NotNull(result);
        Assert.False(result.EnableGlamours);
        Assert.False(result.EnableGarbler);
        Assert.False(result.EnableGarblerChannels);
        Assert.False(result.EnableMoodles);
    }

    [Fact]
    public async Task GetProfileConfigByUidAsync_NoConfig_ReturnsNull()
    {
        await Fixture.ResetDatabaseAsync();

        await CreateTestUserWithProfileAsync(111111111111111302, "CFGTEST3");

        var result = await _profileConfigService.GetProfileConfigByUidAsync("CFGTEST3");

        Assert.Null(result);
    }

    [Fact]
    public async Task GetProfileConfigByUidAsync_NoProfile_ReturnsNull()
    {
        await Fixture.ResetDatabaseAsync();

        var result = await _profileConfigService.GetProfileConfigByUidAsync("NONEXISTENT");

        Assert.Null(result);
    }

    #endregion

    #region UpdateProfileConfigAsync Tests

    [Fact]
    public async Task UpdateProfileConfigAsync_ProfileExists_CreatesAndReturnsConfig()
    {
        await Fixture.ResetDatabaseAsync();

        await CreateTestUserWithProfileAsync(111111111111111303, "CFGTEST4");

        var result = await _profileConfigService.UpdateProfileConfigAsync(
            "CFGTEST4",
            true,
            true,
            false,
            true
        );

        Assert.NotNull(result);
        Assert.True(result.EnableGlamours);
        Assert.True(result.EnableGarbler);
        Assert.False(result.EnableGarblerChannels);
        Assert.True(result.EnableMoodles);
    }

    [Fact]
    public async Task UpdateProfileConfigAsync_ExistingConfig_UpdatesAndReturnsConfig()
    {
        await Fixture.ResetDatabaseAsync();

        var (_, profileId, uid) = await CreateTestUserWithProfileAsync(
            111111111111111304,
            "CFGTEST5"
        );

        await TestHarness.InsertTestProfileConfigAsync(
            new InsertTestProfileConfigParams
            {
                Id = profileId,
                EnableGlamours = false,
                EnableGarbler = false,
                EnableGarblerChannels = false,
                EnableMoodles = false,
            }
        );

        var result = await _profileConfigService.UpdateProfileConfigAsync(
            "CFGTEST5",
            true,
            true,
            true,
            true
        );

        Assert.NotNull(result);
        Assert.True(result.EnableGlamours);
        Assert.True(result.EnableGarbler);
        Assert.True(result.EnableGarblerChannels);
        Assert.True(result.EnableMoodles);
    }

    [Fact]
    public async Task UpdateProfileConfigAsync_NoProfile_ReturnsNull()
    {
        await Fixture.ResetDatabaseAsync();

        var result = await _profileConfigService.UpdateProfileConfigAsync(
            "NONEXISTENT",
            true,
            false,
            true,
            false
        );

        Assert.Null(result);
    }

    #endregion

    #region DeleteProfileConfigByUidAsync Tests

    [Fact]
    public async Task DeleteProfileConfigByUidAsync_ConfigExists_ReturnsDeletedConfig()
    {
        await Fixture.ResetDatabaseAsync();

        var (_, profileId, uid) = await CreateTestUserWithProfileAsync(
            111111111111111305,
            "CFGTEST6"
        );

        await TestHarness.InsertTestProfileConfigAsync(
            new InsertTestProfileConfigParams
            {
                Id = profileId,
                EnableGlamours = true,
                EnableGarbler = true,
                EnableGarblerChannels = false,
                EnableMoodles = true,
            }
        );

        var result = await _profileConfigService.DeleteProfileConfigByUidAsync("CFGTEST6");

        Assert.NotNull(result);
        Assert.True(result.EnableGlamours);
        Assert.True(result.EnableGarbler);
        Assert.False(result.EnableGarblerChannels);
        Assert.True(result.EnableMoodles);

        // Verify config is actually deleted
        var getResult = await _profileConfigService.GetProfileConfigByUidAsync("CFGTEST6");
        Assert.Null(getResult);
    }

    [Fact]
    public async Task DeleteProfileConfigByUidAsync_NoConfig_ReturnsNull()
    {
        await Fixture.ResetDatabaseAsync();

        await CreateTestUserWithProfileAsync(111111111111111306, "CFGTEST7");

        var result = await _profileConfigService.DeleteProfileConfigByUidAsync("CFGTEST7");

        Assert.Null(result);
    }

    [Fact]
    public async Task DeleteProfileConfigByUidAsync_NoProfile_ReturnsNull()
    {
        await Fixture.ResetDatabaseAsync();

        var result = await _profileConfigService.DeleteProfileConfigByUidAsync("NONEXISTENT");

        Assert.Null(result);
    }

    #endregion
}
