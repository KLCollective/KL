using System;
using System.Threading.Tasks;
using KinkLinkCommon.Domain;
using KinkLinkCommon.Domain.Enums;
using KinkLinkCommon.Domain.Network;
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

    public async Task<ActionResultEc> LockPairLayer(
        string targetFriendCode,
        WardrobeLayer layer,
        LockInfoDto lockInfo
    )
    {
        try
        {
            var dto = new LightWardrobeItemDto(
                Guid.Empty,
                string.Empty,
                string.Empty,
                layer,
                RelationshipPriority.Casual,
                lockInfo
            );

            var response = await _network
                .InvokeAsync<ActionResultEc>(
                    HubMethod.InteractionApplyWardrobe,
                    new object[] { targetFriendCode, dto }
                )
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
        string lockId,
        string? password
    )
    {
        try
        {
            // TODO: Implement this flow as follows
            // var lockInfo = get current lock for the wardrobe id.
            // Check if able to unlock based on permissions
            var response = await _network
                .InvokeAsync<ActionResultEc>(
                    HubMethod.InteractionRemoveLock,
                    new
                    {
                        targetFriendCode,
                        lockId,
                        password,
                    }
                )
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
            var dto = new LightWardrobeItemDto(
                wardrobeId,
                string.Empty,
                string.Empty,
                layer,
                RelationshipPriority.Casual,
                null
            );

            var response = await _network
                .InvokeAsync<ActionResultEc>(
                    HubMethod.InteractionApplyWardrobe,
                    new object[] { targetFriendCode, dto }
                )
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
            var response = await _network
                .InvokeAsync<ActionResultEc>(
                    HubMethod.InteractionRemoveWardrobe,
                    new object[] { targetFriendCode, layer }
                )
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

    public void Dispose() { }
}
