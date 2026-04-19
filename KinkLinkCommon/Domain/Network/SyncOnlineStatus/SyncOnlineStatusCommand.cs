using KinkLinkCommon.Database;
using KinkLinkCommon.Domain.Enums;
using MessagePack;

namespace KinkLinkCommon.Domain.Network.SyncOnlineStatus;

[MessagePackObject]
public record SyncOnlineStatusCommand(
    [property: Key(1)] string SenderFriendCode,
    [property: Key(2)] FriendOnlineStatus Status,
    [property: Key(3)] UserPermissions? Permissions
) : ActionCommand(SenderFriendCode);
