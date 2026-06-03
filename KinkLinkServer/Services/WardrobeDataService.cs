using System.Diagnostics;
using System.Text.Json;
using KinkLinkCommon.Database;
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

    public WardrobeDataService(
        WardrobeSql wardrobeSql,
        Configuration config,
        ILogger<WardrobeDataService> logger,
        IMetricsService metricsService,
        LockService lockService
    )
    {
        _logger = logger;
        _wardrobeSql = wardrobeSql;
        _dataSource = NpgsqlDataSource.Create(config.DatabaseConnectionString);
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
                        (WardrobeLayer)row.Layer,
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
            if (!Enum.TryParse<WardrobeLayer>(type, ignoreCase: true, out var layer))
            {
                _logger.LogWarning(
                    "GetAllWardrobeByTypeAsync: invalid type '{Type}' for profileId={ProfileId}",
                    type,
                    profileId
                );
                return [];
            }

            var rows = await _wardrobeSql.GetAllWardrobeByTypeAsync(
                new WardrobeSql.GetAllWardrobeByTypeArgs(profileId, (int)layer)
            );

            return rows.Select(row => new WardrobeDto(
                    row.Id,
                    row.Name ?? string.Empty,
                    row.Description ?? string.Empty,
                    (WardrobeLayer)row.Layer,
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
                return null;
            }

            success = true;
            return new WardrobeDto(
                row.Value.Id,
                row.Value.Name ?? string.Empty,
                row.Value.Description ?? string.Empty,
                (WardrobeLayer)row.Value.Layer,
                row.Value.Data,
                (RelationshipPriority)(row.Value.RelationshipPriority ?? 0)
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
                    (int)dto.Layer,
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

            await _wardrobeSql.DeleteWardrobeAsync(new(profileId, wardrobeId));

            success = true;
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
