using MessagePack;

namespace KinkLinkCommon.Domain.Network.Emote;

[MessagePackObject]
public record EmoteCommand(
    [property: Key(1)] string SenderFriendCode,
    [property: Key(2)] string Emote,
    [property: Key(3)] bool DisplayLogMessage
) : ActionCommand(SenderFriendCode);
