using MessagePack;

namespace KinkLinkCommon.Domain.Network.SyncPermissions;

[MessagePackObject(keyAsPropertyName: true)]
public record SyncPermissionsCommand(
    string SenderFriendCode,
    UserPermissions PermissionsGrantedBySender
);
