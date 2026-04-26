using KinkLinkCommon.Dependencies.Glamourer.Components;
using KinkLinkCommon.Domain.CharacterState;
using KinkLinkCommon.Domain.Enums;
using MessagePack;

namespace KinkLinkCommon.Domain.Network.SyncPairState;

[MessagePackObject(keyAsPropertyName: true)]
public record LockStateDto(
    string LockId,
    bool IsLocked,
    string LockedByAlias,
    RelationshipPriority LockPriority,
    bool CanSelfUnlock,
    DateTime Expires,
    string? Password
);
