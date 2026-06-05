using KinkLinkCommon.Dependencies.Glamourer;
using KinkLinkCommon.Domain.Enums;
using MessagePack;

namespace KinkLinkCommon.Domain.Wardrobe;

// These are the valid wardrobe layers that can be used for storing glamourer designs and such
public enum WardrobeLayer
{
    Outfit,
    Head,
    Chest,
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
public record LightWardrobeItemDto(
    [property: Key(0)] Guid Id,
    [property: Key(1)] string Name,
    [property: Key(2)] string Description,
    [property: Key(3)] WardrobeLayer Layer,
    [property: Key(4)] RelationshipPriority Priority,
    [property: Key(5)] LockInfoDto? LockId
);

// Trimmed down wardrobe data _exclusively_ for sending to pairs as a friend update
[MessagePackObject]
public record class PairWardrobeStateDto(
    // GlamourerDesign serialized as a base64 string (sent over wire)
    [property: Key(0)] Dictionary<WardrobeLayer, LightWardrobeItemDto> Layers
)
{
    public static PairWardrobeStateDto PopulateLockIds(
        PairWardrobeStateDto wardrobe,
        List<LockInfoDto> locks
    )
    {
        // Assert that the wardrobe exists
        if (wardrobe == null)
        {
            throw new NullReferenceException("Wardrobe is null, this is not allowed");
        }
        if (wardrobe.Layers == null || locks == null || locks.Count == 0)
        {
            return wardrobe;
        }

        foreach (var lockInfo in locks)
        {
            // Convert LockKind to WardrobeLayer using the extension method
            WardrobeLayer layer;
            try
            {
                layer = lockInfo.LockID.ToWardrobeLayer();
            }
            catch (ArgumentOutOfRangeException)
            {
                System.Diagnostics.Trace.WriteLine(
                    $"[Wardrobe] PopulateLockIds: LockKind {lockInfo.LockID} is not a wardrobe lock, skipping."
                );
                continue;
            }

            if (wardrobe.Layers.TryGetValue(layer, out var item))
            {
                // assign lock info to that wardrobe layer's item
                var updated = item with
                {
                    LockId = lockInfo,
                };
                wardrobe.Layers[layer] = updated;
            }
        }

        return wardrobe;
    }
}
