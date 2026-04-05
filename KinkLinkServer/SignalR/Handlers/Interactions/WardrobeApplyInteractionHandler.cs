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
            _logger.LogWarning(
                "[WardrobeApplyInteractionHandler] No wardrobe items in payload"
            );
            return ActionResultBuilder.Fail<Unit>(ActionResultEc.ClientBadData);
        }

        _logger.LogInformation(
            "[WardrobeApplyInteractionHandler] Handling wardrobe apply from {Sender} to {Target}, {Count} items",
            context.SenderFriendCode,
            context.TargetFriendCode,
            payload.WardrobeItems.Count
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

        var currentState = await wardrobeDataService.GetWardrobeStateAsync(targetProfileId.Value);
        var equipment = currentState?.Equipment ?? new Dictionary<string, WardrobeItemData>();
        var modSettings = currentState?.ModSettings ?? new Dictionary<string, WardrobeItemData>();
        string? baseLayerBase64 = currentState?.BaseLayerBase64;

        var allWardrobeItems = await wardrobeDataService.GetAllWardrobeItemsAsync(targetProfileId.Value);
        var setItems = allWardrobeItems.Where(i => i.Type == "set").ToDictionary(i => i.Id);
        var itemItems = allWardrobeItems.Where(i => i.Type == "item").ToDictionary(i => i.Id);
        var modItems = allWardrobeItems.Where(i => i.Type == "moditem").ToDictionary(i => i.Id);

        foreach (var item in payload.WardrobeItems)
        {
            if (item.DataBase64 == null)
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
                                "[WardrobeApplyInteractionHandler] Sender {Sender} cannot modify baseset lock for {Target}",
                                context.SenderFriendCode,
                                context.TargetFriendCode
                            );
                            return ActionResultBuilder.Fail<Unit>(canRemoveSet.Result);
                        }
                        baseLayerBase64 = null;
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
                                "[WardrobeApplyInteractionHandler] Sender {Sender} cannot modify lock {LockId} for {Target}",
                                context.SenderFriendCode,
                                lockId,
                                context.TargetFriendCode
                            );
                            return ActionResultBuilder.Fail<Unit>(canRemoveItem.Result);
                        }
                        equipment.Remove(slotKey);
                        break;
                    case "moditem":
                        modSettings.Remove(item.Name);
                        break;
                }
                continue;
            }

            switch (item.Type)
            {
                case "set":
                    var canApplySet = await _locksHandler.CheckCanModifySlotAsync(
                        context.SenderFriendCode,
                        context.TargetFriendCode,
                        "wardrobe-baseset"
                    );
                    if (canApplySet.Result != ActionResultEc.Success)
                    {
                        _logger.LogWarning(
                            "[WardrobeApplyInteractionHandler] Sender {Sender} cannot apply set to {Target}",
                            context.SenderFriendCode,
                            context.TargetFriendCode
                        );
                        return ActionResultBuilder.Fail<Unit>(canApplySet.Result);
                    }
                    if (setItems.TryGetValue(item.Id, out var setItem) && setItem.DataBase64 != null)
                    {
                        baseLayerBase64 = setItem.DataBase64;
                    }
                    else
                    {
                        _logger.LogWarning(
                            "[WardrobeApplyInteractionHandler] Set item not found or has no data for ID {Id}",
                            item.Id
                        );
                        return ActionResultBuilder.Fail<Unit>(ActionResultEc.ValueNotSet);
                    }
                    break;

                case "item":
                    var itemLockId = $"wardrobe-{item.Slot.ToString().ToLowerInvariant()}";
                    var canApplyItem = await _locksHandler.CheckCanModifySlotAsync(
                        context.SenderFriendCode,
                        context.TargetFriendCode,
                        itemLockId
                    );
                    if (canApplyItem.Result != ActionResultEc.Success)
                    {
                        _logger.LogWarning(
                            "[WardrobeApplyInteractionHandler] Sender {Sender} cannot apply item to slot {Slot} for {Target}",
                            context.SenderFriendCode,
                            item.Slot,
                            context.TargetFriendCode
                        );
                        return ActionResultBuilder.Fail<Unit>(canApplyItem.Result);
                    }
                    if (itemItems.TryGetValue(item.Id, out var wardrobeItemData) && wardrobeItemData.DataBase64 != null)
                    {
                        var wardrobeItem = DeserializeWardrobeItem(wardrobeItemData);
                        if (wardrobeItem != null)
                        {
                            equipment[item.Slot.ToString()] = wardrobeItem;
                        }
                    }
                    else
                    {
                        _logger.LogWarning(
                            "[WardrobeApplyInteractionHandler] Item not found or has no data for ID {Id}",
                            item.Id
                        );
                    }
                    break;

                case "moditem":
                    if (modItems.TryGetValue(item.Id, out var modItemData) && modItemData.DataBase64 != null)
                    {
                        var modItem = DeserializeWardrobeItem(modItemData);
                        if (modItem != null)
                        {
                            modSettings[item.Name] = modItem;
                        }
                    }
                    else
                    {
                        _logger.LogWarning(
                            "[WardrobeApplyInteractionHandler] ModItem not found or has no data for ID {Id}",
                            item.Id
                        );
                    }
                    break;
            }
        }

        var newState = new WardrobeStateDto(baseLayerBase64, equipment, modSettings);
        var success = await wardrobeDataService.UpdateWardrobeStateAsync(
            targetProfileId.Value,
            newState
        );

        if (!success)
        {
            _logger.LogError(
                "[WardrobeApplyInteractionHandler] Failed to apply wardrobe for {Target}",
                context.TargetFriendCode
            );
            return ActionResultBuilder.Fail<Unit>(ActionResultEc.Unknown);
        }

        _logger.LogInformation(
            "[WardrobeApplyInteractionHandler] Successfully applied wardrobe for {Target}",
            context.TargetFriendCode
        );
        return ActionResultBuilder.Ok(Unit.Empty);
    }

    private WardrobeItemData? DeserializeWardrobeItem(WardrobeDto dto)
    {
        try
        {
            if (dto.DataBase64 == null)
                return null;

            var item = System.Text.Json.JsonSerializer.Deserialize<GlamourerItem>(
                Convert.FromBase64String(dto.DataBase64),
                new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true }
            );

            if (item == null)
                return null;

            return new WardrobeItemData(
                dto.Id,
                dto.Name,
                dto.Description,
                dto.Slot,
                item,
                [],
                [],
                dto.Priority
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "[WardrobeApplyInteractionHandler] Failed to deserialize wardrobe item {Name}",
                dto.Name
            );
            return null;
        }
    }
}
