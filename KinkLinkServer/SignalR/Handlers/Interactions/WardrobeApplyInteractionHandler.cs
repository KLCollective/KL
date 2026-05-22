using KinkLinkCommon.Dependencies.Glamourer.Components;
using KinkLinkCommon.Domain;
using KinkLinkCommon.Domain.Enums;
using KinkLinkCommon.Domain.Enums.Permissions;
using KinkLinkCommon.Domain.Network;
using KinkLinkCommon.Domain.Network.PairInteractions;
using KinkLinkCommon.Domain.Wardrobe;
using KinkLinkServer.Domain.Interfaces;
using KinkLinkServer.Services;

namespace KinkLinkServer.SignalR.Handlers.Interactions;

public class WardrobeApplyInteractionHandler(
    LocksHandler locksHandler,
    KinkLinkProfilesService profilesService,
    WardrobeDataService wardrobeDataService,
    ILogger<WardrobeApplyInteractionHandler> logger
) : BasePairInteractionHandler(locksHandler, profilesService, logger)
{
    public override PairAction ActionType => PairAction.ApplyWardrobe;

    public override async Task<ActionResult<Unit>> HandleAsync(
        InteractionContext context,
        InteractionPayload? payload
    )
    {
        if (payload?.WardrobeItems == null || payload.WardrobeItems.Count == 0)
        {
            _logger.LogWarning("[WardrobeApplyInteractionHandler] No wardrobe items in payload");
            return ActionResultBuilder.Fail<Unit>(ActionResultEc.ClientBadData);
        }

        _logger.LogInformation(
            "[WardrobeApplyInteractionHandler] Handling wardrobe apply from {Sender} to {Target}, {Count} items",
            context.SenderFriendCode,
            context.TargetFriendCode
        );

        var targetProfileId = await GetTargetProfileIdAsync(context.TargetFriendCode);
        if (targetProfileId == null)
        {
            _logger.LogWarning(
                "[WardrobeApplyInteractionHandler] Target profile not found: {Target}",
                context.TargetFriendCode
            );
            return ActionResultBuilder.Fail<Unit>(ActionResultEc.TargetNotFriends);
        }

        return await HandleApplyAsync(context, payload, targetProfileId.Value);
    }

    private async Task<ActionResult<Unit>> HandleApplyAsync(
        InteractionContext context,
        InteractionPayload payload,
        int targetProfileId
    )
    {
        // TODO: Implement the logic to add/uupate wardrobe items
        // Check the lock for the slot is not blocking replacement.
        // i.e. For each wardrobe item in the list, remove the entry in the WardrobeItem. layer it is on using Update
        //UpdateWardrobeState
        return ActionResultBuilder.Fail<Unit>(ActionResultEc.NotImplemented);
    }
}
