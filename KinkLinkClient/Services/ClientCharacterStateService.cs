using System;
using System.Threading.Tasks;
using KinkLinkClient.Services;
using KinkLinkCommon.Domain;
using KinkLinkCommon.Domain.CharacterState;
using KinkLinkCommon.Domain.Enums;
using KinkLinkCommon.Domain.Enums.Permissions;
using KinkLinkCommon.Domain.Network;
using KinkLinkCommon.Domain.Network.PairInteractions;
using KinkLinkCommon.Domain.Wardrobe;

namespace KinkLinkClient.Services;

public class ClientCharacterStateService : IDisposable
{
    private readonly NetworkService _network;

    public ClientCharacterStateService(NetworkService network)
    {
        _network = network;
    }

    public void UpdateLocalState(QueryPairStateResponse state) { }

    public QueryPairStateResponse? GetLocalState() => null;

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

    public async Task<ActionResult<Unit>> ApplyInteractionAsync(
        string targetFriendCode,
        PairAction action,
        InteractionPayload? payload
    )
    {
        try
        {
            var command = new ApplyInteractionCommand(targetFriendCode, action, payload);
            var response = await _network
                .InvokeAsync<ActionResult<Unit>>(HubMethod.ApplyInteraction, command)
                .ConfigureAwait(false);

            return response;
        }
        catch (Exception ex)
        {
            Plugin.Log.Error(ex, "Failed to apply interaction to {FriendCode}", targetFriendCode);
            return new ActionResult<Unit>(ActionResultEc.Unknown, Unit.Empty);
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

    public void Dispose() { }
}
