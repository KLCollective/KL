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
            conn);
        
        cmd.Parameters.AddWithValue("discord_id", @params.DiscordId);
        cmd.Parameters.AddWithValue("secret_key_hash", @params.SecretKeyHash ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("verified", @params.Verified ?? false);
        cmd.Parameters.AddWithValue("banned", @params.Banned ?? false);
        
        var result = await cmd.ExecuteScalarAsync();
        return result as int?;
    }

    public async Task<int?> InsertTestUserAsync(long discordId, string? secretKeyHash = null, bool? verified = null, bool? banned = null)
    {
        return await InsertTestUserAsync(new InsertTestUserParams
        {
            DiscordId = discordId,
            SecretKeyHash = secretKeyHash,
            Verified = verified,
            Banned = banned
        });
    }

    public async Task<int?> InsertTestProfileAsync(InsertTestProfileParams @params)
    {
        await using var conn = GetConnection();
        await conn.OpenAsync();
        
        await using var cmd = new NpgsqlCommand(
            @"INSERT INTO Profiles (user_id, uid, chat_role, alias, title, description)
              VALUES (@user_id, @uid, @chat_role, @alias, @title, @description)
              RETURNING id",
            conn);
        
        cmd.Parameters.AddWithValue("user_id", @params.UserId);
        cmd.Parameters.AddWithValue("uid", @params.Uid);
        cmd.Parameters.AddWithValue("chat_role", @params.ChatRole ?? "");
        cmd.Parameters.AddWithValue("alias", @params.Alias ?? "");
        cmd.Parameters.AddWithValue("title", @params.Title ?? "Kinkster");
        cmd.Parameters.AddWithValue("description", @params.Description ?? (object)DBNull.Value);
        
        var result = await cmd.ExecuteScalarAsync();
        return result as int?;
    }

    public async Task<int?> InsertTestProfileAsync(int userId, string uid, string? chatRole = null, string? alias = null, string? title = null, string? description = null)
    {
        return await InsertTestProfileAsync(new InsertTestProfileParams
        {
            UserId = userId,
            Uid = uid,
            ChatRole = chatRole,
            Alias = alias,
            Title = title,
            Description = description
        });
    }

    public async Task<PairRecord?> InsertTestPairAsync(InsertTestPairParams @params)
    {
        await using var conn = GetConnection();
        await conn.OpenAsync();
        
        await using var cmd = new NpgsqlCommand(
            @"INSERT INTO Pairs (id, pair_id, priority, gags, wardrobe, moodles)
              VALUES (@id, @pair_id, @priority, @gags, @wardrobe, @moodles)
              RETURNING id, pair_id, expires, priority, gags, wardrobe, moodles",
            conn);
        
        cmd.Parameters.AddWithValue("id", @params.Id);
        cmd.Parameters.AddWithValue("pair_id", @params.PairId);
        cmd.Parameters.AddWithValue("priority", @params.Priority ?? 0);
        cmd.Parameters.AddWithValue("gags", @params.Gags ?? 0);
        cmd.Parameters.AddWithValue("wardrobe", @params.Wardrobe ?? 0);
        cmd.Parameters.AddWithValue("moodles", @params.Moodles ?? 0);
        
        await using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return new PairRecord
            {
                Id = reader.GetInt32(0),
                PairId = reader.GetInt32(1),
                Expires = reader.IsDBNull(2) ? null : reader.GetDateTime(2),
                Priority = reader.GetInt32(3),
                Gags = reader.GetInt32(4),
                Wardrobe = reader.GetInt32(5),
                Moodle = reader.GetInt32(6)
            };
        }
        return null;
    }

    public async Task<PairRecord?> InsertTestPairWithPermissionsAsync(InsertTestPairWithPermissionsParams @params)
    {
        await using var conn = GetConnection();
        await conn.OpenAsync();
        
        await using var cmd = new NpgsqlCommand(
            @"INSERT INTO Pairs (id, pair_id, expires, priority, gags, wardrobe, moodles)
              VALUES (@id, @pair_id, @expires, @priority, @gags, @wardrobe, @moodles)
              RETURNING id, pair_id, expires, priority, gags, wardrobe, moodles",
            conn);
        
        cmd.Parameters.AddWithValue("id", @params.Id);
        cmd.Parameters.AddWithValue("pair_id", @params.PairId);
        cmd.Parameters.AddWithValue("expires", @params.Expires ?? (object)DBNull.Value);
        cmd.Parameters.AddWithValue("priority", @params.Priority);
        cmd.Parameters.AddWithValue("gags", @params.Gags);
        cmd.Parameters.AddWithValue("wardrobe", @params.Wardrobe);
        cmd.Parameters.AddWithValue("moodles", @params.Moodles);
        
        await using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return new PairRecord
            {
                Id = reader.GetInt32(0),
                PairId = reader.GetInt32(1),
                Expires = reader.IsDBNull(2) ? null : reader.GetDateTime(2),
                Priority = reader.GetInt32(3),
                Gags = reader.GetInt32(4),
                Wardrobe = reader.GetInt32(5),
                Moodle = reader.GetInt32(6)
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
            conn);
        
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
            conn);
        
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
            conn);
        
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
              RETURNING id, pair_id, expires, priority, gags, wardrobe, moodles",
            conn);
        
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
                Gags = reader.GetInt32(4),
                Wardrobe = reader.GetInt32(5),
                Moodle = reader.GetInt32(6)
            };
        }
        return null;
    }

    public async Task<int?> GetProfileIdByUidAsync(string uid)
    {
        await using var conn = GetConnection();
        await conn.OpenAsync();
        
        await using var cmd = new NpgsqlCommand(
            "SELECT id FROM Profiles WHERE uid = @uid",
            conn);
        
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
            conn);
        
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
            conn);
        
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
            conn);
        
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
            "TRUNCATE TABLE Pairs, Profiles, Users RESTART IDENTITY CASCADE",
            conn);
        
        await cmd.ExecuteNonQueryAsync();
    }
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
    public int? Gags { get; set; }
    public int? Wardrobe { get; set; }
    public int? Moodles { get; set; }
}

public class InsertTestPairWithPermissionsParams
{
    public int Id { get; set; }
    public int PairId { get; set; }
    public DateTime? Expires { get; set; }
    public int Priority { get; set; }
    public int Gags { get; set; }
    public int Wardrobe { get; set; }
    public int Moodles { get; set; }
}

public class DeleteTestPairParams
{
    public int Id { get; set; }
    public int PairId { get; set; }
}

public class PairRecord
{
    public int Id { get; set; }
    public int PairId { get; set; }
    public DateTime? Expires { get; set; }
    public int Priority { get; set; }
    public int Gags { get; set; }
    public int Wardrobe { get; set; }
    public int Moodle { get; set; }
}
