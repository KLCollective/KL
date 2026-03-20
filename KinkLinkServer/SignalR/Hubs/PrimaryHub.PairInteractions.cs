using System;
using KinkLinkCommon.Domain;
using KinkLinkCommon.Domain.CharacterState;
using KinkLinkCommon.Domain.Enums;
using KinkLinkCommon.Domain.Network;
using KinkLinkCommon.Domain.Network.PairInteractions;
using Microsoft.AspNetCore.SignalR;

namespace KinkLinkServer.SignalR.Hubs;

public partial class PrimaryHub
{
    public void PushMyState(CharacterStateDto state)
    {
        _pairInteractionsHandler.PushMyState(FriendCode, state);
    }

    private ActionResult<T>? isValidPair<T>(string sender, string target)
    {
        if (string.IsNullOrWhiteSpace(target))
        {
            return ActionResultBuilder.Fail<T>(ActionResultEc.ClientBadData);
        }

        if (sender == target)
        {
            return ActionResultBuilder.Fail<T>(ActionResultEc.ClientBadData);
        }

        return null;
    }

    [HubMethodName(HubMethod.QueryPairState)]
    public async Task<ActionResult<QueryPairStateResponse>> QueryPairState(QueryPairStateRequest request)
    {
        if (
            isValidPair<QueryPairStateResponse>(FriendCode, request.TargetFriendCode) is
            { } result
        )
        {
            return result;
        }
        return await _pairInteractionsHandler.QueryPairState(FriendCode, request);
    }

    [HubMethodName(HubMethod.QueryPairWardrobeState)]
    public async Task<ActionResult<QueryPairWardrobeStateResponse>> QueryWardrobeState(
        QueryPairWardrobeStateRequest request
    )
    {
        if (
            isValidPair<QueryPairWardrobeStateResponse>(FriendCode, request.TargetFriendCode) is
            { } result
        )
        {
            return result;
        }
        return await _pairInteractionsHandler.QueryWardrobeStateAsync(FriendCode, request);
    }

    [HubMethodName(HubMethod.QueryPairWardrobe)]
    public async Task<ActionResult<QueryPairWardrobeResponse>> QueryWardrobe(
        QueryPairWardrobeRequest request
    )
    {
        if (
            isValidPair<QueryPairWardrobeResponse>(FriendCode, request.TargetFriendCode) is
            { } result
        )
        {
            return result;
        }
        return await _pairInteractionsHandler.QueryWardrobeAsync(FriendCode, request);
    }

    [HubMethodName(HubMethod.ApplyInteraction)]
    public async Task<ActionResult<Unit>> ApplyInteraction(ApplyInteractionCommand command)
    {
        if (isValidPair<Unit>(FriendCode, command.SenderFriendCode) is { } result)
        {
            return result;
        }
        return await _pairInteractionsHandler.ApplyInteraction(FriendCode, command, Clients);
    }
}
