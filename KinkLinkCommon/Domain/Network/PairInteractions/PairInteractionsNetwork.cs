using KinkLinkCommon.Dependencies.Moodles.Domain;
using KinkLinkCommon.Domain;
using KinkLinkCommon.Domain.CharacterState;
using KinkLinkCommon.Domain.Enums.Permissions;
using KinkLinkCommon.Domain.Network.SyncPairState;
using KinkLinkCommon.Domain.Wardrobe;
using MessagePack;

namespace KinkLinkCommon.Domain.Network.PairInteractions;

[MessagePackObject(keyAsPropertyName: true)]
public record QueryPairStateRequest(string TargetFriendCode);

[MessagePackObject(keyAsPropertyName: true)]
public record QueryPairStateResponse(
    string TargetFriendCode,
    UserPermissions GrantedTo,
    PairWardrobeStateDto WardrobeState,
    List<LockInfoDto> LockStates
);

[MessagePackObject(keyAsPropertyName: true)]
public record QueryPairWardrobeStateRequest(string TargetFriendCode);

[MessagePackObject(keyAsPropertyName: true)]
public record QueryPairWardrobeStateResponse(
    string TargetFriendCode,
    bool HasWardrobePermission,
    PairWardrobeStateDto? State
);

[MessagePackObject(keyAsPropertyName: true)]
public record QueryPairWardrobeRequest(string TargetFriendCode);

[MessagePackObject(keyAsPropertyName: true)]
public record QueryPairWardrobeResponse(string TargetFriendCode, List<LightWardrobeItemDto> Items)
{
    public QueryPairWardrobeResponse()
        : this("", []) { }
}

[MessagePackObject(keyAsPropertyName: true)]
public record ApplyWardrobeRequest(
    string TargetFriendCode,
    WardrobeLayer Layer,
    Guid? Id
);

[MessagePackObject(keyAsPropertyName: true)]
public record RemoveWardrobeRequest(
    string TargetFriendCode,
    WardrobeLayer Layer
);

[MessagePackObject(keyAsPropertyName: true)]
public record PairApplyLockRequest(
    string TargetFriendCode,
    LockInfoDto LockInfo
);

[MessagePackObject(keyAsPropertyName: true)]
public record PairRemoveLockRequest(
    string TargetFriendCode,
    LockKind LockId,
    string? Password
);
