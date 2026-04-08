using System.Text.Json;
using System.Text.Json.Serialization;

namespace KinkLinkBot.Configuration;

/// <summary>
///     Bot configuration loaded from config.json
/// </summary>
public class BotConfiguration
{
    [JsonPropertyName("bot")]
    public BotConfig Bot { get; set; } = new();

    [JsonPropertyName("db_connection_string")]
    public String DbConnectionString { get; set; } = "";

    public class BotConfig
    {
        [JsonPropertyName("token")]
        public string Token { get; set; } = string.Empty;

        [JsonPropertyName("guild_id")]
        public ulong GuildId { get; set; }

        [JsonPropertyName("channel_id")]
        public ulong ChannelId { get; set; }

        [JsonPropertyName("admin_role_id")]
        public ulong AdminRoleId { get; set; }

        [JsonPropertyName("alpha_tester_role_id")]
        public ulong AlphaTesterRoleId { get; set; }
    }

    public static BotConfiguration? Load(string configPath = "config.json")
    {
        try
        {
            if (!File.Exists(configPath))
            {
                Console.WriteLine($"[Configuration] Config file not found at {configPath}");
                return null;
            }

            var json = File.ReadAllText(configPath);
            var config = JsonSerializer.Deserialize<BotConfiguration>(json);

            if (config == null || !config.IsValid())
            {
                Console.WriteLine("[Configuration] Invalid configuration");
                return null;
            }

            Console.WriteLine("[Configuration] Configuration loaded successfully");
            return config;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Configuration] Error loading config: {ex.Message}");
            return null;
        }
    }

    private bool IsValid()
    {
        return !string.IsNullOrEmpty(Bot.Token) &&
               Bot.GuildId != 0 &&
               Bot.ChannelId != 0 &&
               !string.IsNullOrEmpty(DbConnectionString);
    }
}
