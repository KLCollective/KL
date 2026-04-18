using KinkLinkCommon.Domain.Network;
using MessagePack;

namespace KinkLinkCommon.Domain;

[MessagePackObject(keyAsPropertyName: true)]
public record KinkLinkProfile(
    string Uid,
    string? ChatRole,
    string? Alias,
    Title Title,
    string? Description,
    DateTime? CreatedAt,
    DateTime? UpdatedAt
)
{
    public KinkLinkProfile() : this("", null, null, Title.Kinkster, null, null, null) { }
}

[MessagePackObject(keyAsPropertyName: true)]
public record KinkLinkProfileConfig(
    bool EnableGlamours,
    bool EnableGarbler,
    bool EnableGarblerChannels,
    bool EnableMoodles
)
{
    public KinkLinkProfileConfig() : this(false, false, false, false) { }
}
