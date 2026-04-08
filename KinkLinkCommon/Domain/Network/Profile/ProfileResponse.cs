using KinkLinkCommon.Domain;
using KinkLinkCommon.Domain.Enums;
using MessagePack;

namespace KinkLinkCommon.Domain.Network.Profile;

[MessagePackObject]
public record GetProfileResponse(
    [property: Key(0)] ActionResultEc Result,
    [property: Key(1)] KinkLinkProfile? Profile
);

[MessagePackObject]
public record UpdateProfileResponse(
    [property: Key(0)] ActionResultEc Result,
    [property: Key(1)] KinkLinkProfile? Profile
);
