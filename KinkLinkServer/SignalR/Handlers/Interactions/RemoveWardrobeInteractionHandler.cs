using System.Text.Json;
using KinkLinkCommon.Database;
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
        if (payload?.WardrobeItems == null || payload.WardrobeItems.Count == 0)
        {
            _logger.LogWarning("[RemoveWardrobeInteractionHandler] No wardrobe items in payload");
            return ActionResultBuilder.Fail<Unit>(ActionResultEc.ClientBadData);
        }

        _logger.LogInformation(
            "[RemoveWardrobeInteractionHandler] Removing {Count} items from {Sender} to {Target}",
            payload.WardrobeItems.Count,
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
        return await RemoveWardrobeAsync(
            context.TargetFriendCode,
            targetProfileId.Value,
            payload.WardrobeItems
        );
    }

    private async Task<ActionResult<Unit>> RemoveWardrobeAsync(
        string targetFriendCode,
        int targetProfileId,
        List<WardrobeItem> wardrobeItems
    )
    {
        // TODO: Implement the logic to remove wardrobe items
        // i.e. For each wardrobe item in the list, remove the entry in the WardrobeItem. layer it is on using ClearWardrobeLayer
        return ActionResultBuilder.Ok(Unit.Empty);
    }
}
