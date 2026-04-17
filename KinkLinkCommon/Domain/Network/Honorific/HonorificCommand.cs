using KinkLinkCommon.Dependencies.Honorific.Domain;
using MessagePack;

namespace KinkLinkCommon.Domain.Network.Honorific;

[MessagePackObject]
public record HonorificCommand(
    [property: Key(0)] string SenderFriendCode,
    [property: Key(1)] HonorificInfo Honorific
) : ActionCommand(SenderFriendCode);
