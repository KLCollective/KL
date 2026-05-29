using System.Text.Json;
using Npgsql;

namespace KinkLinkServerTests.Database;

public class TestHarnessSql
{
    private readonly string _connectionString;

    public TestHarnessSql(string connectionString)
    {
        _connectionString = connectionString;
    }

    private NpgsqlConnection GetConnection() => new(_connectionString);

    public async Task<int?> InsertTestUserAsync(InsertTestUserParams @params)
    {
        await using var conn = GetConnection();
        await conn.OpenAsync();

        await using var cmd = new NpgsqlCommand(
            @"INSERT INTO Users (discord_id, secret_key_hash, verified, banned)
              VALUES (@discord_id, @secret_key_hash, @verified, @banned)
              RETURNING id",
            conn
        );

        cmd.Parameters.AddWithValue("discord_id", @params.DiscordId);
        cmd.Parameters.AddWithValue(
            "secret_key_hash",
            @params.SecretKeyHash ?? (object)DBNull.Value
        );
        cmd.Parameters.AddWithValue("verified", @params.Verified ?? false);
        cmd.Parameters.AddWithValue("banned", @params.Banned ?? false);

        var result = await cmd.ExecuteScalarAsync();
        return result as int?;
    }

    public async Task<int?> InsertTestUserAsync(
        long discordId,
        string? secretKeyHash = null,
        bool? verified = null,
        bool? banned = null
    )
    {
        return await InsertTestUserAsync(
            new InsertTestUserParams
            {
                DiscordId = discordId,
                SecretKeyHash = secretKeyHash,
                Verified = verified,
                Banned = banned,
            }
        );
    }

    public async Task<int?> InsertTestProfileAsync(InsertTestProfileParams @params)
    {
        await using var conn = GetConnection();
        await conn.OpenAsync();

        await using var cmd = new NpgsqlCommand(
            @"INSERT INTO Profiles (user_id, uid, chat_role, alias, title, description)
              VALUES (@user_id, @uid, @chat_role, @alias, @title, @description)
              RETURNING id",
            conn
        );

        cmd.Parameters.AddWithValue("user_id", @params.UserId);
        cmd.Parameters.AddWithValue("uid", @params.Uid);
        cmd.Parameters.AddWithValue("chat_role", @params.ChatRole ?? "");
        cmd.Parameters.AddWithValue("alias", @params.Alias ?? "");
        cmd.Parameters.AddWithValue("title", @params.Title ?? "Kinkster");
        cmd.Parameters.AddWithValue("description", @params.Description ?? (object)DBNull.Value);

        var result = await cmd.ExecuteScalarAsync();
        return result as int?;
    }

    public async Task<int?> InsertTestProfileAsync(
        int userId,
        string uid,
        string? chatRole = null,
        string? alias = null,
        string? title = null,
        string? description = null
    )
    {
        return await InsertTestProfileAsync(
            new InsertTestProfileParams
            {
                UserId = userId,
                Uid = uid,
                ChatRole = chatRole,
                Alias = alias,
                Title = title,
                Description = description,
            }
        );
    }

    public async Task<PairRecord?> InsertTestPairAsync(InsertTestPairParams @params)
    {
        await using var conn = GetConnection();
        await conn.OpenAsync();

        await using var cmd = new NpgsqlCommand(
            @"INSERT INTO Pairs (id, pair_id, priority, controls_perm, controls_config, disable_safeword, interactions)
              VALUES (@id, @pair_id, @priority, @controls_perm, @controls_config, @disable_safeword, @interaction)
              RETURNING id, pair_id, expires, priority, controls_perm, controls_config, disable_safeword, interactions",
            conn
        );

        cmd.Parameters.AddWithValue("id", @params.Id);
        cmd.Parameters.AddWithValue("pair_id", @params.PairId);
        cmd.Parameters.AddWithValue("priority", @params.Priority ?? 0);
        cmd.Parameters.AddWithValue("controls_perm", @params.ControlsPerm ?? false);
        cmd.Parameters.AddWithValue("controls_config", @params.ControlsConfig ?? false);
        cmd.Parameters.AddWithValue("disable_safeword", @params.DisableSafeword ?? false);
        cmd.Parameters.AddWithValue("interaction", @params.Interaction ?? 0L);

        await using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return new PairRecord
            {
                Id = reader.GetInt32(0),
                PairId = reader.GetInt32(1),
                Expires = reader.IsDBNull(2) ? null : reader.GetDateTime(2),
                Priority = reader.GetInt32(3),
                ControlsPerm = reader.GetBoolean(4),
                ControlsConfig = reader.GetBoolean(5),
                DisableSafeword = reader.GetBoolean(6),
                Interaction = reader.GetInt64(7),
            };
        }
        return null;
    }

    public async Task<PairRecord?> InsertTestPairWithPermissionsAsync(
        InsertTestPairWithPermissionsParams @params
    )
    {
        await using var conn = GetConnection();
        await conn.OpenAsync();

        await using var cmd = new NpgsqlCommand(
            @"INSERT INTO Pairs (id, pair_id, expires, priority, controls_perm, controls_config, disable_safeword, interactions)
              VALUES (@id, @pair_id, @expires, @priority, @controls_perm, @controls_config, @disable_safeword, @interaction)
              RETURNING id, pair_id, expires, priority, controls_perm, controls_config, disable_safeword, interactions",
            conn
        );

        cmd.Parameters.AddWithValue("id", @params.Id);
        cmd.Parameters.AddWithValue("pair_id", @params.PairId);
        cmd.Parameters.AddWithValue("expires", @params.Expires ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("priority", @params.Priority);
        cmd.Parameters.AddWithValue("controls_perm", @params.ControlsPerm);
        cmd.Parameters.AddWithValue("controls_config", @params.ControlsConfig);
        cmd.Parameters.AddWithValue("disable_safeword", @params.DisableSafeword);
        cmd.Parameters.AddWithValue("interaction", @params.Interaction);

        await using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return new PairRecord
            {
                Id = reader.GetInt32(0),
                PairId = reader.GetInt32(1),
                Expires = reader.IsDBNull(2) ? null : reader.GetDateTime(2),
                Priority = reader.GetInt32(3),
                ControlsPerm = reader.GetBoolean(4),
                ControlsConfig = reader.GetBoolean(5),
                DisableSafeword = reader.GetBoolean(6),
                Interaction = reader.GetInt64(7),
            };
        }
        return null;
    }

    public async Task<int?> DeleteTestUserByDiscordIdAsync(long discordId)
    {
        await using var conn = GetConnection();
        await conn.OpenAsync();

        await using var cmd = new NpgsqlCommand(
            @"WITH deleted_profiles AS (
                DELETE FROM Profiles WHERE user_id IN (SELECT id FROM Users WHERE Users.discord_id = @discord_id)
                RETURNING id
            )
            DELETE FROM Users WHERE Users.discord_id = @discord_id
            RETURNING id",
            conn
        );

        cmd.Parameters.AddWithValue("discord_id", discordId);

        var result = await cmd.ExecuteScalarAsync();
        return result as int?;
    }

    public async Task<int?> GetUserIdByDiscordIdAsync(long discordId)
    {
        await using var conn = GetConnection();
        await conn.OpenAsync();

        await using var cmd = new NpgsqlCommand(
            "SELECT id FROM Users WHERE discord_id = @discord_id",
            conn
        );

        cmd.Parameters.AddWithValue("discord_id", discordId);

        var result = await cmd.ExecuteScalarAsync();
        return result as int?;
    }

    public async Task<int?> DeleteTestProfileByUidAsync(string uid)
    {
        await using var conn = GetConnection();
        await conn.OpenAsync();

        await using var cmd = new NpgsqlCommand(
            "DELETE FROM Profiles WHERE uid = @uid RETURNING id",
            conn
        );

        cmd.Parameters.AddWithValue("uid", uid);

        var result = await cmd.ExecuteScalarAsync();
        return result as int?;
    }

    public async Task<PairRecord?> DeleteTestPairAsync(DeleteTestPairParams @params)
    {
        await using var conn = GetConnection();
        await conn.OpenAsync();

        await using var cmd = new NpgsqlCommand(
            @"DELETE FROM Pairs WHERE (id = @id AND pair_id = @pair_id) OR (id = @pair_id AND pair_id = @id)
              RETURNING id, pair_id, expires, priority, interactions",
            conn
        );

        cmd.Parameters.AddWithValue("id", @params.Id);
        cmd.Parameters.AddWithValue("pair_id", @params.PairId);

        await using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return new PairRecord
            {
                Id = reader.GetInt32(0),
                PairId = reader.GetInt32(1),
                Expires = reader.IsDBNull(2) ? null : reader.GetDateTime(2),
                Priority = reader.GetInt32(3),
                Interaction = reader.GetInt64(4),
            };
        }
        return null;
    }

    public async Task<int?> GetProfileIdByUidAsync(string uid)
    {
        await using var conn = GetConnection();
        await conn.OpenAsync();

        await using var cmd = new NpgsqlCommand("SELECT id FROM Profiles WHERE uid = @uid", conn);

        cmd.Parameters.AddWithValue("uid", uid);

        var result = await cmd.ExecuteScalarAsync();
        return result as int?;
    }

    public async Task<int?> GetUserIdByDiscordIdAsync(string discordId)
    {
        await using var conn = GetConnection();
        await conn.OpenAsync();

        await using var cmd = new NpgsqlCommand(
            "SELECT id FROM Users WHERE discord_id = @discord_id",
            conn
        );

        cmd.Parameters.AddWithValue("discord_id", discordId);

        var result = await cmd.ExecuteScalarAsync();
        return result as int?;
    }

    public async Task<bool> TableExistsAsync(string tableName)
    {
        await using var conn = GetConnection();
        await conn.OpenAsync();

        await using var cmd = new NpgsqlCommand(
            @"SELECT EXISTS (
                SELECT 1 FROM information_schema.tables 
                WHERE table_schema = 'public' 
                AND LOWER(table_name) = LOWER(@table_name)
            )",
            conn
        );

        cmd.Parameters.AddWithValue("table_name", tableName);

        var result = await cmd.ExecuteScalarAsync();
        return result is bool b && b;
    }

    public async Task<bool> ColumnExistsAsync(string tableName, string columnName)
    {
        await using var conn = GetConnection();
        await conn.OpenAsync();

        await using var cmd = new NpgsqlCommand(
            @"SELECT EXISTS (
                SELECT 1 FROM information_schema.columns 
                WHERE table_schema = 'public' 
                AND LOWER(table_name) = LOWER(@table_name)
                AND LOWER(column_name) = LOWER(@column_name)
            )",
            conn
        );

        cmd.Parameters.AddWithValue("table_name", tableName);
        cmd.Parameters.AddWithValue("column_name", columnName);

        var result = await cmd.ExecuteScalarAsync();
        return result is bool b && b;
    }

    public async Task TruncateTablesAsync()
    {
        await using var conn = GetConnection();
        await conn.OpenAsync();

        await using var cmd = new NpgsqlCommand(
            "TRUNCATE TABLE Pairs, Profiles, Users, ProfileConfig, wardrobe, active_wardrobe RESTART IDENTITY CASCADE",
            conn
        );

        await cmd.ExecuteNonQueryAsync();
    }

    public async Task<ProfileConfigRecord?> InsertTestProfileConfigAsync(
        InsertTestProfileConfigParams @params
    )
    {
        await using var conn = GetConnection();
        await conn.OpenAsync();

        await using var cmd = new NpgsqlCommand(
            @"INSERT INTO ProfileConfig (id, enable_glamours, enable_garbler, enable_garbler_channels, enable_moodles)
              VALUES (@id, @enable_glamours, @enable_garbler, @enable_garbler_channels, @enable_moodles)
              ON CONFLICT (id) DO UPDATE SET
                  enable_glamours = EXCLUDED.enable_glamours,
                  enable_garbler = EXCLUDED.enable_garbler,
                  enable_garbler_channels = EXCLUDED.enable_garbler_channels,
                  enable_moodles = EXCLUDED.enable_moodles
              RETURNING id, enable_glamours, enable_garbler, enable_garbler_channels, enable_moodles",
            conn
        );

        cmd.Parameters.AddWithValue("id", @params.Id);
        cmd.Parameters.AddWithValue("enable_glamours", @params.EnableGlamours);
        cmd.Parameters.AddWithValue("enable_garbler", @params.EnableGarbler);
        cmd.Parameters.AddWithValue("enable_garbler_channels", @params.EnableGarblerChannels);
        cmd.Parameters.AddWithValue("enable_moodles", @params.EnableMoodles);

        await using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return new ProfileConfigRecord
            {
                Id = reader.GetInt32(0),
                EnableGlamours = reader.GetBoolean(1),
                EnableGarbler = reader.GetBoolean(2),
                EnableGarblerChannels = reader.GetBoolean(3),
                EnableMoodles = reader.GetBoolean(4),
            };
        }
        return null;
    }

    public async Task<Guid?> InsertTestWardrobeAsync(InsertTestWardrobeParams @params)
    {
        await using var conn = GetConnection();
        await conn.OpenAsync();

        await using var cmd = new NpgsqlCommand(
            @"INSERT INTO wardrobe (id, profile_id, name, description, layer, relationship_priority, data)
              VALUES (@id, @profile_id, @name, @description, @layer, @priority, @data)
              RETURNING id",
            conn
        );

        cmd.Parameters.AddWithValue("id", @params.Id);
        cmd.Parameters.AddWithValue("profile_id", @params.ProfileId);
        cmd.Parameters.AddWithValue("name", @params.Name);
        cmd.Parameters.AddWithValue("description", @params.Description ?? (object)DBNull.Value);

        // determine layer: prefer explicit Slot mapping, otherwise map Type
        int layer = 0;
        if (@params.Slot is int s)
        {
            // map GlamourerEquipmentSlot to WardrobeLayer
            switch ((KinkLinkCommon.Dependencies.Glamourer.GlamourerEquipmentSlot)s)
            {
                case KinkLinkCommon.Dependencies.Glamourer.GlamourerEquipmentSlot.Head:
                    layer = (int)KinkLinkCommon.Domain.Wardrobe.WardrobeLayer.Head; break;
                case KinkLinkCommon.Dependencies.Glamourer.GlamourerEquipmentSlot.Body:
                    layer = (int)KinkLinkCommon.Domain.Wardrobe.WardrobeLayer.Chest; break;
                case KinkLinkCommon.Dependencies.Glamourer.GlamourerEquipmentSlot.Hands:
                    layer = (int)KinkLinkCommon.Domain.Wardrobe.WardrobeLayer.Hands; break;
                case KinkLinkCommon.Dependencies.Glamourer.GlamourerEquipmentSlot.Legs:
                    layer = (int)KinkLinkCommon.Domain.Wardrobe.WardrobeLayer.Legs; break;
                case KinkLinkCommon.Dependencies.Glamourer.GlamourerEquipmentSlot.Feet:
                    layer = (int)KinkLinkCommon.Domain.Wardrobe.WardrobeLayer.Feet; break;
                case KinkLinkCommon.Dependencies.Glamourer.GlamourerEquipmentSlot.Ears:
                    layer = (int)KinkLinkCommon.Domain.Wardrobe.WardrobeLayer.Ears; break;
                case KinkLinkCommon.Dependencies.Glamourer.GlamourerEquipmentSlot.Neck:
                    layer = (int)KinkLinkCommon.Domain.Wardrobe.WardrobeLayer.Neck; break;
                case KinkLinkCommon.Dependencies.Glamourer.GlamourerEquipmentSlot.Wrists:
                    layer = (int)KinkLinkCommon.Domain.Wardrobe.WardrobeLayer.Wrists; break;
                case KinkLinkCommon.Dependencies.Glamourer.GlamourerEquipmentSlot.RFinger:
                    layer = (int)KinkLinkCommon.Domain.Wardrobe.WardrobeLayer.RFinger; break;
                case KinkLinkCommon.Dependencies.Glamourer.GlamourerEquipmentSlot.LFinger:
                    layer = (int)KinkLinkCommon.Domain.Wardrobe.WardrobeLayer.LFinger; break;
                default:
                    layer = (int)KinkLinkCommon.Domain.Wardrobe.WardrobeLayer.Outfit; break;
            }
        }
        else if (!string.IsNullOrEmpty(@params.Type) && @params.Type.Equals("set", StringComparison.OrdinalIgnoreCase))
        {
            layer = (int)KinkLinkCommon.Domain.Wardrobe.WardrobeLayer.Outfit;
        }
        else if (!string.IsNullOrEmpty(@params.Type) && @params.Type.Equals("moditem", StringComparison.OrdinalIgnoreCase))
        {
            layer = (int)KinkLinkCommon.Domain.Wardrobe.WardrobeLayer.Mods;
        }
        else
        {
            layer = (int)KinkLinkCommon.Domain.Wardrobe.WardrobeLayer.Outfit;
        }

        cmd.Parameters.AddWithValue("layer", layer);
        cmd.Parameters.AddWithValue("priority", @params.Priority);
        cmd.Parameters.AddWithValue("data", @params.Data ?? @"{{""item"":null,""mods"":[]}}");

        var result = await cmd.ExecuteScalarAsync();
        return result as Guid?;
    }

    public async Task<long?> InsertTestActiveWardrobeAsync(InsertTestActiveWardrobeParams @params)
    {
        await using var conn = GetConnection();
        await conn.OpenAsync();

        // active_wardrobe now stores per-layer rows: (profile_id, layer, glamourer_data)
        // For each provided field, insert or update a row with the corresponding layer.
        async Task InsertOrUpdateActiveLayerAsync(int layer, string? data)
        {
            await using var c = new NpgsqlCommand(
                @"INSERT INTO active_wardrobe (profile_id, layer, glamourer_data)
                  VALUES (@profile_id, @layer, @glamourer_data)
                  ON CONFLICT (profile_id, layer) DO UPDATE SET
                      glamourer_data = EXCLUDED.glamourer_data",
                conn
            );
            c.Parameters.AddWithValue("profile_id", @params.ProfileId);
            c.Parameters.AddWithValue("layer", layer);
            c.Parameters.AddWithValue("glamourer_data", (object?)data ?? DBNull.Value);
            await c.ExecuteNonQueryAsync();
        }

        // Glamourer set -> Outfit layer
        if (!string.IsNullOrEmpty(@params.Glamourerset))
        {
            await InsertOrUpdateActiveLayerAsync((int)KinkLinkCommon.Domain.Wardrobe.WardrobeLayer.Outfit, @params.Glamourerset);
        }

        if (@params.Head != null)
            await InsertOrUpdateActiveLayerAsync((int)KinkLinkCommon.Domain.Wardrobe.WardrobeLayer.Head, @params.Head.Value.GetRawText());
        if (@params.Body != null)
            await InsertOrUpdateActiveLayerAsync((int)KinkLinkCommon.Domain.Wardrobe.WardrobeLayer.Chest, @params.Body.Value.GetRawText());
        if (@params.Hand != null)
            await InsertOrUpdateActiveLayerAsync((int)KinkLinkCommon.Domain.Wardrobe.WardrobeLayer.Hands, @params.Hand.Value.GetRawText());
        if (@params.Legs != null)
            await InsertOrUpdateActiveLayerAsync((int)KinkLinkCommon.Domain.Wardrobe.WardrobeLayer.Legs, @params.Legs.Value.GetRawText());
        if (@params.Feet != null)
            await InsertOrUpdateActiveLayerAsync((int)KinkLinkCommon.Domain.Wardrobe.WardrobeLayer.Feet, @params.Feet.Value.GetRawText());
        if (@params.Earring != null)
            await InsertOrUpdateActiveLayerAsync((int)KinkLinkCommon.Domain.Wardrobe.WardrobeLayer.Ears, @params.Earring.Value.GetRawText());
        if (@params.Neck != null)
            await InsertOrUpdateActiveLayerAsync((int)KinkLinkCommon.Domain.Wardrobe.WardrobeLayer.Neck, @params.Neck.Value.GetRawText());
        if (@params.Bracelet != null)
            await InsertOrUpdateActiveLayerAsync((int)KinkLinkCommon.Domain.Wardrobe.WardrobeLayer.Wrists, @params.Bracelet.Value.GetRawText());
        if (@params.Lring != null)
            await InsertOrUpdateActiveLayerAsync((int)KinkLinkCommon.Domain.Wardrobe.WardrobeLayer.LFinger, @params.Lring.Value.GetRawText());
        if (@params.Rring != null)
            await InsertOrUpdateActiveLayerAsync((int)KinkLinkCommon.Domain.Wardrobe.WardrobeLayer.RFinger, @params.Rring.Value.GetRawText());
        if (@params.Moditems != null)
            await InsertOrUpdateActiveLayerAsync((int)KinkLinkCommon.Domain.Wardrobe.WardrobeLayer.Mods, @params.Moditems.Value.GetRawText());

        return null;
    }
}

public class InsertTestWardrobeParams
{
    public Guid Id { get; set; }
    public int ProfileId { get; set; }
    public string Name { get; set; } = "";
    public string Type { get; set; } = "item";
    public string? Description { get; set; }
    public int? Slot { get; set; }
    public int Priority { get; set; }
    public string? Data { get; set; }
}

public class InsertTestActiveWardrobeParams
{
    public int ProfileId { get; set; }
    public string? Glamourerset { get; set; }
    public JsonElement? Head { get; set; }
    public JsonElement? Body { get; set; }
    public JsonElement? Hand { get; set; }
    public JsonElement? Legs { get; set; }
    public JsonElement? Feet { get; set; }
    public JsonElement? Earring { get; set; }
    public JsonElement? Neck { get; set; }
    public JsonElement? Bracelet { get; set; }
    public JsonElement? Lring { get; set; }
    public JsonElement? Rring { get; set; }
    public JsonElement? Moditems { get; set; }
}

public class InsertTestUserParams
{
    public long DiscordId { get; set; }
    public string? SecretKeyHash { get; set; }
    public bool? Verified { get; set; }
    public bool? Banned { get; set; }
}

public class InsertTestProfileParams
{
    public int UserId { get; set; }
    public string Uid { get; set; } = "";
    public string? ChatRole { get; set; }
    public string? Alias { get; set; }
    public string? Title { get; set; }
    public string? Description { get; set; }
}

public class InsertTestPairParams
{
    public int Id { get; set; }
    public int PairId { get; set; }
    public int? Priority { get; set; }
    public bool? ControlsPerm { get; set; }
    public bool? ControlsConfig { get; set; }
    public bool? DisableSafeword { get; set; }
    public long? Interaction { get; set; }
}

public class DeleteTestPairParams
{
    public int Id { get; set; }
    public int PairId { get; set; }
}

public class InsertTestPairWithPermissionsParams
{
    public int Id { get; set; }
    public int PairId { get; set; }
    public DateTime? Expires { get; set; }
    public int Priority { get; set; }
    public bool ControlsPerm { get; set; }
    public bool ControlsConfig { get; set; }
    public bool DisableSafeword { get; set; }
    public long Interaction { get; set; }
}

public class PairRecord
{
    public int Id { get; set; }
    public int PairId { get; set; }
    public DateTime? Expires { get; set; }
    public int Priority { get; set; }
    public bool ControlsPerm { get; set; }
    public bool ControlsConfig { get; set; }
    public bool DisableSafeword { get; set; }
    public long Interaction { get; set; }
}

public class InsertTestProfileConfigParams
{
    public int Id { get; set; }
    public bool EnableGlamours { get; set; }
    public bool EnableGarbler { get; set; }
    public bool EnableGarblerChannels { get; set; }
    public bool EnableMoodles { get; set; }
}

public class ProfileConfigRecord
{
    public int Id { get; set; }
    public bool EnableGlamours { get; set; }
    public bool EnableGarbler { get; set; }
    public bool EnableGarblerChannels { get; set; }
    public bool EnableMoodles { get; set; }
}
