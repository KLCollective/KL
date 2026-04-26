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
public record ApplyInteractionCommand(
    string TargetFriendCode,
    PairAction Action,
    InteractionPayload? Payload
);

[MessagePackObject(keyAsPropertyName: true)]
public record InteractionPayload(
    GagStateDto? Gag,
    GarblerStateDto? Garbler,
    List<WardrobeDto>? WardrobeItems,
    Dependencies.Moodles.Domain.MoodleInfo? Moodle
);

[MessagePackObject(keyAsPropertyName: true)]
public record QueryPairWardrobeStateRequest(string TargetFriendCode);

[MessagePackObject(keyAsPropertyName: true)]
public record QueryPairWardrobeStateResponse(
    string TargetFriendCode,
    bool HasWardrobePermission,
    WardrobeStateDto? State
);

[MessagePackObject(keyAsPropertyName: true)]
public record QueryPairWardrobeRequest(string TargetFriendCode);

[MessagePackObject(keyAsPropertyName: true)]
public record QueryPairWardrobeResponse(string TargetFriendCode, List<PairWardrobeItemDto> Items)
{
    public QueryPairWardrobeResponse()
        : this("", []) { }
}
