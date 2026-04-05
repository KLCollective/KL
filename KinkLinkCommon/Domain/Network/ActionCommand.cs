using MessagePack;

namespace KinkLinkCommon.Domain.Network;

[MessagePackObject]
public record ActionCommand([property: Key(0)] string TargetFriendCode);
