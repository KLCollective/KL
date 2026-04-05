using System;
using System.Collections.Generic;
using KinkLinkClient.Domain;

namespace KinkLinkClient.Services;

public class FriendsListService
{
    public IReadOnlyList<Friend> Friends => _friends;
    private readonly List<Friend> _friends = [];

    public event EventHandler<Friend>? FriendAdded;
    public event EventHandler<Friend>? FriendDeleted;
    public event EventHandler? FriendsListCleared;
    public event EventHandler<Friend>? FriendStateUpdated;

    public Friend? Get(string friendCode)
    {
        foreach (var friend in _friends)
            if (friend.FriendCode == friendCode)
                return friend;

        return null;
    }

    public bool Contains(string friendCode)
    {
        foreach (var friend in _friends)
            if (friend.FriendCode == friendCode)
                return true;

        return false;
    }

    public void Add(Friend friend)
    {
        _friends.Add(friend);
        FriendAdded?.Invoke(this, friend);
    }

    public void Delete(Friend friend)
    {
        _friends.Remove(friend);
        FriendDeleted?.Invoke(this, friend);
    }

    public void Clear()
    {
        _friends.Clear();
        FriendsListCleared?.Invoke(this, EventArgs.Empty);
    }

    public void UpdateFriendState(InteractionContext state)
    {
        var friend = Get(state.FriendCode);
        if (friend == null)
            return;

        friend.InteractionState = state;
        FriendStateUpdated?.Invoke(this, friend);
    }

    public void ClearFriendState(string friendCode)
    {
        var friend = Get(friendCode);
        if (friend == null)
            return;

        friend.InteractionState = null;
        FriendStateUpdated?.Invoke(this, friend);
    }
}
