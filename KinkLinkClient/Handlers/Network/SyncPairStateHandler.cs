using System;
using System.Collections.Generic;
using KinkLinkClient.Domain;
using KinkLinkClient.Handlers.Network;
using KinkLinkClient.Services;
using KinkLinkClient.Utils;
using KinkLinkCommon.Dependencies.Glamourer;
using KinkLinkCommon.Domain.Network;
using KinkLinkCommon.Domain.Network.PairInteractions;
using KinkLinkCommon.Domain.Network.SyncPairState;
using Microsoft.AspNetCore.SignalR.Client;

namespace KinkLinkClient.Handlers.Network;

public class SyncPairStateHandler : IDisposable
{
    private readonly FriendsListService _friendsList;
    private readonly IDisposable _handler;

    public SyncPairStateHandler(FriendsListService friendsList, NetworkService network)
    {
        _friendsList = friendsList;
        _handler = network.Connection.On<QueryPairStateResponse>(HubMethod.SyncPairState, Handle);
    }

    private void Handle(QueryPairStateResponse response)
    {
        try
        {
            var friend = _friendsList.Get(response.TargetFriendCode);
            if (friend == null)
            {
                Plugin.Log.Warning(
                    "[SyncPairState] Friend not found: {Friend}",
                    response.TargetFriendCode
                );
                return;
            }

            var updatedState = InteractionContext.FromPairState(response);
            _friendsList.UpdateFriendState(updatedState);

            NotificationHelper.Info(
                "Pair State Synced",
                $"{response.TargetFriendCode}: has {response.LockStates.Count} locks"
            );
        }
        catch (Exception ex)
        {
            Plugin.Log.Error(ex, "[SyncPairState] Failed to handle SyncPairState");
        }
    }

    public void Dispose()
    {
        _handler.Dispose();
        GC.SuppressFinalize(this);
    }
}
