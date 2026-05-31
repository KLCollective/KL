using KinkLinkCommon.Domain.Wardrobe;
using System.Threading.Tasks;

namespace KinkLinkServer.Services;

public interface IActiveWardrobeStateService
{
    Task<bool> RandomizeActiveWardrobeAsync(int profileId);
    Task<WardrobeStateDto?> GetWardrobeStateAsync(int profileId);
    Task<PairWardrobeStateDto> GetPairWardrobeStateAsync(int profileId);
    Task<bool> UpdateWardrobeStateAsync(int profileId, KinkLinkCommon.Domain.Wardrobe.WardrobeLayer layer, Guid? id, string? base64GlamourerData = null);
}
