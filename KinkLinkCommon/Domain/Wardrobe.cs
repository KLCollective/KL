using KinkLinkCommon.Dependencies.Glamourer;
using KinkLinkCommon.Domain.Enums;
using MessagePack;
using Microsoft.Extensions.Logging;

namespace KinkLinkCommon.Domain.Wardrobe;

// These are the valid wardrobe layers that can be used for storing glamourer designs and such
public enum WardrobeLayer
{
    BaseLayer,
    Head,
    Hands,
    Legs,
    Feet,
    Ears,
    Neck,
    Wrists,
    RFinger,
    LFinger,
    Mods,
}

/// This is the wardrobe item itself, this is used for saving and returning the actual wardrobe data to the client.
/// i.e. Thisis what is currently applied
[MessagePackObject]
public record WardrobeDto(
    [property: Key(0)] Guid Id,
    [property: Key(1)] string Name,
    [property: Key(2)] string Description,
    [property: Key(3)] WardrobeLayer Layer,
    // GlamourerDesign serialized as a base64 string (sent over wire)
    [property: Key(4)] string Base64GlamourerData,
    [property: Key(5)] RelationshipPriority Priority
);

// This is the Users full _active_ wardrobe state
// I.e. it is what is currently applied
[MessagePackObject]
public record WardrobeStateDto(
    // GlamourerDesign serialized as a base64 string (sent over wire and to glamourer)
    [property: Key(0)] Dictionary<WardrobeLayer, string> Layers
);

// Trimmed down wardrobe data _exclusively_ for sending to pairs as an info update, contains no glamourer data
[MessagePackObject]
public record PairWardrobeItemDto(
    [property: Key(0)] Guid Id,
    [property: Key(1)] string Name,
    [property: Key(2)] string Description,
    [property: Key(3)] GlamourerEquipmentSlot Slot,
    [property: Key(4)] RelationshipPriority Priority,
    [property: Key(5)] LockInfoDto? LockId
);

// Trimmed down wardrobe data _exclusively_ for sending to pairs as a friend update
[MessagePackObject]
public record class PairWardrobeStateDto(
    // GlamourerDesign serialized as a base64 string (sent over wire)
    [property: Key(0)] Dictionary<WardrobeLayer, PairWardrobeItemDto> Layers
)
{
    public static PairWardrobeStateDto PopulateLockIds<T>(
        PairWardrobeStateDto wardrobe,
        List<LockInfoDto> locks,
        ILogger<T> logger
    )
    {
        // TODO: For each item, check it against the lock DTO by parsing the lockid into the WardrobeLayer and then applying the lock info to the entry.
        // Just stuff each one directly into the appropriate layer.
        return wardrobe;
    }
}
