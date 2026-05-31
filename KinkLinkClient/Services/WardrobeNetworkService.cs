using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using KinkLinkClient.Utils;
using KinkLinkCommon.Domain;
using KinkLinkCommon.Domain.Enums;
using KinkLinkCommon.Domain.Network;
using KinkLinkCommon.Domain.Network.PairInteractions;
using KinkLinkCommon.Domain.Network.Wardrobe;
using KinkLinkCommon.Domain.Wardrobe;

namespace KinkLinkClient.Services;

public class WardrobeNetworkService : IDisposable, IWardrobeNetworkService
{
    private readonly NetworkService _networkService;

    public WardrobeNetworkService(NetworkService networkService)
    {
        _networkService = networkService;
    }

    public async Task<List<LightWardrobeItemDto>> QueryPairWardrobe(string friendCode)
    {
        var sw = Stopwatch.StartNew();
        Plugin.Log.Information(
            $"[WardrobeNetworkService] Enter QueryPairWardrobe friendCode={friendCode}"
        );
        try
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

            return new List<LightWardrobeItemDto>();
        }
        finally
        {
            sw.Stop();
            Plugin.Log.Information(
                $"[WardrobeNetworkService] Exit QueryPairWardrobe duration={sw.ElapsedMilliseconds}ms"
            );
        }
    }

    public async Task<ActionResult<AddWardrobeItemResponse>> AddWardrobeItemAsync(
        AddWardrobeItemRequest request
    )
    {
        var sw = Stopwatch.StartNew();
        Plugin.Log.Information(
            $"[WardrobeNetworkService] Enter AddWardrobeItemAsync requestType={request?.GetType().Name}"
        );
        try
        {
            try
            {
                var response = await _networkService
                    .InvokeAsync<ActionResult<AddWardrobeItemResponse>>(
                        HubMethod.AddWardrobeItem,
                        request
                    )
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
                NotificationHelper.Error(
                    "Add Wardrobe Item",
                    "Failed to add wardrobe item to server"
                );
                return new ActionResult<AddWardrobeItemResponse>(ActionResultEc.Unknown, null);
            }
        }
        finally
        {
            sw.Stop();
            Plugin.Log.Information(
                $"[WardrobeNetworkService] Exit AddWardrobeItemAsync duration={sw.ElapsedMilliseconds}ms"
            );
        }
    }

    public async Task<ActionResult<RemoveWardrobeItemResponse>> RemoveWardrobeItemAsync(
        RemoveWardrobeItemRequest request
    )
    {
        var sw = Stopwatch.StartNew();
        Plugin.Log.Information(
            $"[WardrobeNetworkService] Enter RemoveWardrobeItemAsync requestType={request?.GetType().Name}"
        );
        try
        {
            try
            {
                var response = await _networkService
                    .InvokeAsync<ActionResult<RemoveWardrobeItemResponse>>(
                        HubMethod.RemoveWardrobeItem,
                        request
                    )
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
        finally
        {
            sw.Stop();
            Plugin.Log.Information(
                $"[WardrobeNetworkService] Exit RemoveWardrobeItemAsync duration={sw.ElapsedMilliseconds}ms"
            );
        }
    }

    public async Task<ActionResult<GetWardrobeItemResponse>> GetWardrobeItemAsync(
        GetWardrobeItemRequest request
    )
    {
        var sw = Stopwatch.StartNew();
        Plugin.Log.Information(
            $"[WardrobeNetworkService] Enter GetWardrobeItemAsync requestType={request?.GetType().Name}"
        );
        try
        {
            try
            {
                var response = await _networkService
                    .InvokeAsync<ActionResult<GetWardrobeItemResponse>>(
                        HubMethod.GetWardrobeItem,
                        request
                    )
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
        finally
        {
            sw.Stop();
            Plugin.Log.Information(
                $"[WardrobeNetworkService] Exit GetWardrobeItemAsync duration={sw.ElapsedMilliseconds}ms"
            );
        }
    }

    public async Task<ActionResult<ListWardrobeItemsResponse>> ListWardrobeItemsAsync()
    {
        var sw = Stopwatch.StartNew();
        Plugin.Log.Information("[WardrobeNetworkService] Enter ListWardrobeItemsAsync");
        try
        {
            try
            {
                var response = await _networkService
                    .InvokeAsync<ActionResult<ListWardrobeItemsResponse>>(
                        HubMethod.ListWardrobeItems
                    )
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
        finally
        {
            sw.Stop();
            Plugin.Log.Information(
                $"[WardrobeNetworkService] Exit ListWardrobeItemsAsync duration={sw.ElapsedMilliseconds}ms"
            );
        }
    }

    public async Task<ActionResult<bool>> SetActiveWardrobeLayerAsync(
        WardrobeLayer layer,
        WardrobeItem item
    )
    {
        Plugin.Log.Information(
            $"[WardrobeNetworkService] Enter SetActiveWardrobeLayerAsync layer={layer} itemId={item.Id}"
        );
        try
        {
            var dto = new WardrobeDto(
                item.Id,
                item.Name,
                item.Description,
                item.Layer,
                GlamourerDesignHelper.ToBase64(item.Design),
                item.Priority
            );
            var request = new SetActiveWardrobeLayerRequest(layer, dto);

            // debug log: layer, item id, base64 length
            Plugin.Log.Information("[WardrobeNetworkService] Invoke SetActiveWardrobeLayer layer={Layer} itemId={ItemId} dto_layer={DtoLayer} base64_len={Len}", layer, item.Id, dto.Layer, dto.Base64GlamourerData?.Length ?? 0);

            return await _networkService
                .InvokeAsync<ActionResult<bool>>(HubMethod.SetActiveWardrobeLayer, request)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Plugin.Log.Error(ex, "[WardrobeNetworkService] Failed to set active wardrobe layer");
            NotificationHelper.Error(
                "Set Active Wardrobe Layer",
                "Failed to set active wardrobe layer on server"
            );
            return new ActionResult<bool>(ActionResultEc.Unknown, false);
        }
    }

    public async Task<ActionResult<GetWardrobeStatusResponse>> GetWardrobeStatusAsync()
    {
        var sw = Stopwatch.StartNew();
        Plugin.Log.Information("[WardrobeNetworkService] Enter GetWardrobeStatusAsync");
        try
        {
            try
            {
                var response = await _networkService
                    .InvokeAsync<ActionResult<GetWardrobeStatusResponse>>(
                        HubMethod.GetWardrobeStatus
                    )
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
        finally
        {
            sw.Stop();
            Plugin.Log.Information(
                $"[WardrobeNetworkService] Exit GetWardrobeStatusAsync duration={sw.ElapsedMilliseconds}ms"
            );
        }
    }

    public async Task<ActionResult<RandomizeActiveWardrobeResponse>> RandomizeActiveWardrobeAsync(
        RandomizeActiveWardrobeRequest request
    )
    {
        var sw = Stopwatch.StartNew();
        Plugin.Log.Information("[WardrobeNetworkService] Enter RandomizeActiveWardrobeAsync");
        try
        {
            var response = await _networkService
                .InvokeAsync<ActionResult<RandomizeActiveWardrobeResponse>>(
                    HubMethod.RandomizeActiveWardrobe,
                    request
                )
                .ConfigureAwait(false);

            if (response.Result != ActionResultEc.Success)
            {
                NotificationHelper.Error(
                    "Randomize Wardrobe",
                    $"Failed to randomize wardrobe: {response.Result}"
                );
            }

            return response;
        }
        catch (Exception ex)
        {
            Plugin.Log.Error(ex, "[WardrobeNetworkService] Failed to randomize wardrobe");
            NotificationHelper.Error(
                "Randomize Wardrobe",
                "Failed to randomize wardrobe on server"
            );
            return new ActionResult<RandomizeActiveWardrobeResponse>(ActionResultEc.Unknown, null);
        }
        finally
        {
            sw.Stop();
            Plugin.Log.Information(
                $"[WardrobeNetworkService] Exit RandomizeActiveWardrobeAsync duration={sw.ElapsedMilliseconds}ms"
            );
        }
    }

    public async Task<ActionResult<bool>> ClearActiveWardrobeLayerAsync(WardrobeLayer layer)
    {
        var correlationId = Guid.NewGuid();
        Plugin.Log.Information("[WardrobeNetworkService] Enter ClearActiveWardrobeLayerAsync correlationId={CorrelationId} layer={Layer}", correlationId, layer);
        try
        {
            var request = new SetActiveWardrobeLayerRequest(layer, null);
            var response = await _networkService
                .InvokeAsync<ActionResult<bool>>(HubMethod.SetActiveWardrobeLayer, request)
                .ConfigureAwait(false);

            Plugin.Log.Information("[WardrobeNetworkService] Exit ClearActiveWardrobeLayerAsync correlationId={CorrelationId} success={Success}", correlationId, response.Result == ActionResultEc.Success);
            return response;
        }
        catch (Exception ex)
        {
            Plugin.Log.Error(ex, "[WardrobeNetworkService] Failed to clear active wardrobe layer correlationId={CorrelationId}", correlationId);
            NotificationHelper.Error(
                "Clear Active Wardrobe Layer",
                "Failed to clear active wardrobe layer on server"
            );
            return new ActionResult<bool>(ActionResultEc.Unknown, false);
        }
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }
}
