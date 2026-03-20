using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KinkLinkClient.Utils;
using KinkLinkCommon.Dependencies.Glamourer;
using KinkLinkCommon.Dependencies.Glamourer.Components;
using KinkLinkCommon.Domain.Enums;
using KinkLinkCommon.Domain.Network;
using KinkLinkCommon.Domain.Wardrobe;

namespace KinkLinkClient.Services;

public class WardrobeNetworkService : IDisposable
{
    private readonly NetworkService _networkService;
    private WardrobeService? _wardrobeService;

    public WardrobeNetworkService(NetworkService networkService)
    {
        _networkService = networkService;
    }

    public void SetWardrobeService(WardrobeService wardrobeService)
    {
        _wardrobeService = wardrobeService;
    }

    public async Task SyncFromServerAsync()
    {
        if (_wardrobeService == null)
        {
            Plugin.Log.Warning("[WardrobeNetworkService] WardrobeService not set, skipping sync");
            return;
        }

        try
        {
            var result = await ListWardrobeItemsAsync();
            if (result.Result == ActionResultEc.Success && result.Value != null)
            {
                _wardrobeService.LoadFromWardrobeDto(result.Value);
            }

            var statusResult = await GetWardrobeStatusAsync();
            if (statusResult.Result == ActionResultEc.Success && statusResult.Value != null)
            {
                ApplyWardrobeState(statusResult.Value);
                Plugin.Log.Information(
                    "[WardrobeNetworkService] Synced wardrobe status from server"
                );
            }

            NotificationHelper.Success("Wardrobe Sync", "Synced wardrobe from server");
        }
        catch (Exception ex)
        {
            Plugin.Log.Error(ex, "[WardrobeNetworkService] Failed to sync from server");
            NotificationHelper.Error("Wardrobe Sync Failed", "Failed to sync wardrobe from server");
        }
    }

    private static WardrobeItem DtoToWardrobeItem(WardrobeDto dto)
    {
        return new WardrobeItem
        {
            Id = dto.Id,
            Name = dto.Name,
            Description = dto.Description,
            Slot = (GlamourerEquipmentSlot)dto.Slot,
            Priority = dto.Priority,
        };
    }

    private static WardrobeSet DtoToWardrobeSet(WardrobeDto dto)
    {
        return new WardrobeSet
        {
            Design = new GlamourerDesign
            {
                Identifier = dto.Id,
                Name = dto.Name,
                Description = dto.Description,
            },
            Priority = dto.Priority,
        };
    }

    private void ApplyWardrobeState(WardrobeStateDto state)
    {
        if (_wardrobeService == null)
            return;

        if (state.BaseLayerBase64 != null)
        {
            var baseLayerDesign = GlamourerDesignHelper.FromBase64(state.BaseLayerBase64);
            if (baseLayerDesign != null)
            {
                var baseLayerId = baseLayerDesign.Identifier;
                var set = _wardrobeService.GetSetById(baseLayerId);
                if (set != null)
                {
                    _wardrobeService.ApplySetByIdSync(baseLayerId);
                }
            }
        }

        if (state.Equipment != null)
        {
            foreach (var kvp in state.Equipment)
            {
                var itemData = kvp.Value;
                var slot = ConvertSlotKey(kvp.Key);
                if (slot != GlamourerEquipmentSlot.None)
                {
                    var piece = new WardrobeItem
                    {
                        Id = itemData.Id,
                        Name = itemData.Name,
                        Description = itemData.Description,
                        Slot = itemData.Slot,
                        Item = itemData.Item,
                        Mods = itemData.Mods ?? [],
                        Materials =
                            itemData.Materials ?? new Dictionary<string, GlamourerMaterial>(),
                        Priority = itemData.Priority,
                    };
                    _wardrobeService.ApplyPieceSync(slot, piece);
                }
            }
        }

        if (state.ModSettings != null)
        {
            foreach (var kvp in state.ModSettings)
            {
                var itemData = kvp.Value;
                var modItem = new WardrobeItem
                {
                    Id = itemData.Id,
                    Name = itemData.Name,
                    Description = itemData.Description,
                    Slot = itemData.Slot,
                    Item = itemData.Item,
                    Mods = itemData.Mods ?? [],
                    Materials = itemData.Materials ?? new Dictionary<string, GlamourerMaterial>(),
                    Priority = itemData.Priority,
                };
                _wardrobeService.ApplyCharacterItemSync(modItem);
            }
        }
        _wardrobeService.ApplyModsAsync(true);
    }

    private static GlamourerEquipmentSlot ConvertSlotKey(string slotName)
    {
        return slotName switch
        {
            "Head" => GlamourerEquipmentSlot.Head,
            "Body" => GlamourerEquipmentSlot.Body,
            "Hands" => GlamourerEquipmentSlot.Hands,
            "Legs" => GlamourerEquipmentSlot.Legs,
            "Feet" => GlamourerEquipmentSlot.Feet,
            "Ears" => GlamourerEquipmentSlot.Ears,
            "Neck" => GlamourerEquipmentSlot.Neck,
            "Wrists" => GlamourerEquipmentSlot.Wrists,
            "RFinger" => GlamourerEquipmentSlot.RFinger,
            "LFinger" => GlamourerEquipmentSlot.LFinger,
            _ => GlamourerEquipmentSlot.None,
        };
    }

    public async Task<ActionResult<WardrobeDto>> AddWardrobeItemAsync(WardrobeDto request)
    {
        try
        {
            if (request.DataBase64 != null)
                Plugin.Log.Info($"Design Base64 length: {request.DataBase64.Length}");
            var response = await _networkService
                .InvokeAsync<ActionResult<WardrobeDto>>(HubMethod.AddWardrobeItem, request)
                .ConfigureAwait(false);

            if (response.Result != ActionResultEc.Success)
            {
                NotificationHelper.Error(
                    "Add Wardrobe Item",
                    $"Failed to add wardrobe item: {response.Result}"
                );
            }

            return response;
        }
        catch (Exception ex)
        {
            Plugin.Log.Error(ex, "[WardrobeNetworkService] Failed to add wardrobe item");
            NotificationHelper.Error("Add Wardrobe Item", "Failed to add wardrobe item to server");
            return new ActionResult<WardrobeDto>(ActionResultEc.Unknown, null);
        }
    }

    public async Task<ActionResult<bool>> RemoveWardrobeItemAsync(Guid wardrobeId)
    {
        try
        {
            var response = await _networkService
                .InvokeAsync<ActionResult<bool>>(HubMethod.RemoveWardrobeItem, wardrobeId)
                .ConfigureAwait(false);

            if (response.Result != ActionResultEc.Success)
            {
                NotificationHelper.Error(
                    "Remove Wardrobe Item",
                    $"Failed to remove wardrobe item: {response.Result}"
                );
            }

            return response;
        }
        catch (Exception ex)
        {
            Plugin.Log.Error(ex, "[WardrobeNetworkService] Failed to remove wardrobe item");
            NotificationHelper.Error(
                "Remove Wardrobe Item",
                "Failed to remove wardrobe item from server"
            );
            return new ActionResult<bool>(ActionResultEc.Unknown, false);
        }
    }

    public async Task<ActionResult<WardrobeDto>> GetWardrobeItemAsync(Guid wardrobeId)
    {
        try
        {
            var response = await _networkService
                .InvokeAsync<ActionResult<WardrobeDto>>(HubMethod.GetWardrobeItem, wardrobeId)
                .ConfigureAwait(false);

            return response;
        }
        catch (Exception ex)
        {
            Plugin.Log.Error(ex, "[WardrobeNetworkService] Failed to get wardrobe item");
            NotificationHelper.Error(
                "Get Wardrobe Item",
                "Failed to get wardrobe item from server"
            );
            return new ActionResult<WardrobeDto>(ActionResultEc.Unknown, null);
        }
    }

    public async Task<ActionResult<List<WardrobeDto>>> ListWardrobeItemsAsync()
    {
        try
        {
            var response = await _networkService
                .InvokeAsync<ActionResult<List<WardrobeDto>>>(HubMethod.ListWardrobeItems)
                .ConfigureAwait(false);

            return response;
        }
        catch (Exception ex)
        {
            Plugin.Log.Error(ex, "[WardrobeNetworkService] Failed to list wardrobe items");
            NotificationHelper.Error(
                "List Wardrobe Items",
                "Failed to list wardrobe items from server"
            );
            return new ActionResult<List<WardrobeDto>>(ActionResultEc.Unknown, []);
        }
    }

    public async Task<ActionResult<bool>> SetWardrobeStatusAsync(WardrobeStateDto state)
    {
        try
        {
            Plugin.Log.Information("[WardrobeNetworkService] SetWardrobeStatusAsync called - Equipment count: {Count}, ModSettings count: {ModCount}",
                state.Equipment?.Count ?? 0, state.ModSettings?.Count ?? 0);

            var response = await _networkService
                .InvokeAsync<ActionResult<bool>>(HubMethod.SetWardrobeStatus, state)
                .ConfigureAwait(false);

            Plugin.Log.Information("[WardrobeNetworkService] SetWardrobeStatusAsync result: {Result}", response.Result);

            if (response.Result != ActionResultEc.Success)
            {
                Plugin.Log.Warning("[WardrobeNetworkService] SetWardrobeStatusAsync failed: {Result}", response.Result);
                NotificationHelper.Error(
                    "Set Wardrobe Status",
                    $"Failed to set wardrobe status: {response.Result}"
                );
            }
            else
            {
                Plugin.Log.Information("[WardrobeNetworkService] SetWardrobeStatusAsync succeeded");
            }

            return response;
        }
        catch (Exception ex)
        {
            Plugin.Log.Error(ex, "[WardrobeNetworkService] Failed to set wardrobe status");
            NotificationHelper.Error(
                "Set Wardrobe Status",
                "Failed to set wardrobe status on server"
            );
            return new ActionResult<bool>(ActionResultEc.Unknown, false);
        }
    }

    public async Task<ActionResult<WardrobeStateDto>> GetWardrobeStatusAsync()
    {
        try
        {
            var response = await _networkService
                .InvokeAsync<ActionResult<WardrobeStateDto>>(HubMethod.GetWardrobeStatus)
                .ConfigureAwait(false);

            return response;
        }
        catch (Exception ex)
        {
            Plugin.Log.Error(ex, "[WardrobeNetworkService] Failed to get wardrobe status");
            NotificationHelper.Error(
                "Get Wardrobe Status",
                "Failed to get wardrobe status from server"
            );
            return new ActionResult<WardrobeStateDto>(ActionResultEc.Unknown, null);
        }
    }

    public async void ResetWardrobe()
    {
        _wardrobeService.ClearActive();
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }
}
