using KinkLinkCommon.Domain;
using KinkLinkCommon.Domain.Enums;
using KinkLinkServerTests.Database;
using KinkLinkServerTests.TestInfrastructure;

namespace KinkLinkServerTests.ServiceTests;

public class GetPermissionsTests : DatabaseServiceTestBase
{
    public GetPermissionsTests(TestDatabaseFixture fixture)
        : base(fixture) { }

    [Fact]
    public async Task GetPermissions_ExistingPair_ReturnsPermissions()
    {
        await Fixture.ResetDatabaseAsync();

        var (userId1, profileId1, uid1) = await CreateTestUserWithProfileAsync(
            111111111111111111,
            "GETPERM1"
        );
        var (userId2, profileId2, uid2) = await CreateTestUserWithProfileAsync(
            222222222222222222,
            "GETPERM2"
        );

        await TestHarness.InsertTestPairAsync(
            new InsertTestPairParams
            {
                Id = profileId1,
                PairId = profileId2,
                Priority = 1,
                Interaction = 14,
            }
        );
        await TestHarness.InsertTestPairAsync(
            new InsertTestPairParams
            {
                Id = profileId2,
                PairId = profileId1,
                Priority = 1,
                Interaction = 14,
            }
        );

        var result = await PermissionsService.GetPermissions(uid1, uid2);

        Assert.NotNull(result);
    }

    [Fact]
    public async Task GetPermissions_NonExistentPair_ReturnsNull()
    {
        await Fixture.ResetDatabaseAsync();

        var (_, _, uid1) = await CreateTestUserWithProfileAsync(111111111111111111, "GETPERM1");
        var (_, _, uid2) = await CreateTestUserWithProfileAsync(222222222222222222, "GETPERM2");

        var result = await PermissionsService.GetPermissions(uid1, uid2);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetPermissions_SameUid_ReturnsNull()
    {
        await Fixture.ResetDatabaseAsync();

        var (_, _, uid) = await CreateTestUserWithProfileAsync(111111111111111111, "SAMEUID1");

        var result = await PermissionsService.GetPermissions(uid, uid);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetPermissions_EmptyUids_ReturnsNull()
    {
        await Fixture.ResetDatabaseAsync();

        var result = await PermissionsService.GetPermissions("", "VALID");

        Assert.Null(result);
    }

    [Fact]
    public async Task GetPermissions_NonExistentUser_ReturnsNull()
    {
        await Fixture.ResetDatabaseAsync();

        var (_, _, uid) = await CreateTestUserWithProfileAsync(111111111111111111, "EXISTS1");

        var result = await PermissionsService.GetPermissions(uid, "NONEXISTENT");

        Assert.Null(result);
    }
}
