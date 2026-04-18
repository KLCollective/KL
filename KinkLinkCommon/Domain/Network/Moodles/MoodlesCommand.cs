using KinkLinkCommon.Dependencies.Moodles.Domain;
using MessagePack;

namespace KinkLinkCommon.Domain.Network.Moodles;

[MessagePackObject]
public record MoodlesCommand(string SenderFriendCode, MoodleInfo Info);
