using System.Diagnostics;
using KinkLinkCommon.Database;
using KinkLinkCommon.Domain.Enums;
using KinkLinkCommon.Domain.Wardrobe;

namespace KinkLinkServer.Services;

public class ActiveWardrobeStateService : IActiveWardrobeStateService
{
    private readonly ILogger<ActiveWardrobeStateService> _logger;
    private readonly WardrobeSql _wardrobeSql;
    private readonly IMetricsService _metricsService;
    private readonly LockService _lockService;

    public ActiveWardrobeStateService(
        WardrobeSql wardrobeSql,
        ILogger<ActiveWardrobeStateService> logger,
        IMetricsService metricsService,
        LockService lockService
    )
    {
        _logger = logger;
        _wardrobeSql = wardrobeSql;
        _metricsService = metricsService;
        _lockService = lockService;
    }

    public async Task<bool> RandomizeActiveWardrobeAsync(int profileId)
    {
        var stopwatch = Stopwatch.StartNew();
        bool success = false;
        try
        {
            // List wardrobe items for profile
            var rows = await _wardrobeSql.ListWardrobeByProfileIdAsync(new(profileId));

            // Group by layer
            var groups = rows.GroupBy(r => (WardrobeLayer)r.Layer).ToList();
            var rand = new Random();
            var anyUpdated = false;

            foreach (var g in groups)
            {
                var list = g.ToList();
                if (list.Count == 0)
                    continue;
                var pick = list[rand.Next(list.Count)];
                // pick.Data corresponds to glamourer data for this layer
                var updateResult = await _wardrobeSql.UpdateWardrobeStateAsync(new(profileId, (int)g.Key, pick.Data));
                anyUpdated = anyUpdated || updateResult != null;
            }

            success = anyUpdated;
            return success;
        }
        finally
        {
            stopwatch.Stop();
            _metricsService.IncrementDatabaseOperation("RandomizeActiveWardrobe", success);
            _metricsService.RecordDatabaseOperationDuration("RandomizeActiveWardrobe", stopwatch.ElapsedMilliseconds);
        }
    }

    public async Task<WardrobeStateDto?> GetWardrobeStateAsync(int profileId)
    {
        var stopwatch = Stopwatch.StartNew();
        bool success = false;
        try
        {
            var rows = await _wardrobeSql.GetWardrobeStateAsync(
                new WardrobeSql.GetWardrobeStateArgs(profileId)
            );

            if (rows == null || rows.Count == 0)
            {
                success = true;
                return null;
            }

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

    public async Task<bool> UpdateWardrobeStateAsync(int profileId, WardrobeLayer layer, Guid? id, string? base64GlamourerData = null)
    {
        var stopwatch = Stopwatch.StartNew();
        bool success = false;
        try
        {
            if (!string.IsNullOrEmpty(base64GlamourerData))
            {
                // Update active layer directly with provided glamourer data
                var updateResult = await _wardrobeSql.UpdateWardrobeStateAsync(
                    new(profileId, (int)layer, base64GlamourerData)
                );

                success = updateResult != null;
                if (success)
                {
                    _logger.LogInformation(
                        "Successfully updated active layer from provided data: {ProfileId} {Layer}",
                        profileId,
                        layer
                    );
                }
                else
                {
                    _logger.LogWarning(
                        "Failed to update active layer from data: {ProfileId} {Layer} — upsert returned no row",
                        profileId,
                        layer
                    );
                }
            }
            else if (id is { } wardrobeId)
            {
                var result = await _wardrobeSql.GetWardrobeItemByGuidAsync(
                    new(profileId, wardrobeId)
                );
                if (result.HasValue)
                {
                    var updateResult = await _wardrobeSql.UpdateWardrobeStateAsync(
                        new(profileId, (int)layer, result.Value.Data)
                    );
                    success = updateResult != null;
                    if (success)
                    {
                        _logger.LogInformation(
                            "Successfully updated: {ProfileId} {Layer} to {WardrobeId}",
                            profileId,
                            layer,
                            wardrobeId
                        );
                    }
                    else
                    {
                        _logger.LogWarning(
                            "Upsert returned no row for: {ProfileId} {Layer} item={WardrobeId}",
                            profileId,
                            layer,
                            wardrobeId
                        );
                    }
                }
                else
                {
                    _logger.LogWarning(
                        "Failed to update: {ProfileId} {Layer}. WardrobeId ({WardrobeId}) not found.",
                        profileId,
                        layer,
                        wardrobeId
                    );
                    success = false;
                }
            }
            else
            {
                // If null and no data provided, clear the wardrobe layer
                await _wardrobeSql.ClearWardrobeLayerAsync(new(profileId, (int)layer));
                success = true;
            }

            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "[ActiveWardrobeStateService] Failed to update wardrobe state for {ProfileId}",
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


}
