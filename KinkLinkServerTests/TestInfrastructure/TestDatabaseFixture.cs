using System.Reflection;
using DbUp;
using DotNet.Testcontainers.Builders;
using Testcontainers.PostgreSql;

namespace KinkLinkServerTests.TestInfrastructure;

public sealed class TestDatabaseFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres;
    private bool _initialized;

    public string ConnectionString { get; private set; } = null!;

    public TestDatabaseFixture()
    {
        _postgres = new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .WithDatabase("kinklink_test")
            .WithUsername("test")
            .WithPassword("test")
            .WithCleanUp(true)
            .Build();
    }

    public async Task InitializeAsync()
    {
        try
        {
            if (_initialized) 
            {
                Console.WriteLine("Database already initialized, skipping");
                return;
            }
            
            Console.WriteLine("Starting PostgreSQL test container...");
            
            await _postgres.StartAsync();
            ConnectionString = _postgres.GetConnectionString();
            
            Console.WriteLine($"PostgreSQL test container started on port {_postgres.GetMappedPublicPort(5432)}");
            
            await RunMigrationsAsync();
            
            _initialized = true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error during initialization: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            throw;
        }
    }

    private async Task RunMigrationsAsync()
    {
        Console.WriteLine("Running database migrations...");
        
        EnsureDatabase.For.PostgresqlDatabase(ConnectionString);
        
        Console.WriteLine("Loading KinkLinkServer assembly...");
        
        Assembly? assembly = null;
        try
        {
            assembly = Assembly.Load("KinkLinkServer");
            Console.WriteLine($"Loaded assembly: {assembly.FullName}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to load assembly: {ex.Message}");
            throw;
        }
        
        var upgrader = DeployChanges.To.PostgresqlDatabase(ConnectionString)
            .WithScriptsEmbeddedInAssembly(assembly)
            .LogToConsole()
            .Build();
        
        var result = upgrader.PerformUpgrade();
        
        if (!result.Successful)
        {
            Console.WriteLine($"Database migration failed: {result.Error}");
            throw new InvalidOperationException("Database migration failed", result.Error);
        }
        
        Console.WriteLine("Database migrations completed successfully");
    }

    public async Task ResetDatabaseAsync()
    {
        Console.WriteLine("Resetting database for test isolation...");
        
        await using var conn = new Npgsql.NpgsqlConnection(ConnectionString);
        await conn.OpenAsync();
        
        await using var cmd = new Npgsql.NpgsqlCommand(
            "TRUNCATE TABLE Pairs, Profiles, Users RESTART IDENTITY CASCADE",
            conn);
        
        await cmd.ExecuteNonQueryAsync();
        
        Console.WriteLine("Database reset complete");
    }

    public async Task DisposeAsync()
    {
        Console.WriteLine("Stopping PostgreSQL test container...");
        await _postgres.DisposeAsync();
    }
}

[CollectionDefinition("DatabaseCollection")]
public class DatabaseCollection : ICollectionFixture<TestDatabaseFixture>
{
}
