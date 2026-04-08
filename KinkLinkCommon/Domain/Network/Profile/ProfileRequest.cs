using MessagePack;

namespace KinkLinkCommon.Domain.Network.Profile;

[MessagePackObject]
public record GetProfileRequest([property: Key(0)] string Uid);

[MessagePackObject]
public record UpdateProfileRequest(
    [property: Key(0)] string? Alias,
    [property: Key(1)] KinkLinkCommon.Domain.Network.Title Title,
    [property: Key(2)] string? Description,
    [property: Key(3)] string? ChatRole
);
