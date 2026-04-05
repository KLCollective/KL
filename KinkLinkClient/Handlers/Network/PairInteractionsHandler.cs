using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KinkLinkClient.Dependencies.Moodles.Services;
using KinkLinkClient.Handlers.Network.Base;
using KinkLinkClient.Services;
using KinkLinkClient.Utils;
using KinkLinkCommon.Domain;
using KinkLinkCommon.Domain.CharacterState;
using KinkLinkCommon.Domain.Enums;
using KinkLinkCommon.Domain.Enums.Permissions;
using KinkLinkCommon.Domain.Network;
using KinkLinkCommon.Domain.Network.PairInteractions;
using KinkLinkCommon.Domain.Wardrobe;
using Microsoft.AspNetCore.SignalR.Client;

namespace KinkLinkClient.Handlers.Network;

public class PairInteractionsHandler : IDisposable
{
    private readonly LogService _log;
    private readonly NetworkService _network;
    private readonly WardrobeService _wardrobeService;
    private readonly IDisposable _applyInteractionHandler;

    public event Action<ApplyInteractionCommand, ActionResult<Unit>>? OnInteractionReceived;

    public PairInteractionsHandler(
        LogService log,
        NetworkService network,
        WardrobeService wardrobeService
    )
    {
        _log = log;
        _network = network;
        _wardrobeService = wardrobeService;

        _applyInteractionHandler = network.Connection.On<
            ApplyInteractionCommand,
            ActionResult<Unit>
        >(HubMethod.ApplyInteraction, HandleApplyInteraction);
    }

    private async Task<ActionResult<Unit>> HandleApplyInteraction(ApplyInteractionCommand command)
    {
        var sender = command.TargetFriendCode;
        _log.Custom($"{sender} applied interaction to you");

        if (command.Action == PairAction.ApplyWardrobe)
        {
            if (command.Payload == null)
            {
                Plugin.Log.Warning("[PairInteractions] ApplyWardrobe but payload is null");
            }
            else if (command.Payload.WardrobeItems == null)
            {
                Plugin.Log.Warning("[PairInteractions] ApplyWardrobe but WardrobeItems is null");
            }
            else
            {
                await HandleApplyWardrobeAsync(command.Payload.WardrobeItems);
            }
        }

        OnInteractionReceived?.Invoke(command, ActionResultBuilder.Ok(Unit.Empty));
        return ActionResultBuilder.Ok(Unit.Empty);
    }

    private async Task HandleApplyWardrobeAsync(List<WardrobeDto> items)
    {
        try
        {
            foreach (var item in items)
            {
                if (item.DataBase64 == null)
                {
                    switch (item.Type)
                    {
                        case "set":
                            await _wardrobeService.RemoveActiveSetAsync();
                            _log.Custom("Removed wardrobe set from pair");
                            break;
                        case "item":
                            await _wardrobeService.RemovePieceFromSlotAsync(item.Slot);
                            _log.Custom($"Removed wardrobe item from slot {item.Slot} from pair");
                            break;
                        case "moditem":
                            await _wardrobeService.RemoveWardrobeItemFromActive(item.Id);
                            _log.Custom($"Removed moditem {item.Name} from pair");
                            break;
                    }
                    continue;
                }

                switch (item.Type)
                {
                    case "set":
                        var design = GlamourerDesignHelper.FromBase64(item.DataBase64);
                        if (design != null)
                        {
                            await _wardrobeService.ApplyDesignFromPairAsync(design, item.Priority);
                        }
                        break;

                    case "item":
                        var wardrobeItem = GlamourerDesignHelper.FromItemBase64(item.DataBase64);
                        if (wardrobeItem != null)
                        {
                            wardrobeItem.Id = item.Id;
                            wardrobeItem.Name = item.Name;
                            wardrobeItem.Description = item.Description;
                            wardrobeItem.Slot = item.Slot;
                            wardrobeItem.Priority = item.Priority;
                            await _wardrobeService.ApplyPieceAsync(wardrobeItem);
                        }
                        break;

                    case "moditem":
                        var modItem = GlamourerDesignHelper.FromItemBase64(item.DataBase64);
                        if (modItem != null)
                        {
                            modItem.Id = item.Id;
                            modItem.Name = item.Name;
                            modItem.Description = item.Description;
                            modItem.Slot = item.Slot;
                            modItem.Priority = item.Priority;
                            await _wardrobeService.ApplyWardrobeItem(modItem);
                        }
                        break;

                    default:
                        Plugin.Log.Warning("Unknown wardrobe item type: {Type}", item.Type);
                        break;
                }
            }

            _log.Custom($"Applied {items.Count} wardrobe items from pair");
        }
        catch (Exception ex)
        {
            Plugin.Log.Error(ex, "Failed to apply wardrobe from pair interaction");
        }
    }

    public void Dispose()
    {
        _applyInteractionHandler.Dispose();
        GC.SuppressFinalize(this);
    }
}
