using KinkLinkCommon.Dependencies.Moodles.Domain;
using MessagePack;

namespace KinkLinkCommon.Domain.Network.Moodles;

[MessagePackObject]
public record MoodlesCommand(
    [property: Key(0)] string SenderFriendCode,
    [property: Key(1)] MoodleInfo Info
) : ActionCommand(SenderFriendCode);
