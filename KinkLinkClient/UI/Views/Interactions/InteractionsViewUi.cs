using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using KinkLinkClient.Domain;
using KinkLinkClient.Domain.Interfaces;
using KinkLinkClient.Services;
using KinkLinkClient.Style;
using KinkLinkClient.UI.Components.Friends;
using KinkLinkClient.UI.Views.Pairs;
using KinkLinkClient.Utils;
using KinkLinkCommon.Dependencies.Glamourer;
using KinkLinkCommon.Domain.Enums;
using KinkLinkCommon.Domain.Enums.Permissions;
using KinkLinkCommon.Domain.Network.PairInteractions;
using KinkLinkCommon.Domain.Wardrobe;

namespace KinkLinkClient.UI.Views.Interactions;

public class InteractionsViewUi(
    InteractionsViewUiController controller,
    PairsListComponentUi friendsList,
    ViewService viewService
) : IDrawable
{
    private const int ActionButtonHeight = 40;
    private View _lastView;

    public void Draw()
    {
        if (_lastView != viewService.CurrentView)
        {
            _lastView = viewService.CurrentView;
            if (viewService.CurrentView == View.Interactions)
            {
                _ = controller.RefreshSelectedPairAsync();
            }
        }

        ImGui.BeginChild(
            "PermissionContent",
            KinkLinkStyle.ContentSize,
            false,
            KinkLinkStyle.ContentFlags
        );

        var width = ImGui.GetWindowWidth();

        SharedUserInterfaces.ContentBox(
            "InteractionSelect",
            KinkLinkStyle.PanelBackground,
            true,
            () =>
            {
                var buttonWidth = (width - 3 * KinkLinkImGui.WindowPadding.X) * 0.3f;
                var buttonDimensions = new Vector2(
                    buttonWidth,
                    KinkLinkDimensions.SendCommandButtonHeight
                );
                // Header
                SharedUserInterfaces.PushMediumFont();
                SharedUserInterfaces.TextCentered("Interactions");
                SharedUserInterfaces.PopMediumFont();
                // Draw the tooltip/tutorial
                ImGui.SameLine(width - ImGui.GetFontSize() - KinkLinkImGui.WindowPadding.X * 2);
                SharedUserInterfaces.Icon(FontAwesomeIcon.QuestionCircle);

                if (ImGui.IsItemHovered())
                {
                    ImGui.SetNextWindowSize(KinkLinkDimensions.Tooltip);
                    ImGui.BeginTooltip();

                    SharedUserInterfaces.MediumText("Tutorial");
                    ImGui.Separator();
                    ImGui.TextWrapped("Send commands to control your pairs");
                    ImGui.TextWrapped("Each permission is set in the permissions menu");
                    ImGui.TextWrapped(
                        "If you don't have permissions, you will be unable to set them."
                    );
                    ImGui.EndTooltip();
                }
            }
        );

        var selectedPair = controller.SelectedPair;
        if (selectedPair != null)
        {
            SharedUserInterfaces.ContentBox(
                "SelectedPairContent",
                KinkLinkStyle.PanelBackground,
                true,
                () => DrawSelectedPairContent(selectedPair)
            );
        }

        ImGui.EndChild();
        ImGui.SameLine();
        friendsList.Draw(true, true);
    }

    private void DrawSelectedPairContent(PairInteractionState pair)
    {
        var width = ImGui.GetWindowWidth() - KinkLinkImGui.WindowPadding.X * 2;

        SharedUserInterfaces.PushMediumFont();
        SharedUserInterfaces.TextCentered($"Pair: {pair.Note ?? pair.FriendCode}");
        SharedUserInterfaces.PopMediumFont();

        if (pair.IsLoading)
        {
            ImGui.TextUnformatted("Loading...");
            return;
        }

        if (pair.CachedState == null)
        {
            if (ImGui.Button("Query Pair State", new Vector2(width, ActionButtonHeight)))
            {
                _ = controller.QueryPairStateAsync(pair);
            }
        }

        ImGui.Separator();

        DrawGagSection(pair, width);
        ImGui.Separator();

        DrawGarblerSection(pair, width);
        ImGui.Separator();

        DrawWardrobeSection(pair, width);
        ImGui.Separator();

        DrawMoodleSection(pair, width);
    }

    private void DrawGagSection(PairInteractionState pair, float width)
    {
        SharedUserInterfaces.MediumText("Gag");

        if (!pair.HasGagPermission)
        {
            ImGui.TextUnformatted("  No permission");
            return;
        }

        var buttonWidth = (width - 2 * KinkLinkImGui.WindowPadding.X) * 0.25f;
        var buttonDimensions = new Vector2(buttonWidth, KinkLinkDimensions.SendCommandButtonHeight);

        if (ImGui.Button("Apply", buttonDimensions))
        {
            _ = controller.ApplyInteractionAsync(PairAction.ApplyGag, null);
        }
        ImGui.SameLine();
        if (ImGui.Button("Lock", buttonDimensions))
        {
            _ = controller.ApplyInteractionAsync(PairAction.LockGag, null);
        }
        ImGui.SameLine();
        if (ImGui.Button("Unlock", buttonDimensions))
        {
            _ = controller.ApplyInteractionAsync(PairAction.UnlockGag, null);
        }
        ImGui.SameLine();
        if (ImGui.Button("Remove", buttonDimensions))
        {
            _ = controller.ApplyInteractionAsync(PairAction.RemoveGag, null);
        }
    }

    private void DrawGarblerSection(PairInteractionState pair, float width)
    {
        SharedUserInterfaces.MediumText("Garbler");

        if (!pair.HasGarblerPermission)
        {
            ImGui.TextUnformatted("  No permission");
            return;
        }

        var buttonWidth = (width - 2 * KinkLinkImGui.WindowPadding.X) * 0.25f;
        var buttonDimensions = new Vector2(buttonWidth, KinkLinkDimensions.SendCommandButtonHeight);

        if (ImGui.Button("Enable", buttonDimensions))
        {
            _ = controller.ApplyInteractionAsync(PairAction.EnableGarbler, null);
        }
        ImGui.SameLine();
        if (ImGui.Button("Lock", buttonDimensions))
        {
            _ = controller.ApplyInteractionAsync(PairAction.LockGarbler, null);
        }
        ImGui.SameLine();
        ImGui.TextUnformatted("Channels: TBD");
        ImGui.SameLine();
        if (ImGui.Button("Lock Channels", buttonDimensions))
        {
            _ = controller.ApplyInteractionAsync(PairAction.LockGarblerChannels, null);
        }
    }

    private void DrawWardrobeSection(PairInteractionState pair, float width)
    {
        SharedUserInterfaces.MediumText("Wardrobe");

        if (!pair.HasWardrobePermission)
        {
            ImGui.TextUnformatted("  No permission");
            return;
        }

        if (pair.WardrobeItems == null || pair.WardrobeItems.Count == 0)
        {
            ImGui.TextUnformatted("  No wardrobe items available");
            return;
        }

        var setItems = pair
            .WardrobeItems.Where(w => w.Type == "set")
            .ToList();
        var setNames = setItems.Select(w => w.Name).Distinct().ToList();
        var types = setNames.Prepend("(None)").ToList();
        while (pair.SelectedBaseSetIndex >= types.Count)
            pair.SelectedBaseSetIndex = 0;
        var baseSetIndex = pair.SelectedBaseSetIndex;

        ImGui.TextUnformatted("Base Set:");
        ImGui.SameLine();
        if (ImGui.Combo("##WardrobeBaseSet", ref baseSetIndex, types.ToArray(), types.Count))
        {
            pair.SelectedBaseSetIndex = baseSetIndex;
        }
        ImGui.SameLine();
        if (ImGui.SmallButton("Apply##BaseSet"))
        {
            var selectedSet = baseSetIndex > 0 && baseSetIndex - 1 < setItems.Count
                ? setItems[baseSetIndex - 1]
                : null;
            var payload = new InteractionPayload(
                null,
                null,
                [new WardrobeDto(
                    selectedSet?.Id ?? Guid.Empty,
                    selectedSet?.Name ?? string.Empty,
                    selectedSet?.Description ?? string.Empty,
                    "set",
                    GlamourerEquipmentSlot.None,
                    selectedSet?.DataBase64,
                    selectedSet?.Priority ?? RelationshipPriority.Casual
                )],
                null
            );
            _ = controller.ApplyInteractionAsync(PairAction.ApplyWardrobe, payload);
        }
        ImGui.SameLine();
        if (ImGui.SmallButton("Lock##BaseSet"))
        {
            _ = controller.ApplyInteractionAsync(PairAction.LockWardrobe, null);
        }

        ImGui.TextUnformatted("Equipment Slots:");

        var slots = new[]
        {
            GlamourerEquipmentSlot.Head,
            GlamourerEquipmentSlot.Body,
            GlamourerEquipmentSlot.Hands,
            GlamourerEquipmentSlot.Legs,
            GlamourerEquipmentSlot.Feet,
            GlamourerEquipmentSlot.Ears,
            GlamourerEquipmentSlot.Neck,
            GlamourerEquipmentSlot.Wrists,
            GlamourerEquipmentSlot.RFinger,
            GlamourerEquipmentSlot.LFinger,
        };

        var labelWidth = 80f;

        foreach (var slot in slots)
        {
            var slotItems = pair.WardrobeItems.Where(w => w.Slot == slot).ToList();
            var slotName = slot.ToString();

            var items = slotItems.Select(w => w.Name).Prepend("(None)").ToList();

            if (!pair.SelectedSlotIndices.TryGetValue(slot, out var selectedIndex) || selectedIndex >= items.Count)
                selectedIndex = 0;
            var currentIndex = selectedIndex;

            ImGui.TextUnformatted(slotName);
            ImGui.SameLine(labelWidth);
            if (ImGui.Combo($"##{slotName}", ref currentIndex, items.ToArray(), items.Count))
            {
                pair.SelectedSlotIndices[slot] = currentIndex;
            }
            ImGui.SameLine();
            if (ImGui.SmallButton($"Apply##{slotName}"))
            {
                var selectedItem = currentIndex > 0 && currentIndex - 1 < slotItems.Count
                    ? slotItems[currentIndex - 1]
                    : null;
                var payload = new InteractionPayload(
                    null,
                    null,
                    [new WardrobeDto(
                        selectedItem?.Id ?? Guid.Empty,
                        selectedItem?.Name ?? string.Empty,
                        selectedItem?.Description ?? string.Empty,
                        "item",
                        slot,
                        selectedItem?.DataBase64,
                        selectedItem?.Priority ?? RelationshipPriority.Casual
                    )],
                    null
                );
                _ = controller.ApplyInteractionAsync(PairAction.ApplyWardrobe, payload);
            }
            ImGui.SameLine();
            if (ImGui.SmallButton($"Lock##{slotName}"))
            {
                _ = controller.ApplyInteractionAsync(PairAction.LockWardrobe, null);
            }
        }

        if (
            ImGui.Button(
                "Apply All",
                new Vector2(width * 0.3f, KinkLinkDimensions.SendCommandButtonHeight)
            )
        )
        {
            var allItems = new List<WardrobeDto>();
            foreach (var slot in slots)
            {
                if (pair.SelectedSlotIndices.TryGetValue(slot, out var selectedIndex))
                {
                    var slotItems = pair.WardrobeItems.Where(w => w.Slot == slot).ToList();
                    if (selectedIndex > 0 && selectedIndex - 1 < slotItems.Count)
                    {
                        allItems.Add(slotItems[selectedIndex - 1]);
                    }
                }
            }
            var payload = allItems.Count > 0 ? new InteractionPayload(null, null, allItems, null) : null;
            if (payload != null)
                _ = controller.ApplyInteractionAsync(PairAction.ApplyWardrobe, payload);
        }
        ImGui.SameLine();
        if (
            ImGui.Button(
                "Lock All",
                new Vector2(width * 0.15f, KinkLinkDimensions.SendCommandButtonHeight)
            )
        )
        {
            _ = controller.ApplyInteractionAsync(PairAction.LockWardrobe, null);
        }
        ImGui.SameLine();
        if (
            ImGui.Button(
                "Unlock All",
                new Vector2(width * 0.15f, KinkLinkDimensions.SendCommandButtonHeight)
            )
        )
        {
            _ = controller.ApplyInteractionAsync(PairAction.UnlockWardrobe, null);
        }
    }

    private void DrawMoodleSection(PairInteractionState pair, float width)
    {
        SharedUserInterfaces.MediumText("Moodle");

        if (!pair.HasMoodlePermission)
        {
            ImGui.TextUnformatted("  No permission");
            return;
        }

        ImGui.TextUnformatted("Apply own moodle: TBD");
        ImGui.TextUnformatted("Apply pair's moodle: TBD");
    }

    private void DrawPairSelection()
    {
        ImGui.TextUnformatted("Select a pair:");
        ImGui.Separator();

        var pairs = controller.PairStates;
        foreach (var pair in pairs)
        {
            var label = $"{pair.Note ?? pair.FriendCode}";
            if (ImGui.Selectable(label, false))
            {
                controller.SelectPair(pair);
                _ = controller.QueryPairStateAsync(pair);
            }
        }

        if (pairs.Count == 0)
        {
            SharedUserInterfaces.BigTextCentered("No pairs available");
        }
    }
}
