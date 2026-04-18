using KinkLinkCommon.Domain.Enums;
using MessagePack;

namespace KinkLinkCommon.Domain.Network.SyncOnlineStatus;

[MessagePackObject(keyAsPropertyName: true)]
public record SyncOnlineStatusCommand(
    string SenderFriendCode,
    FriendOnlineStatus Status,
    UserPermissions Permissions
);
