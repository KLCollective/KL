using System;
using System.Collections.Generic;
using System.Linq;
using KinkLinkCommon.Dependencies.Glamourer;
using KinkLinkCommon.Domain;
using KinkLinkCommon.Domain.CharacterState;
using KinkLinkCommon.Domain.Network.PairInteractions;
using KinkLinkCommon.Domain.Network.SyncPairState;
using KinkLinkCommon.Domain.Wardrobe;

namespace KinkLinkClient.Domain;

/// <summary>
///     Represents the interaction context for a specific friend/pair
/// </summary>
public record InteractionContext
{
    public required string FriendCode { get; init; }

    public Dictionary<WardrobeLayer, PairWardrobeItemDto> WardrobeLayers { get; init; } = new();

    public Dictionary<string, LockInfoDto> SlotLocks { get; init; } = new();

    public static InteractionContext FromPairState(QueryPairStateResponse pairState)
    {
        var wardrobeLayers = new Dictionary<WardrobeLayer, PairWardrobeItemDto>();

        if (pairState.WardrobeState?.Layers != null)
        {
            foreach (var (layer, item) in pairState.WardrobeState.Layers)
            {
                if (Enum.TryParse<WardrobeLayer>(layer.ToString(), out var slot))
                {
                    wardrobeLayers[slot] = item;
                }
            }
        }

        var slotLocks = pairState.LockStates.ToDictionary(l => l.LockID);

        return new InteractionContext
        {
            FriendCode = pairState.TargetFriendCode,
            WardrobeLayers = wardrobeLayers,
            SlotLocks = slotLocks,
        };
    }
}
