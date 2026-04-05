using KinkLinkCommon.Dependencies.Glamourer;
using KinkLinkCommon.Dependencies.Glamourer.Components;
using KinkLinkCommon.Domain.Enums;
using MessagePack;
using Microsoft.Extensions.Logging;

namespace KinkLinkCommon.Domain.Wardrobe;

/// |This is the request sent as a request to update the wardrobe state.
[MessagePackObject]
public record WardrobeDto(
    [property: Key(0)] Guid Id,
    [property: Key(1)] string Name,
    [property: Key(2)] string Description,
    [property: Key(3)] string Type,
    [property: Key(4)] GlamourerEquipmentSlot Slot,
    // GlamourerDesign serialized as a base64 string (sent over wire)
    [property: Key(5)] string DataBase64,
    [property: Key(6)] RelationshipPriority Priority,
    [property: Key(7)] string? LockId
);

// Wardrobe state for an individual slot
[MessagePackObject]
public record WardrobeItemData(
    [property: Key(0)] Guid Id,
    [property: Key(1)] string Name,
    [property: Key(2)] string Description,
    [property: Key(3)] GlamourerEquipmentSlot Slot,
    [property: Key(4)] GlamourerItem? Item,
    [property: Key(5)] List<GlamourerMod>? Mods,
    [property: Key(6)] Dictionary<string, GlamourerMaterial>? Materials,
    [property: Key(7)] RelationshipPriority Priority
);

// This is the Users actual full wardrobe state
[MessagePackObject]
public record WardrobeStateDto(
    // GlamourerDesign serialized as a base64 string (sent over wire)
    [property: Key(0)] string? BaseLayerBase64,
    // Slot name to WardrobeItemData mapping
    [property: Key(1)] Dictionary<string, WardrobeItemData>? Equipment,
    // Mod name to WardrobeItemData mapping
    [property: Key(2)] Dictionary<string, WardrobeItemData>? ModSettings
);

// Trimmed down wardrobe data _exclusively_ for sending to pairs as a friend update
[MessagePackObject]
public record PairWardrobeItemDto(
    [property: Key(0)] Guid Id,
    [property: Key(1)] string Name,
    [property: Key(2)] string Description,
    [property: Key(3)] GlamourerEquipmentSlot Slot,
    [property: Key(4)] RelationshipPriority Priority,
    [property: Key(5)] string? LockId
);

// Trimmed down wardrobe data _exclusively_ for sending to pairs as a friend update
[MessagePackObject]
public record class PairWardrobeStateDto(
    // GlamourerDesign serialized as a base64 string (sent over wire)
    [property: Key(0)] PairWardrobeItemDto? BaseLayer,
    // Slot name to WardrobeItemData mapping
    [property: Key(1)] Dictionary<string, PairWardrobeItemDto>? Equipment
)
{
    public static PairWardrobeStateDto PopulateLockIds<T>(
        PairWardrobeStateDto wardrobe,
        List<LockInfoDto> locks,
        ILogger<T> logger
    )
    {
        var lockLookup = locks.ToDictionary(l => l.LockID);
        logger.LogInformation("[PopulateLockIds] Total locks in lookup: {Count}", lockLookup.Count);

        PairWardrobeItemDto? updatedBaseSet = null;
        if (wardrobe.BaseLayer != null)
        {
            var baseSetLockId = "wardrobe-baseset";
            var hasLock = lockLookup.ContainsKey(baseSetLockId);
            logger.LogInformation(
                "[PopulateLockIds] BaseSet={Name}, BaseSetLockId={LockId}, HasLock={HasLock}",
                wardrobe.BaseLayer.Name,
                baseSetLockId,
                hasLock
            );
            updatedBaseSet = wardrobe.BaseLayer with { LockId = hasLock ? baseSetLockId : null };
        }

        var updatedEquipment = new Dictionary<string, PairWardrobeItemDto>();
        if (wardrobe.Equipment != null)
            foreach (var kvp in wardrobe.Equipment)
            {
                var slotKey = kvp.Key;
                var item = kvp.Value;
                var lockId = $"wardrobe-{slotKey.ToLowerInvariant()}";
                var hasLock = lockLookup.ContainsKey(lockId);
                logger.LogInformation(
                    "[PopulateLockIds] Slot={Slot}, Item={Item}, ExpectedLockId={LockId}, HasLock={HasLock}",
                    slotKey,
                    item.Name,
                    lockId,
                    hasLock
                );
                updatedEquipment[slotKey] = item with { LockId = hasLock ? lockId : null };
            }

        return new PairWardrobeStateDto(updatedBaseSet, updatedEquipment);
    }
}
