using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using KinkLinkClient.Domain;
using KinkLinkClient.Services;
using KinkLinkCommon.Domain;
using KinkLinkCommon.Domain.Enums;
using KinkLinkCommon.Domain.Network;
using KinkLinkCommon.Domain.Network.GetAccountData;
using KinkLinkCommon.Domain.Network.Locks;
using KinkLinkCommon.Domain.Network.PairInteractions;

namespace KinkLinkClient.Managers;

/// <summary>
///     Manages connection and disconnection events from the server
/// </summary>
public class ConnectionManager : IDisposable
{
    private readonly FriendsListService _friendsListService;
    private readonly IdentityService _identityService;
    private readonly LockService _lockService;
    private readonly NetworkService _networkService;
    private readonly ViewService _viewService;
    private readonly WardrobeManager _wardrobeManager;

    /// <summary>
    ///     <inheritdoc cref="ConnectionManager"/>
    /// </summary>
    public ConnectionManager(
        FriendsListService friendsListService,
        IdentityService identityService,
        LockService lockService,
        NetworkService networkService,
        ViewService viewService,
        WardrobeManager wardrobeManager
    )
    {
        _friendsListService = friendsListService;
        _identityService = identityService;
        _lockService = lockService;
        _networkService = networkService;
        _viewService = viewService;
        _wardrobeManager = wardrobeManager;

        _networkService.Connected += OnConnected;
        _networkService.Disconnected += OnDisconnected;
    }

    private async Task OnConnected()
    {
        if (Plugin.CharacterConfiguration is not { } character)
            return;

        // Get account data from the server
        var request = new GetAccountDataRequest(character.Name, character.World);
        var response = await _networkService
            .InvokeAsync<GetAccountDataResponse>(HubMethod.GetAccountData, request)
            .ConfigureAwait(false);

        // If there wasn't a success, don't stay connected; the plugin is not usable in this state
        if (response.Result is not GetAccountDataEc.Success)
        {
            Plugin.Log.Fatal($"[ConnectionManager] Failed to get account data {response.Result}");
            await _networkService.StopAsync().ConfigureAwait(false);
            return;
        }

        // Set the friend code
        _identityService.SetFriendCode(response.FriendCode);

        // Clear the friend list in preparation for adding friends returned from the server
        _friendsListService.Clear();

        // Iterate over all the relationships to transform them into domain models
        foreach (var relationship in response.Relationships)
        {
            // Try to extract the note
            Plugin.Configuration.Notes.TryGetValue(relationship.TargetFriendCode, out var note);

            var pairState = response.PairStates.TryGetValue(relationship.TargetFriendCode, out var ps)
                ? ps
                : null;

            // Add the new friend with all the data required
            var friend = new Friend(
                relationship.TargetFriendCode,
                relationship.Status,
                note,
                relationship.PermissionsGrantedTo,
                relationship.PermissionsGrantedBy
            );
            _friendsListService.Add(friend);

            if (pairState != null)
            {
                _friendsListService.UpdateFriendWardrobeState(relationship.TargetFriendCode, pairState.WardrobeState);
            }
        }

        // Set the view to the 'home screen'
        _viewService.CurrentView = View.Status;

        // Sync wardrobe from server
        await _wardrobeManager.SyncFromServerAsync().ConfigureAwait(false);

        // Sync locks from server
        var locksResult = await _networkService
            .InvokeAsync<ActionResult<SyncLocksResponse>>(HubMethod.SyncLocks)
            .ConfigureAwait(false);
        if (locksResult.Result == ActionResultEc.Success && locksResult.Value != null)
        {
            _lockService.SyncLocks(locksResult.Value.Locks);
        }
    }

    private Task OnDisconnected()
    {
        // Clear the friend list
        _friendsListService.Clear();
        _identityService.ClearFriendCode();
        // Reset the view if required
        _viewService.ResetView();
        _wardrobeManager.ClearActive();
        // Return
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _networkService.Connected -= OnConnected;
        _networkService.Disconnected -= OnDisconnected;
        GC.SuppressFinalize(this);
    }
}
