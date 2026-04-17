using MessagePack;

namespace KinkLinkCommon.Domain.Network.Customize;

/// <summary>
///     Forwarded object containing the information to handle a customize plus request on a client
/// </summary>
[MessagePackObject]
public record CustomizeCommand(
    [property: Key(0)] string SenderFriendCode,
    [property: Key(1)] byte[] JsonBoneDataBytes
) : ActionCommand(SenderFriendCode);
