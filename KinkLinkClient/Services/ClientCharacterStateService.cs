using System;
using System.Threading.Tasks;
using KinkLinkCommon.Domain;
using KinkLinkCommon.Domain.Enums;
using KinkLinkCommon.Domain.Network;
using KinkLinkCommon.Domain.Network.Locks;
using KinkLinkCommon.Domain.Network.PairInteractions;
using KinkLinkCommon.Domain.Network.SyncPairState;
using KinkLinkCommon.Domain.Wardrobe;

namespace KinkLinkClient.Services;

public class ClientCharacterStateService : IDisposable
{
    private readonly NetworkService _network;

    public ClientCharacterStateService(NetworkService network)
    {
        _network = network;
    }

    public void UpdateLocalState(SyncPairStateCommand state) { }

    public SyncPairStateCommand? GetLocalState() => null;

    public async Task<ActionResult<QueryPairStateResponse>> QueryPairStateAsync(
        string targetFriendCode
    )
    {
        try
        {
            var request = new QueryPairStateRequest(targetFriendCode);
            var response = await _network
                .InvokeAsync<ActionResult<QueryPairStateResponse>>(
                    HubMethod.QueryPairState,
                    request
                )
                .ConfigureAwait(false);
            return response;
        }
        catch (Exception ex)
        {
            Plugin.Log.Error(ex, "Failed to query pair state for {FriendCode}", targetFriendCode);
            return new ActionResult<QueryPairStateResponse>(ActionResultEc.Unknown, default);
        }
    }

    public async Task<ActionResult<QueryPairWardrobeStateResponse>> QueryPairWardrobeStateAsync(
        string targetFriendCode
    )
    {
        try
        {
            var request = new QueryPairWardrobeStateRequest(targetFriendCode);
            var response = await _network
                .InvokeAsync<ActionResult<QueryPairWardrobeStateResponse>>(
                    HubMethod.QueryPairWardrobeState,
                    request
                )
                .ConfigureAwait(false);
            return response;
        }
        catch (Exception ex)
        {
            Plugin.Log.Error(
                ex,
                "Failed to query pair wardrobe state for {FriendCode}",
                targetFriendCode
            );
            return new ActionResult<QueryPairWardrobeStateResponse>(
                ActionResultEc.Unknown,
                default
            );
        }
    }

    public async Task<ActionResult<QueryPairWardrobeResponse>> QueryPairWardrobeAsync(
        string targetFriendCode
    )
    {
        try
        {
            var request = new QueryPairWardrobeRequest(targetFriendCode);
            var response = await _network
                .InvokeAsync<ActionResult<QueryPairWardrobeResponse>>(
                    HubMethod.QueryPairWardrobe,
                    request
                )
                .ConfigureAwait(false);
            return response;
        }
        catch (Exception ex)
        {
            Plugin.Log.Error(
                ex,
                "Failed to query pair wardrobe for {FriendCode}",
                targetFriendCode
            );
            return new ActionResult<QueryPairWardrobeResponse>(ActionResultEc.Unknown, default);
        }
    }

    public async Task<ActionResultEc> LockPairLayer(string targetFriendCode, LockInfoDto lockInfo)
    {
        try
        {
            var request = new PairApplyLockRequest(targetFriendCode, lockInfo);

            var response = await _network
                .InvokeAsync<ActionResultEc>(HubMethod.InteractionApplyLock, request)
                .ConfigureAwait(false);

            return response;
        }
        catch (Exception ex)
        {
            Plugin.Log.Error(ex, "Failed to lock pair layer for {FriendCode}", targetFriendCode);
            return ActionResultEc.Unknown;
        }
    }

    public async Task<ActionResultEc> UnlockPairLock(
        string targetFriendCode,
        LockKind lockId,
        string? password
    )
    {
        try
        {
            var request = new PairRemoveLockRequest(targetFriendCode, lockId, password);

            var response = await _network
                .InvokeAsync<ActionResultEc>(HubMethod.InteractionRemoveLock, request)
                .ConfigureAwait(false);

            return response;
        }
        catch (Exception ex)
        {
            Plugin.Log.Error(ex, "Failed to unlock pair layer for {FriendCode}", targetFriendCode);
            return ActionResultEc.Unknown;
        }
    }

    public async Task<ActionResultEc> ApplyPairWardrobeLayer(
        string targetFriendCode,
        WardrobeLayer layer,
        Guid wardrobeId
    )
    {
        try
        {
            var request = new ApplyWardrobeRequest(targetFriendCode, layer, wardrobeId);

            var response = await _network
                .InvokeAsync<ActionResultEc>(HubMethod.InteractionApplyWardrobe, request)
                .ConfigureAwait(false);

            return response;
        }
        catch (Exception ex)
        {
            Plugin.Log.Error(
                ex,
                "Failed to apply pair wardrobe layer for {FriendCode}",
                targetFriendCode
            );
            return ActionResultEc.Unknown;
        }
    }

    public async Task<ActionResultEc> RemovePairWardrobeLayer(
        string targetFriendCode,
        WardrobeLayer layer
    )
    {
        try
        {
            var request = new RemoveWardrobeRequest(targetFriendCode, layer);

            var response = await _network
                .InvokeAsync<ActionResultEc>(HubMethod.InteractionRemoveWardrobe, request)
                .ConfigureAwait(false);

            return response;
        }
        catch (Exception ex)
        {
            Plugin.Log.Error(
                ex,
                "Failed to remove pair wardrobe layer for {FriendCode}",
                targetFriendCode
            );
            return ActionResultEc.Unknown;
        }
    }

    public async Task<ActionResult<AddLockResponse>> AddSelfLockAsync(LockInfoDto lockInfo)
    {
        try
        {
            var request = new AddLockRequest(lockInfo);
            var response = await _network
                .InvokeAsync<ActionResult<AddLockResponse>>(HubMethod.AddLock, request)
                .ConfigureAwait(false);
            return response ?? new ActionResult<AddLockResponse>(ActionResultEc.Unknown, null);
        }
        catch (Exception ex)
        {
            Plugin.Log.Error(ex, "Failed to add self lock");
            return new ActionResult<AddLockResponse>(ActionResultEc.Unknown, null);
        }
    }

    public async Task<ActionResult<RemoveLockResponse>> RemoveSelfLockAsync(
        LockKind lockId,
        string lockeeUid
    )
    {
        try
        {
            var request = new RemoveLockRequest(lockId, lockeeUid);
            var response = await _network
                .InvokeAsync<ActionResult<RemoveLockResponse>>(HubMethod.RemoveLock, request)
                .ConfigureAwait(false);
            return response ?? new ActionResult<RemoveLockResponse>(ActionResultEc.Unknown, null);
        }
        catch (Exception ex)
        {
            Plugin.Log.Error(ex, "Failed to remove self lock");
            return new ActionResult<RemoveLockResponse>(ActionResultEc.Unknown, null);
        }
    }

    public void Dispose() { }
}
