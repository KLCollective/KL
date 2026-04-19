using KinkLinkCommon.Dependencies.Moodles.Domain;
using MessagePack;

namespace KinkLinkCommon.Domain.Network.Moodles;

[MessagePackObject]
public record MoodlesCommand([property: Key(1)] string SenderFriendCode, [property: Key(2)] MoodleInfo Info)
    : ActionCommand(SenderFriendCode);
