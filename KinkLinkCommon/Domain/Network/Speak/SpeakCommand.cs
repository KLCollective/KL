using KinkLinkCommon.Domain.Enums;
using MessagePack;

namespace KinkLinkCommon.Domain.Network.Speak;

[MessagePackObject]
public record SpeakCommand(
    [property: Key(1)] string SenderFriendCode,
    [property: Key(2)] string Message,
    [property: Key(3)] ChatChannel ChatChannel,
    [property: Key(4)] string? Extra
) : ActionCommand(SenderFriendCode);
