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
    private readonly WardrobeManager _wardrobeManager;
    private readonly IDisposable _applyInteractionHandler;

    public event Action<ApplyInteractionRequest, ActionResult<Unit>>? OnInteractionReceived;

    public PairInteractionsHandler(
        LogService log,
        NetworkService network,
        WardrobeManager wardrobeManager
    )
    {
        _log = log;
        _network = network;
        _wardrobeManager = wardrobeManager;

        _applyInteractionHandler = network.Connection.On<
            ApplyInteractionRequest,
            ActionResult<Unit>
        >(HubMethod.ApplyInteraction, HandleApplyInteraction);
    }

    private async Task<ActionResult<Unit>> HandleApplyInteraction(ApplyInteractionRequest request)
    {
        var sender = request.TargetFriendCode;
        _log.Custom($"{sender} applied interaction to you");

        if (request.Action == PairAction.ApplyWardrobe)
        {
            if (request.Payload == null)
            {
                Plugin.Log.Warning("[PairInteractions] ApplyWardrobe but payload is null");
                OnInteractionReceived?.Invoke(request, ActionResultBuilder.Fail<Unit>(ActionResultEc.ClientBadData));
                return ActionResultBuilder.Fail<Unit>(ActionResultEc.ClientBadData);
            }

            if (request.Payload.WardrobeItems == null)
            {
                Plugin.Log.Warning("[PairInteractions] ApplyWardrobe but WardrobeItems is null");
                OnInteractionReceived?.Invoke(request, ActionResultBuilder.Fail<Unit>(ActionResultEc.ClientBadData));
                return ActionResultBuilder.Fail<Unit>(ActionResultEc.ClientBadData);
            }

            var success = await HandleApplyWardrobeAsync(request.Payload.WardrobeItems);
            if (!success)
            {
                OnInteractionReceived?.Invoke(request, ActionResultBuilder.Fail<Unit>(ActionResultEc.Unknown));
                return ActionResultBuilder.Fail<Unit>(ActionResultEc.Unknown);
            }
        }

        OnInteractionReceived?.Invoke(request, ActionResultBuilder.Ok(Unit.Empty));
        return ActionResultBuilder.Ok(Unit.Empty);
    }

    private async Task<bool> HandleApplyWardrobeAsync(List<WardrobeDto> items)
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
                            await _wardrobeManager.RemoveActiveSetAsync();
                            _log.Custom("Removed wardrobe set from pair");
                            break;
                        case "item":
                            await _wardrobeManager.RemovePieceFromSlotAsync(item.Slot);
                            _log.Custom($"Removed wardrobe item from slot {item.Slot} from pair");
                            break;
                        case "moditem":
                            await _wardrobeManager.RemoveWardrobeItemFromActive(item.Id);
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
                            await _wardrobeManager.ApplyDesignFromPairAsync(design, item.Priority);
                        }
                        break;

                    case "item":
                        var wardrobeItem = GlamourerDesignHelper.FromItemBase64(item.DataBase64);
                        if (wardrobeItem != null && wardrobeItem.Item != null)
                        {
                            wardrobeItem = wardrobeItem with
                            {
                                Id = item.Id,
                                Name = item.Name,
                                Description = item.Description,
                                Slot = item.Slot,
                                Priority = item.Priority,
                            };
                            wardrobeItem.Item.Apply = true;
                            wardrobeItem.Item.ApplyStain = true;
                            await _wardrobeManager.ApplyPieceAsync(wardrobeItem);
                        }
                        break;

                    case "moditem":
                        var modItem = GlamourerDesignHelper.FromItemBase64(item.DataBase64);
                        if (modItem != null)
                        {
                            modItem = modItem with
                            {
                                Id = item.Id,
                                Name = item.Name,
                                Description = item.Description,
                                Slot = item.Slot,
                                Priority = item.Priority,
                            };
                            await _wardrobeManager.ApplyWardrobeItem(modItem);
                        }
                        break;

                    default:
                        Plugin.Log.Warning("Unknown wardrobe item type: {Type}", item.Type);
                        break;
                }
            }

            _log.Custom($"Applied {items.Count} wardrobe items from pair");
            return true;
        }
        catch (Exception ex)
        {
            Plugin.Log.Error(ex, "Failed to apply wardrobe from pair interaction");
            return false;
        }
    }

    public void Dispose()
    {
        _applyInteractionHandler.Dispose();
        GC.SuppressFinalize(this);
    }
}
