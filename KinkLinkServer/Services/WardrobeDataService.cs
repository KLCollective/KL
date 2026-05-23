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

    public async Task<bool> RandomizeActiveWardrobeAsync(int profileId)
    {
        // For this profile Id, grab a random selection of items from the full wardrobe and apply each to the wardrobe state
    }

    public async Task<WardrobeStateDto> GetWardrobeStateAsync(int profileId)
    {
        var stopwatch = Stopwatch.StartNew();
        bool success = false;
        try
        {
            var rows = await _wardrobeSql.GetWardrobeStateAsync(
                new WardrobeSql.GetWardrobeStateArgs(profileId)
            );

            var layers = new Dictionary<WardrobeLayer, string>();

            foreach (var row in rows)
            {
                if (row.HasValue)
                {
                    layers[row.Value.layer] = row.Value.GlamourerData;
                }
            }

            var result = WardrobeStateDto(layers);
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

    public async Task<PairWardrobeStateDto> GetPairWardrobeLayersAsync(int profileId)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            var rows = await _wardrobeSql.GetWardrobeStateAsync(
                new WardrobeSql.GetWardrobeStateArgs(profileId)
            );

            var layers = new Dictionary<WardrobeLayer, string>();

            foreach (var row in rows)
            {
                if (row.HasValue)
                {
                    layers[row.Value.layer] = row.Value.GlamourerData;
                }
            }
            return new PairWardrobeStateDto(layers);
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
