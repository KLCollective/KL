using KinkLinkCommon.Domain.CharacterState;
using KinkLinkCommon.Domain.Enums.Permissions;
using KinkLinkCommon.Domain.Wardrobe;
using MessagePack;

namespace KinkLinkCommon.Domain.Network.PairInteractions;

[MessagePackObject]
public record QueryPairStateRequest(
    [property: Key(0)] string TargetFriendCode
);

[MessagePackObject]
public record QueryPairStateResponse(
    [property: Key(0)] string TargetFriendCode,
    [property: Key(1)] CharacterStateDto? State,
    [property: Key(2)] bool HasGagPermission,
    [property: Key(3)] bool HasGarblerPermission,
    [property: Key(4)] bool HasWardrobePermission,
    [property: Key(5)] bool HasMoodlePermission
);

[MessagePackObject]
public record ApplyInteractionCommand(
    [property: Key(0)] string SenderFriendCode,
    [property: Key(1)] PairAction Action,
    [property: Key(2)] InteractionPayload? Payload
) : ActionCommand(SenderFriendCode);

[MessagePackObject]
public record InteractionPayload(
    [property: Key(0)] GagStateDto? Gag,
    [property: Key(1)] GarblerStateDto? Garbler,
    [property: Key(2)] List<WardrobeDto>? WardrobeItems,
    [property: Key(3)] Dependencies.Moodles.Domain.MoodleInfo? Moodle
);

[MessagePackObject]
public record QueryPairWardrobeStateRequest(
    [property: Key(0)] string TargetFriendCode
);

[MessagePackObject]
public record QueryPairWardrobeStateResponse(
    [property: Key(0)] string TargetFriendCode,
    [property: Key(1)] bool HasWardrobePermission,
    [property: Key(2)] WardrobeStateDto? State
);

[MessagePackObject]
public record QueryPairWardrobeRequest(
    [property: Key(0)] string TargetFriendCode
);

[MessagePackObject]
public record QueryPairWardrobeResponse(
    [property: Key(0)] string TargetFriendCode,
    [property: Key(1)] bool HasWardrobePermission,
    [property: Key(2)] List<WardrobeDto> Items
);
