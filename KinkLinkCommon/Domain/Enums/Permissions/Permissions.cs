namespace KinkLinkCommon.Domain.Enums;

/// <summary>
/// Indicate when permissions should be overriden temporarily such as for Permission overrides
/// </summary>
[Flags]
public enum PermissionsModes
{
    None,
    // Indicates that the permissions should be disabled for all but the temporarily white listed pairs.
    Busy,
    // Permissions and interactions remain active, but features will be disabled temporarily.
    SafeForWork,
    // Indicates that the safeword has been used and all permissions are suspended with all features disabled.
    Safeword,
}

/// These permissions are related to the gag and garbler settings in the gags menu
[Flags]
public enum GagPermissions
{
    None = 0,
    CanApply = 1 << 0,
    CanLock = 1 << 1,
    CanUnlock = 1 << 2,
    CanRemove = 1 << 3,
    CanForceEnableGlamour = 1 << 4,
    CanEnableGarbler = 1 << 5,
    CanLockGarbler = 1 << 6,
    CanSetGarlblerChannels = 1 << 7,
    CanLockGarlbleChannels = 1 << 8,
}

/// These permissions are specifically related to the wardrobe section of the interaction menu
[Flags]
public enum WardrobePermissions
{
    None = 0,
    CanApply = 1 << 0,
    CanLock = 1 << 1,
    CanUnlock = 1 << 2,
    CanRemove = 1 << 3,
    CanForceEnableGlamour = 1 << 4,
}

/// These permissions influence all moodles permissions in the interaction menu
[Flags]
public enum MoodlesPermissions
{
    None = 1 << 0,
    // User allows pair to apply users moodles
    CanApplyOwn = 1 << 1,
    // User allows pair to apply the pairs moodles
    CanApplyPairs = 1 << 2,
    CanLock = 1 << 3,
    CanUnlock = 1 << 4,
    CanRemove = 1 << 5,
}

/// <summary>
///     Acts as a priority for whether locks can be overwritten or not.
/// </summary>
public enum RelationshipPriority
{
    Casual = 0,
    Serious = 1,
    Devotional = 2,
}
