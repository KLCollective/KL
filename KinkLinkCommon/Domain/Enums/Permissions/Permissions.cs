namespace KinkLinkCommon.Domain.Enums;

// Submission flags level is used for the more... sticky and difficult to get out of systems.
[Flags]
public enum SubmissionLevel
{
    // Normal
    Normal = 0,

    // No control over permissions, safeword still works
    Slave = 1 << 0,

    // No control over configuration/profiles/etc
    TotalSubmission = 1 << 1,

    // Removal of safeword from ingame. Only available when logged out.
    EternalBondange = 1 << 2,
}

//
/// <summary>
/// Indicate when permissions should be overriden temporarily such as for Permission overrides
/// </summary>
[Flags]
public enum PermissionsModes
{
    None,

    // Permissions and interactions remain active, but features will be disabled temporarily.
    // (Intended for content or syncing at non-kink venues/meetups without interrupting your dynamics)
    SafeForWork,

    // Indicates that the permissions should be disabled for all but the temporarily white listed pairs.
    Busy,

    // Indicates that the safeword has been used and all permissions are suspended with all features disabled.
    Safeword,
}

/// These permissions are related to the gag and garbler settings in the gags menu
[Flags]
public enum InteractionPerms
{
    None = 0,
    CanApplyGag = 1 << 0,
    CanLockGag = 1 << 1,
    CanUnlockGag = 1 << 2,
    CanRemoveGag = 1 << 3,
    CanForceEnableGlamourGag = 1 << 4,
    CanEnableGarbler = 1 << 5,
    CanLockGarbler = 1 << 6,
    CanSetGarlblerChannels = 1 << 7,
    CanLockGarlbleChannels = 1 << 8,

    CanApplyWardrobe = 1 << 9,
    CanLockWardrobe = 1 << 10,
    CanUnlockWardrobe = 1 << 11,
    CanRemoveWardrobe = 1 << 12,
    CanForceEnableGlamour = 1 << 13,

    // User allows pair to apply users moodles
    CanApplyOwnMoodles = 1 << 14,

    // User allows pair to apply the pairs moodles
    CanApplyPairsMoodles = 1 << 15,
    CanLockMoodles = 1 << 16,
    CanUnlockMoodles = 1 << 17,
    CanRemoveMoodles = 1 << 18,
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
