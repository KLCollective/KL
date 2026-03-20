using KinkLinkCommon.Dependencies.Glamourer;
using KinkLinkCommon.Dependencies.Glamourer.Components;
using KinkLinkCommon.Domain;
using KinkLinkCommon.Domain.CharacterState;
using KinkLinkCommon.Domain.Enums;
using KinkLinkCommon.Domain.Enums.Permissions;
using KinkLinkCommon.Domain.Network;
using KinkLinkCommon.Domain.Network.PairInteractions;
using KinkLinkCommon.Domain.Wardrobe;
using KinkLinkCommon.Util;
using KinkLinkServer.Domain;
using KinkLinkServer.Domain.Interfaces;
using KinkLinkServer.Managers;
using KinkLinkServer.Services;
using Microsoft.AspNetCore.SignalR;

namespace KinkLinkServer.SignalR.Handlers;

public class PairInteractionsHandler(
    PermissionsService permissionsService,
    CharacterStateService characterStateService,
    WardrobeDataService wardrobeDataService,
    KinkLinkProfilesService profilesService,
    IPresenceService presenceService,
    IForwardedRequestManager forwardedRequestManager,
    ILogger<PairInteractionsHandler> logger
)
{
    public void PushMyState(string friendCode, CharacterStateDto state)
    {
        characterStateService.UpdateState(friendCode, state);
        logger.LogDebug("Pushed state for {FriendCode}", friendCode);
    }

    public async Task<ActionResult<QueryPairStateResponse>> QueryPairState(
        string senderFriendCode,
        QueryPairStateRequest request
    )
    {
        if (string.IsNullOrWhiteSpace(request.TargetFriendCode))
        {
            return ActionResultBuilder.Fail<QueryPairStateResponse>(ActionResultEc.ClientBadData);
        }

        if (senderFriendCode == request.TargetFriendCode)
        {
            return ActionResultBuilder.Fail<QueryPairStateResponse>(ActionResultEc.ClientBadData);
        }

        var permissions = await permissionsService.GetPermissions(
            senderFriendCode,
            request.TargetFriendCode
        );
        if (permissions == null)
        {
            return ActionResultBuilder.Fail<QueryPairStateResponse>(
                ActionResultEc.TargetNotFriends
            );
        }

        var grantedBy = permissions.PermissionsGrantedBy;
        if (grantedBy == null)
        {
            return ActionResultBuilder.Fail<QueryPairStateResponse>(
                ActionResultEc.TargetHasNotGrantedSenderPermissions
            );
        }

        var cachedState = characterStateService.GetState(request.TargetFriendCode);
        var hasGag = characterStateService.HasGagPermission(grantedBy);
        var hasGarbler = characterStateService.HasGarblerPermission(grantedBy);
        var hasWardrobe = characterStateService.HasWardrobePermission(grantedBy);
        var hasMoodle = characterStateService.HasMoodlePermission(grantedBy);

        var filteredState = FilterStateByPermissions(
            cachedState,
            hasGag,
            hasGarbler,
            hasWardrobe,
            hasMoodle
        );

        if (hasWardrobe)
        {
            var targetProfileId = await profilesService.GetIdFromUidAsync(request.TargetFriendCode);
            if (targetProfileId != null)
            {
                var wardrobeState = await wardrobeDataService.GetWardrobeStateAsync(targetProfileId.Value);
                logger.LogDebug("QueryPairState: wardrobeState for {Target} is {IsNull}",
                    request.TargetFriendCode, wardrobeState == null ? "null" : "not null");
                if (wardrobeState?.Equipment != null)
                {
                    logger.LogDebug("QueryPairState: Equipment count = {Count}", wardrobeState.Equipment.Count);
                    foreach (var kvp in wardrobeState.Equipment)
                    {
                        logger.LogDebug("QueryPairState:   {Slot} = {Name}", kvp.Key, kvp.Value.Name);
                    }
                }

                if (wardrobeState != null)
                {
                    if (filteredState != null)
                    {
                        filteredState = filteredState with
                        {
                            Wardrobe = wardrobeState
                        };
                    }
                    else
                    {
                        filteredState = new CharacterStateDto(null, null, wardrobeState, null);
                    }
                }
            }
        }

        return new ActionResult<QueryPairStateResponse>(
            ActionResultEc.Success,
            new QueryPairStateResponse(
                request.TargetFriendCode,
                filteredState,
                hasGag,
                hasGarbler,
                hasWardrobe,
                hasMoodle
            )
        );
    }

    public async Task<ActionResult<Unit>> ApplyInteraction(
        string senderFriendCode,
        ApplyInteractionCommand command,
        IHubCallerClients clients
    )
    {
        logger.LogInformation(
            "[PairInteractionsHandler] ApplyInteraction: Sender={Sender}, Target={Target}, Action={Action}",
            senderFriendCode,
            command.SenderFriendCode,
            command.Action
        );

        if (command.Action == PairAction.ApplyWardrobe && command.Payload?.WardrobeItems != null)
        {
            logger.LogInformation(
                "[PairInteractionsHandler] ApplyWardrobe: {Count} items in payload",
                command.Payload.WardrobeItems.Count
            );
        }

        if (presenceService.TryGet(senderFriendCode) is not { } sender)
        {
            logger.LogWarning(
                "[PairInteractionsHandler] Sender {Sender} not online",
                senderFriendCode
            );
            return ActionResultBuilder.Fail<Unit>(ActionResultEc.TargetOffline);
        }

        var permissions = await permissionsService.GetPermissions(
            senderFriendCode,
            command.SenderFriendCode
        );
        if (permissions == null)
        {
            logger.LogWarning(
                "[PairInteractionsHandler] No permissions between {Sender} and {Target}",
                senderFriendCode,
                command.SenderFriendCode
            );
            return ActionResultBuilder.Fail<Unit>(ActionResultEc.TargetNotFriends);
        }

        var grantedBy = permissions.PermissionsGrantedBy;
        if (grantedBy == null)
        {
            logger.LogWarning(
                "[PairInteractionsHandler] Target {Target} has not granted permissions to {Sender}",
                command.SenderFriendCode,
                senderFriendCode
            );
            return ActionResultBuilder.Fail<Unit>(
                ActionResultEc.TargetHasNotGrantedSenderPermissions
            );
        }

        logger.LogInformation(
            "[PairInteractionsHandler] Permissions check: GrantedBy={Perms}, Required={Required}",
            grantedBy.Perms,
            command.Action.ToInteractionPerm()
        );

        if (!characterStateService.CanPerformAction(grantedBy, command.Action))
        {
            logger.LogWarning(
                "[PairInteractionsHandler] Action {Action} not permitted for {Sender}. Has={HasPerms}",
                command.Action,
                senderFriendCode,
                grantedBy.Perms
            );
            return ActionResultBuilder.Fail<Unit>(
                ActionResultEc.TargetHasNotGrantedSenderPermissions
            );
        }

        var targetFriendCode = command.SenderFriendCode;
        // Target is offline - handle offline wardrobe application
        logger.LogWarning(
            "[PairInteractionsHandler] Target {Target} not online, checking for offline wardrobe apply",
            targetFriendCode
        );

        if (command.Action == PairAction.ApplyWardrobe && command.Payload?.WardrobeItems != null)
        {
            return await HandleWardrobeApplication(
                senderFriendCode,
                targetFriendCode,
                command.Payload.WardrobeItems
            );
        }

        // If the target is online, send them an update.
        var target = presenceService.TryGet(targetFriendCode);
        if (target != null)
        {
            // Target is online - forward the command to them
            logger.LogInformation(
                "[PairInteractionsHandler] Target {Target} is online, forwarding",
                targetFriendCode
            );

            var response = await ForwardedRequestManager.ForwardRequestWithTimeout<Unit>(
                HubMethod.ApplyInteraction,
                clients.Client(target.ConnectionId),
                command
            );

            logger.LogInformation(
                "[PairInteractionsHandler] Forward result: {Result}",
                response.Result
            );
            return response;
        }

        return ActionResultBuilder.Ok();
    }

    private async Task<ActionResult<Unit>> HandleWardrobeApplication(
        string senderFriendCode,
        string targetFriendCode,
        List<WardrobeDto> items
    )
    {
        logger.LogInformation(
            "[PairInteractionsHandler] Handling offline wardrobe apply from {Sender} to {Target}, {Count} items",
            senderFriendCode,
            targetFriendCode,
            items.Count
        );

        var targetProfileId = await profilesService.GetIdFromUidAsync(targetFriendCode);
        if (targetProfileId == null)
        {
            logger.LogWarning(
                "[PairInteractionsHandler] Target profile not found: {Target}",
                targetFriendCode
            );
            return ActionResultBuilder.Fail<Unit>(ActionResultEc.TargetNotFriends);
        }

        // Get current wardrobe state
        var currentState = await wardrobeDataService.GetWardrobeStateAsync(targetProfileId.Value);
        var equipment = currentState?.Equipment ?? new Dictionary<string, WardrobeItemData>();
        var modSettings = currentState?.ModSettings ?? new Dictionary<string, WardrobeItemData>();
        string? baseLayerBase64 = currentState?.BaseLayerBase64;

        // Apply each item
        foreach (var item in items)
        {
            if (item.DataBase64 == null)
            {
                switch (item.Type)
                {
                    case "set":
                        baseLayerBase64 = null;
                        logger.LogInformation(
                            "[PairInteractionsHandler] Offline apply: removed base layer"
                        );
                        break;
                    case "item":
                        var slotKey = item.Slot.ToString();
                        if (equipment.Remove(slotKey))
                        {
                            logger.LogInformation(
                                "[PairInteractionsHandler] Offline apply: removed item from slot {Slot}",
                                item.Slot
                            );
                        }
                        break;
                    case "moditem":
                        if (modSettings.Remove(item.Name))
                        {
                            logger.LogInformation(
                                "[PairInteractionsHandler] Offline apply: removed moditem {Name}",
                                item.Name
                            );
                        }
                        break;
                }
                continue;
            }

            switch (item.Type)
            {
                case "set":
                    // For sets, apply the base layer
                    baseLayerBase64 = item.DataBase64;
                    logger.LogInformation(
                        "[PairInteractionsHandler] Offline apply: set {Name} applied as base layer",
                        item.Name
                    );
                    break;

                case "item":
                    var wardrobeItem = DeserializeWardrobeItem(item);
                    if (wardrobeItem != null)
                    {
                        equipment[item.Slot.ToString()] = wardrobeItem;
                        logger.LogInformation(
                            "[PairInteractionsHandler] Offline apply: added item {Name} to slot {Slot}",
                            item.Name,
                            item.Slot
                        );
                    }
                    break;

                case "moditem":
                    var modItem = DeserializeWardrobeItem(item);
                    if (modItem != null)
                    {
                        modSettings[item.Name] = modItem;
                        logger.LogInformation(
                            "[PairInteractionsHandler] Offline apply: added moditem {Name}",
                            item.Name
                        );
                    }
                    break;
            }
        }

        // Update the wardrobe state
        var newState = new WardrobeStateDto(baseLayerBase64, equipment, modSettings);
        var success = await wardrobeDataService.UpdateWardrobeStateAsync(
            targetProfileId.Value,
            newState
        );

        if (success)
        {
            logger.LogInformation(
                "[PairInteractionsHandler] Successfully applied offline wardrobe for {Target}",
                targetFriendCode
            );
            return ActionResultBuilder.Ok(Unit.Empty);
        }

        logger.LogError(
            "[PairInteractionsHandler] Failed to apply offline wardrobe for {Target}",
            targetFriendCode
        );
        return ActionResultBuilder.Fail<Unit>(ActionResultEc.Unknown);
    }

    private WardrobeItemData? DeserializeWardrobeItem(WardrobeDto dto)
    {
        try
        {
            if (dto.DataBase64 == null)
                return null;

            // The DataBase64 contains the GlamourerItem data
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
                new List<GlamourerMod>(),
                new Dictionary<string, GlamourerMaterial>(),
                dto.Priority
            );
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "[PairInteractionsHandler] Failed to deserialize wardrobe item {Name}",
                dto.Name
            );
            return null;
        }
    }

    public async Task<ActionResult<QueryPairWardrobeStateResponse>> QueryWardrobeStateAsync(
        string senderFriendCode,
        QueryPairWardrobeStateRequest request
    )
    {
        if (string.IsNullOrWhiteSpace(request.TargetFriendCode))
        {
            return ActionResultBuilder.Fail<QueryPairWardrobeStateResponse>(
                ActionResultEc.ClientBadData
            );
        }

        if (senderFriendCode == request.TargetFriendCode)
        {
            return ActionResultBuilder.Fail<QueryPairWardrobeStateResponse>(
                ActionResultEc.ClientBadData
            );
        }

        var permissions = await permissionsService.GetPermissions(
            senderFriendCode,
            request.TargetFriendCode
        );
        if (permissions == null)
        {
            return ActionResultBuilder.Fail<QueryPairWardrobeStateResponse>(
                ActionResultEc.TargetNotFriends
            );
        }

        var grantedBy = permissions.PermissionsGrantedBy;
        if (grantedBy == null)
        {
            return ActionResultBuilder.Fail<QueryPairWardrobeStateResponse>(
                ActionResultEc.TargetHasNotGrantedSenderPermissions
            );
        }

        var hasWardrobe = characterStateService.HasWardrobePermission(grantedBy);

        var targetProfileId = await profilesService.GetIdFromUidAsync(request.TargetFriendCode);
        if (targetProfileId == null)
        {
            return ActionResultBuilder.Fail<QueryPairWardrobeStateResponse>(
                ActionResultEc.TargetNotFriends
            );
        }

        var wardrobeState = await wardrobeDataService.GetWardrobeStateAsync(targetProfileId.Value);

        return new ActionResult<QueryPairWardrobeStateResponse>(
            ActionResultEc.Success,
            new QueryPairWardrobeStateResponse(
                request.TargetFriendCode,
                hasWardrobe,
                hasWardrobe ? wardrobeState : null
            )
        );
    }

    public async Task<ActionResult<QueryPairWardrobeResponse>> QueryWardrobeAsync(
        string senderFriendCode,
        QueryPairWardrobeRequest request
    )
    {
        if (string.IsNullOrWhiteSpace(request.TargetFriendCode))
        {
            return ActionResultBuilder.Fail<QueryPairWardrobeResponse>(
                ActionResultEc.ClientBadData
            );
        }

        if (senderFriendCode == request.TargetFriendCode)
        {
            return ActionResultBuilder.Fail<QueryPairWardrobeResponse>(
                ActionResultEc.ClientBadData
            );
        }

        var permissions = await permissionsService.GetPermissions(
            senderFriendCode,
            request.TargetFriendCode
        );
        if (permissions == null)
        {
            return ActionResultBuilder.Fail<QueryPairWardrobeResponse>(
                ActionResultEc.TargetNotFriends
            );
        }

        var grantedBy = permissions.PermissionsGrantedBy;
        if (grantedBy == null)
        {
            return ActionResultBuilder.Fail<QueryPairWardrobeResponse>(
                ActionResultEc.TargetHasNotGrantedSenderPermissions
            );
        }

        var hasWardrobe = characterStateService.HasWardrobePermission(grantedBy);

        var targetProfileId = await profilesService.GetIdFromUidAsync(request.TargetFriendCode);
        if (targetProfileId == null)
        {
            return ActionResultBuilder.Fail<QueryPairWardrobeResponse>(
                ActionResultEc.TargetNotFriends
            );
        }

        var allItems = await wardrobeDataService.GetAllWardrobeItemsAsync(targetProfileId.Value);

        var filteredItems = hasWardrobe
            ? allItems.Where(item => item.Priority <= grantedBy.Priority).ToList()
            : [];

        return new ActionResult<QueryPairWardrobeResponse>(
            ActionResultEc.Success,
            new QueryPairWardrobeResponse(request.TargetFriendCode, hasWardrobe, filteredItems)
        );
    }

    private static CharacterStateDto? FilterStateByPermissions(
        CharacterStateDto? state,
        bool hasGag,
        bool hasGarbler,
        bool hasWardrobe,
        bool hasMoodle
    )
    {
        if (state == null)
            return null;

        return new CharacterStateDto(
            hasGag ? state.Gag : null,
            hasGarbler ? state.Garbler : null,
            hasWardrobe ? state.Wardrobe : null,
            hasMoodle ? state.Moodles : null
        );
    }
}
