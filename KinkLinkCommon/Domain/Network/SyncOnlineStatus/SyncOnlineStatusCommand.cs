using KinkLinkCommon.Database;
using KinkLinkCommon.Domain.Enums;
using MessagePack;

namespace KinkLinkCommon.Domain.Network.SyncOnlineStatus;

[MessagePackObject]
public record SyncOnlineStatusCommand(
    [property: Key(0)] string SenderFriendCode,
    [property: Key(1)] FriendOnlineStatus Status,
    [property: Key(2)] UserPermissions? Permissions
) : ActionCommand(SenderFriendCode);
