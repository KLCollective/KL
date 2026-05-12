using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using KinkLinkClient.Utils;
using KinkLinkCommon.Domain;
using KinkLinkCommon.Domain.Enums;
using KinkLinkCommon.Domain.Network;
using KinkLinkCommon.Domain.Network.PairInteractions;
using KinkLinkCommon.Domain.Network.Wardrobe;
using KinkLinkCommon.Domain.Wardrobe;

namespace KinkLinkClient.Services;

public class WardrobeNetworkService : IDisposable
{
    private readonly NetworkService _networkService;

    public WardrobeNetworkService(NetworkService networkService)
    {
        _networkService = networkService;
    }

    public async Task<List<PairWardrobeItemDto>> QueryPairWardrobe(string friendCode)
    {
        var request = new QueryPairWardrobeRequest(friendCode);
        var response = await _networkService
            .InvokeAsync<ActionResult<QueryPairWardrobeResponse>>(
                HubMethod.QueryPairWardrobe,
                request
            )
            .ConfigureAwait(false);

        if (response.Result == ActionResultEc.Success && response.Value != null)
        {
            return response.Value.Items;
        }

        return [];
    }

    public async Task<ActionResult<AddWardrobeItemResponse>> AddWardrobeItemAsync(AddWardrobeItemRequest request)
    {
        try
        {
            var response = await _networkService
                .InvokeAsync<ActionResult<AddWardrobeItemResponse>>(HubMethod.AddWardrobeItem, request)
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
            return new ActionResult<AddWardrobeItemResponse>(ActionResultEc.Unknown, null);
        }
    }

    public async Task<ActionResult<RemoveWardrobeItemResponse>> RemoveWardrobeItemAsync(RemoveWardrobeItemRequest request)
    {
        try
        {
            var response = await _networkService
                .InvokeAsync<ActionResult<RemoveWardrobeItemResponse>>(HubMethod.RemoveWardrobeItem, request)
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
            return new ActionResult<RemoveWardrobeItemResponse>(ActionResultEc.Unknown, null);
        }
    }

    public async Task<ActionResult<GetWardrobeItemResponse>> GetWardrobeItemAsync(GetWardrobeItemRequest request)
    {
        try
        {
            var response = await _networkService
                .InvokeAsync<ActionResult<GetWardrobeItemResponse>>(HubMethod.GetWardrobeItem, request)
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
            return new ActionResult<GetWardrobeItemResponse>(ActionResultEc.Unknown, null);
        }
    }

    public async Task<ActionResult<ListWardrobeItemsResponse>> ListWardrobeItemsAsync()
    {
        try
        {
            var response = await _networkService
                .InvokeAsync<ActionResult<ListWardrobeItemsResponse>>(HubMethod.ListWardrobeItems)
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
            return new ActionResult<ListWardrobeItemsResponse>(ActionResultEc.Unknown, null);
        }
    }

    public async Task<ActionResult<SetWardrobeStatusResponse>> SetWardrobeStatusAsync(SetWardrobeStatusRequest request)
    {
        try
        {
            var response = await _networkService
                .InvokeAsync<ActionResult<SetWardrobeStatusResponse>>(HubMethod.SetWardrobeStatus, request)
                .ConfigureAwait(false);

            if (response.Result != ActionResultEc.Success)
            {
                NotificationHelper.Error(
                    "Set Wardrobe Status",
                    $"Failed to set wardrobe status: {response.Result}"
                );
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
            return new ActionResult<SetWardrobeStatusResponse>(ActionResultEc.Unknown, null);
        }
    }

    public async Task<ActionResult<GetWardrobeStatusResponse>> GetWardrobeStatusAsync()
    {
        try
        {
            var response = await _networkService
                .InvokeAsync<ActionResult<GetWardrobeStatusResponse>>(HubMethod.GetWardrobeStatus)
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
            return new ActionResult<GetWardrobeStatusResponse>(ActionResultEc.Unknown, null);
        }
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }
}