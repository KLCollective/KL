using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using KinkLinkClient.Domain;
using KinkLinkClient.Domain.Enums;
using KinkLinkClient.Domain.Filters;
using KinkLinkClient.Managers;
using KinkLinkClient.Services;
using KinkLinkClient.Utils;
using KinkLinkCommon.Domain.Network;
using KinkLinkCommon.Domain.Network.AddFriend;

namespace KinkLinkClient.UI.Components.Friends;

/// <summary>
///     Handles events and other tasks for <see cref="PairsListComponentUi" />
/// </summary>
public class FriendsListComponentUiController : IDisposable
{
    private readonly FriendsListService _friendsListService;
    private readonly NetworkService _networkService;
    private readonly SelectionManager _selectionManager;

    public readonly FilterFriends Filter;

    public readonly List<Friend> Pending = [];

    public FriendsListComponentUiController(
        FriendsListService friendsListService,
        NetworkService networkService,
        SelectionManager selectionManager
    )
    {
        _friendsListService = friendsListService;
        _networkService = networkService;
        _selectionManager = selectionManager;

        Filter = new FilterFriends(() => _friendsListService.Friends);

        _friendsListService.FriendAdded += OnFriendsListChanged;
        _friendsListService.FriendDeleted += OnFriendsListChanged;
        _friendsListService.FriendsListCleared += OnFriendsListChanged;

        _selectionManager.FriendsInteractedWith += OnFriendsListChanged;
    }

    /// <summary>
    ///     String containing the friend code you intend to add
    /// </summary>
    public string FriendCodeToAdd = string.Empty;

    /// <summary>
    ///     String containing the friend you intend to find
    /// </summary>
    public string SearchText = string.Empty;

    /// <summary>
    ///     Handles adding a friend to your friends list
    /// </summary>
    public async Task Add()
    {
        if (FriendCodeToAdd == string.Empty)
            return;

        // Remove spaces in the beginning or end
        FriendCodeToAdd = FriendCodeToAdd.Trim();
        if (FriendCodeToAdd.Length < 10)
            return;
        // The Friendcode _must_ be exactly _10_ characters
        FriendCodeToAdd =
            FriendCodeToAdd.Length > 10 ? FriendCodeToAdd.Substring(0, 10) : FriendCodeToAdd;

        if (_friendsListService.Contains(FriendCodeToAdd))
        {
            NotificationHelper.Warning(
                "Friend Already Exists",
                "Unable to add friend because friend already exists"
            );
            return;
        }

        var request = new AddFriendRequest(FriendCodeToAdd);
        var response = await _networkService
            .InvokeAsync<AddFriendResponse>(HubMethod.AddFriend, request)
            .ConfigureAwait(false);
        switch (response.Result)
        {
            case PairRequestResult.Success:
            case PairRequestResult.Pending:

                // Try to get the name from our notes just in case they are a previous friend
                Plugin.Configuration.Notes.TryGetValue(request.TargetFriendCode, out var note);

                // Create the new object with notes
                var friend = new Friend(request.TargetFriendCode, response.Status, note);

                // Add to the friends list
                _friendsListService.Add(friend);

                // Add to the list of selected friends to allow for immediate editing
                _selectionManager.Select(friend, false);

                NotificationHelper.Success(
                    "Successfully Added Friend",
                    $"Successfully added {FriendCodeToAdd} as a friend"
                );
                // Reset the UI element
                FriendCodeToAdd = string.Empty;
                break;

            case PairRequestResult.AlreadyFriends:
                // Reset the UI element
                FriendCodeToAdd = string.Empty;
                NotificationHelper.Info("You are already friends", string.Empty);
                break;

            case PairRequestResult.Uninitialized:
            case PairRequestResult.NoSuchFriendCode:
            case PairRequestResult.Unknown:
            default:
                NotificationHelper.Error("Failed to Add Friend", $"{response.Result}");
                break;
        }
    }

    public void ToggleSortMode()
    {
        Filter.SortMode =
            Filter.SortMode is FilterSortMode.Alphabetically
                ? FilterSortMode.Recency
                : FilterSortMode.Alphabetically;
        Filter.Refresh();
    }

    private void OnFriendsListChanged(object? sender, object? _)
    {
        Filter.Refresh();
    }

    public void Dispose()
    {
        _friendsListService.FriendAdded -= OnFriendsListChanged;
        _friendsListService.FriendDeleted -= OnFriendsListChanged;
        _friendsListService.FriendsListCleared -= OnFriendsListChanged;

        _selectionManager.FriendsInteractedWith -= OnFriendsListChanged;

        GC.SuppressFinalize(this);
    }
}
