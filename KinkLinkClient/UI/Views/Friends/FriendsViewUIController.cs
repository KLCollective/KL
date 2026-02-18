using System;
using System.Linq;
using System.Threading.Tasks;
using KinkLinkClient.Domain;
using KinkLinkClient.Managers;
using KinkLinkClient.Services;
using KinkLinkClient.UI.Components.Friends;
using KinkLinkClient.Utils;
using KinkLinkCommon.Domain.Network;
using KinkLinkCommon.Domain.Network.RemoveFriend;
using KinkLinkCommon.Domain.Network.UpdateFriend;

namespace KinkLinkClient.UI.Views.Friends;

/// <summary>
///     Handles events and other tasks for <see cref="PairsViewUi" />
/// </summary>
public class PairsViewUiController : IDisposable
{
    public enum SubView
    {
        PairPerms,
        DefaultPerms,
        ViewPairPerms,
    }

    // Injected
    private readonly IdentityService _identityService;
    private readonly FriendsListService _friendsListService;
    private readonly NetworkService _networkService;
    private readonly SelectionManager _selectionManager;

    // Instantiated
    private Friend? _friendBeingEdited;
    private PermissionsUiState _permissionsToBeGrantedToFriendOriginal = new();
    private PermissionsUiState _permissionsGrantedByFriendOriginal = new();

    // TODO: Pull this from local config
    private PermissionsUiState _defaultPermissionsOriginal = new();

    /// <summary>
    ///     Friend Code is display
    /// </summary>
    public string FriendCode = string.Empty;

    public bool PairSelected => _friendBeingEdited != null;

    /// <summary>
    ///     Note to display
    /// </summary>
    public string Note = string.Empty;

    /// <summary>
    ///     The current friend whose permissions you are editing
    /// </summary>
    public PermissionsUiState EditingPermissions = new();

    /// <summary>
    ///     <inheritdoc cref="PairsViewUiController" />
    /// </summary>
    public PairsViewUiController(FriendsListService friendsListService, NetworkService networkService, SelectionManager selectionManager)
    {
        _identityService = identityService;
        _friendsListService = friendsListService;
        _networkService = networkService;
        _selectionManager = selectionManager;

        _selectionManager.FriendSelected += OnSelectedChangedEvent;
    }

    public void RefreshViewData()
    {
        switch (View)
        {
            case SubView.PairPerms:
                // Ensure that it is looking at the pair perms
                EditingPermissions = _permissionsToBeGrantedToFriendOriginal;
                break;
            case SubView.DefaultPerms:
                EditingPermissions = _defaultPermissionsOriginal;
                // Swap to default permissions
                break;
            case SubView.ViewPairPerms:
                // Ensure that it is looking at the to viewperms
                EditingPermissions = _permissionsGrantedByFriendOriginal;
                break;
        }
    }

    private async Task SaveDefaultPermissions()
    {
        if (Plugin.CharacterConfiguration == null)
        {
            NotificationHelper.Error("Error", "CharacterConfiguration is null somehow!");
            return;
        }
        NotificationHelper.Info("Saved Defaults!", "Default configuration saved to {}");
        _defaultPermissionsOriginal = EditingPermissions;
        Plugin.CharacterConfiguration.DefaultPermissions = PermissionsUiState.To(
            EditingPermissions
        );
        await Plugin.CharacterConfiguration.Save();
    }

    /// <summary>
    ///     Handles the Save Button from the UI based on what has changed.
    /// </summary>
    public async Task Save()
    {
        try
        {
            // Update the default permissions if needed
            if (View == SubView.DefaultPerms)
            {
                await SaveDefaultPermissions();
                return;
            }

            if (_friendBeingEdited is null)
                return;

            if (Note == string.Empty)
                Plugin.Configuration.Notes.Remove(FriendCode);
            else
                Plugin.Configuration.Notes[FriendCode] = Note;

            await Plugin.Configuration.Save();

            if (PendingChanges() is false)
                return;

            var permissions = PermissionsUiState.To(EditingPermissions);

            var request = new UpdateFriendRequest(FriendCode, permissions);
            var response = await _networkService.InvokeAsync<UpdateFriendResponse>(
                HubMethod.UpdateFriend,
                request
            );
            if (response.Result is UpdateFriendEc.Success)
            {
                _friendBeingEdited.Note = Note == string.Empty ? null : Note;
                _friendBeingEdited.PermissionsGrantedToFriend = permissions;
                _permissionsToBeGrantedToFriendOriginal = PermissionsUiState.From(permissions);

                NotificationHelper.Success("Successfully saved friend permissions", string.Empty);
            }
            else
            {
                NotificationHelper.Warning("Unable to save friend", string.Empty);
            }
        }
        catch (Exception e)
        {
            Plugin.Log.Warning($"Unable to save friend, {e.Message}");
        }
    }

    /// <summary>
    ///     Handles the Delete Button from the UI
    /// </summary>
    public async void Delete()
    {
        try
        {
            if (_friendBeingEdited is null)
                return;

            var request = new RemoveFriendRequest(FriendCode);
            var response = await _networkService.InvokeAsync<RemovePair>(
                HubMethod.RemoveFriend,
                request
            );
            if (response.Result is RemovePairEc.Success)
            {
                _friendsListService.Delete(_friendBeingEdited);
                _friendBeingEdited = null;

                NotificationHelper.Success("Successfully deleted friend", string.Empty);
            }
            else
            {
                NotificationHelper.Warning("Unable to delete friend", string.Empty);
            }
        }
        catch (Exception e)
        {
            Plugin.Log.Warning($"Unable to delete friend, {e.Message}");
        }
    }

    /// <summary>
    ///     Checks to see if there are pending changes to the friend you are currently editing
    /// </summary>
    public bool PendingChanges()
    {
        // Default perms can be edited anytime.
        if (
            View == SubView.DefaultPerms
            && EditingPermissions.Equals(_defaultPermissionsOriginal) is false
        )
            return true;

        if (_friendBeingEdited is null)
            return false;

        // It ain't pretty, but it works?
        if (
            View == SubView.PairPerms
            && EditingPermissions.Equals(_permissionsToBeGrantedToFriendOriginal) is false
        )
            return true;
        if (View == SubView.ViewPairPerms)
            return false;

        var note = Note == string.Empty ? null : Note;
        return note != _friendBeingEdited.Note;
    }

    /// <summary>
    ///     Handles event fired from <see cref="FriendsListComponentUiController" />
    /// </summary>
    private void OnSelectedChangedEvent(object? sender, Friend _)
    {
        if (_selectionManager.Selected.FirstOrDefault() is not { } friend)
            return;

        _friendBeingEdited = friend;
        _permissionsToBeGrantedToFriendOriginal = PermissionsUiState.From(
            friend.PermissionsGrantedToFriend
        );
        _permissionsGrantedByFriendOriginal = PermissionsUiState.From(
            friend.PermissionsGrantedByFriend
        );

        FriendCode = friend.FriendCode;
        Note = friend.Note ?? string.Empty;
        EditingPermissions = PermissionsUiState.From(_friendBeingEdited.PermissionsGrantedToFriend);
    }

    public void Dispose()
    {
        _selectionManager.FriendSelected -= OnSelectedChangedEvent;
        GC.SuppressFinalize(this);
    }
}
