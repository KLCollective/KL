using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using KinkLinkClient.Domain;
using KinkLinkClient.Managers;
using KinkLinkClient.Services;
using KinkLinkClient.UI.Views.Pairs;
using KinkLinkClient.Utils;
using KinkLinkCommon.Domain.Enums.Permissions;
using KinkLinkCommon.Domain.Network;
using KinkLinkCommon.Domain.Network.PairInteractions;

namespace KinkLinkClient.UI.Views.Interactions;

public class InteractionsViewUiController : IDisposable
{
    public List<PairInteractionState> PairStates => _pairsController.PairStates;
    public PairInteractionState? SelectedPair => _pairsController.SelectedPair;

    // Need to have the pair's active state as well as the pair's full server side wardrobe information.
    // This should be directly queriable from the back and stored as a result of the friend being selected.
    // It should be cleared when they are deselected.

    private readonly NetworkService _network;
    private readonly WorldService _world;
    private readonly PairsInteractionUiController _pairsController;
    private readonly SelectionManager _selectionManager;
    private bool _busy = false;

    public InteractionsViewUiController(
        NetworkService network,
        WorldService world,
        PairsInteractionUiController pairsController,
        SelectionManager selectionManager
    )
    {
        _network = network;
        _world = world;
        _pairsController = pairsController;
        _selectionManager = selectionManager;

        _selectionManager.FriendSelected += OnFriendSelected;
        _selectionManager.FriendsDeselected += OnFriendsDeselected;
    }

    private void OnFriendSelected(object? sender, Friend friend)
    {
        var pair = PairStates.Find(p => p.FriendCode == friend.FriendCode);
        if (pair != null)
        {
            _pairsController.SelectPair(pair);
        }
    }

    private void OnFriendsDeselected(object? sender, HashSet<Friend> friends) { }

    public void RefreshPairs()
    {
        _pairsController.RefreshPairs();
    }

    public void SelectPair(PairInteractionState pair)
    {
        _pairsController.SelectPair(pair);
    }

    public Task QueryPairStateAsync(PairInteractionState pair)
    {
        return _pairsController.QueryPairStateAsync(pair);
    }

    public Task QueryPairWardrobeAsync(PairInteractionState pair)
    {
        return _pairsController.QueryPairWardrobeAsync(pair);
    }

    public Task RefreshSelectedPairAsync()
    {
        return _pairsController.RefreshSelectedPairAsync();
    }

    public async Task ApplyInteractionAsync(PairAction action, InteractionPayload? payload)
    {
        await _pairsController.ApplyInteractionAsync(action, payload);
    }

    public void Dispose()
    {
        _selectionManager.FriendSelected -= OnFriendSelected;
        _selectionManager.FriendsDeselected -= OnFriendsDeselected;
        GC.SuppressFinalize(this);
    }
}

