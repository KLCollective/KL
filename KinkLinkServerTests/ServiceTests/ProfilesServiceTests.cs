using KinkLinkCommon.Domain.Network;
using KinkLinkServer.Domain;
using KinkLinkServer.Services;
using KinkLinkServerTests.Database;
using KinkLinkServerTests.TestInfrastructure;
using Microsoft.Extensions.Logging;

namespace KinkLinkServerTests.ServiceTests;

[Collection("DatabaseCollection")]
public class ProfilesServiceTests : DatabaseServiceTestBase
{
    private readonly KinkLinkProfilesService _profilesService;

    public ProfilesServiceTests(TestDatabaseFixture fixture)
        : base(fixture)
    {
        var config = new Configuration(
            Fixture.ConnectionString,
            "test_signing_key_that_is_long_enough_for_hs256",
            "http://localhost:5006"
        );

        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var logger = loggerFactory.CreateLogger<KinkLinkProfilesService>();
        var metricsService = new MetricsService();

        _profilesService = new KinkLinkProfilesService(config, metricsService, logger);
    }

    #region ExistsAsync Tests

    [Fact]
    public async Task ExistsAsync_ProfileExists_ReturnsTrue()
    {
        await Fixture.ResetDatabaseAsync();

        await CreateTestUserWithProfileAsync(111111111111111200, "PRTEST1");

        var result = await _profilesService.ExistsAsync("PRTEST1");

        Assert.True(result);
    }

    [Fact]
    public async Task ExistsAsync_ProfileNotExists_ReturnsFalse()
    {
        await Fixture.ResetDatabaseAsync();

        var result = await _profilesService.ExistsAsync("NONEXISTENT");

        Assert.False(result);
    }

    #endregion

    #region GetIdFromUidAsync Tests

    [Fact]
    public async Task GetIdFromUidAsync_ProfileExists_ReturnsUserId()
    {
        await Fixture.ResetDatabaseAsync();

        var (userId, _, _) = await CreateTestUserWithProfileAsync(111111111111111201, "PRTEST2");

        var result = await _profilesService.GetIdFromUidAsync("PRTEST2");

        Assert.Equal(userId, result);
    }

    [Fact]
    public async Task GetIdFromUidAsync_ProfileNotExists_ReturnsNull()
    {
        await Fixture.ResetDatabaseAsync();

        var result = await _profilesService.GetIdFromUidAsync("NONEXISTENT");

        Assert.Null(result);
    }

    #endregion

    #region GetProfileByUidAsync Tests

    [Fact]
    public async Task GetProfileByUidAsync_ProfileExists_ReturnsProfile()
    {
        await Fixture.ResetDatabaseAsync();

        var (_, _, _) = await CreateTestUserWithProfileAsync(
            111111111111111202,
            "PRTEST3",
            "test user key",
            "TestRole",
            "TestAlias",
            "Doll",
            "Test description"
        );

        var result = await _profilesService.GetProfileByUidAsync("PRTEST3");

        Assert.NotNull(result);
        Assert.Equal("PRTEST3", result.Uid);
        Assert.Equal("TestRole", result.ChatRole);
        Assert.Equal("TestAlias", result.Alias);
        Assert.Equal(Title.Doll, result.Title);
        Assert.Equal("Test description", result.Description);
        Assert.Null(result.CreatedAt);
        Assert.Null(result.UpdatedAt);
    }

    [Fact]
    public async Task GetProfileByUidAsync_NullTitle_ReturnsKinkster()
    {
        await Fixture.ResetDatabaseAsync();

        await CreateTestUserWithProfileAsync(
            111111111111111203,
            "PRTEST4",
            "test user key",
            "Role",
            "Alias",
            null,
            "test description"
        );

        var result = await _profilesService.GetProfileByUidAsync("PRTEST4");

        Assert.NotNull(result);
        Assert.Equal(Title.Kinkster, result.Title);
    }

    [Fact]
    public async Task GetProfileByUidAsync_ProfileNotExists_ReturnsNull()
    {
        await Fixture.ResetDatabaseAsync();

        var result = await _profilesService.GetProfileByUidAsync("NONEXISTENT");

        Assert.Null(result);
    }

    #endregion

    #region UpdateDetailsByUidAsync Tests

    [Fact]
    public async Task UpdateDetailsByUidAsync_ProfileExists_ReturnsUpdatedProfile()
    {
        await Fixture.ResetDatabaseAsync();

        await CreateTestUserWithProfileAsync(
            111111111111111204,
            "PRTEST5",
            "Test secrete key",
            "OldRole",
            "OldAlias",
            "OldTitle",
            "Old description"
        );

        var result = await _profilesService.UpdateDetailsByUidAsync(
            "PRTEST5",
            Title.Doll,
            "NewAlias",
            "NewRole",
            "New description"
        );

        Assert.NotNull(result);
        Assert.Equal("PRTEST5", result.Uid);
        Assert.Equal("NewAlias", result.Alias);
        Assert.Equal("NewRole", result.ChatRole);
        Assert.Equal(Title.Doll, result.Title);
        Assert.Equal("New description", result.Description);
        Assert.NotNull(result.CreatedAt);
        Assert.NotNull(result.UpdatedAt);
    }

    [Fact]
    public async Task UpdateDetailsByUidAsync_ProfileNotExists_ReturnsNull()
    {
        await Fixture.ResetDatabaseAsync();

        var result = await _profilesService.UpdateDetailsByUidAsync(
            "NONEXISTENT",
            Title.Kinkster,
            "Alias",
            "Role",
            "Desc"
        );

        Assert.Null(result);
    }

    #endregion
}
