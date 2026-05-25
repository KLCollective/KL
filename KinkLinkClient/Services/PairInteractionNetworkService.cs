using System;
using System.Diagnostics;
using System.Threading.Tasks;
using KinkLinkClient.Utils;
using KinkLinkCommon.Domain;
using KinkLinkCommon.Domain.Enums.Permissions;
using KinkLinkCommon.Domain.Network;
using KinkLinkCommon.Domain.Network.PairInteractions;
using KinkLinkCommon.Domain.Wardrobe;

namespace KinkLinkClient.Services;

public class PairInteractionNetworkService : IDisposable
{
    private readonly NetworkService _networkService;

    public PairInteractionNetworkService(NetworkService networkService)
    {
        _networkService = networkService;
    }

    public async Task<ActionResult<Unit>> ApplyWardrobeLayer(
        string targetFriendCode,
        WardrobeLayer layer,
        Guid wardrobeItemId
    )
    {
        throw new NotImplementedException();
    }

    public async Task<ActionResult<Unit>> RemoveWardrobeLayer(
        string targetFriendCode,
        WardrobeLayer layer,
        Guid wardrobeItemId
    )
    {
        throw new NotImplementedException();
    }

    public Task<ActionResult<Unit>> ApplyLockToWardrobeLayer(
        string targetFriendCode,
        WardrobeLayer layer,
        object lockInfo
    )
    {
        throw new NotImplementedException(
            "ApplyLockToWardrobeLayer not implemented in client service"
        );
    }

    public Task<ActionResult<Unit>> RemoveLockFromWardrobeLayer(
        string targetFriendCode,
        WardrobeLayer layer,
        object lockInfo
    )
    {
        throw new NotImplementedException(
            "RemoveLockFromWardrobeLayer not implemented in client service"
        );
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }
}
