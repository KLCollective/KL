using System.Collections.Generic;
using System.Threading.Tasks;
using KinkLinkCommon.Domain.Network.Wardrobe;
using KinkLinkCommon.Domain.Wardrobe;
using KinkLinkCommon.Domain.Network;

namespace KinkLinkClient.Services;

public interface IWardrobeNetworkService
{
    Task<ActionResult<ListWardrobeItemsResponse>> ListWardrobeItemsAsync();
    Task<ActionResult<GetWardrobeStatusResponse>> GetWardrobeStatusAsync();
    Task<ActionResult<bool>> SetActiveWardrobeLayerAsync(WardrobeLayer layer, WardrobeItem item);
    Task<ActionResult<bool>> ClearActiveWardrobeLayerAsync(WardrobeLayer layer);
    Task<ActionResult<RandomizeActiveWardrobeResponse>> RandomizeActiveWardrobeAsync(RandomizeActiveWardrobeRequest request);
    Task<ActionResult<AddWardrobeItemResponse>> AddWardrobeItemAsync(AddWardrobeItemRequest request);
    Task<ActionResult<RemoveWardrobeItemResponse>> RemoveWardrobeItemAsync(RemoveWardrobeItemRequest request);
    Task<ActionResult<GetWardrobeItemResponse>> GetWardrobeItemAsync(GetWardrobeItemRequest request);
    Task<List<LightWardrobeItemDto>> QueryPairWardrobe(string friendCode);
}
