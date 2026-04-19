using KinkLinkCommon.Dependencies.Honorific.Domain;
using MessagePack;

namespace KinkLinkCommon.Domain.Network.Honorific;

[MessagePackObject]
public record HonorificCommand([property: Key(1)] string SenderFriendCode, [property: Key(2)] HonorificInfo Honorific)
    : ActionCommand(SenderFriendCode);
