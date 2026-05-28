using System;
using System.Collections.Generic;
using KinkLinkClient.Domain;
using KinkLinkClient.Handlers.Network;
using KinkLinkClient.Services;
using KinkLinkClient.Utils;
using KinkLinkCommon.Dependencies.Glamourer;
using KinkLinkCommon.Domain.Enums;
using KinkLinkCommon.Domain.Network;
using KinkLinkCommon.Domain.Network.PairInteractions;
using KinkLinkCommon.Domain.Network.SyncPairState;
using KinkLinkCommon.Domain;
using Microsoft.AspNetCore.SignalR.Client;

namespace KinkLinkClient.Handlers.Network;

public class SyncPairStateHandler : IDisposable
{
    private readonly FriendsListService _friendsList;
    private readonly IDisposable _handler;

    public SyncPairStateHandler(FriendsListService friendsList, NetworkService network)
    {
        _friendsList = friendsList;
        _handler = network.Connection.On<SyncPairStateCommand>(HubMethod.SyncPairState, Handle);
    }

    private void Handle(SyncPairStateCommand response)
    {
        try
        {
            var friend = _friendsList.Get(response.TargetFriendCode);
            if (friend == null)
            {
                friend = new Friend(
                    response.TargetFriendCode,
                    FriendOnlineStatus.Online,
                    permissionsGrantedByFriend: response.GrantedTo
                );
                _friendsList.Add(friend);
            }
            else
            {
                friend.PermissionsGrantedByFriend = response.GrantedTo;
            }

            var pairState = new QueryPairStateResponse(
                response.TargetFriendCode,
                response.GrantedTo,
                response.WardrobeState,
                response.LockStates
            );

            _friendsList.UpdateFriendWardrobeState(response.TargetFriendCode, response.WardrobeState);

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
