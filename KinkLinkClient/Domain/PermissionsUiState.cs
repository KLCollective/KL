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
    public PermissionsModes Mode= PermissionsModes.None;

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
        return new PermissionsUiState
        {
            Temporary = permissions.Expires != null,
            Priority = permissions.Priority,
            
            // Gag permissions
            CanApplyGag = (permissions.Gags & GagPermissions.CanApply) != 0,
            CanLockGag = (permissions.Gags & GagPermissions.CanLock) != 0,
            CanUnlockGag = (permissions.Gags & GagPermissions.CanUnlock) != 0,
            CanRemoveGag = (permissions.Gags & GagPermissions.CanRemove) != 0,
            CanForceEnableGagGlamour = (permissions.Gags & GagPermissions.CanForceEnableGlamour) != 0,
            CanEnableGarbler = (permissions.Gags & GagPermissions.CanEnableGarbler) != 0,
            CanLockGarbler = (permissions.Gags & GagPermissions.CanLockGarbler) != 0,
            CanSetGarlblerChannels = (permissions.Gags & GagPermissions.CanSetGarlblerChannels) != 0,
            CanLockGarlbleChannels = (permissions.Gags & GagPermissions.CanLockGarlbleChannels) != 0,
            
            // Wardrobe permissions
            CanApplyWardrobe = (permissions.Wardrobe & WardrobePermissions.CanApply) != 0,
            CanLockWardrobe = (permissions.Wardrobe & WardrobePermissions.CanLock) != 0,
            CanUnlockWardrobe = (permissions.Wardrobe & WardrobePermissions.CanUnlock) != 0,
            CanRemoveWardrobe = (permissions.Wardrobe & WardrobePermissions.CanRemove) != 0,
            CanForceEnableWardrobeGlamour = (permissions.Wardrobe & WardrobePermissions.CanForceEnableGlamour) != 0,
            
            // Moodle permissions
            CanApplyOwn = (permissions.Moodles & MoodlesPermissions.CanApplyOwn) != 0,
            CanApplyPairs = (permissions.Moodles & MoodlesPermissions.CanApplyPairs) != 0,
            CanLockMoodles = (permissions.Moodles & MoodlesPermissions.CanLock) != 0,
            CanUnlockMoodles = (permissions.Moodles & MoodlesPermissions.CanUnlock) != 0,
            CanRemoveMoodles = (permissions.Moodles & MoodlesPermissions.CanRemove) != 0,
        };
    }

    /// <summary>
    ///     Converts a <see cref="PermissionsUiState"/> to <see cref="UserPermissions"/>
    /// </summary>
    public static UserPermissions To(PermissionsUiState permissions)
    {
        // Initialization
        var gagPerms = GagPermissions.None;
        var wardrobePerms = WardrobePermissions.None;
        var moodlePerms = MoodlesPermissions.None;
        
        // Gag permissions
        if(permissions.CanApplyGag) gagPerms |= GagPermissions.CanApply;
        if(permissions.CanLockGag) gagPerms |= GagPermissions.CanLock;
        if(permissions.CanUnlockGag) gagPerms |= GagPermissions.CanUnlock;
        if(permissions.CanRemoveGag) gagPerms |= GagPermissions.CanRemove;
        if(permissions.CanForceEnableGagGlamour) gagPerms |= GagPermissions.CanForceEnableGlamour;
        if(permissions.CanEnableGarbler) gagPerms |= GagPermissions.CanEnableGarbler;
        if(permissions.CanLockGarbler) gagPerms |= GagPermissions.CanLockGarbler;
        if(permissions.CanSetGarlblerChannels) gagPerms |= GagPermissions.CanSetGarlblerChannels;
        if(permissions.CanLockGarlbleChannels) gagPerms |= GagPermissions.CanLockGarlbleChannels;

        // Wardrobe permissions
        if (permissions.CanApplyWardrobe) wardrobePerms |= WardrobePermissions.CanApply;
        if (permissions.CanLockWardrobe) wardrobePerms |= WardrobePermissions.CanLock;
        if (permissions.CanUnlockWardrobe) wardrobePerms |= WardrobePermissions.CanUnlock;
        if (permissions.CanRemoveWardrobe) wardrobePerms |= WardrobePermissions.CanRemove;
        if (permissions.CanForceEnableWardrobeGlamour) wardrobePerms |= WardrobePermissions.CanForceEnableGlamour;
        
        // Moodle permissions
        if (permissions.CanApplyOwn) moodlePerms |= MoodlesPermissions.CanApplyOwn;
        if (permissions.CanApplyPairs) moodlePerms |= MoodlesPermissions.CanApplyPairs;
        if (permissions.CanLockMoodles) moodlePerms |= MoodlesPermissions.CanLock;
        if (permissions.CanUnlockMoodles) moodlePerms |= MoodlesPermissions.CanUnlock;
        if (permissions.CanRemoveMoodles) moodlePerms |= MoodlesPermissions.CanRemove;

        return new UserPermissions(permissions.Temporary, permissions.Priority, gagPerms, wardrobePerms, moodlePerms);
    }

}
