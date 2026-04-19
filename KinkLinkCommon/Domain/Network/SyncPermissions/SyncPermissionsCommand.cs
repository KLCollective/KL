using MessagePack;

namespace KinkLinkCommon.Domain.Network.SyncPermissions;

[MessagePackObject]
public record SyncPermissionsCommand(
    [property: Key(1)] string SenderFriendCode,
    [property: Key(2)] UserPermissions PermissionsGrantedBySender
) : ActionCommand(SenderFriendCode);
