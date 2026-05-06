using KinkLinkCommon.Domain;
using KinkLinkCommon.Domain.Enums;
using KinkLinkCommon.Domain.Enums.Permissions;
using KinkLinkCommon.Domain.Network;
using KinkLinkCommon.Domain.Network.PairInteractions;
using KinkLinkServer.Domain.Interfaces;
using KinkLinkServer.Services;
using Microsoft.AspNetCore.SignalR;

namespace KinkLinkServer.SignalR.Handlers.Interactions;

public class UnlockWardrobeInteractionHandler(
    LocksHandler locksHandler,
    KinkLinkProfilesService profilesService,
    ILogger<UnlockWardrobeInteractionHandler> logger
) : BasePairInteractionHandler(locksHandler, profilesService, logger)
{
    private static readonly Dictionary<string, string> SlotToLockIdMap = new()
    {
        ["set"] = "wardrobe-baseset",
        ["Head"] = "wardrobe-head",
        ["Body"] = "wardrobe-body",
        ["Hands"] = "wardrobe-hands",
        ["Legs"] = "wardrobe-legs",
        ["Feet"] = "wardrobe-feet",
        ["Ears"] = "wardrobe-ears",
        ["Neck"] = "wardrobe-neck",
        ["Wrists"] = "wardrobe-wrists",
        ["RFinger"] = "wardrobe-rfinger",
        ["LFinger"] = "wardrobe-lfinger",
    };

    private static readonly List<string> AllLockIds = [.. SlotToLockIdMap.Values];

    public override PairAction ActionType => PairAction.UnlockWardrobe;

    public override Task<ActionResult<Unit>> HandleAsync(
        InteractionContext context,
        InteractionPayload? payload
    )
    {
        return HandleUnlockAsync(context, payload);
    }

    public async Task<ActionResult<Unit>> HandleUnlockAsync(
        InteractionContext context,
        InteractionPayload? payload
    )
    {
        _logger.LogInformation(
            "[UnlockWardrobeInteractionHandler] Handling unlock from {Sender} to {Target}",
            context.SenderFriendCode,
            context.TargetFriendCode
        );

        var lockIdsToProcess = GetLockIdsToProcess(payload);

        var successCount = 0;
        var failCount = 0;

        foreach (var lockId in lockIdsToProcess)
        {
            var result = await locksHandler.HandleRemoveLockAsync(
                context.SenderFriendCode,
                lockId,
                context.TargetFriendCode,
                // TODO: When passwords are implemented plumb it here
                null
            );

            if (result.Result.Result == ActionResultEc.Success)
            {
                successCount++;
            }
            else
            {
                failCount++;
            }
        }

        _logger.LogInformation(
            "[UnlockWardrobeInteractionHandler] Unlock completed: {Success} succeeded, {Fail} failed",
            successCount,
            failCount
        );

        return ActionResultBuilder.Ok(Unit.Empty);
    }

    private static List<string> GetLockIdsToProcess(InteractionPayload? payload)
    {
        if (payload?.WardrobeItems != null && payload.WardrobeItems.Count > 0)
        {
            var lockIds = payload
                .WardrobeItems.Where(item => item.Type == "set" || item.Type == "item")
                .Select(item =>
                {
                    var slotKey = item.Type == "set" ? "set" : item.Slot.ToString();
                    return SlotToLockIdMap.TryGetValue(slotKey, out var lockId) ? lockId : null;
                })
                .Where(lockId => lockId != null)
                .Cast<string>()
                .ToList();

            return lockIds.Count > 0 ? lockIds : AllLockIds;
        }

        return AllLockIds;
    }
}
