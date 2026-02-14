using KinkLinkCommon.Domain;
using KinkLinkCommon.Domain.Enums;
using KinkLinkServerTests.TestInfrastructure;

namespace KinkLinkServerTests.ServiceTests;

public class UpdatePermissionsTests : DatabaseServiceTestBase
{
    public UpdatePermissionsTests(TestDatabaseFixture fixture) : base(fixture) { }

    [Fact]
    public async Task UpdatePermissions_ValidPair_ReturnsSuccess()
    {
        await Fixture.ResetDatabaseAsync();
        
        var (profileId1, profileId2, uid1, uid2) = await CreatePairedProfilesAsync();
        
        var permissions = new UserPermissions(
            false,
            RelationshipPriority.Serious,
            GagPermissions.CanApply | GagPermissions.CanRemove,
            WardrobePermissions.CanApply | WardrobePermissions.CanRemove,
            MoodlesPermissions.CanApplyOwn
        );
        
        var result = await DatabaseService.UpdatePermissions(uid1, uid2, permissions);
        
        Assert.Equal(DBPairResult.Success, result);
    }

    [Fact]
    public async Task UpdatePermissions_NonExistentPair_ReturnsNoOp()
    {
        await Fixture.ResetDatabaseAsync();
        
        var (_, _, uid1) = await CreateTestUserWithProfileAsync(111111111111111111, "UPDATE1");
        var (_, _, uid2) = await CreateTestUserWithProfileAsync(222222222222222222, "UPDATE2");
        
        var permissions = new UserPermissions();
        
        var result = await DatabaseService.UpdatePermissions(uid1, uid2, permissions);
        
        Assert.Equal(DBPairResult.NoOp, result);
    }

    [Fact]
    public async Task UpdatePermissions_SameFriendCode_ReturnsPairUIDDoesNotExist()
    {
        await Fixture.ResetDatabaseAsync();
        
        var (_, _, uid) = await CreateTestUserWithProfileAsync(111111111111111111, "SAME1");
        
        var permissions = new UserPermissions();
        
        var result = await DatabaseService.UpdatePermissions(uid, uid, permissions);
        
        Assert.Equal(DBPairResult.PairUIDDoesNotExist, result);
    }

    [Fact]
    public async Task UpdatePermissions_EmptyFriendCodes_ReturnsPairUIDDoesNotExist()
    {
        await Fixture.ResetDatabaseAsync();
        
        var permissions = new UserPermissions();
        
        var result = await DatabaseService.UpdatePermissions("", "VALID", permissions);
        
        Assert.Equal(DBPairResult.PairUIDDoesNotExist, result);
    }

    [Fact]
    public async Task UpdatePermissions_NonExistentSender_ReturnsPairUIDDoesNotExist()
    {
        await Fixture.ResetDatabaseAsync();
        
        var (_, _, uid) = await CreateTestUserWithProfileAsync(111111111111111111, "EXISTS1");
        
        var permissions = new UserPermissions();
        
        var result = await DatabaseService.UpdatePermissions(uid, "NONEXISTENT", permissions);
        
        Assert.Equal(DBPairResult.PairUIDDoesNotExist, result);
    }
}
