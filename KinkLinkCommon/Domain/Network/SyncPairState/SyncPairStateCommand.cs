using KinkLinkCommon.Dependencies.Glamourer.Components;
using KinkLinkCommon.Domain.CharacterState;
using KinkLinkCommon.Domain.Enums;
using MessagePack;

namespace KinkLinkCommon.Domain.Network.SyncPairState;

[MessagePackObject]
public record LockStateDto(
    [property: Key(0)] string LockId,
    [property: Key(1)] bool IsLocked,
    [property: Key(2)] string LockedByAlias,
    [property: Key(3)] RelationshipPriority LockPriority,
    [property: Key(4)] bool CanSelfUnlock,
    [property: Key(5)] DateTime Expires,
    [property: Key(6)] string? Password
);
