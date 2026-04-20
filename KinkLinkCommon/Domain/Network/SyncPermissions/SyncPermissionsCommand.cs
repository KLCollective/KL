using MessagePack;

namespace KinkLinkCommon.Domain.Network.SyncPermissions;

[MessagePackObject]
public record SyncPermissionsCommand(
    string SenderFriendCode,
    UserPermissions PermissionsGrantedBySender
);
