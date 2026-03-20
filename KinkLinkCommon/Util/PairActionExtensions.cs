using KinkLinkCommon.Domain.Enums;
using KinkLinkCommon.Domain.Enums.Permissions;

namespace KinkLinkCommon.Util;

public static class PairActionExtensions
{
    public static InteractionPerms ToInteractionPerm(this PairAction action)
    {
        return action switch
        {
            PairAction.ApplyGag => InteractionPerms.CanApplyGag,
            PairAction.LockGag => InteractionPerms.CanLockGag,
            PairAction.UnlockGag => InteractionPerms.CanUnlockGag,
            PairAction.RemoveGag => InteractionPerms.CanRemoveGag,
            PairAction.EnableGarbler => InteractionPerms.CanEnableGarbler,
            PairAction.LockGarbler => InteractionPerms.CanLockGarbler,
            PairAction.SetGarblerChannels => InteractionPerms.CanSetGarblerChannels,
            PairAction.LockGarblerChannels => InteractionPerms.CanLockGarblerChannels,
            PairAction.ApplyWardrobe => InteractionPerms.CanApplyWardrobe,
            PairAction.LockWardrobe => InteractionPerms.CanLockWardrobe,
            PairAction.UnlockWardrobe => InteractionPerms.CanUnlockWardrobe,
            PairAction.RemoveWardrobe => InteractionPerms.CanRemoveWardrobe,
            PairAction.ApplyOwnMoodle => InteractionPerms.CanApplyOwnMoodles,
            PairAction.ApplyPairsMoodle => InteractionPerms.CanApplyPairsMoodles,
            PairAction.LockMoodle => InteractionPerms.CanLockMoodles,
            PairAction.UnlockMoodle => InteractionPerms.CanUnlockMoodles,
            PairAction.RemoveMoodle => InteractionPerms.CanRemoveMoodles,
            _ => InteractionPerms.None,
        };
    }

    public static InteractionPerms GetViewPermission(this PairAction action)
    {
        return action switch
        {
            PairAction.ApplyGag or PairAction.LockGag or PairAction.UnlockGag or PairAction.RemoveGag
                or PairAction.EnableGarbler or PairAction.LockGarbler or PairAction.SetGarblerChannels or PairAction.LockGarblerChannels
                => InteractionPerms.CanApplyGag,
            PairAction.ApplyWardrobe or PairAction.LockWardrobe or PairAction.UnlockWardrobe or PairAction.RemoveWardrobe
                => InteractionPerms.CanApplyWardrobe,
            PairAction.ApplyOwnMoodle or PairAction.ApplyPairsMoodle or PairAction.LockMoodle or PairAction.UnlockMoodle or PairAction.RemoveMoodle
                => InteractionPerms.CanApplyOwnMoodles,
            _ => InteractionPerms.None,
        };
    }
}
