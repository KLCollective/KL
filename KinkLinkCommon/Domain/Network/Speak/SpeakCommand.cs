using KinkLinkCommon.Domain.Enums;
using MessagePack;

namespace KinkLinkCommon.Domain.Network.Speak;

[MessagePackObject]
public record SpeakCommand(
    string SenderFriendCode,
    string Message,
    ChatChannel ChatChannel,
    string? Extra
);
