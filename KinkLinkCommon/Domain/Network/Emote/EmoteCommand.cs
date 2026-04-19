using MessagePack;

namespace KinkLinkCommon.Domain.Network.Emote;

[MessagePackObject]
public record EmoteCommand(
    string SenderFriendCode,
    [property: Key(1)] string Emote,
    [property: Key(2)] bool DisplayLogMessage
) : ActionCommand(SenderFriendCode);
