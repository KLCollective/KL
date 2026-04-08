using KinkLinkCommon.Domain;
using KinkLinkCommon.Domain.Enums;
using MessagePack;

namespace KinkLinkCommon.Domain.Network.ProfileConfig;

[MessagePackObject]
public record GetProfileConfigResponse(
    [property: Key(0)] ActionResultEc Result,
    [property: Key(1)] KinkLinkProfileConfig? Config
);

[MessagePackObject]
public record UpdateProfileConfigResponse(
    [property: Key(0)] ActionResultEc Result,
    [property: Key(1)] KinkLinkProfileConfig? Config
);
