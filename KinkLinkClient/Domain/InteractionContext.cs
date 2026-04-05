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

    public PairWardrobeItemDto? BaseSet { get; init; }

    public Dictionary<GlamourerEquipmentSlot, PairWardrobeItemDto> WardrobeSlots { get; init; } =
        new();

    public Dictionary<string, LockInfoDto> SlotLocks { get; init; } = new();

    public static InteractionContext FromPairState(QueryPairStateResponse pairState)
    {
        var wardrobeSlots = new Dictionary<GlamourerEquipmentSlot, PairWardrobeItemDto>();

        if (pairState.WardrobeState.Equipment != null)
        {
            foreach (var (slotKey, item) in pairState.WardrobeState.Equipment)
            {
                wardrobeSlots[item.Slot] = item;
            }
        }

        var slotLocks = pairState.LockStates.ToDictionary(l => l.LockID);

        return new InteractionContext
        {
            FriendCode = pairState.TargetFriendCode,
            BaseSet = pairState.WardrobeState.BaseLayer,
            WardrobeSlots = wardrobeSlots,
            SlotLocks = slotLocks,
        };
    }
}
