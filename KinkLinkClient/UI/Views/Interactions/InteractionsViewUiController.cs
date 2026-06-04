using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KinkLinkClient.Domain;
using KinkLinkClient.Managers;
using KinkLinkClient.Services;
using KinkLinkCommon.Dependencies.Glamourer;
using KinkLinkCommon.Domain;
using KinkLinkCommon.Domain.Enums;
using KinkLinkCommon.Domain.Enums.Permissions;
using KinkLinkCommon.Domain.Network.PairInteractions;
using KinkLinkCommon.Domain.Wardrobe;

namespace KinkLinkClient.UI.Views.Interactions;

public class InteractionsViewUiController : IDisposable
{
    public bool Busy
    {
        get { return _busy; }
    }
    public Friend? SelectedFriend = null;

    public int SelectedBaseSetIndice = 0;
    public Dictionary<WardrobeLayer, int> SelectedWardrobeIndices = new()
    {
        { WardrobeLayer.Outfit, 0 },
        { WardrobeLayer.Head, 0 },
        { WardrobeLayer.Chest, 0 },
        { WardrobeLayer.Hands, 0 },
        { WardrobeLayer.Legs, 0 },
        { WardrobeLayer.Feet, 0 },
        { WardrobeLayer.Ears, 0 },
        { WardrobeLayer.Neck, 0 },
        { WardrobeLayer.Wrists, 0 },
        { WardrobeLayer.RFinger, 0 },
        { WardrobeLayer.LFinger, 0 },
    };

    public Dictionary<WardrobeLayer, List<LightWardrobeItemDto>> PairLayers = new();

    // Dedicated to the timer settings and creation
    public RelationshipPriority LockPriority;
    public bool CanSelfUnlock;
    public bool UseTimer;
    public TimeSpan Expires;
    public bool UsePassword;
    public string Password = string.Empty;

    private readonly NetworkService _network;
    private readonly WorldService _world;
    private readonly SelectionManager _selectionManager;
    private readonly WardrobeNetworkService _wardrobeNetworkService;
    private readonly LockService _lockService;
    private readonly ClientCharacterStateService _characterState;
    private bool _busy = false;

    public InteractionsViewUiController(
        NetworkService network,
        WorldService world,
        SelectionManager selectionManager,
        LockService lockService,
        WardrobeNetworkService wardrobeNetworkService,
        ClientCharacterStateService stateService
    )
    {
        _network = network;
        _world = world;
        _selectionManager = selectionManager;
        _lockService = lockService;
        _wardrobeNetworkService = wardrobeNetworkService;
        _characterState = stateService;

        _selectionManager.FriendSelected += OnFriendSelected;
        _selectionManager.FriendsDeselected += OnFriendsDeselected;
    }

    private void OnFriendSelected(object? sender, Friend friend)
    {
        SelectedFriend = friend;
        QueryPairWardrobeAsync(friend);
    }

    private void OnFriendsDeselected(object? sender, HashSet<Friend> friends)
    {
        SelectedFriend = null;
        PairLayers = new();
    }

    // TODO: Evaluate if needed, if not delete
    // public async void QueryPairStateAsync(Friend friend)
    // {
    //     await _pairsController.QueryPairStateAsync(friend);
    // }

    public async void QueryPairWardrobeAsync(Friend friend)
    {
        try
        {
            var result = await _wardrobeNetworkService.QueryPairWardrobe(friend.FriendCode);
            this.PairLayers.Clear();

            foreach (var item in result)
            {
                if (!this.PairLayers.ContainsKey(item.Layer))
                    this.PairLayers[item.Layer] = new List<LightWardrobeItemDto>();
                this.PairLayers[item.Layer].Add(item);
            }

            foreach (var slot in this.SelectedWardrobeIndices.Keys.ToList())
            {
                var currentItem = friend.WardrobeState?.Layers?.GetValueOrDefault(slot);
                if (currentItem != null && this.PairLayers.TryGetValue(slot, out var items))
                {
                    var itemIndex = items.FindIndex(i => i.Id == currentItem.Id);
                    if (itemIndex >= 0)
                        this.SelectedWardrobeIndices[slot] = itemIndex + 1;
                }
            }
        }
        catch (Exception ex)
        {
            Plugin.Log.Error(
                ex,
                "QueryPairWardrobeAsync exception for {FriendCode}",
                friend.FriendCode
            );
        }
    }

    // public async void RefreshSelectedFriendAsync()
    // {
    //     return _pairsController.RefreshSelectedFriendAsync();
    // }

    public async Task ApplyLayerAsync(WardrobeLayer layer, int itemIndex)
    {
        if (SelectedFriend == null)
            return;

        if (itemIndex == 0)
        {
            await _characterState.RemovePairWardrobeLayer(SelectedFriend.FriendCode, layer);
            return;
        }

        if (!PairLayers.TryGetValue(layer, out var items))
            return;

        var actualIndex = itemIndex - 1;
        if (actualIndex < 0 || actualIndex >= items.Count)
            return;

        var item = items[actualIndex];
        var applyItem = new WardrobeDto(
            item.Id,
            item.Name,
            item.Description,
            item.Layer,
            string.Empty,
            item.Priority
        );

        await _characterState.ApplyPairWardrobeLayer(
            SelectedFriend.FriendCode,
            layer,
            applyItem.Id
        );
    }

    public async Task LockSlotAsync(WardrobeLayer wardrobeLayer)
    {
        if (SelectedFriend == null)
            return;

        DateTime? expires = UseTimer ? DateTime.UtcNow.Add(Expires) : null;
        string? password = UsePassword ? Password : null;
        var lockInfo = new LockInfoDto
        {
            LockID = LockKindExtensions.From(wardrobeLayer),
            CanSelfUnlock = CanSelfUnlock,
            Expires = expires,
            Password = password,
        };
        await _characterState.LockPairLayer(SelectedFriend.FriendCode, lockInfo);
    }

    public async Task UnlockSlotAsync(WardrobeLayer layer)
    {
        if (SelectedFriend == null)
            return;

        await _characterState.UnlockPairLock(SelectedFriend.FriendCode, LockKindExtensions.From(layer), null);
    }

    public LockInfoDto? GetSlotLock(LockKind lockId)
    {
        if (this.SelectedFriend is { } friend && friend.WardrobeState is { } state)
        {
            foreach (var kv in state.Layers)
            {
                if (kv.Value?.LockId is { } lid && lid.LockID == lockId)
                    return lid;
            }
        }
        return null;
    }

    public LockKind? GetBaseSetLockId()
    {
        // BaseSet not tracked in pair wardrobe state
        return null;
    }

    public LockKind? GetEquipmentLockId(WardrobeLayer layer)
    {
        if (this.SelectedFriend is { } friend && friend.WardrobeState is { } state)
        {
            return state.Layers.TryGetValue(layer, out var item) ? item.LockId?.LockID : null;
        }
        return null;
    }

    // Compatibility aliases used by UI
    public Dictionary<WardrobeLayer, List<LightWardrobeItemDto>> PairLayerOptions =>
        this.PairLayers;

    public Task ApplySlotItemAsync(WardrobeLayer layer, int itemIndex)
    {
        return ApplyLayerAsync(layer, itemIndex);
    }

    public void Dispose()
    {
        _selectionManager.FriendSelected -= OnFriendSelected;
        _selectionManager.FriendsDeselected -= OnFriendsDeselected;
        GC.SuppressFinalize(this);
    }
}
