using System.Reflection;
using System.Text;
using DbUp;
using KinkLinkCommon.Database;
using KinkLinkCommon.Security;
using KinkLinkServer.Domain;
using KinkLinkServer.Domain.Interfaces;
using KinkLinkServer.Extensions;
using KinkLinkServer.Infrastructure;
using KinkLinkServer.Managers;
using KinkLinkServer.Services;
using KinkLinkServer.SignalR.Handlers;
using KinkLinkServer.SignalR.Handlers.Interactions;
using KinkLinkServer.SignalR.Hubs;
using MessagePack;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Npgsql;
using Prometheus;
using Serilog;
using Serilog.Events;

namespace KinkLinkServer;

// ReSharper disable once ClassNeverInstantiated.Global

public class Program
{
    private static LogEventLevel GetLogLevel() =>
        Environment.GetEnvironmentVariable("LOG_LEVEL") is { } levelStr
        && Enum.TryParse<LogEventLevel>(levelStr, ignoreCase: true, out var level)
            ? level
            : LogEventLevel.Information;

    private static async Task Main(string[] args)
    {
        // Attempt to load configuration values
        if (Configuration.Load() is not { } configuration)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            System.Console.WriteLine(
                "Error cannot load Configuiration at {Configuration.ConfigurationPath}"
            );
            Console.ResetColor();
            Environment.Exit(1);
            return;
        }

        // Configure Serilog
        var logLevel = GetLogLevel();
        var logConf = new LoggerConfiguration().MinimumLevel.Is(logLevel);

        // Configure Serilog
        logConf
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("Discord", LogEventLevel.Information)
            .Enrich.FromLogContext()
            .Enrich.WithMachineName()
            .Enrich.WithEnvironmentName();

        // For structured logging in prod, but relatively readable for local testing.
        if (Environment.GetEnvironmentVariable("LOGGER_OUTPUT") == "JSON")
        {
            logConf.WriteTo.Console(formatter: new Serilog.Formatting.Json.JsonFormatter());
        }
        else
        {
            logConf.WriteTo.Console(
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}"
            );
        }

        Log.Logger = logConf.CreateLogger();

        try
        {
            Log.Information("Starting KinkLink Server");

            // Migrate the database prior to building the WebApplication
            var upgrader = DeployChanges
                .To.PostgresqlDatabase(configuration.DatabaseConnectionString)
                .WithScriptsEmbeddedInAssembly(Assembly.GetExecutingAssembly())
                .LogToConsole()
                .Build();
            var result = upgrader.PerformUpgrade();
            if (!result.Successful)
            {
                Log.Fatal("Database migration failed: {Error}", result.Error);
                Environment.Exit(1);
            }
            if (TestDataSeeder.ShouldSeed())
            {
                var seed_result = await TestDataSeeder.SeedAsync(configuration);

                if (!seed_result)
                {
                    Log.Fatal("Seeding data failed in test environment", result.Error);
                    Environment.Exit(1);
                }
            }

            Log.Information("Database migration completed successfully");

            // Create service builder
            var builder = WebApplication.CreateBuilder(args);
            builder.Host.UseSerilog();

            // Configuration Authentication and Authorization
            ConfigureJwtAuthentication(builder.Services, configuration);

            // Add services to the container
            builder.Services.AddControllers();

            // Add metrics
            builder.Services.AddKinkLinkMetrics();
            builder
                .Services.AddSignalR(options => options.EnableDetailedErrors = true)
                .AddMessagePackProtocol(options =>
                    options.SerializerOptions = MessagePackSerializerOptions
                        .Standard.WithSecurity(MessagePackSecurity.UntrustedData)
                        .WithJsonElementSupport()
                );
            builder.Services.AddSingleton(configuration);

            // Database services
            builder.Services.AddSingleton<PairsService>();
            builder.Services.AddSingleton<AuthService>();
            builder.Services.AddSingleton<KinkLinkProfileConfigService>();
            builder.Services.AddSingleton<KinkLinkProfilesService>();
            builder.Services.AddSingleton<ProfilesSql>();
            builder.Services.AddSingleton<PermissionsService>();
            builder.Services.AddSingleton<WardrobeDataService>();
            builder.Services.AddSingleton<LockService>();

            // Business services
            builder.Services.AddSingleton<IPresenceService, PresenceService>();
            builder.Services.AddSingleton<ISecretHasher, SecretHasher>();
            builder.Services.AddSingleton<IRequestLoggingService, RequestLoggingService>();

            // Managers
            builder.Services.AddSingleton<IForwardedRequestManager, ForwardedRequestManager>();

            // Interaction handlers (auto-registered)
            builder.Services.AddSingleton<WardrobeApplyInteractionHandler>();
            builder.Services.AddSingleton<LockWardrobeInteractionHandler>();
            builder.Services.AddSingleton<UnlockWardrobeInteractionHandler>();
            builder.Services.AddSingleton<
                IPairInteractionHandlerFactory,
                PairInteractionHandlerFactory
            >();
            builder.Services.AddSingleton<INotificationService, NotificationService>();

            // Handles
            builder.Services.AddSingleton<OnlineStatusUpdateHandler>();
            builder.Services.AddSingleton<AddFriendHandler>();
            builder.Services.AddSingleton<ChatHandler>();
            builder.Services.AddSingleton<CustomizePlusHandler>();
            builder.Services.AddSingleton<EmoteHandler>();
            builder.Services.AddSingleton<GetAccountDataHandler>();
            builder.Services.AddSingleton<HonorificHandler>();
            builder.Services.AddSingleton<LocksHandler>();
            builder.Services.AddSingleton<MoodlesHandler>();
            builder.Services.AddSingleton<PairInteractionsHandler>();
            builder.Services.AddSingleton<RemoveFriendHandler>();
            builder.Services.AddSingleton<SpeakHandler>();
            builder.Services.AddSingleton<UpdateFriendHandler>();
            // NOTE: HTTP endpoint is configured as traefik will be used as a reverse proxy
            // with TLS termination. This will never be exposed to the open internet.
            builder.WebHost.UseUrls("http://*:5006");
            var app = builder.Build();

            // Configure the HTTP request pipeline
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
                app.UseHsts();
            }

            app.UseRouting();

            // Add metrics middleware
            app.UseMiddleware<MetricsMiddleware>();

            // Add Prometheus metrics endpoint
            app.UseMetricServer();

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapHub<PrimaryHub>("/primaryHub");
            app.MapControllers();

            app.Run();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Application terminated unexpectedly");
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }

    private static void ConfigureJwtAuthentication(
        IServiceCollection services,
        Configuration configuration
    )
    {
        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateAudience = false,
                    ValidateIssuer = false,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.ASCII.GetBytes(configuration.SigningKey)
                    ),
                };
            });
    }
}
