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
            _logger.LogWarning(
                "[RemoveWardrobeInteractionHandler] No wardrobe items in payload"
            );
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

        try
        {
            return await wardrobeDataService.WithWardrobeTransactionAsync(
                targetProfileId.Value,
                async sql =>
                {
                    var row = await sql.GetWardrobeStateAsync(
                        new WardrobeSql.GetWardrobeStateArgs(targetProfileId.Value)
                    );

                    if (row == null)
                    {
                        _logger.LogInformation(
                            "[RemoveWardrobeInteractionHandler] No active wardrobe state for {Target}, nothing to remove",
                            context.TargetFriendCode
                        );
                        return ActionResultBuilder.Ok(Unit.Empty);
                    }

                    var glamourerset = row.Value.Glamourerset;
                    var head = row.Value.Head;
                    var body = row.Value.Body;
                    var hand = row.Value.Hand;
                    var legs = row.Value.Legs;
                    var feet = row.Value.Feet;
                    var earring = row.Value.Earring;
                    var neck = row.Value.Neck;
                    var bracelet = row.Value.Bracelet;
                    var lring = row.Value.Lring;
                    var rring = row.Value.Rring;
                    var moditems = row.Value.Moditems;

                    foreach (var item in payload.WardrobeItems)
                    {
                        switch (item.Type)
                        {
                            case "set":
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
                                glamourerset = null;
                                break;

                            case "item":
                                var slotKey = item.Slot.ToString();
                                var lockId = $"wardrobe-{slotKey.ToLowerInvariant()}";
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
                                        slotKey,
                                        context.TargetFriendCode
                                    );
                                    return ActionResultBuilder.Fail<Unit>(canRemoveItem.Result);
                                }

                                head = slotKey == "Head" ? null : head;
                                body = slotKey == "Body" ? null : body;
                                hand = slotKey == "Hands" ? null : hand;
                                legs = slotKey == "Legs" ? null : legs;
                                feet = slotKey == "Feet" ? null : feet;
                                earring = slotKey == "Ears" ? null : earring;
                                neck = slotKey == "Neck" ? null : neck;
                                bracelet = slotKey == "Wrists" ? null : bracelet;
                                lring = slotKey == "LFinger" ? null : lring;
                                rring = slotKey == "RFinger" ? null : rring;
                                break;

                            case "moditem":
                                moditems = null;
                                break;
                        }
                    }

                    var allNull = glamourerset == null
                        && head == null
                        && body == null
                        && hand == null
                        && legs == null
                        && feet == null
                        && earring == null
                        && neck == null
                        && bracelet == null
                        && lring == null
                        && rring == null
                        && moditems == null;

                    if (allNull)
                    {
                        await sql.ClearWardrobeStateAsync(
                            new WardrobeSql.ClearWardrobeStateArgs(targetProfileId.Value)
                        );
                    }
                    else
                    {
                        await sql.UpdateWardrobeStateAsync(new(
                            targetProfileId.Value,
                            glamourerset,
                            head, body, hand, legs, feet,
                            earring, neck, bracelet, lring, rring,
                            moditems
                        ));
                    }

                    _logger.LogInformation(
                        "[RemoveWardrobeInteractionHandler] Successfully removed wardrobe items for {Target}",
                        context.TargetFriendCode
                    );
                    return ActionResultBuilder.Ok(Unit.Empty);
                }
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "[RemoveWardrobeInteractionHandler] Failed to remove wardrobe for {Target}",
                context.TargetFriendCode
            );
            return ActionResultBuilder.Fail<Unit>(ActionResultEc.Unknown);
        }
    }
}