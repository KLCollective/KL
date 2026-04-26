using Discord;
using Discord.WebSocket;
using KinkLinkBot.Configuration;
using KinkLinkBot.Services;
using KinkLinkCommon.Security;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;

namespace KinkLinkBot;

public class Program
{
    public static async Task Main(string[] args)
    {
        // Configure Serilog
        var logConf = new LoggerConfiguration()
            .MinimumLevel.Information()
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
            Log.Information("Starting KinkLink Bot");

            // Load configuration
            var configPath = args.Length > 0 ? args[0] : "/app/config.json";
            var config = BotConfiguration.Load(configPath);

            if (config == null)
            {
                Log.Fatal("Failed to load configuration from {ConfigPath}. Exiting.", configPath);
                return;
            }

            // Validate database connection string
            if (string.IsNullOrEmpty(config.DbConnectionString))
            {
                Log.Fatal("Database connection string not configured. Exiting.");
                return;
            }

            Log.Information("Configuration loaded successfully");

            // Set up Discord client
            var discordConfig = new DiscordSocketConfig
            {
                GatewayIntents = GatewayIntents.Guilds | GatewayIntents.GuildMessages,
                LogLevel = LogSeverity.Info,
            };

            var client = new DiscordSocketClient(discordConfig);

            // Set up services
            var services = new ServiceCollection();

            // Add Serilog to logging
            services.AddLogging(builder =>
            {
                builder.AddSerilog();
            });

            services.AddSingleton(client);
            services.AddSingleton(_ => config);
            services.AddSingleton<ISecretHasher, SecretHasher>();

            // Register application services
            // RegistrationService creates its own QueriesSql with connection string
            services.AddSingleton<RegistrationService>(sp =>
            {
                var logger = sp.GetRequiredService<ILogger<RegistrationService>>();
                var discordClient = sp.GetRequiredService<DiscordSocketClient>();
                var secretHasher = sp.GetRequiredService<ISecretHasher>();
                return new RegistrationService(logger, discordClient, config, secretHasher);
            });
            services.AddSingleton<DiscordInteractionHandler>();

            var serviceProvider = services.BuildServiceProvider();

            // Get services
            var registrationService = serviceProvider.GetRequiredService<RegistrationService>();
            var interactionHandler =
                serviceProvider.GetRequiredService<DiscordInteractionHandler>();

            // Initialize interaction handler
            interactionHandler.Initialize();

            // Set up logging
            client.Log += LogAsync;

            // Login and start
            await client.LoginAsync(TokenType.Bot, config.Bot.Token);
            await client.StartAsync();

            // Check server health
            Log.Information("Bot is ready. Press Ctrl+C to stop.");

            // Keep the bot running
            await Task.Delay(-1);
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Bot terminated unexpectedly");
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }

    private static Task LogAsync(LogMessage message)
    {
        var level = message.Severity switch
        {
            LogSeverity.Critical => LogEventLevel.Fatal,
            LogSeverity.Error => LogEventLevel.Error,
            LogSeverity.Warning => LogEventLevel.Warning,
            LogSeverity.Info => LogEventLevel.Information,
            LogSeverity.Verbose => LogEventLevel.Debug,
            LogSeverity.Debug => LogEventLevel.Debug,
            _ => LogEventLevel.Information,
        };

        Log.Write(level, "[Discord] [{Severity}] {Message}", message.Severity, message.Message);
        return Task.CompletedTask;
    }
}
