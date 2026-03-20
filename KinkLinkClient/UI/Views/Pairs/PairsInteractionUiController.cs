using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using KinkLinkClient.Domain;
using KinkLinkClient.Services;
using KinkLinkCommon.Dependencies.Glamourer;
using KinkLinkCommon.Domain.CharacterState;
using KinkLinkCommon.Domain.Enums;
using KinkLinkCommon.Domain.Enums.Permissions;
using KinkLinkCommon.Domain.Network;
using KinkLinkCommon.Domain.Network.PairInteractions;
using KinkLinkCommon.Domain.Wardrobe;

namespace KinkLinkClient.UI.Views.Pairs;

public class PairInteractionState
{
    public string FriendCode { get; set; } = string.Empty;
    public string? Note { get; set; }
    public CharacterStateDto? CachedState { get; set; }
    public DateTime LastUpdated { get; set; }

    public bool HasGagPermission { get; set; }
    public bool HasGarblerPermission { get; set; }
    public bool HasWardrobePermission { get; set; }
    public bool HasMoodlePermission { get; set; }

    public WardrobeStateDto? WardrobeState { get; set; }
    public List<WardrobeDto>? WardrobeItems { get; set; }

    public bool IsLoading { get; set; }

    public int SelectedBaseSetIndex { get; set; }
    public Dictionary<GlamourerEquipmentSlot, int> SelectedSlotIndices { get; set; } = new();
}

public class PairsInteractionUiController : IDisposable
{
    public bool IsBusy => _busy;
    public List<PairInteractionState> PairStates { get; } = [];
    public PairInteractionState? SelectedPair { get; private set; }
    public int SelectedTab { get; set; }

    private readonly NetworkService _network;
    private readonly FriendsListService _friendsList;
    private readonly ClientCharacterStateService _characterState;
    private bool _busy;

    public PairsInteractionUiController(
        NetworkService network,
        FriendsListService friendsList,
        ClientCharacterStateService characterState
    )
    {
        _network = network;
        _friendsList = friendsList;
        _characterState = characterState;

        _friendsList.FriendAdded += OnFriendsChanged;
        _friendsList.FriendDeleted += OnFriendsChanged;
        _friendsList.FriendsListCleared += OnFriendsChanged;
    }

    private void OnFriendsChanged(object? sender, object? _)
    {
        RefreshPairs();
    }

    public void RefreshPairs()
    {
        PairStates.Clear();
        foreach (var friend in _friendsList.Friends)
        {
            PairStates.Add(
                new PairInteractionState { FriendCode = friend.FriendCode, Note = friend.Note }
            );
        }
    }

    public void SelectPair(PairInteractionState pair)
    {
        SelectedPair = pair;
        SelectedTab = 0;
        _ = QueryPairStateAsync(pair);
    }

    public async Task RefreshSelectedPairAsync()
    {
        if (SelectedPair != null)
        {
            await QueryPairStateAsync(SelectedPair);
        }
    }

    public async Task QueryPairStateAsync(PairInteractionState pair)
    {
        if (pair.IsLoading)
            return;

        pair.IsLoading = true;
        pair.SelectedSlotIndices.Clear();
        pair.SelectedBaseSetIndex = 0;

        try
        {
            var result = await _characterState.QueryPairStateAsync(pair.FriendCode);
            if (result.Result == ActionResultEc.Success && result.Value != null)
            {
                pair.CachedState = result.Value.State;
                pair.HasGagPermission = result.Value.HasGagPermission;
                pair.HasGarblerPermission = result.Value.HasGarblerPermission;
                pair.HasWardrobePermission = result.Value.HasWardrobePermission;
                pair.HasMoodlePermission = result.Value.HasMoodlePermission;
                pair.LastUpdated = DateTime.UtcNow;
            }

            await QueryPairWardrobeAsync(pair);

            if (pair.CachedState != null)
            {
                UpdateSelectedIndicesFromWardrobeState(pair);
                UpdateSelectedIndicesFromGagState(pair.CachedState.Gag);
                UpdateSelectedIndicesFromGarblerState(pair.CachedState.Garbler);
                UpdateSelectedIndicesFromMoodleState(pair.CachedState.Moodles);
            }
        }
        catch (Exception ex)
        {
            Plugin.Log.Error(ex, "QueryPairStateAsync exception for {FriendCode}", pair.FriendCode);
        }
        finally
        {
            pair.IsLoading = false;
        }
    }

    public async Task QueryPairWardrobeAsync(PairInteractionState pair)
    {
        try
        {
            var result = await _characterState.QueryPairWardrobeAsync(pair.FriendCode);
            if (result.Result == ActionResultEc.Success && result.Value != null)
            {
                pair.WardrobeItems = result.Value.Items;
            }
        }
        catch (Exception ex)
        {
            Plugin.Log.Error(ex, "QueryPairWardrobeAsync exception for {FriendCode}", pair.FriendCode);
        }
    }

    public async Task ApplyInteractionAsync(PairAction action, InteractionPayload? payload)
    {
        if (SelectedPair == null || _busy)
            return;

        _busy = true;
        try
        {
            await _characterState.ApplyInteractionAsync(SelectedPair.FriendCode, action, payload);
        }
        finally
        {
            _busy = false;
        }
    }

    private void UpdateSelectedIndicesFromWardrobeState(PairInteractionState pair)
    {
        var equipment = pair.CachedState?.Wardrobe?.Equipment;
        if (equipment == null || pair.WardrobeItems == null)
        {
            return;
        }

        foreach (var kvp in equipment)
        {
            var slotName = kvp.Key;
            var equippedItem = kvp.Value;

            var slot = slotName switch
            {
                "Head" => GlamourerEquipmentSlot.Head,
                "Body" => GlamourerEquipmentSlot.Body,
                "Hands" => GlamourerEquipmentSlot.Hands,
                "Legs" => GlamourerEquipmentSlot.Legs,
                "Feet" => GlamourerEquipmentSlot.Feet,
                "Ears" => GlamourerEquipmentSlot.Ears,
                "Neck" => GlamourerEquipmentSlot.Neck,
                "Wrists" => GlamourerEquipmentSlot.Wrists,
                "LFinger" => GlamourerEquipmentSlot.LFinger,
                "RFinger" => GlamourerEquipmentSlot.RFinger,
                _ => GlamourerEquipmentSlot.None
            };

            if (slot != GlamourerEquipmentSlot.None)
            {
                var matchIndex = pair.WardrobeItems.FindIndex(w => w.Slot == slot && w.Name == equippedItem.Name);
                if (matchIndex >= 0)
                {
                    pair.SelectedSlotIndices[slot] = matchIndex + 1;
                }
            }
        }

        var baseSetName = pair.CachedState?.Wardrobe?.BaseLayerBase64;
        string? decodedSetName = null;
        if (!string.IsNullOrEmpty(baseSetName))
        {
            try
            {
                var json = Encoding.UTF8.GetString(Convert.FromBase64String(baseSetName));
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("Name", out var nameElement))
                {
                    decodedSetName = nameElement.GetString();
                }
            }
            catch (Exception ex)
            {
                Plugin.Log.Warning(ex, "Failed to decode base layer base64");
            }
        }

        if (!string.IsNullOrEmpty(decodedSetName))
        {
            var setItems = pair.WardrobeItems.Where(w => w.Type == "set").ToList();
            var matchIndex = setItems.FindIndex(w => w.Name == decodedSetName);
            if (matchIndex >= 0)
            {
                pair.SelectedBaseSetIndex = matchIndex + 1;
            }
        }
    }

    public void UpdateSelectedIndicesFromGagState(GagStateDto? gag)
    {
    }

    public void UpdateSelectedIndicesFromGarblerState(GarblerStateDto? garbler)
    {
    }

    public void UpdateSelectedIndicesFromMoodleState(List<KinkLinkCommon.Dependencies.Moodles.Domain.MoodleInfo>? moodles)
    {
    }

    public void Dispose()
    {
        _friendsList.FriendAdded -= OnFriendsChanged;
        _friendsList.FriendDeleted -= OnFriendsChanged;
        _friendsList.FriendsListCleared -= OnFriendsChanged;
        GC.SuppressFinalize(this);
    }
}
