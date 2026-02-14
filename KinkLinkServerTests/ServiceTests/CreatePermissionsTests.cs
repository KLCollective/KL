using KinkLinkCommon.Domain.Enums;
using KinkLinkServerTests.Database;
using KinkLinkServerTests.TestInfrastructure;

namespace KinkLinkServerTests.ServiceTests;

public class CreatePermissionsTests : DatabaseServiceTestBase
{
    public CreatePermissionsTests(TestDatabaseFixture fixture) : base(fixture) { }

    [Fact]
    public async Task CreatePermissions_ValidUids_ReturnsPairCreated()
    {
        await Fixture.ResetDatabaseAsync();
        
        var (_, _, uid1) = await CreateTestUserWithProfileAsync(111111111111111111, "CREATE1");
        var (_, _, uid2) = await CreateTestUserWithProfileAsync(222222222222222222, "CREATE2");
        
        var result = await DatabaseService.CreatePermissions(uid1, uid2);
        
        Assert.Equal(DBPairResult.PairCreated, result);
    }

    [Fact]
    public async Task CreatePermissions_SameUid_ReturnsPairUIDDoesNotExist()
    {
        await Fixture.ResetDatabaseAsync();
        
        var (_, _, uid) = await CreateTestUserWithProfileAsync(111111111111111111, "SAMEUSER1");
        
        var result = await DatabaseService.CreatePermissions(uid, uid);
        
        Assert.Equal(DBPairResult.PairUIDDoesNotExist, result);
    }

    [Fact]
    public async Task CreatePermissions_EmptyUids_ReturnsPairUIDDoesNotExist()
    {
        await Fixture.ResetDatabaseAsync();
        
        var result = await DatabaseService.CreatePermissions("", "VALIDUID");
        
        Assert.Equal(DBPairResult.PairUIDDoesNotExist, result);
    }

    [Fact]
    public async Task CreatePermissions_ExistingOneSidedPair_ReturnsOnesidedPairExists()
    {
        await Fixture.ResetDatabaseAsync();
        
        var (_, profileId1, uid1) = await CreateTestUserWithProfileAsync(111111111111111111, "ONEWAY1");
        var (_, profileId2, uid2) = await CreateTestUserWithProfileAsync(222222222222222222, "ONEWAY2");
        
        await TestHarness.InsertTestPairAsync(new InsertTestPairParams
        {
            Id = profileId1,
            PairId = profileId2
        });
        
        var result = await DatabaseService.CreatePermissions(uid1, uid2);
        
        Assert.Equal(DBPairResult.OnesidedPairExists, result);
    }

    [Fact]
    public async Task CreatePermissions_BidirectionalPair_ReturnsPaired()
    {
        await Fixture.ResetDatabaseAsync();
        
        var (_, profileId1, uid1) = await CreateTestUserWithProfileAsync(111111111111111111, "BIDIR1");
        var (_, profileId2, uid2) = await CreateTestUserWithProfileAsync(222222222222222222, "BIDIR2");
        
        await TestHarness.InsertTestPairAsync(new InsertTestPairParams
        {
            Id = profileId1,
            PairId = profileId2
        });
        await TestHarness.InsertTestPairAsync(new InsertTestPairParams
        {
            Id = profileId2,
            PairId = profileId1
        });
        
        var result = await DatabaseService.CreatePermissions(uid1, uid2);
        
        Assert.Equal(DBPairResult.Paired, result);
    }

    [Fact]
    public async Task CreatePermissions_NonExistentUser_ReturnsPairUIDDoesNotExist()
    {
        await Fixture.ResetDatabaseAsync();
        
        var (_, _, uid) = await CreateTestUserWithProfileAsync(111111111111111111, "EXISTING1");
        
        var result = await DatabaseService.CreatePermissions(uid, "NONEXISTENT");
        
        Assert.Equal(DBPairResult.PairUIDDoesNotExist, result);
    }
}
