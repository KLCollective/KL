using KinkLinkCommon.Domain;
using KinkLinkCommon.Domain.Enums;
using KinkLinkCommon.Domain.Enums.Permissions;
using KinkLinkCommon.Domain.Network;
using KinkLinkCommon.Domain.Network.PairInteractions;
using KinkLinkServer.Domain.Interfaces;
using KinkLinkServer.Services;

namespace KinkLinkServer.SignalR.Handlers.Interactions;

public class LockWardrobeInteractionHandler(
    LockService lockService,
    LocksHandler locksHandler,
    KinkLinkProfilesService profilesService,
    ILogger<LockWardrobeInteractionHandler> logger
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

    public override PairAction ActionType => PairAction.LockWardrobe;

    public override Task<ActionResult<Unit>> HandleAsync(
        InteractionContext context,
        InteractionPayload? payload
    )
    {
        return HandleLockAsync(context, payload);
    }

    private async Task<ActionResult<Unit>> HandleLockAsync(
        InteractionContext context,
        InteractionPayload? payload
    )
    {
        _logger.LogInformation(
            "[LockWardrobeInteractionHandler] Handling lock from {Sender} to {Target}",
            context.SenderFriendCode,
            context.TargetFriendCode
        );

        var targetProfileId = await GetTargetProfileIdAsync(context.TargetFriendCode);
        if (targetProfileId == null)
        {
            _logger.LogWarning(
                "[LockWardrobeInteractionHandler] Target profile not found: {Target}",
                context.TargetFriendCode
            );
            return ActionResultBuilder.Fail<Unit>(ActionResultEc.TargetNotFriends);
        }

        var senderProfileId = await GetTargetProfileIdAsync(context.SenderFriendCode);
        if (senderProfileId == null)
        {
            _logger.LogError(
                "[LockWardrobeInteractionHandler] Sender profile not found: {Sender}",
                context.SenderFriendCode
            );
            return ActionResultBuilder.Fail<Unit>(ActionResultEc.Unknown);
        }

        var lockIdsToProcess = GetLockIdsToProcess(payload);

        var successCount = 0;
        var failCount = 0;

        foreach (var lockId in lockIdsToProcess)
        {
            var lockInfo = new LockInfoDto
            {
                LockID = lockId,
                LockeeID = targetProfileId.Value,
                LockerID = senderProfileId.Value,
                LockPriority = context.Permissions.PermissionsGrantedTo.Priority,
                CanSelfUnlock = false,
            };
            var result = await lockService.AddOrUpdateLockAsync(lockInfo);
            if (result != null)
            {
                successCount++;
            }
            else
            {
                failCount++;
            }
        }

        _logger.LogInformation(
            "[LockWardrobeInteractionHandler] Lock completed: {Success} succeeded, {Fail} failed for {Target}",
            successCount,
            failCount,
            context.TargetFriendCode
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
