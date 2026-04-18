using MessagePack;

namespace KinkLinkCommon.Domain.Network.Emote;

[MessagePackObject]
public record EmoteCommand(
    string SenderFriendCode,
    string Emote,
    bool DisplayLogMessage
);
