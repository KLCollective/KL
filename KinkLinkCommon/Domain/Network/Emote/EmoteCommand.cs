using MessagePack;

namespace KinkLinkCommon.Domain.Network.Emote;

[MessagePackObject]
public record EmoteCommand(
    [property: Key(0)] string SenderFriendCode,
    [property: Key(1)] string Emote,
    [property: Key(2)] bool DisplayLogMessage
) : ActionCommand(SenderFriendCode);
