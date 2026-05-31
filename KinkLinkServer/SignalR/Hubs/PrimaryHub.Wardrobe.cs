using System.Diagnostics;
using KinkLinkCommon.Domain.Enums;
using KinkLinkCommon.Domain.Network;
using KinkLinkCommon.Domain.Network.Wardrobe;
using KinkLinkCommon.Domain.Wardrobe;
using Microsoft.AspNetCore.SignalR;

namespace KinkLinkServer.SignalR.Hubs;

public partial class PrimaryHub
{
    [HubMethodName(HubMethod.AddWardrobeItem)]
    public async Task<ActionResult<AddWardrobeItemResponse>> AddWardrobeItem(
        AddWardrobeItemRequest request
    )
    {
        var stopwatch = Stopwatch.StartNew();
        var friendCode = FriendCode;
        var correlationId = Guid.NewGuid();
        var success = false;
        ActionResult<AddWardrobeItemResponse> result = null!;

        using (
            logger.BeginScope(
                new Dictionary<string, object?>
                {
                    ["CorrelationId"] = correlationId,
                    ["Method"] = "AddWardrobeItem",
                    ["FriendCode"] = friendCode,
                }
            )
        )
            try
            {
                logger.LogInformation(
                    "[SignalR] Enter AddWardrobeItem ItemId={ItemId}",
                    request.Item.Id
                );

                var profileId = await profilesService.GetProfileIdFromUidAsync(friendCode);
                if (profileId is not { } id)
                {
                    result = new ActionResult<AddWardrobeItemResponse>(
                        ActionResultEc.Unknown,
                        null
                    );
                    return result;
                }

                var op = await wardrobeDataService.CreateOrUpdateWardrobeItemsByNameAsync(
                    id,
                    request.Item.Id,
                    request.Item
                );

                success = op;
                result = op
                    ? new ActionResult<AddWardrobeItemResponse>(
                        ActionResultEc.Success,
                        new AddWardrobeItemResponse(request.Item)
                    )
                    : new ActionResult<AddWardrobeItemResponse>(ActionResultEc.Unknown, null);

                return result;
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "[SignalR] AddWardrobeItem failed for FriendCode={FriendCode}",
                    friendCode
                );
                throw;
            }
            finally
            {
                stopwatch.Stop();
                logger.LogInformation(
                    "[SignalR] Exit AddWardrobeItem success={Success} duration_ms={DurationMs}",
                    success,
                    stopwatch.ElapsedMilliseconds
                );
                metricsService.IncrementSignalRMessage("AddWardrobeItem", success);
                metricsService.RecordSignalRMessageDuration(
                    "AddWardrobeItem",
                    stopwatch.ElapsedMilliseconds
                );
            }
    }

    [HubMethodName(HubMethod.RemoveWardrobeItem)]
    public async Task<ActionResult<RemoveWardrobeItemResponse>> RemoveWardrobeItem(
        RemoveWardrobeItemRequest request
    )
    {
        var stopwatch = Stopwatch.StartNew();
        var friendCode = FriendCode;
        var correlationId = Guid.NewGuid();
        var success = false;
        ActionResult<RemoveWardrobeItemResponse> result = null!;

        using (
            logger.BeginScope(
                new Dictionary<string, object?>
                {
                    ["CorrelationId"] = correlationId,
                    ["Method"] = "RemoveWardrobeItem",
                    ["FriendCode"] = friendCode,
                }
            )
        )
            try
            {
                logger.LogInformation(
                    "[SignalR] Enter RemoveWardrobeItem WardrobeId={WardrobeId}",
                    request.WardrobeId
                );

                var profileId = await profilesService.GetProfileIdFromUidAsync(friendCode);
                if (profileId is not { } id)
                {
                    result = new ActionResult<RemoveWardrobeItemResponse>(
                        ActionResultEc.Unknown,
                        null
                    );
                    return result;
                }

                var op = await wardrobeDataService.DeleteWardrobeItemAsync(id, request.WardrobeId);

                success = op;
                result = op
                    ? new ActionResult<RemoveWardrobeItemResponse>(
                        ActionResultEc.Success,
                        new RemoveWardrobeItemResponse(true)
                    )
                    : new ActionResult<RemoveWardrobeItemResponse>(ActionResultEc.Unknown, null);

                return result;
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "[SignalR] RemoveWardrobeItem failed for FriendCode={FriendCode}",
                    friendCode
                );
                throw;
            }
            finally
            {
                stopwatch.Stop();
                logger.LogInformation(
                    "[SignalR] Exit RemoveWardrobeItem success={Success} duration_ms={DurationMs}",
                    success,
                    stopwatch.ElapsedMilliseconds
                );
                metricsService.IncrementSignalRMessage("RemoveWardrobeItem", success);
                metricsService.RecordSignalRMessageDuration(
                    "RemoveWardrobeItem",
                    stopwatch.ElapsedMilliseconds
                );
            }
    }

    [HubMethodName(HubMethod.GetWardrobeItem)]
    public async Task<ActionResult<GetWardrobeItemResponse>> GetWardrobeItem(
        GetWardrobeItemRequest request
    )
    {
        var stopwatch = Stopwatch.StartNew();
        var friendCode = FriendCode;
        var correlationId = Guid.NewGuid();
        var success = false;
        ActionResult<GetWardrobeItemResponse> result = null!;

        using (
            logger.BeginScope(
                new Dictionary<string, object?>
                {
                    ["CorrelationId"] = correlationId,
                    ["Method"] = "GetWardrobeItem",
                    ["FriendCode"] = friendCode,
                }
            )
        )
            try
            {
                logger.LogInformation(
                    "[SignalR] Enter GetWardrobeItem WardrobeId={WardrobeId}",
                    request.WardrobeId
                );

                var profileId = await profilesService.GetProfileIdFromUidAsync(friendCode);
                if (profileId is not { } id)
                {
                    result = new ActionResult<GetWardrobeItemResponse>(
                        ActionResultEc.Unknown,
                        null
                    );
                    return result;
                }

                var item = await wardrobeDataService.GetWardrobeItemByGuid(id, request.WardrobeId);

                success = item != null;
                result =
                    item != null
                        ? new ActionResult<GetWardrobeItemResponse>(
                            ActionResultEc.Success,
                            new GetWardrobeItemResponse(item)
                        )
                        : new ActionResult<GetWardrobeItemResponse>(
                            ActionResultEc.ValueNotSet,
                            null
                        );

                return result;
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "[SignalR] GetWardrobeItem failed for FriendCode={FriendCode}",
                    friendCode
                );
                throw;
            }
            finally
            {
                stopwatch.Stop();
                logger.LogInformation(
                    "[SignalR] Exit GetWardrobeItem success={Success} duration_ms={DurationMs}",
                    success,
                    stopwatch.ElapsedMilliseconds
                );
                metricsService.IncrementSignalRMessage("GetWardrobeItem", success);
                metricsService.RecordSignalRMessageDuration(
                    "GetWardrobeItem",
                    stopwatch.ElapsedMilliseconds
                );
            }
    }

    [HubMethodName(HubMethod.ListWardrobeItems)]
    public async Task<ActionResult<ListWardrobeItemsResponse>> ListWardrobeItems()
    {
        var stopwatch = Stopwatch.StartNew();
        var friendCode = FriendCode;
        var correlationId = Guid.NewGuid();
        var success = false;
        ActionResult<ListWardrobeItemsResponse> result = null!;

        using (
            logger.BeginScope(
                new Dictionary<string, object?>
                {
                    ["CorrelationId"] = correlationId,
                    ["Method"] = "ListWardrobeItems",
                    ["FriendCode"] = friendCode,
                }
            )
        )
            try
            {
                logger.LogInformation("[SignalR] Enter ListWardrobeItems");
                var profileId = await profilesService.GetProfileIdFromUidAsync(friendCode);
                if (profileId is not { } id)
                {
                    result = new ActionResult<ListWardrobeItemsResponse>(
                        ActionResultEc.Unknown,
                        null
                    );
                    return result;
                }

                var items = await wardrobeDataService.GetAllWardrobeItemsAsync(id);

                success = true;
                result = new ActionResult<ListWardrobeItemsResponse>(
                    ActionResultEc.Success,
                    new ListWardrobeItemsResponse(items)
                );
                return result;
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "[SignalR] ListWardrobeItems failed for FriendCode={FriendCode}",
                    friendCode
                );
                throw;
            }
            finally
            {
                stopwatch.Stop();
                logger.LogInformation(
                    "[SignalR] Exit ListWardrobeItems success={Success} duration_ms={DurationMs}",
                    success,
                    stopwatch.ElapsedMilliseconds
                );
                metricsService.IncrementSignalRMessage("ListWardrobeItems", success);
                metricsService.RecordSignalRMessageDuration(
                    "ListWardrobeItems",
                    stopwatch.ElapsedMilliseconds
                );
            }
    }

    [HubMethodName(HubMethod.RandomizeActiveWardrobe)]
    public async Task<ActionResult<RandomizeActiveWardrobeResponse>> RandomizeActiveWardrobe(
        RandomizeActiveWardrobeRequest request
    )
    {
        var stopwatch = Stopwatch.StartNew();
        var friendCode = FriendCode;
        var correlationId = Guid.NewGuid();
        var success = false;
        ActionResult<RandomizeActiveWardrobeResponse> result = null!;

        using (
            logger.BeginScope(
                new Dictionary<string, object?>
                {
                    ["CorrelationId"] = correlationId,
                    ["Method"] = "RandomizeActiveWardrobe",
                    ["FriendCode"] = friendCode,
                }
            )
        )
            try
            {
                logger.LogInformation("[SignalR] Enter RandomizeActiveWardrobe");

                var profileId = await profilesService.GetProfileIdFromUidAsync(friendCode);
                if (profileId is not { } id)
                {
                    result = new ActionResult<RandomizeActiveWardrobeResponse>(
                        ActionResultEc.Unknown,
                        null
                    );
                    return result;
                }

                var op = await activeWardrobeStateService.RandomizeActiveWardrobeAsync(id);

                success = op;
                result = op
                    ? new ActionResult<RandomizeActiveWardrobeResponse>(
                        ActionResultEc.Success,
                        new RandomizeActiveWardrobeResponse(true)
                    )
                    : new ActionResult<RandomizeActiveWardrobeResponse>(
                        ActionResultEc.Unknown,
                        null
                    );

                return result;
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "[SignalR] RandomizeActiveWardrobe failed for FriendCode={FriendCode}",
                    friendCode
                );
                throw;
            }
            finally
            {
                stopwatch.Stop();
                logger.LogInformation(
                    "[SignalR] Exit RandomizeActiveWardrobe success={Success} duration_ms={DurationMs}",
                    success,
                    stopwatch.ElapsedMilliseconds
                );
                metricsService.IncrementSignalRMessage("RandomizeActiveWardrobe", success);
                metricsService.RecordSignalRMessageDuration(
                    "RandomizeActiveWardrobe",
                    stopwatch.ElapsedMilliseconds
                );
            }
    }

    [HubMethodName(HubMethod.GetWardrobeStatus)]
    public async Task<ActionResult<GetWardrobeStatusResponse>> GetWardrobeStatus()
    {
        var stopwatch = Stopwatch.StartNew();
        var friendCode = FriendCode;
        var correlationId = Guid.NewGuid();
        var success = false;
        ActionResult<GetWardrobeStatusResponse> result = null!;

        using (
            logger.BeginScope(
                new Dictionary<string, object?>
                {
                    ["CorrelationId"] = correlationId,
                    ["Method"] = "GetWardrobeStatus",
                    ["FriendCode"] = friendCode,
                }
            )
        )
            try
            {
                logger.LogInformation("[SignalR] Enter GetWardrobeStatus");
                var profileId = await profilesService.GetProfileIdFromUidAsync(friendCode);
                if (profileId is not { } id)
                {
                    result = new ActionResult<GetWardrobeStatusResponse>(
                        ActionResultEc.Unknown,
                        null
                    );
                    return result;
                }

                var state = await activeWardrobeStateService.GetWardrobeStateAsync(id);

                success = state != null;
                result =
                    state != null
                        ? new ActionResult<GetWardrobeStatusResponse>(
                            ActionResultEc.Success,
                            new GetWardrobeStatusResponse(state)
                        )
                        : new ActionResult<GetWardrobeStatusResponse>(
                            ActionResultEc.ValueNotSet,
                            null
                        );

                return result;
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "[SignalR] GetWardrobeStatus failed for FriendCode={FriendCode}",
                    friendCode
                );
                throw;
            }
            finally
            {
                stopwatch.Stop();
                logger.LogInformation(
                    "[SignalR] Exit GetWardrobeStatus success={Success} duration_ms={DurationMs}",
                    success,
                    stopwatch.ElapsedMilliseconds
                );
                metricsService.IncrementSignalRMessage("GetWardrobeStatus", success);
                metricsService.RecordSignalRMessageDuration(
                    "GetWardrobeStatus",
                    stopwatch.ElapsedMilliseconds
                );
            }
    }

    [HubMethodName(HubMethod.SetActiveWardrobeLayer)]
    public async Task<ActionResult<bool>> SetActiveWardrobeLayer(
        SetActiveWardrobeLayerRequest request
    )
    {
        var stopwatch = Stopwatch.StartNew();
        var friendCode = FriendCode;
        var correlationId = Guid.NewGuid();
        var success = false;
        ActionResult<bool> result = null!;

        try
        {
            logger.LogInformation(
                "[SignalR] Enter SetActiveWardrobeLayer Layer={Layer} ItemId={ItemId}",
                request.Layer,
                request.LayerData?.Id
            );

            var profileId = await profilesService.GetProfileIdFromUidAsync(friendCode);
            if (profileId is not { } id)
            {
                result = new ActionResult<bool>(ActionResultEc.Unknown, false);
                return result;
            }

            var op = await activeWardrobeStateService.UpdateWardrobeStateAsync(id, request.Layer, request.LayerData?.Id, request.LayerData?.Base64GlamourerData);

            success = op;
            result = op
                ? new ActionResult<bool>(ActionResultEc.Success, true)
                : new ActionResult<bool>(ActionResultEc.Unknown, false);

            return result;
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "[SignalR] SetActiveWardrobeLayer failed for FriendCode={FriendCode}",
                friendCode
            );
            throw;
        }
        finally
        {
            stopwatch.Stop();
            logger.LogInformation(
                "[SignalR] Exit SetActiveWardrobeLayer success={Success} duration_ms={DurationMs}",
                success,
                stopwatch.ElapsedMilliseconds
            );
            metricsService.IncrementSignalRMessage("SetActiveWardrobeLayer", success);
            metricsService.RecordSignalRMessageDuration(
                "SetActiveWardrobeLayer",
                stopwatch.ElapsedMilliseconds
            );
        }
    }
}
