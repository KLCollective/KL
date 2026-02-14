using KinkLinkCommon.Domain.Enums;
using KinkLinkServerTests.TestInfrastructure;

namespace KinkLinkServerTests.ServiceTests;

public class LoginUserTests : DatabaseServiceTestBase
{
    public LoginUserTests(TestDatabaseFixture fixture) : base(fixture) { }

    [Fact]
    public async Task LoginUser_ValidSecretAndUid_ReturnsAuthorized()
    {
        await Fixture.ResetDatabaseAsync();
        
        await CreateTestUserWithProfileAsync(123456789012345678, "LOGINUSER1", "dummy_hash");
        
        var result = await DatabaseService.LoginUser("test_secret_key_123", "LOGINUSER1");
        
        Assert.Equal(DBAuthenticationStatus.Authorized, result);
    }

    [Fact]
    public async Task LoginUser_InvalidUid_ReturnsUnauthorized()
    {
        await Fixture.ResetDatabaseAsync();
        
        var result = await DatabaseService.LoginUser("some_secret", "NONEXISTENT");
        
        Assert.Equal(DBAuthenticationStatus.Unauthorized, result);
    }

    [Fact]
    public async Task LoginUser_NullSecret_ReturnsUnauthorized()
    {
        await Fixture.ResetDatabaseAsync();
        
        var result = await DatabaseService.LoginUser(null!, "some_uid");
        
        Assert.Equal(DBAuthenticationStatus.Unauthorized, result);
    }

    [Fact]
    public async Task LoginUser_EmptySecret_ReturnsUnauthorized()
    {
        await Fixture.ResetDatabaseAsync();
        
        var result = await DatabaseService.LoginUser("", "some_uid");
        
        Assert.Equal(DBAuthenticationStatus.Unauthorized, result);
    }

    [Fact]
    public async Task LoginUser_NullUid_ReturnsUnauthorized()
    {
        await Fixture.ResetDatabaseAsync();
        
        var result = await DatabaseService.LoginUser("some_secret", null!);
        
        Assert.Equal(DBAuthenticationStatus.Unauthorized, result);
    }

    [Fact]
    public async Task LoginUser_EmptyUid_ReturnsUnauthorized()
    {
        await Fixture.ResetDatabaseAsync();
        
        var result = await DatabaseService.LoginUser("some_secret", "");
        
        Assert.Equal(DBAuthenticationStatus.Unauthorized, result);
    }
}
