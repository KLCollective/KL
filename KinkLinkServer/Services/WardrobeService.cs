using System.Diagnostics;
using System.Text;
using System.Text.Json;
using KinkLinkCommon.Database;
using KinkLinkCommon.Dependencies.Glamourer;
using KinkLinkCommon.Dependencies.Glamourer.Components;
using KinkLinkCommon.Domain;
using KinkLinkCommon.Domain.Enums;
using KinkLinkCommon.Domain.Wardrobe;
using KinkLinkServer.Domain;

namespace KinkLinkServer.Services;

public class WardrobeDataService
{
    private readonly ILogger<WardrobeDataService> _logger;
    private readonly WardrobeSql _wardrobeSql;
    private readonly IMetricsService _metricsService;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public WardrobeDataService(Configuration config, ILogger<WardrobeDataService> logger, IMetricsService metricsService)
    {
        _logger = logger;
        _wardrobeSql = new WardrobeSql(config.DatabaseConnectionString);
        _metricsService = metricsService;
    }

    public async Task<List<WardrobeDto>> GetAllWardrobeItemsAsync(int profileId)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            var rows = await _wardrobeSql.ListWardrobeByProfileIdAsync(new(profileId));

            return rows.Select(row => new WardrobeDto(
                    row.Id,
                    row.Name ?? string.Empty,
                    row.Description ?? string.Empty,
                    row.Type,
                    (GlamourerEquipmentSlot)(row.Slot ?? 0),
                    row.Data,
                    (RelationshipPriority)(row.RelationshipPriority ?? 0),
                    null
                ))
                .ToList();
        }
        finally
        {
            stopwatch.Stop();
            _metricsService.IncrementDatabaseOperation("GetAllWardrobeItems", true);
            _metricsService.RecordDatabaseOperationDuration("GetAllWardrobeItems", stopwatch.ElapsedMilliseconds);
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
                    row.Type,
                    (GlamourerEquipmentSlot)(row.Slot ?? 0),
                    row.Data,
                    (RelationshipPriority)(row.RelationshipPriority ?? 0),
                    null
                ))
                .ToList();
        }
        finally
        {
            stopwatch.Stop();
            _metricsService.IncrementDatabaseOperation("GetAllWardrobeByType", true);
            _metricsService.RecordDatabaseOperationDuration("GetAllWardrobeByType", stopwatch.ElapsedMilliseconds);
        }
    }

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
            success = true;
            _metricsService.IncrementDatabaseOperation("GetWardrobeItemByGuid", success);
            _metricsService.RecordDatabaseOperationDuration("GetWardrobeItemByGuid", stopwatch.ElapsedMilliseconds);
        }
    }

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
                    dto.Type,
                    dto.Description,
                    (int)dto.Slot,
                    (int)dto.Priority,
                    dto.DataBase64
                )
            );

            success = result != null;
            return success;
        }
        finally
        {
            stopwatch.Stop();
            _metricsService.IncrementDatabaseOperation("CreateOrUpdateWardrobeItems", success);
            _metricsService.RecordDatabaseOperationDuration("CreateOrUpdateWardrobeItems", stopwatch.ElapsedMilliseconds);
        }
    }

    public async Task<bool> DeleteWardrobeItemAsync(int profileId, Guid wardrobeId)
    {
        var stopwatch = Stopwatch.StartNew();
        bool success = false;
        try
        {
            var result = await _wardrobeSql.DeleteWardrobeAsync(new(profileId, wardrobeId));

            success = result != null;
            return success;
        }
        finally
        {
            stopwatch.Stop();
            _metricsService.IncrementDatabaseOperation("DeleteWardrobeItem", success);
            _metricsService.RecordDatabaseOperationDuration("DeleteWardrobeItem", stopwatch.ElapsedMilliseconds);
        }
    }

    public async Task<bool> UpdateWardrobeStateAsync(int profileId, WardrobeStateDto state)
    {
        var stopwatch = Stopwatch.StartNew();
        bool success = false;
        try
        {
            _logger.LogInformation(
                "UpdateWardrobeStateAsync called with profileId: {ProfileId}, equipment count: {EquipmentCount}, characterItems count: {CharacterItemsCount}",
                profileId,
                state.Equipment?.Count ?? 0,
                state.ModSettings?.Count ?? 0
            );

            WardrobeItemData? head = null,
                body = null,
                hands = null,
                legs = null;
            WardrobeItemData? feet = null,
                ears = null,
                neck = null,
                wrists = null;
            WardrobeItemData? lFinger = null,
                rFinger = null;
            state.Equipment?.TryGetValue("Head", out head);
            state.Equipment?.TryGetValue("Body", out body);
            state.Equipment?.TryGetValue("Hands", out hands);
            state.Equipment?.TryGetValue("Legs", out legs);
            state.Equipment?.TryGetValue("Feet", out feet);
            state.Equipment?.TryGetValue("Ears", out ears);
            state.Equipment?.TryGetValue("Neck", out neck);
            state.Equipment?.TryGetValue("Wrists", out wrists);
            state.Equipment?.TryGetValue("LFinger", out lFinger);
            state.Equipment?.TryGetValue("RFinger", out rFinger);

            var result = await _wardrobeSql.UpdateWardrobeStateAsync(
                new(
                    profileId,
                    state.BaseLayerBase64,
                    SerializeToJsonElement(head),
                    SerializeToJsonElement(body),
                    SerializeToJsonElement(hands),
                    SerializeToJsonElement(legs),
                    SerializeToJsonElement(feet),
                    SerializeToJsonElement(ears),
                    SerializeToJsonElement(neck),
                    SerializeToJsonElement(wrists),
                    SerializeToJsonElement(lFinger),
                    SerializeToJsonElement(rFinger),
                    SerializeToJsonElement(state.ModSettings?.Values)
                )
            );

            success = result != null;
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
            _metricsService.RecordDatabaseOperationDuration("UpdateWardrobeState", stopwatch.ElapsedMilliseconds);
        }
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

            if (row == null)
            {
                return null;
            }

            var equipment = new Dictionary<string, WardrobeItemData>();
            var modSettings = new Dictionary<string, WardrobeItemData>();

            if (row.Value.Head.HasValue)
            {
                var item = DeserializeNullable<WardrobeItemData>(row.Value.Head.Value);
                if (item != null)
                    equipment["Head"] = item;
            }
            if (row.Value.Body.HasValue)
            {
                var item = DeserializeNullable<WardrobeItemData>(row.Value.Body.Value);
                if (item != null)
                    equipment["Body"] = item;
            }
            if (row.Value.Hand.HasValue)
            {
                var item = DeserializeNullable<WardrobeItemData>(row.Value.Hand.Value);
                if (item != null)
                    equipment["Hands"] = item;
            }
            if (row.Value.Legs.HasValue)
            {
                var item = DeserializeNullable<WardrobeItemData>(row.Value.Legs.Value);
                if (item != null)
                    equipment["Legs"] = item;
            }
            if (row.Value.Feet.HasValue)
            {
                var item = DeserializeNullable<WardrobeItemData>(row.Value.Feet.Value);
                if (item != null)
                    equipment["Feet"] = item;
            }
            if (row.Value.Earring.HasValue)
            {
                var item = DeserializeNullable<WardrobeItemData>(row.Value.Earring.Value);
                if (item != null)
                    equipment["Ears"] = item;
            }
            if (row.Value.Neck.HasValue)
            {
                var item = DeserializeNullable<WardrobeItemData>(row.Value.Neck.Value);
                if (item != null)
                    equipment["Neck"] = item;
            }
            if (row.Value.Bracelet.HasValue)
            {
                var item = DeserializeNullable<WardrobeItemData>(row.Value.Bracelet.Value);
                if (item != null)
                    equipment["Wrists"] = item;
            }
            if (row.Value.Lring.HasValue)
            {
                var item = DeserializeNullable<WardrobeItemData>(row.Value.Lring.Value);
                if (item != null)
                    equipment["LFinger"] = item;
            }
            if (row.Value.Rring.HasValue)
            {
                var item = DeserializeNullable<WardrobeItemData>(row.Value.Rring.Value);
                if (item != null)
                    equipment["RFinger"] = item;
            }

            if (row.Value.Moditems.HasValue)
            {
                var modItems = DeserializeList<WardrobeItemData>(row.Value.Moditems.Value);
                if (modItems != null)
                {
                    foreach (var item in modItems)
                    {
                        if (item != null)
                        {
                            if (item.Mods != null)
                            {
                                foreach (var mod in item.Mods)
                                {
                                    modSettings[mod.Name] = item;
                                }
                            }
                        }
                    }
                }
            }

            return new WardrobeStateDto(
                row.Value.Glamourerset,
                equipment.Count > 0 ? equipment : null,
                modSettings.Count > 0 ? modSettings : null
            );
        }
        finally
        {
            stopwatch.Stop();
            success = true;
            _metricsService.IncrementDatabaseOperation("GetWardrobeState", success);
            _metricsService.RecordDatabaseOperationDuration("GetWardrobeState", stopwatch.ElapsedMilliseconds);
        }
    }

    public async Task<PairWardrobeStateDto> GetPairWardrobeItemsAsync(int profileId)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            var row = await _wardrobeSql.GetWardrobeStateAsync(
                new WardrobeSql.GetWardrobeStateArgs(profileId)
            );

            if (row == null)
            {
                return new PairWardrobeStateDto(null, new Dictionary<string, PairWardrobeItemDto>());
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
                        new JsonSerializerOptions { PropertyNamingPolicy = null, IncludeFields = true }
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
            _metricsService.RecordDatabaseOperationDuration("GetPairWardrobeItems", stopwatch.ElapsedMilliseconds);
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
}