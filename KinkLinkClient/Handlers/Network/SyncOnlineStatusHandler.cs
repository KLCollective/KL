using System;
using KinkLinkClient.Domain;
using KinkLinkClient.Managers;
using KinkLinkClient.Services;
using KinkLinkCommon.Domain.Enums;
using KinkLinkCommon.Domain.Network;
using KinkLinkCommon.Domain.Network.SyncOnlineStatus;
using Microsoft.AspNetCore.SignalR.Client;

namespace KinkLinkClient.Handlers.Network;

/// <summary>
///     Handles a <see cref="SyncOnlineStatusCommand"/>
/// </summary>
public class SyncOnlineStatusHandler : IDisposable
{
    // Injected
    private readonly FriendsListService _friends;
    private readonly SelectionManager _selection;

    // Instantiated
    private readonly IDisposable _handler;

    /// <summary>
    ///     <inheritdoc cref="SyncOnlineStatusHandler"/>
    /// </summary>
    public SyncOnlineStatusHandler(
        FriendsListService friends,
        NetworkService network,
        SelectionManager selection
    )
    {
        _friends = friends;
        _selection = selection;

        _handler = network.Connection.On<SyncOnlineStatusCommand>(
            HubMethod.SyncOnlineStatus,
            Handle
        );
    }

    /// <summary>
    ///     <inheritdoc cref="SyncOnlineStatusHandler"/>
    /// </summary>
    private void Handle(SyncOnlineStatusCommand action)
    {
        var friend = _friends.Get(action.SenderFriendCode);
        if (friend == null)
        {
            friend = new Friend(
                action.SenderFriendCode,
                action.Status,
                permissionsGrantedByFriend: action.Permissions
            );
            _friends.Add(friend);
            return;
        }

        friend.Status = action.Status;
        friend.PermissionsGrantedByFriend = action.Permissions;

        if (friend.Status is FriendOnlineStatus.Offline)
        {
            _selection.Deselect(friend);
        }
    }

    public void Dispose()
    {
        _handler.Dispose();
        GC.SuppressFinalize(this);
    }
}
