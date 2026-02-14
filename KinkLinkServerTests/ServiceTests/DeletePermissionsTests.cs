using KinkLinkCommon.Domain.Enums;
using KinkLinkServerTests.TestInfrastructure;

namespace KinkLinkServerTests.ServiceTests;

public class DeletePermissionsTests : DatabaseServiceTestBase
{
    public DeletePermissionsTests(TestDatabaseFixture fixture) : base(fixture) { }

    [Fact]
    public async Task DeletePermissions_ExistingPair_ReturnsSuccess()
    {
        await Fixture.ResetDatabaseAsync();
        
        var (profileId1, profileId2, uid1, uid2) = await CreatePairedProfilesAsync();
        
        var result = await DatabaseService.DeletePermissions(uid1, uid2);
        
        Assert.Equal(DBPairResult.Success, result);
    }

    [Fact]
    public async Task DeletePermissions_NonExistentPair_ReturnsNoOp()
    {
        await Fixture.ResetDatabaseAsync();
        
        var (_, _, uid1) = await CreateTestUserWithProfileAsync(111111111111111111, "DEL1");
        var (_, _, uid2) = await CreateTestUserWithProfileAsync(222222222222222222, "DEL2");
        
        var result = await DatabaseService.DeletePermissions(uid1, uid2);
        
        Assert.Equal(DBPairResult.NoOp, result);
    }

    [Fact]
    public async Task DeletePermissions_SameUid_ReturnsPairUIDDoesNotExist()
    {
        await Fixture.ResetDatabaseAsync();
        
        var (_, _, uid) = await CreateTestUserWithProfileAsync(111111111111111111, "SAME2");
        
        var result = await DatabaseService.DeletePermissions(uid, uid);
        
        Assert.Equal(DBPairResult.PairUIDDoesNotExist, result);
    }

    [Fact]
    public async Task DeletePermissions_EmptyUids_ReturnsPairUIDDoesNotExist()
    {
        await Fixture.ResetDatabaseAsync();
        
        var result = await DatabaseService.DeletePermissions("", "VALID");
        
        Assert.Equal(DBPairResult.PairUIDDoesNotExist, result);
    }

    [Fact]
    public async Task DeletePermissions_NonExistentUser_ReturnsPairUIDDoesNotExist()
    {
        await Fixture.ResetDatabaseAsync();
        
        var (_, _, uid) = await CreateTestUserWithProfileAsync(111111111111111111, "EXISTS2");
        
        var result = await DatabaseService.DeletePermissions(uid, "NONEXISTENT");
        
        Assert.Equal(DBPairResult.PairUIDDoesNotExist, result);
    }
}
