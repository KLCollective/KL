using KinkLinkCommon.Domain;
using KinkLinkCommon.Domain.Enums;
using KinkLinkCommon.Domain.Enums.Permissions;

namespace KinkLinkClient.Domain;

public class Friend(
    string friendCode,
    FriendOnlineStatus status,
    // TODO: Add this in
    //string alias,
    string? note = null,
    UserPermissions? permissionsGrantedToFriend = null,
    UserPermissions? permissionsGrantedByFriend = null,
    InteractionContext? interactionContext = null
)
{
    /// <summary>
    ///     The unique friend code identifying this friend/pair relationship.
    /// </summary>
    public readonly string FriendCode = friendCode;

    // TODO: Implment displaying the alias rather than friendcode
    // public readonly string Alias = alias;

    /// <summary>
    ///     An optional user-defined note (AR legacy, may be removed layer)
    /// </summary>
    public string? Note { get; set; } = note;

    /// <summary>
    ///     The current online status of this friend.
    /// </summary>
    public FriendOnlineStatus Status { get; set; } = status;

    public UserPermissions PermissionsGrantedToFriend =
        permissionsGrantedToFriend ?? new UserPermissions();
    public UserPermissions PermissionsGrantedByFriend =
        permissionsGrantedByFriend ?? new UserPermissions();

    public long LastInteractedWith = 0;
    public InteractionContext? InteractionState { get; set; } = interactionContext;

    public string NoteOrFriendCode => Note ?? FriendCode;
    public bool HasGagPermission =>
        PermissionsGrantedByFriend.Perms.HasFlag(InteractionPerms.CanApplyGag);
    public bool HasGarblerPermission =>
        PermissionsGrantedByFriend.Perms.HasFlag(InteractionPerms.CanEnableGarbler);
    public bool HasWardrobePermission =>
        PermissionsGrantedByFriend.Perms.HasFlag(InteractionPerms.CanApplyWardrobe);
    public bool HasMoodlePermission =>
        PermissionsGrantedByFriend.Perms.HasFlag(InteractionPerms.CanApplyOwnMoodles)
        || PermissionsGrantedByFriend.Perms.HasFlag(InteractionPerms.CanApplyPairsMoodles);
}
