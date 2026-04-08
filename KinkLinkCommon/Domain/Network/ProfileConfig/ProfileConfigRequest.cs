using MessagePack;

namespace KinkLinkCommon.Domain.Network.ProfileConfig;

[MessagePackObject]
public record GetProfileConfigRequest(
    [property: Key(0)] string Uid
);

[MessagePackObject]
public record UpdateProfileConfigRequest(
    [property: Key(0)] string Uid,
    [property: Key(1)] bool EnableGlamours,
    [property: Key(2)] bool EnableGarbler,
    [property: Key(3)] bool EnableGarblerChannels,
    [property: Key(4)] bool EnableMoodles
);
