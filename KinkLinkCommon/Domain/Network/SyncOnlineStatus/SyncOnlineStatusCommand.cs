using KinkLinkCommon.Database;
using KinkLinkCommon.Domain.Enums;
using MessagePack;

namespace KinkLinkCommon.Domain.Network.SyncOnlineStatus;

[MessagePackObject]
public record SyncOnlineStatusCommand(
    string SenderFriendCode,
    FriendOnlineStatus Status,
    UserPermissions? Permissions
);
