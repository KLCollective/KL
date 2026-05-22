using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.Json;
using KinkLinkCommon.Database;
using KinkLinkCommon.Dependencies.Glamourer;
using KinkLinkCommon.Dependencies.Glamourer.Components;
using KinkLinkCommon.Domain;
using KinkLinkCommon.Domain.Enums;
using KinkLinkCommon.Domain.Wardrobe;
using KinkLinkServer.Domain;
using Npgsql;

namespace KinkLinkServer.Services;

public class WardrobeDataService : IDisposable, IAsyncDisposable
{
    private readonly ILogger<WardrobeDataService> _logger;
    private readonly WardrobeSql _wardrobeSql;
    private readonly IMetricsService _metricsService;
    private readonly LockService _lockService;
    private readonly NpgsqlDataSource _dataSource;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public WardrobeDataService(
        Configuration config,
        ILogger<WardrobeDataService> logger,
        IMetricsService metricsService,
        LockService lockService
    )
    {
        _logger = logger;
        _dataSource = NpgsqlDataSource.Create(config.DatabaseConnectionString);
        _wardrobeSql = new WardrobeSql(config.DatabaseConnectionString);
        _metricsService = metricsService;
        _lockService = lockService;
    }

    public async ValueTask DisposeAsync()
    {
        await _dataSource.DisposeAsync();
        GC.SuppressFinalize(this);
    }

    public async Task<List<WardrobeDto>> GetAllWardrobeItemsAsync(int profileId)
    {
        var sw = Stopwatch.StartNew();
        var correlationId = Guid.NewGuid();
        using (
            _logger.BeginScope(
                new Dictionary<string, object?>
                {
                    ["CorrelationId"] = correlationId,
                    ["Method"] = "GetAllWardrobeItems",
                    ["ProfileId"] = profileId,
                }
            )
        )
            try
            {
                _logger.LogInformation(
                    "[WardrobeDataService] Enter GetAllWardrobeItems profileId={ProfileId}",
                    profileId
                );
                var rows = await _wardrobeSql.ListWardrobeByProfileIdAsync(new(profileId));

                // TODO: Convert wardrobe data to DTO properly
                var result = rows.Select(row => new WardrobeDto(
                        row.Id,
                        row.Name ?? string.Empty,
                        row.Description ?? string.Empty,
                        row.Layer,
                        row.Data,
                        (RelationshipPriority)(row.RelationshipPriority ?? 0)
                    ))
                    .ToList();

                _logger.LogInformation(
                    "[WardrobeDataService] Exit GetAllWardrobeItems profileId={ProfileId} items={Count}",
                    profileId,
                    result.Count
                );
                return result;
            }
            finally
            {
                sw.Stop();
                _metricsService.IncrementDatabaseOperation("GetAllWardrobeItems", true);
                _metricsService.RecordDatabaseOperationDuration(
                    "GetAllWardrobeItems",
                    sw.ElapsedMilliseconds
                );
            }
    }

    public async Task<List<WardrobeDto>> GetAllWardrobeByTypeAsync(int profileId, string type)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            var rows = await _wardrobeSql.GetAllWardrobeByTypeAsync(new(profileId, type));

            return rows.Select(row => new WardrobeDto(
                    row.Id,
                    row.Name ?? string.Empty,
                    row.Description ?? string.Empty,
                    row.Layer,
                    row.Data,
                    (RelationshipPriority)(row.RelationshipPriority ?? 0)
                ))
                .ToList();
        }
        finally
        {
            stopwatch.Stop();
            _metricsService.IncrementDatabaseOperation("GetAllWardrobeByType", true);
            _metricsService.RecordDatabaseOperationDuration(
                "GetAllWardrobeByType",
                stopwatch.ElapsedMilliseconds
            );
        }
    }

    // Getting the wardrobe listing
    public async Task<WardrobeDto?> GetWardrobeItemByGuid(int profileId, Guid wardrobeId)
    {
        var stopwatch = Stopwatch.StartNew();
        bool success = false;
        try
        {
            var row = await _wardrobeSql.GetWardrobeItemByGuidAsync(new(profileId, wardrobeId));

            if (row == null)
            {
                success = true;
                return null;
            }

            success = true;
            return new WardrobeDto(
                row.Value.Id,
                row.Value.Name ?? string.Empty,
                row.Value.Description ?? string.Empty,
                row.Value.Type,
                (GlamourerEquipmentSlot)(row.Value.Slot ?? 0),
                row.Value.Data,
                (RelationshipPriority)(row.Value.RelationshipPriority ?? 0),
                null
            );
        }
        finally
        {
            stopwatch.Stop();
            _metricsService.IncrementDatabaseOperation("GetWardrobeItemByGuid", success);
            _metricsService.RecordDatabaseOperationDuration(
                "GetWardrobeItemByGuid",
                stopwatch.ElapsedMilliseconds
            );
        }
    }

    // Updating wardrobe listing
    public async Task<bool> CreateOrUpdateWardrobeItemsByNameAsync(
        int profileId,
        Guid uuid,
        WardrobeDto dto
    )
    {
        var stopwatch = Stopwatch.StartNew();
        bool success = false;
        try
        {
            var result = await _wardrobeSql.CreateOrUpdateWardrobeAsync(
                new(
                    uuid,
                    profileId,
                    dto.Name,
                    dto.Layer,
                    dto.Description,
                    (int)dto.Priority,
                    dto.Base64GlamourerData
                )
            );

            success = result != null;
            return success;
        }
        finally
        {
            stopwatch.Stop();
            _metricsService.IncrementDatabaseOperation("CreateOrUpdateWardrobeItems", success);
            _metricsService.RecordDatabaseOperationDuration(
                "CreateOrUpdateWardrobeItems",
                stopwatch.ElapsedMilliseconds
            );
        }
    }

    // Deleting a wardrobe listing
    public async Task<bool> DeleteWardrobeItemAsync(int profileId, Guid wardrobeId)
    {
        var stopwatch = Stopwatch.StartNew();
        bool success = false;
        try
        {
            var item = await GetWardrobeItemByGuid(profileId, wardrobeId);
            if (item == null)
            {
                _logger.LogWarning(
                    "DeleteWardrobeItemAsync: item not found for wardrobeId: {WardrobeId}, profileId: {ProfileId}",
                    wardrobeId,
                    profileId
                );
                return false;
            }

            var result = await _wardrobeSql.DeleteWardrobeAsync(new(profileId, wardrobeId));

            success = result != null;
            return success;
        }
        finally
        {
            stopwatch.Stop();
            _metricsService.IncrementDatabaseOperation("DeleteWardrobeItem", success);
            _metricsService.RecordDatabaseOperationDuration(
                "DeleteWardrobeItem",
                stopwatch.ElapsedMilliseconds
            );
        }
    }

    // Updating the users active wardrobe (if permitted)
    public async Task<bool> UpdateWardrobeStateAsync(int profileId, WardrobeStateDto state)
    {
        var stopwatch = Stopwatch.StartNew();
        bool success = false;
        try
        {
            _logger.LogInformation(
                "UpdateWardrobeStateAsync called with profileId: {ProfileId}, equipment count: {EquipmentCount}",
                profileId,
                state.Layers?.Count ?? 0
            );

            foreach (var kvp in state.Layers)
            {
                success = WardrobeSql.UpdateWardrobeStateAsync(profileId, kvp.Key, kvp.Value);
            }

            if (success)
            {
                _logger.LogInformation(
                    "UpdateWardrobeStateAsync successfully updated wardrobe state for profileId: {ProfileId}",
                    profileId
                );
            }
            else
            {
                _logger.LogWarning(
                    "UpdateWardrobeStateAsync failed to update wardrobe state for profileId: {ProfileId}",
                    profileId
                );
            }

            return success;
        }
        finally
        {
            stopwatch.Stop();
            _metricsService.IncrementDatabaseOperation("UpdateWardrobeState", success);
            _metricsService.RecordDatabaseOperationDuration(
                "UpdateWardrobeState",
                stopwatch.ElapsedMilliseconds
            );
        }
    }

    public async Task<bool> UpdateWardrobeStateAsync(
        int profileId,
        WardrobeStateDto state,
        WardrobeSql sql
    )
    {
        _logger.LogInformation(
            "UpdateWardrobeStateAsync (transactional) called with profileId: {ProfileId}, equipment count: {EquipmentCount}, characterItems count: {CharacterItemsCount}",
            profileId,
            state.Equipment?.Count ?? 0,
            state.ModSettings?.Count ?? 0
        );

        var success = await SaveWardrobeStateAsync(sql, profileId, state);

        if (success)
        {
            _logger.LogInformation(
                "UpdateWardrobeStateAsync (transactional) successfully updated wardrobe state for profileId: {ProfileId}",
                profileId
            );
        }
        else
        {
            _logger.LogWarning(
                "UpdateWardrobeStateAsync (transactional) failed to update wardrobe state for profileId: {ProfileId}",
                profileId
            );
        }

        return success;
    }

    public async Task<bool> RandomizeActiveWardrobeAsync(int profileId)
    {
        // For each
    }

    public async Task<WardrobeStateDto?> GetWardrobeStateAsync(int profileId)
    {
        var stopwatch = Stopwatch.StartNew();
        bool success = false;
        try
        {
            var row = await _wardrobeSql.GetWardrobeStateAsync(
                new WardrobeSql.GetWardrobeStateArgs(profileId)
            );

            var result = RowToWardrobeStateDto(row);
            success = true;
            return result;
        }
        finally
        {
            stopwatch.Stop();
            _metricsService.IncrementDatabaseOperation("GetWardrobeState", success);
            _metricsService.RecordDatabaseOperationDuration(
                "GetWardrobeState",
                stopwatch.ElapsedMilliseconds
            );
        }
    }

    public virtual async Task<PairWardrobeStateDto> GetPairWardrobeItemsAsync(int profileId)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            var row = await _wardrobeSql.GetWardrobeStateAsync(
                new WardrobeSql.GetWardrobeStateArgs(profileId)
            );

            if (row == null)
            {
                return new PairWardrobeStateDto(
                    null,
                    new Dictionary<string, PairWardrobeItemDto>()
                );
            }

            PairWardrobeItemDto? baseLayer = null;

            if (!string.IsNullOrEmpty(row.Value.Glamourerset))
            {
                try
                {
                    var glamourerJson = Encoding.UTF8.GetString(
                        Convert.FromBase64String(row.Value.Glamourerset)
                    );
                    var glamourerDesign = JsonSerializer.Deserialize<GlamourerDesign>(
                        glamourerJson,
                        new JsonSerializerOptions
                        {
                            PropertyNamingPolicy = null,
                            IncludeFields = true,
                        }
                    );
                    if (glamourerDesign != null)
                    {
                        baseLayer = new PairWardrobeItemDto(
                            glamourerDesign.Identifier,
                            glamourerDesign.Name,
                            glamourerDesign.Description,
                            GlamourerEquipmentSlot.None,
                            RelationshipPriority.Casual,
                            null
                        );
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(
                        ex,
                        "Failed to deserialize GlamourerDesign for profileId: {ProfileId}",
                        profileId
                    );
                }
            }

            var equipment = new Dictionary<string, PairWardrobeItemDto>();

            if (row.Value.Head.HasValue)
            {
                var item = DeserializeNullable<WardrobeItemData>(row.Value.Head.Value);
                if (item != null)
                    equipment["Head"] = ConvertToPairWardrobeItem(item);
            }
            if (row.Value.Body.HasValue)
            {
                var item = DeserializeNullable<WardrobeItemData>(row.Value.Body.Value);
                if (item != null)
                    equipment["Body"] = ConvertToPairWardrobeItem(item);
            }
            if (row.Value.Hand.HasValue)
            {
                var item = DeserializeNullable<WardrobeItemData>(row.Value.Hand.Value);
                if (item != null)
                    equipment["Hands"] = ConvertToPairWardrobeItem(item);
            }
            if (row.Value.Legs.HasValue)
            {
                var item = DeserializeNullable<WardrobeItemData>(row.Value.Legs.Value);
                if (item != null)
                    equipment["Legs"] = ConvertToPairWardrobeItem(item);
            }
            if (row.Value.Feet.HasValue)
            {
                var item = DeserializeNullable<WardrobeItemData>(row.Value.Feet.Value);
                if (item != null)
                    equipment["Feet"] = ConvertToPairWardrobeItem(item);
            }
            if (row.Value.Earring.HasValue)
            {
                var item = DeserializeNullable<WardrobeItemData>(row.Value.Earring.Value);
                if (item != null)
                    equipment["Ears"] = ConvertToPairWardrobeItem(item);
            }
            if (row.Value.Neck.HasValue)
            {
                var item = DeserializeNullable<WardrobeItemData>(row.Value.Neck.Value);
                if (item != null)
                    equipment["Neck"] = ConvertToPairWardrobeItem(item);
            }
            if (row.Value.Bracelet.HasValue)
            {
                var item = DeserializeNullable<WardrobeItemData>(row.Value.Bracelet.Value);
                if (item != null)
                    equipment["Wrists"] = ConvertToPairWardrobeItem(item);
            }
            if (row.Value.Lring.HasValue)
            {
                var item = DeserializeNullable<WardrobeItemData>(row.Value.Lring.Value);
                if (item != null)
                    equipment["LFinger"] = ConvertToPairWardrobeItem(item);
            }
            if (row.Value.Rring.HasValue)
            {
                var item = DeserializeNullable<WardrobeItemData>(row.Value.Rring.Value);
                if (item != null)
                    equipment["RFinger"] = ConvertToPairWardrobeItem(item);
            }

            return new PairWardrobeStateDto(baseLayer, equipment);
        }
        finally
        {
            stopwatch.Stop();
            _metricsService.IncrementDatabaseOperation("GetPairWardrobeItems", true);
            _metricsService.RecordDatabaseOperationDuration(
                "GetPairWardrobeItems",
                stopwatch.ElapsedMilliseconds
            );
        }
    }

    private static PairWardrobeItemDto ConvertToPairWardrobeItem(WardrobeItemData data)
    {
        return new PairWardrobeItemDto(
            data.Id,
            data.Name,
            data.Description,
            data.Slot,
            data.Priority,
            null
        );
    }

    private static JsonElement? SerializeToJsonElement<T>(T? value)
    {
        if (value == null)
            return null;
        return JsonSerializer.SerializeToElement(value);
    }

    private static T? DeserializeNullable<T>(JsonElement element)
        where T : class
    {
        try
        {
            return JsonSerializer.Deserialize<T>(element.GetRawText());
        }
        catch
        {
            return null;
        }
    }

    private static List<T> DeserializeList<T>(JsonElement element)
    {
        try
        {
            return JsonSerializer.Deserialize<List<T>>(element.GetRawText()) ?? [];
        }
        catch
        {
            return [];
        }
    }

    public void Dispose()
    {
        _dataSource.Dispose();
    }

    // Incluuded directly instead of in sqlc due to incompatibility between void types and code generation.
    private static async Task AcquireAdvisoryLockAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        long profileId
    )
    {
        await using var cmd = new NpgsqlCommand(
            "SELECT pg_advisory_xact_lock(@p)",
            connection,
            transaction
        );
        cmd.Parameters.AddWithValue("@p", profileId);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task<T> WithWardrobeTransactionAsync<T>(
        int profileId,
        Func<WardrobeSql, Task<T>> action
    )
    {
        await using var connection = await _dataSource.OpenConnectionAsync();
        await using var transaction = await connection.BeginTransactionAsync();

        var sql = WardrobeSql.WithTransaction(transaction);
        await AcquireAdvisoryLockAsync(connection, transaction, profileId);

        var result = await action(sql);
        await transaction.CommitAsync();
        return result;
    }

    public async Task WithWardrobeTransactionAsync(int profileId, Func<WardrobeSql, Task> action)
    {
        await using var connection = await _dataSource.OpenConnectionAsync();
        await using var transaction = await connection.BeginTransactionAsync();

        var sql = WardrobeSql.WithTransaction(transaction);
        await AcquireAdvisoryLockAsync(connection, transaction, profileId);

        await action(sql);
        await transaction.CommitAsync();
    }
}
