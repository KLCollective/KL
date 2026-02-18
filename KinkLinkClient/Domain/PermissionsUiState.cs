using KinkLinkCommon.Domain;
using KinkLinkCommon.Domain.Enums;
using KinkLinkCommon.Domain.Enums.Permissions;

namespace KinkLinkClient.Domain;

// ReSharper disable ConvertIfStatementToReturnStatement

/// <summary>
///     Class representing the <see cref="UserPermissions"/> object but as booleans for use with ImGui
/// </summary>
public class PermissionsUiState
{

    public bool Temporary = false;
    /// Help define the permissions modes
    public PermissionsModes Mode = PermissionsModes.None;

    /// Gag perms
    public bool CanApplyGag, CanLockGag, CanUnlockGag, CanRemoveGag, CanForceEnableGagGlamour, CanEnableGarbler, CanLockGarbler, CanSetGarlblerChannels, CanLockGarlbleChannels;
    /// Wardrobe permsThese permissions are specifically related to the wardrobe section of the interaction menu
    public bool CanApplyWardrobe, CanLockWardrobe, CanUnlockWardrobe, CanRemoveWardrobe, CanForceEnableWardrobeGlamour;
    /// These permissions influence all moodles permissions in the interaction menu
    public bool CanApplyOwn, CanApplyPairs, CanLockMoodles, CanUnlockMoodles, CanRemoveMoodles;
    public RelationshipPriority Priority = RelationshipPriority.Casual;
    /// <summary>
    ///     Tests if this object is equal to another <see cref="PermissionsUiState"/>
    /// </summary>
    public bool Equals(PermissionsUiState other)
    {
        if (Temporary != other.Temporary) return false;
        if (Mode != other.Mode) return false;
        if (Priority != other.Priority) return false;

        // Gag permissions
        if (CanApplyGag != other.CanApplyGag) return false;
        if (CanLockGag != other.CanLockGag) return false;
        if (CanUnlockGag != other.CanUnlockGag) return false;
        if (CanRemoveGag != other.CanRemoveGag) return false;
        if (CanForceEnableGagGlamour != other.CanForceEnableGagGlamour) return false;
        if (CanEnableGarbler != other.CanEnableGarbler) return false;
        if (CanLockGarbler != other.CanLockGarbler) return false;
        if (CanSetGarlblerChannels != other.CanSetGarlblerChannels) return false;
        if (CanLockGarlbleChannels != other.CanLockGarlbleChannels) return false;

        // Wardrobe permissions
        if (CanApplyWardrobe != other.CanApplyWardrobe) return false;
        if (CanLockWardrobe != other.CanLockWardrobe) return false;
        if (CanUnlockWardrobe != other.CanUnlockWardrobe) return false;
        if (CanRemoveWardrobe != other.CanRemoveWardrobe) return false;
        if (CanForceEnableWardrobeGlamour != other.CanForceEnableWardrobeGlamour) return false;

        // Moodle permissions
        if (CanApplyOwn != other.CanApplyOwn) return false;
        if (CanApplyPairs != other.CanApplyPairs) return false;
        if (CanLockMoodles != other.CanLockMoodles) return false;
        if (CanUnlockMoodles != other.CanUnlockMoodles) return false;
        if (CanRemoveMoodles != other.CanRemoveMoodles) return false;

        return true;
    }

    /// <summary>
    ///     Converts a <see cref="UserPermissions"/> into a <see cref="PermissionsUiState"/>
    /// </summary>
    public static PermissionsUiState From(UserPermissions permissions)
    {
        var perms = permissions.Perms;
        return new PermissionsUiState
        {
            Temporary = permissions.Expires != null,
            Priority = permissions.Priority,

            // Gag permissions
            CanApplyGag = perms.HasFlag(InteractionPerms.CanApplyGag),
            CanLockGag = perms.HasFlag(InteractionPerms.CanLockGag),
            CanUnlockGag = perms.HasFlag(InteractionPerms.CanUnlockGag),
            CanRemoveGag = perms.HasFlag(InteractionPerms.CanRemoveGag),
            CanForceEnableGagGlamour = perms.HasFlag(InteractionPerms.CanForceEnableGlamourGag),
            CanEnableGarbler = perms.HasFlag(InteractionPerms.CanEnableGarbler),
            CanLockGarbler = perms.HasFlag(InteractionPerms.CanLockGarbler),
            CanSetGarlblerChannels = perms.HasFlag(InteractionPerms.CanSetGarlblerChannels),
            CanLockGarlbleChannels = perms.HasFlag(InteractionPerms.CanLockGarlbleChannels),

            // Wardrobe permissions
            CanApplyWardrobe = perms.HasFlag(InteractionPerms.CanApplyWardrobe),
            CanLockWardrobe = perms.HasFlag(InteractionPerms.CanLockWardrobe),
            CanUnlockWardrobe = perms.HasFlag(InteractionPerms.CanUnlockWardrobe),
            CanRemoveWardrobe = perms.HasFlag(InteractionPerms.CanRemoveWardrobe),
            CanForceEnableWardrobeGlamour = perms.HasFlag(InteractionPerms.CanForceEnableGlamour),

            // Moodle permissions
            CanApplyOwn = perms.HasFlag(InteractionPerms.CanApplyOwnMoodles),
            CanApplyPairs = perms.HasFlag(InteractionPerms.CanApplyPairsMoodles),
            CanLockMoodles = perms.HasFlag(InteractionPerms.CanLockMoodles),
            CanUnlockMoodles = perms.HasFlag(InteractionPerms.CanUnlockMoodles),
            CanRemoveMoodles = perms.HasFlag(InteractionPerms.CanRemoveMoodles),
        };
    }

    /// <summary>
    ///     Converts a <see cref="PermissionsUiState"/> to <see cref="UserPermissions"/>
    /// </summary>
    public static UserPermissions To(PermissionsUiState permissions)
    {
        var perms = InteractionPerms.None;

        // Gag permissions
        if (permissions.CanApplyGag) perms |= InteractionPerms.CanApplyGag;
        if (permissions.CanLockGag) perms |= InteractionPerms.CanLockGag;
        if (permissions.CanUnlockGag) perms |= InteractionPerms.CanUnlockGag;
        if (permissions.CanRemoveGag) perms |= InteractionPerms.CanRemoveGag;
        if (permissions.CanForceEnableGagGlamour) perms |= InteractionPerms.CanForceEnableGlamourGag;
        if (permissions.CanEnableGarbler) perms |= InteractionPerms.CanEnableGarbler;
        if (permissions.CanLockGarbler) perms |= InteractionPerms.CanLockGarbler;
        if (permissions.CanSetGarlblerChannels) perms |= InteractionPerms.CanSetGarlblerChannels;
        if (permissions.CanLockGarlbleChannels) perms |= InteractionPerms.CanLockGarlbleChannels;

        // Wardrobe permissions
        if (permissions.CanApplyWardrobe) perms |= InteractionPerms.CanApplyWardrobe;
        if (permissions.CanLockWardrobe) perms |= InteractionPerms.CanLockWardrobe;
        if (permissions.CanUnlockWardrobe) perms |= InteractionPerms.CanUnlockWardrobe;
        if (permissions.CanRemoveWardrobe) perms |= InteractionPerms.CanRemoveWardrobe;
        if (permissions.CanForceEnableWardrobeGlamour) perms |= InteractionPerms.CanForceEnableGlamour;

        // Moodle permissions
        if (permissions.CanApplyOwn) perms |= InteractionPerms.CanApplyOwnMoodles;
        if (permissions.CanApplyPairs) perms |= InteractionPerms.CanApplyPairsMoodles;
        if (permissions.CanLockMoodles) perms |= InteractionPerms.CanLockMoodles;
        if (permissions.CanUnlockMoodles) perms |= InteractionPerms.CanUnlockMoodles;
        if (permissions.CanRemoveMoodles) perms |= InteractionPerms.CanRemoveMoodles;

        return new UserPermissions(permissions.Temporary, permissions.Priority, perms);
    }

}
