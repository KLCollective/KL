using MessagePack;

namespace KinkLinkCommon.Domain.Network.Customize;

/// <summary>
///     Forwarded object containing the information to handle a customize plus request on a client
/// </summary>
[MessagePackObject]
public record CustomizeCommand(string SenderFriendCode, byte[] JsonBoneDataBytes)
    : ActionCommand(SenderFriendCode);
