using KinkLinkCommon.Domain;
using KinkLinkCommon.Domain.Enums;
using KinkLinkCommon.Domain.Enums.Permissions;
using KinkLinkCommon.Domain.Network;
using KinkLinkCommon.Domain.Network.PairInteractions;
using KinkLinkCommon.Domain.Wardrobe;
using KinkLinkServer.Domain.Interfaces;
using KinkLinkServer.Services;

namespace KinkLinkServer.SignalR.Handlers.Interactions;

public class RemoveWardrobeInteractionHandler(
    LocksHandler locksHandler,
    KinkLinkProfilesService profilesService,
    WardrobeDataService wardrobeDataService,
    ILogger<RemoveWardrobeInteractionHandler> logger
) : BasePairInteractionHandler(locksHandler, profilesService, logger)
{
    public override PairAction ActionType => PairAction.RemoveWardrobe;

    public override async Task<ActionResult<Unit>> HandleAsync(
        InteractionContext context,
        InteractionPayload? payload
    )
    {
        _logger.LogInformation(
            "[RemoveWardrobeInteractionHandler] Removing wardrobe from {Sender} to {Target}",
            context.SenderFriendCode,
            context.TargetFriendCode
        );

        var targetProfileId = await GetTargetProfileIdAsync(context.TargetFriendCode);
        if (targetProfileId == null)
        {
            _logger.LogWarning(
                "[RemoveWardrobeInteractionHandler] Target profile not found: {Target}",
                context.TargetFriendCode
            );
            return ActionResultBuilder.Fail<Unit>(ActionResultEc.TargetNotFriends);
        }

        var currentState = await wardrobeDataService.GetWardrobeStateAsync(targetProfileId.Value);
        if (currentState == null)
        {
            _logger.LogInformation(
                "[RemoveWardrobeInteractionHandler] No active wardrobe state for {Target}, nothing to remove",
                context.TargetFriendCode
            );
            return ActionResultBuilder.Ok(Unit.Empty);
        }

        if (currentState.BaseLayerBase64 != null)
        {
            var canRemoveSet = await _locksHandler.CheckCanModifySlotAsync(
                context.SenderFriendCode,
                context.TargetFriendCode,
                "wardrobe-baseset"
            );
            if (canRemoveSet.Result != ActionResultEc.Success)
            {
                _logger.LogWarning(
                    "[RemoveWardrobeInteractionHandler] Sender {Sender} cannot remove baseset for {Target}",
                    context.SenderFriendCode,
                    context.TargetFriendCode
                );
                return ActionResultBuilder.Fail<Unit>(canRemoveSet.Result);
            }
        }

        if (currentState.Equipment != null)
        {
            foreach (var (slotName, _) in currentState.Equipment)
            {
                var lockId = $"wardrobe-{slotName.ToLowerInvariant()}";
                var canRemoveItem = await _locksHandler.CheckCanModifySlotAsync(
                    context.SenderFriendCode,
                    context.TargetFriendCode,
                    lockId
                );
                if (canRemoveItem.Result != ActionResultEc.Success)
                {
                    _logger.LogWarning(
                        "[RemoveWardrobeInteractionHandler] Sender {Sender} cannot remove slot {Slot} for {Target}",
                        context.SenderFriendCode,
                        slotName,
                        context.TargetFriendCode
                    );
                    return ActionResultBuilder.Fail<Unit>(canRemoveItem.Result);
                }
            }
        }

        var emptyState = new WardrobeStateDto(null, null, null);
        var success = await wardrobeDataService.UpdateWardrobeStateAsync(
            targetProfileId.Value,
            emptyState
        );

        if (!success)
        {
            _logger.LogError(
                "[RemoveWardrobeInteractionHandler] Failed to clear wardrobe for {Target}",
                context.TargetFriendCode
            );
            return ActionResultBuilder.Fail<Unit>(ActionResultEc.Unknown);
        }

        _logger.LogInformation(
            "[RemoveWardrobeInteractionHandler] Successfully removed wardrobe for {Target}",
            context.TargetFriendCode
        );
        return ActionResultBuilder.Ok(Unit.Empty);
    }
}
