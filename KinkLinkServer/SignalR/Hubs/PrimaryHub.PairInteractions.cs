using System;
using System.Diagnostics;
using KinkLinkCommon.Domain;
using KinkLinkCommon.Domain.CharacterState;
using KinkLinkCommon.Domain.Enums;
using KinkLinkCommon.Domain.Network;
using KinkLinkCommon.Domain.Network.PairInteractions;
using Microsoft.AspNetCore.SignalR;

namespace KinkLinkServer.SignalR.Hubs;

public partial class PrimaryHub
{
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
    public async Task<ActionResult<QueryPairStateResponse>> QueryPairState(
        QueryPairStateRequest request
    )
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            logger.LogTrace("[SignalR] QueryPairState: {FriendCode} -> {Target}", FriendCode, request.TargetFriendCode);
            if (isValidPair<QueryPairStateResponse>(FriendCode, request.TargetFriendCode) is { } result)
            {
                return result;
            }
            return await _pairInteractionsHandler.QueryPairState(FriendCode, request);
        }
        finally
        {
            stopwatch.Stop();
            metricsService.IncrementSignalRMessage("QueryPairState", true);
            metricsService.RecordSignalRMessageDuration("QueryPairState", stopwatch.ElapsedMilliseconds);
        }
    }

    [HubMethodName(HubMethod.QueryPairWardrobeState)]
    public async Task<ActionResult<QueryPairWardrobeStateResponse>> QueryWardrobeState(
        QueryPairWardrobeStateRequest request
    )
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            logger.LogTrace("[SignalR] QueryWardrobeState: {FriendCode} -> {Target}", FriendCode, request.TargetFriendCode);
            if (
                isValidPair<QueryPairWardrobeStateResponse>(FriendCode, request.TargetFriendCode) is
                { } result
            )
            {
                return result;
            }
            return await _pairInteractionsHandler.QueryWardrobeStateAsync(FriendCode, request);
        }
        finally
        {
            stopwatch.Stop();
            metricsService.IncrementSignalRMessage("QueryPairWardrobeState", true);
            metricsService.RecordSignalRMessageDuration("QueryPairWardrobeState", stopwatch.ElapsedMilliseconds);
        }
    }

    [HubMethodName(HubMethod.QueryPairWardrobe)]
    public async Task<ActionResult<QueryPairWardrobeResponse>> QueryWardrobe(
        QueryPairWardrobeRequest request
    )
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            logger.LogTrace("[SignalR] QueryWardrobe: {FriendCode} -> {Target}", FriendCode, request.TargetFriendCode);
            if (
                isValidPair<QueryPairWardrobeResponse>(FriendCode, request.TargetFriendCode) is
                { } result
            )
            {
                return result;
            }
            return await _pairInteractionsHandler.QueryWardrobeAsync(FriendCode, request);
        }
        finally
        {
            stopwatch.Stop();
            metricsService.IncrementSignalRMessage("QueryPairWardrobe", true);
            metricsService.RecordSignalRMessageDuration("QueryPairWardrobe", stopwatch.ElapsedMilliseconds);
        }
    }

    [HubMethodName(HubMethod.ApplyInteraction)]
    public async Task<ActionResult<Unit>> ApplyInteraction(ApplyInteractionCommand command)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            logger.LogInformation("[SignalR] ApplyInteraction: {FriendCode} -> {Target}, Action: {Action}",
                FriendCode, command.TargetFriendCode, command.Action);
            if (isValidPair<Unit>(FriendCode, command.TargetFriendCode) is { } result)
            {
                return result;
            }
            return await _pairInteractionsHandler.ApplyInteraction(FriendCode, command, Clients);
        }
        finally
        {
            stopwatch.Stop();
            metricsService.IncrementSignalRMessage("ApplyInteraction", true);
            metricsService.RecordSignalRMessageDuration("ApplyInteraction", stopwatch.ElapsedMilliseconds);
        }
    }
}