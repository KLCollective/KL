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
    public Dictionary<GlamourerEquipmentSlot, int> SelectedWardrobeIndices = new()
    {
        { GlamourerEquipmentSlot.Head, 0 },
        { GlamourerEquipmentSlot.Hands, 0 },
        { GlamourerEquipmentSlot.Legs, 0 },
        { GlamourerEquipmentSlot.Feet, 0 },
        { GlamourerEquipmentSlot.Ears, 0 },
        { GlamourerEquipmentSlot.Neck, 0 },
        { GlamourerEquipmentSlot.Wrists, 0 },
        { GlamourerEquipmentSlot.RFinger, 0 },
        { GlamourerEquipmentSlot.LFinger, 0 },
    };

    public List<PairWardrobeItemDto> PairsBaseSets = new();
    public Dictionary<GlamourerEquipmentSlot, List<PairWardrobeItemDto>> PairEquipmentSlots = new();

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
        PairsBaseSets = new();
        PairEquipmentSlots = new();
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
            this.PairsBaseSets.Clear();
            this.PairEquipmentSlots.Clear();

            foreach (var item in result)
            {
                if (item.Slot == GlamourerEquipmentSlot.None)
                {
                    this.PairsBaseSets.Add(item);
                }
                else
                {
                    if (!this.PairEquipmentSlots.ContainsKey(item.Slot))
                        this.PairEquipmentSlots[item.Slot] = new List<PairWardrobeItemDto>();
                    this.PairEquipmentSlots[item.Slot].Add(item);
                }
            }

            var currentBaseSetId = friend.InteractionState?.BaseSet?.Id;
            if (currentBaseSetId.HasValue)
            {
                var baseSetIndex = this.PairsBaseSets.FindIndex(b =>
                    b.Id == currentBaseSetId.Value
                );
                this.SelectedBaseSetIndice = baseSetIndex + 1;
            }

            foreach (var slot in this.SelectedWardrobeIndices.Keys.ToList())
            {
                var currentItem = friend.InteractionState?.WardrobeSlots?.GetValueOrDefault(slot);
                if (currentItem != null && this.PairEquipmentSlots.TryGetValue(slot, out var items))
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

    public async Task ApplyInteractionAsync(PairAction action, InteractionPayload? payload)
    {
        if (SelectedFriend == null || _busy)
            return;

        _busy = true;
        try
        {
            await _characterState.ApplyInteractionAsync(SelectedFriend.FriendCode, action, payload);
        }
        finally
        {
            _busy = false;
        }
    }

    public async Task ApplyBaseSetAsync(int baseSetIndex)
    {
        if (SelectedFriend == null)
            return;

        if (baseSetIndex == 0)
        {
            var removeItem = new WardrobeDto(
                Guid.Empty,
                "None",
                string.Empty,
                "set",
                GlamourerEquipmentSlot.None,
                null!,
                RelationshipPriority.Casual,
                null
            );

            var removePayload = new InteractionPayload(null, null, [removeItem], null);
            await ApplyInteractionAsync(PairAction.ApplyWardrobe, removePayload);
            return;
        }

        var actualIndex = baseSetIndex - 1;
        if (actualIndex < 0 || actualIndex >= PairsBaseSets.Count)
            return;

        var item = PairsBaseSets[actualIndex];
        var applyItem = new WardrobeDto(
            item.Id,
            item.Name,
            item.Description,
            "set",
            GlamourerEquipmentSlot.None,
            string.Empty,
            item.Priority,
            null
        );

        var applyPayload = new InteractionPayload(null, null, [applyItem], null);
        await ApplyInteractionAsync(PairAction.ApplyWardrobe, applyPayload);
    }

    public async Task ApplySlotItemAsync(GlamourerEquipmentSlot slot, int itemIndex)
    {
        if (SelectedFriend == null)
            return;

        if (itemIndex == 0)
        {
            var removeItem = new WardrobeDto(
                Guid.Empty,
                "None",
                string.Empty,
                "item",
                slot,
                null!,
                RelationshipPriority.Casual,
                null
            );

            var removePayload = new InteractionPayload(null, null, [removeItem], null);
            await ApplyInteractionAsync(PairAction.ApplyWardrobe, removePayload);
            return;
        }

        if (!PairEquipmentSlots.TryGetValue(slot, out var items))
            return;

        var actualIndex = itemIndex - 1;
        if (actualIndex < 0 || actualIndex >= items.Count)
            return;

        var item = items[actualIndex];
        var applyItem = new WardrobeDto(
            item.Id,
            item.Name,
            item.Description,
            "item",
            slot,
            string.Empty,
            item.Priority,
            null
        );

        var applyPayload = new InteractionPayload(null, null, [applyItem], null);
        await ApplyInteractionAsync(PairAction.ApplyWardrobe, applyPayload);
    }

    public async Task LockSlotAsync(string slotName)
    {
        if (SelectedFriend == null)
            return;

        var lockId = $"{SelectedFriend.FriendCode}_{slotName}";
        DateTime? expires = UseTimer ? DateTime.UtcNow.Add(Expires) : null;
        string? password = UsePassword ? Password : null;

        var (itemType, slot) =
            slotName == "BaseSet"
                ? ("set", GlamourerEquipmentSlot.None)
                : ("item", Enum.Parse<GlamourerEquipmentSlot>(slotName));

        var payload = new InteractionPayload(
            null,
            null,
            [
                new WardrobeDto(
                    Guid.Empty,
                    slotName,
                    string.Empty,
                    itemType,
                    slot,
                    string.Empty,
                    LockPriority,
                    lockId
                ),
            ],
            null
        );

        var lockPayload = new InteractionPayload(null, null, payload.WardrobeItems, null);
        await ApplyInteractionAsync(PairAction.LockWardrobe, lockPayload);
    }

    public async Task UnlockSlotAsync(string slotName)
    {
        if (SelectedFriend == null)
            return;

        var lockId = $"{SelectedFriend.FriendCode}_{slotName}";

        var (itemType, slot) =
            slotName == "BaseSet"
                ? ("set", GlamourerEquipmentSlot.None)
                : ("item", Enum.Parse<GlamourerEquipmentSlot>(slotName));

        var payload = new InteractionPayload(
            null,
            null,
            [
                new WardrobeDto(
                    Guid.Empty,
                    slotName,
                    string.Empty,
                    itemType,
                    slot,
                    string.Empty,
                    RelationshipPriority.Casual,
                    lockId
                ),
            ],
            null
        );

        await ApplyInteractionAsync(PairAction.UnlockWardrobe, payload);
    }

    public LockInfoDto? GetSlotLock(string lockId)
    {
        if (this.SelectedFriend is { } friend)
        {
            if (friend.InteractionState is { } interactionState)
            {
                return interactionState.SlotLocks.TryGetValue(lockId, out var lockInfo)
                    ? lockInfo
                    : null;
            }
        }
        return null;
    }

    public string? GetBaseSetLockId()
    {
        if (this.SelectedFriend is { } friend && friend.InteractionState is { } interactionState)
        {
            return interactionState.BaseSet?.LockId;
        }
        return null;
    }

    public string? GetEquipmentLockId(GlamourerEquipmentSlot slot)
    {
        if (this.SelectedFriend is { } friend && friend.InteractionState is { } interactionState)
        {
            return interactionState.WardrobeSlots.GetValueOrDefault(slot)?.LockId;
        }
        return null;
    }

    public void Dispose()
    {
        _selectionManager.FriendSelected -= OnFriendSelected;
        _selectionManager.FriendsDeselected -= OnFriendsDeselected;
        GC.SuppressFinalize(this);
    }
}
