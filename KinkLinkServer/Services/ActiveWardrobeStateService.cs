using System.Diagnostics;
using System.Text.Json;
using KinkLinkCommon.Database;
using KinkLinkCommon.Domain.Enums;
using KinkLinkCommon.Domain.Wardrobe;
using KinkLinkServer.Domain;
using Npgsql;

namespace KinkLinkServer.Services;

public class ActiveWardrobeStateService : IDisposable, IAsyncDisposable
{
    private readonly ILogger<ActiveWardrobeStateService> _logger;
    private readonly WardrobeSql _wardrobeSql;
    private readonly IMetricsService _metricsService;
    private readonly LockService _lockService;
    private readonly NpgsqlDataSource _dataSource;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public ActiveWardrobeStateService(
        Configuration config,
        ILogger<ActiveWardrobeStateService> logger,
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

    public async Task<bool> RandomizeActiveWardrobeAsync(int profileId)
    {
        // Not implemented yet
        return false;
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
                // row has properties: ProfileId, Layer, GlamourerData
                layers[(WardrobeLayer)row.Layer] = row.GlamourerData;
            }

            var result = new WardrobeStateDto(layers);
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

    public async Task<PairWardrobeStateDto> GetPairWardrobeStateAsync(int profileId)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            var rows = await _wardrobeSql.GetWardrobeStateAsync(
                new WardrobeSql.GetWardrobeStateArgs(profileId)
            );

            var layers = new Dictionary<WardrobeLayer, LightWardrobeItemDto>();

            foreach (var row in rows)
            {
                var layer = (WardrobeLayer)row.Layer;
                var dto = new LightWardrobeItemDto(
                    Guid.Empty,
                    string.Empty,
                    string.Empty,
                    layer,
                    RelationshipPriority.Casual,
                    null
                );
                layers[layer] = dto;
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

    public async Task<bool> UpdateWardrobeStateAsync(int profileId, WardrobeLayer layer, Guid? id)
    {
        var stopwatch = Stopwatch.StartNew();
        bool success = false;
        try
        {
            if (id is { } wardrobeId)
            {
                var result = await _wardrobeSql.GetWardrobeItemByGuidAsync(
                    new(profileId, wardrobeId)
                );
                if (result.HasValue)
                {
                    var updateResult = await _wardrobeSql.UpdateWardrobeStateAsync(
                        new(profileId, (int)layer, result.Value.Data)
                    );
                    if (updateResult is { } updated)
                    {
                        _logger.LogInformation(
                            "Successfully updated: {ProfileId} {Layer} to {WardrobeId}",
                            profileId,
                            layer,
                            wardrobeId
                        );
                    }
                }
                else
                {
                    _logger.LogInformation(
                        "Failed to update: {ProfileId} {Layer}. WardrobeId ({WardrobeId}) not found.",
                        profileId,
                        layer,
                        wardrobeId
                    );
                }
            }
            else
            {
                // If null, clear the wardrobe layer
                await _wardrobeSql.ClearWardrobeLayerAsync(new(profileId, (int)layer));
            }

            success = true;
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "[WardrobeDataService] Failed to update wardrobe state for {ProfileId}",
                profileId
            );
            return false;
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

    public void Dispose()
    {
        _dataSource.Dispose();
    }
}
