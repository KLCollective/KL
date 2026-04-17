using MessagePack;

namespace KinkLinkCommon.Domain.Network.SyncPermissions;

[MessagePackObject]
public record SyncPermissionsCommand(
    [property: Key(0)] string SenderFriendCode,
    [property: Key(1)] UserPermissions PermissionsGrantedBySender
) : ActionCommand(SenderFriendCode);
