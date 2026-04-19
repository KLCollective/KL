using MessagePack;

namespace KinkLinkCommon.Domain.Network.Customize;

/// <summary>
///     Forwarded object containing the information to handle a customize plus request on a client
/// </summary>
[MessagePackObject]
public record CustomizeCommand([property: Key(1)] string SenderFriendCode, [property: Key(2)] byte[] JsonBoneDataBytes)
    : ActionCommand(SenderFriendCode);
