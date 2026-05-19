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
        using (_logger.BeginScope(new Dictionary<string, object?> { ["CorrelationId"] = correlationId, ["Method"] = "GetAllWardrobeItems", ["ProfileId"] = profileId }))
        try
        {
            _logger.LogInformation("[WardrobeDataService] Enter GetAllWardrobeItems profileId={ProfileId}", profileId);
            var rows = await _wardrobeSql.ListWardrobeByProfileIdAsync(new(profileId));

            var result = rows.Select(row => new WardrobeDto(
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

            _logger.LogInformation("[WardrobeDataService] Exit GetAllWardrobeItems profileId={ProfileId} items={Count}", profileId, result.Count);
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
            _metricsService.RecordDatabaseOperationDuration(
                "GetAllWardrobeByType",
                stopwatch.ElapsedMilliseconds
            );
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
            var slotName = dto.Slot.ToString();
            if (await _lockService.IsSlotLockedAsync(profileId, slotName))
            {
                _logger.LogWarning(
                    "CreateOrUpdateWardrobeItemsByNameAsync: slot {SlotName} is locked for profileId: {ProfileId}",
                    slotName,
                    profileId
                );
                return false;
            }

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
            _metricsService.RecordDatabaseOperationDuration(
                "CreateOrUpdateWardrobeItems",
                stopwatch.ElapsedMilliseconds
            );
        }
    }

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

            var slotName = item.Slot.ToString();
            if (await _lockService.IsSlotLockedAsync(profileId, slotName))
            {
                _logger.LogWarning(
                    "DeleteWardrobeItemAsync: slot {SlotName} is locked for profileId: {ProfileId}",
                    slotName,
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

            await using var connection = await _dataSource.OpenConnectionAsync();
            await using var transaction = await connection.BeginTransactionAsync();

            var sql = WardrobeSql.WithTransaction(transaction);
            await AcquireAdvisoryLockAsync(connection, transaction, profileId);
            success = await SaveWardrobeStateAsync(sql, profileId, state);

            await transaction.CommitAsync();

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

    private static async Task<bool> SaveWardrobeStateAsync(
        WardrobeSql sql,
        int profileId,
        WardrobeStateDto state
    )
    {
        WardrobeItemData? GetSlot(string slot) =>
            state.Equipment?.TryGetValue(slot, out var value) == true ? value : null;

        var head = GetSlot("Head");
        var body = GetSlot("Body");
        var hands = GetSlot("Hands");
        var legs = GetSlot("Legs");
        var feet = GetSlot("Feet");
        var ears = GetSlot("Ears");
        var neck = GetSlot("Neck");
        var wrists = GetSlot("Wrists");
        var lFinger = GetSlot("LFinger");
        var rFinger = GetSlot("RFinger");

        var result = await sql.UpdateWardrobeStateAsync(
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

        return result != null;
    }

    public async Task<bool> RandomizeActiveWardrobeAsync(int profileId)
    {
        var success = await WithWardrobeTransactionAsync(profileId, async sql =>
        {
            var allRows = await sql.ListWardrobeByProfileIdAsync(new(profileId));
            var allWardrobeItems = allRows.Select(row => new WardrobeDto(
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

            var currentState = await GetWardrobeStateAsync(profileId, sql);
            var equipment = currentState?.Equipment ?? new Dictionary<string, WardrobeItemData>();
            var modSettings = currentState?.ModSettings ?? new Dictionary<string, WardrobeItemData>();
            string? baseLayerBase64 = currentState?.BaseLayerBase64;

            var setItems = allWardrobeItems.Where(i => i.Type == "set").ToDictionary(i => i.Id);
            var itemItems = allWardrobeItems.Where(i => i.Type == "item").ToDictionary(i => i.Id);
            var modItems = allWardrobeItems.Where(i => i.Type == "moditem").ToDictionary(i => i.Id);

            // Randomize base set if not locked
            var baseLockId = "wardrobe-baseset";
            if (!await _lockService.IsSlotLockedAsync(profileId, baseLockId))
            {
                var baseCandidates = setItems.Values.Where(i => i.DataBase64 != null).ToList();
                if (baseCandidates.Count > 0)
                {
                    var chosen = baseCandidates[Random.Shared.Next(baseCandidates.Count)];
                    baseLayerBase64 = chosen.DataBase64;
                }
            }

            // Slot map
            var slotMap = new Dictionary<string, GlamourerEquipmentSlot>
            {
                ["Head"] = GlamourerEquipmentSlot.Head,
                ["Body"] = GlamourerEquipmentSlot.Body,
                ["Hands"] = GlamourerEquipmentSlot.Hands,
                ["Legs"] = GlamourerEquipmentSlot.Legs,
                ["Feet"] = GlamourerEquipmentSlot.Feet,
                ["Ears"] = GlamourerEquipmentSlot.Ears,
                ["Neck"] = GlamourerEquipmentSlot.Neck,
                ["Wrists"] = GlamourerEquipmentSlot.Wrists,
                ["LFinger"] = GlamourerEquipmentSlot.LFinger,
                ["RFinger"] = GlamourerEquipmentSlot.RFinger,
            };

            foreach (var kvp in slotMap)
            {
                var slotName = kvp.Key;
                var slotEnum = kvp.Value;
                var lockId = $"wardrobe-{slotName.ToLowerInvariant()}";

                if (await _lockService.IsSlotLockedAsync(profileId, lockId))
                {
                    // keep existing slot
                    continue;
                }

                var candidates = itemItems.Values
                    .Where(i => i.Slot == slotEnum && i.DataBase64 != null)
                    .ToList();

                if (candidates.Count > 0)
                {
                    var chosen = candidates[Random.Shared.Next(candidates.Count)];
                    var deserialized = DeserializeWardrobeDto(chosen);
                    if (deserialized != null)
                        equipment[slotName] = deserialized;
                }

                // Mods for this slot
                var modCandidates = modItems.Values
                    .Where(i => i.Slot == slotEnum && i.DataBase64 != null)
                    .ToList();

                if (modCandidates.Count > 0)
                {
                    var chosenMod = modCandidates[Random.Shared.Next(modCandidates.Count)];
                    var modDeserialized = DeserializeWardrobeDto(chosenMod);
                    if (modDeserialized != null)
                    {
                        var key = chosenMod.Id.ToString();
                        modSettings[key] = modDeserialized;
                    }
                }
            }

            var newState = new WardrobeStateDto(baseLayerBase64, equipment, modSettings);
            var result = await UpdateWardrobeStateAsync(profileId, newState, sql);
            if (!result)
            {
                _logger.LogWarning("RandomizeActiveWardrobe: transactional UpdateWardrobeStateAsync returned false for profileId={ProfileId}", profileId);
            }
            return result;
        });

        if (!success)
        {
            // double-check persisted state as a fallback
            var saved = await GetWardrobeStateAsync(profileId);
            if (saved != null && saved.Equipment != null && saved.Equipment.Count > 0)
                return true;
        }

        return success;
    }

    private WardrobeItemData? DeserializeWardrobeDto(WardrobeDto dto)
    {
        try
        {
            if (dto.DataBase64 == null)
                return null;

            GlamourerItem? item = null;

            // Try base64 decode first (canonical in some code paths)
            try
            {
                var bytes = Convert.FromBase64String(dto.DataBase64);
                item = JsonSerializer.Deserialize<GlamourerItem>(bytes, JsonOptions);
            }
            catch
            {
                // not base64, fall back to raw JSON
                try
                {
                    item = JsonSerializer.Deserialize<GlamourerItem>(dto.DataBase64, JsonOptions);
                }
                catch (Exception ex2)
                {
                    _logger.LogWarning(ex2, "DeserializeWardrobeDto failed to parse data for wardrobe id={Id}", dto.Id);
                    return null;
                }
            }

            if (item == null)
                return null;

            return new WardrobeItemData(
                dto.Id,
                dto.Name,
                dto.Description,
                dto.Slot,
                item,
                new List<GlamourerMod>(),
                new Dictionary<string, GlamourerMaterial>(),
                dto.Priority
            );
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "DeserializeWardrobeDto failed for wardrobe id={Id}", dto.Id);
            return null;
        }
    }

    public virtual async Task<WardrobeStateDto?> GetWardrobeStateAsync(int profileId)
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

    public async Task<WardrobeStateDto?> GetWardrobeStateAsync(int profileId, WardrobeSql sql)
    {
        var row = await sql.GetWardrobeStateAsync(new WardrobeSql.GetWardrobeStateArgs(profileId));
        return RowToWardrobeStateDto(row);
    }

    internal static WardrobeStateDto? RowToWardrobeStateDto(WardrobeSql.GetWardrobeStateRow? row)
    {
        if (row == null)
            return null;

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
                        var key = item.Id.ToString();
                        if (!modSettings.ContainsKey(key))
                            modSettings[key] = item;
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
