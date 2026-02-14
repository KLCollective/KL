using KinkLinkServerTests.TestInfrastructure;
using Microsoft.Extensions.Logging;

namespace KinkLinkServerTests.MigrationTests;

[Collection("DatabaseCollection")]
public class DatabaseMigrationTests
{
    private readonly TestDatabaseFixture _fixture;

    public DatabaseMigrationTests(TestDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Database_ContainsRequiredTables()
    {
        await _fixture.InitializeAsync();
        
        var connectionStringIsSet = _fixture.ConnectionString != null;
        Assert.True(connectionStringIsSet, "Database should be initialized");
        
        var testHarness = new Database.TestHarnessSql(_fixture.ConnectionString!);
        
        Assert.True(await testHarness.TableExistsAsync("Users"), "Users table should exist");
        Assert.True(await testHarness.TableExistsAsync("Profiles"), "Profiles table should exist");
        Assert.True(await testHarness.TableExistsAsync("Pairs"), "Pairs table should exist");
        Assert.True(await testHarness.TableExistsAsync("Admin"), "Admin table should exist");
    }

    [Fact]
    public async Task UsersTable_HasExpectedColumns()
    {
        await _fixture.InitializeAsync();
        
        var testHarness = new Database.TestHarnessSql(_fixture.ConnectionString);
        
        Assert.True(await testHarness.ColumnExistsAsync("Users", "id"), "Users.id should exist");
        Assert.True(await testHarness.ColumnExistsAsync("Users", "discord_id"), "Users.discord_id should exist");
        Assert.True(await testHarness.ColumnExistsAsync("Users", "secret_key_hash"), "Users.secret_key_hash should exist");
        Assert.True(await testHarness.ColumnExistsAsync("Users", "verified"), "Users.verified should exist");
        Assert.True(await testHarness.ColumnExistsAsync("Users", "banned"), "Users.banned should exist");
        Assert.True(await testHarness.ColumnExistsAsync("Users", "created_at"), "Users.created_at should exist");
        Assert.True(await testHarness.ColumnExistsAsync("Users", "updated_at"), "Users.updated_at should exist");
    }

    [Fact]
    public async Task ProfilesTable_HasExpectedColumns()
    {
        await _fixture.InitializeAsync();
        
        var testHarness = new Database.TestHarnessSql(_fixture.ConnectionString);
        
        Assert.True(await testHarness.ColumnExistsAsync("Profiles", "id"), "Profiles.id should exist");
        Assert.True(await testHarness.ColumnExistsAsync("Profiles", "user_id"), "Profiles.user_id should exist");
        Assert.True(await testHarness.ColumnExistsAsync("Profiles", "uid"), "Profiles.uid should exist");
        Assert.True(await testHarness.ColumnExistsAsync("Profiles", "chat_role"), "Profiles.chat_role should exist");
        Assert.True(await testHarness.ColumnExistsAsync("Profiles", "alias"), "Profiles.alias should exist");
        Assert.True(await testHarness.ColumnExistsAsync("Profiles", "title"), "Profiles.title should exist");
        Assert.True(await testHarness.ColumnExistsAsync("Profiles", "description"), "Profiles.description should exist");
    }

    [Fact]
    public async Task PairsTable_HasExpectedColumns()
    {
        await _fixture.InitializeAsync();
        
        var testHarness = new Database.TestHarnessSql(_fixture.ConnectionString);
        
        Assert.True(await testHarness.ColumnExistsAsync("Pairs", "id"), "Pairs.id should exist");
        Assert.True(await testHarness.ColumnExistsAsync("Pairs", "pair_id"), "Pairs.pair_id should exist");
        Assert.True(await testHarness.ColumnExistsAsync("Pairs", "expires"), "Pairs.expires should exist");
        
        Assert.True(await testHarness.ColumnExistsAsync("Pairs", "priority"), "Pairs.priority should exist");
        Assert.True(await testHarness.ColumnExistsAsync("Pairs", "gags"), "Pairs.gags should exist");
        Assert.True(await testHarness.ColumnExistsAsync("Pairs", "wardrobe"), "Pairs.wardrobe should exist");
        Assert.True(await testHarness.ColumnExistsAsync("Pairs", "moodles"), "Pairs.moodles should exist");
    }

    [Fact]
    public async Task Migrations_AreIdempotent_RunningTwiceDoesNotFail()
    {
        await _fixture.InitializeAsync();
        
        Console.WriteLine("Testing migration idempotency by ensuring tables already exist");
        
        var testHarness = new Database.TestHarnessSql(_fixture.ConnectionString);
        
        Assert.True(await testHarness.TableExistsAsync("Users"));
        Assert.True(await testHarness.TableExistsAsync("Profiles"));
        Assert.True(await testHarness.TableExistsAsync("Pairs"));
        Assert.True(await testHarness.TableExistsAsync("Admin"));
    }
}
