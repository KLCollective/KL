using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using KinkLinkClient.Domain;
using KinkLinkClient.Domain.Interfaces;
using KinkLinkClient.Services;
using KinkLinkClient.Style;
using KinkLinkClient.UI.Components.Friends;
using KinkLinkClient.Utils;
using KinkLinkCommon.Dependencies.Glamourer;
using KinkLinkCommon.Domain;
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
            // if (viewService.CurrentView == View.Interactions)
            // {
            //     _ = controller.RefreshSelectedFriendAsync();
            // }
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
                SharedUserInterfaces.PushMediumFont();
                SharedUserInterfaces.TextCentered("Interactions");
                SharedUserInterfaces.PopMediumFont();
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

        if (controller.SelectedFriend != null)
        {
            SharedUserInterfaces.ContentBox(
                "SelectedPairContent",
                KinkLinkStyle.PanelBackground,
                true,
                () => DrawSelectedPairContent()
            );
        }

        ImGui.EndChild();
        ImGui.SameLine();
        friendsList.Draw(true, true);
    }

    private void DrawSelectedPairContent()
    {
        var width = ImGui.GetWindowWidth() - KinkLinkImGui.WindowPadding.X * 2;

        var friend = controller.SelectedFriend;
        SharedUserInterfaces.PushMediumFont();
        SharedUserInterfaces.TextCentered($"Pair: {friend?.Note ?? friend?.FriendCode}");
        SharedUserInterfaces.PopMediumFont();

        // TODO: Add a refresh button to forcibly update the state
        // ImGui.BeginDisabled(controller.Busy);
        // if (
        //     ImGui.Button("Query Pair State", new Vector2(width, ActionButtonHeight))
        //     && !controller.Busy
        // )
        // {
        //     controller.QueryPairStateAsync(controller.SelectedFriend.FriendCode);
        // }
        // ImGui.EndDisabled();
        //
        ImGui.Separator();

        // DrawGarblerSection(width);
        // ImGui.Separator();

        DrawLockConfigSection(width);
        ImGui.Separator();

        DrawPairConfigSection(width);
        ImGui.Separator();

        DrawWardrobeSection(width);
        ImGui.Separator();

        // DrawMoodleSection( width);
    }

    private void DrawLockConfigSection(float width)
    {
        SharedUserInterfaces.MediumText("Lock Settings");

        var padding = ImGui.GetStyle().WindowPadding;
        var labelWidth = 100f;
        var controlWidth = 150f;
        var checkWidth = 100f;

        ImGui.Text("Priority");
        ImGui.SameLine(labelWidth);
        ImGui.SetNextItemWidth(controlWidth);
        var priority = controller.LockPriority.ToString();
        if (ImGui.BeginCombo("##LockPriority", priority))
        {
            foreach (RelationshipPriority p in Enum.GetValues<RelationshipPriority>())
            {
                if (ImGui.Selectable(p.ToString()))
                    controller.LockPriority = p;
            }
            if (!ImGui.IsItemDeactivated())
                ImGui.EndCombo();
        }

        ImGui.Text("Can Self-Unlock");
        ImGui.SameLine(labelWidth + controlWidth + padding.X + checkWidth + padding.X);
        ImGui.Checkbox("##CanSelfUnlock", ref controller.CanSelfUnlock);

        ImGui.Text("Timer");
        ImGui.SameLine(labelWidth);
        ImGui.Checkbox("##UseTimer", ref controller.UseTimer);
        if (controller.UseTimer)
        {
            ImGui.SameLine();
            var expiryStr = controller.Expires.ToString(@"d\.hh\:mm");
            ImGui.SetNextItemWidth(100f);
            if (ImGui.InputText("##Expiry", ref expiryStr, 20))
            {
                if (TimeSpan.TryParse(expiryStr, out var parsed))
                    controller.Expires = parsed;
            }
        }

        var row2Y = ImGui.GetCursorPosY();
        ImGui.SetCursorPosY(row2Y + 20);

        ImGui.Text("Password");
        ImGui.SameLine(labelWidth);
        ImGui.Checkbox("##UsePassword", ref controller.UsePassword);
        if (controller.UsePassword)
        {
            ImGui.SameLine();
            var password = controller.Password ?? string.Empty;
            ImGui.SetNextItemWidth(150f);
            if (ImGui.InputText("##Password", ref password, 32))
                controller.Password = password;
        }
    }

    private void DrawPairConfigSection(float width)
    {
        var friend = controller.SelectedFriend;
        if (friend == null)
            return;

        if (!friend.HasWardrobePermission)
        {
            ImGui.TextUnformatted("  No permission");
            return;
        }

        SharedUserInterfaces.MediumText("Configuration");
        ImGui.TextUnformatted("  Configuration options TBD");
    }

    private void DrawWardrobeSection(float width)
    {
        SharedUserInterfaces.MediumText("Wardrobe");

        if (controller.SelectedFriend == null)
        {
            return;
        }

        if (controller.SelectedFriend.InteractionState == null)
        {
            ImGui.TextUnformatted("  No permission");
            return;
        }

        if (!controller.SelectedFriend.HasWardrobePermission)
        {
            ImGui.TextUnformatted("  No permission");
            return;
        }

        var state = controller.SelectedFriend.InteractionState;
        var wardrobe = state?.WardrobeSlots;
        var baseSetLockId = state?.BaseSet != null ? controller.GetBaseSetLockId() : null;
        var isBaseSetLocked = baseSetLockId != null;

        ImGui.TextUnformatted("Base Set:");
        if (controller.PairsBaseSets.Count > 0)
        {
            ImGui.SameLine();
            ImGui.SetNextItemWidth(width - 180f);
            var currentBaseSetName =
                controller.SelectedBaseSetIndice == 0
                    ? "None"
                    : (
                        controller.SelectedBaseSetIndice > 0
                        && controller.SelectedBaseSetIndice <= controller.PairsBaseSets.Count
                            ? controller.PairsBaseSets[controller.SelectedBaseSetIndice - 1]?.Name
                            : "Select..."
                    );
            ImGui.BeginDisabled(isBaseSetLocked);
            if (ImGui.BeginCombo("##BaseSetCombo", currentBaseSetName))
            {
                if (ImGui.Selectable("None"))
                {
                    controller.SelectedBaseSetIndice = 0;
                }

                for (int i = 0; i < controller.PairsBaseSets.Count; i++)
                {
                    var item = controller.PairsBaseSets[i];
                    if (ImGui.Selectable(item.Name))
                    {
                        controller.SelectedBaseSetIndice = i + 1;
                    }
                }

                if (!ImGui.IsItemDeactivated())
                    ImGui.EndCombo();
            }
            ImGui.SameLine();
            if (ImGui.Button("Apply##BaseSet", new Vector2(60, 24)))
            {
                _ = controller.ApplyBaseSetAsync(controller.SelectedBaseSetIndice);
            }
            ImGui.EndDisabled();

            ImGui.SameLine();
            DrawLockIconButton("BaseSet", controller.GetBaseSetLockId());
        }
        else
        {
            ImGui.SameLine();
            ImGui.SetNextItemWidth(width - 180f);
            ImGui.BeginDisabled(isBaseSetLocked);
            if (ImGui.BeginCombo("##BaseSetCombo", "None"))
            {
                if (ImGui.Selectable("None"))
                {
                    controller.SelectedBaseSetIndice = 0;
                }

                if (!ImGui.IsItemDeactivated())
                    ImGui.EndCombo();
            }
            ImGui.SameLine();
            if (ImGui.Button("Apply##BaseSet", new Vector2(60, 24)))
            {
                _ = controller.ApplyBaseSetAsync(controller.SelectedBaseSetIndice);
            }
            ImGui.EndDisabled();

            ImGui.SameLine();
            DrawLockIconButton("BaseSet", controller.GetBaseSetLockId());
        }

        ImGui.TextUnformatted("Equipment Slots:");

        var padding = ImGui.GetStyle().WindowPadding;
        var labelWidth = 80f;
        var comboWidth = width - labelWidth - 130f;
        var buttonWidth = 60f;

        foreach (var kvp in controller.SelectedWardrobeIndices)
        {
            var slot = kvp.Key;
            var selectedIndice = kvp.Value;
            var lockId = wardrobe?.ContainsKey(slot) == true ? controller.GetEquipmentLockId(slot) : null;
            var isLocked = lockId != null;

            ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 5);
            ImGui.Text(slot.ToString());
            ImGui.SameLine(labelWidth);

            ImGui.BeginDisabled(isLocked);

            if (controller.PairEquipmentSlots.TryGetValue(slot, out var items))
            {
                ImGui.SetNextItemWidth(comboWidth);
                var currentItemName =
                    selectedIndice == 0
                        ? "None"
                        : (
                            selectedIndice > 0 && selectedIndice <= items.Count
                                ? items[selectedIndice - 1]?.Name
                                : "Select..."
                        );
                if (ImGui.BeginCombo($"##SlotCombo_{slot}", currentItemName))
                {
                    if (ImGui.Selectable("None"))
                    {
                        controller.SelectedWardrobeIndices[slot] = 0;
                    }

                    for (int i = 0; i < items.Count; i++)
                    {
                        var item = items[i];
                        if (ImGui.Selectable(item.Name))
                        {
                            controller.SelectedWardrobeIndices[slot] = i + 1;
                        }
                    }

                    if (!ImGui.IsItemDeactivated())
                        ImGui.EndCombo();
                }

                ImGui.SameLine(labelWidth + comboWidth + padding.X);
                if (ImGui.Button($"Apply##{slot}", new Vector2(buttonWidth, 24)))
                {
                    _ = controller.ApplySlotItemAsync(
                        slot,
                        controller.SelectedWardrobeIndices[slot]
                    );
                }
                ImGui.EndDisabled();
                ImGui.SameLine(labelWidth + comboWidth + padding.X + buttonWidth + padding.X);

                DrawLockIconButton(slot.ToString(), controller.GetEquipmentLockId(slot));
            }
            else
            {
                ImGui.SetNextItemWidth(comboWidth);
                if (ImGui.BeginCombo($"##SlotCombo_{slot}", "None"))
                {
                    if (ImGui.Selectable("None"))
                    {
                        controller.SelectedWardrobeIndices[slot] = 0;
                    }

                    if (!ImGui.IsItemDeactivated())
                        ImGui.EndCombo();
                }

                ImGui.SameLine(labelWidth + comboWidth + padding.X);
                if (ImGui.Button($"Apply##{slot}", new Vector2(buttonWidth, 24)))
                {
                    _ = controller.ApplySlotItemAsync(
                        slot,
                        controller.SelectedWardrobeIndices[slot]
                    );
                }
                ImGui.EndDisabled();

                ImGui.SameLine(labelWidth + comboWidth + padding.X + buttonWidth + padding.X);
                DrawLockIconButton(slot.ToString(), controller.GetEquipmentLockId(slot));
            }
        }
    }

    private void DrawLockIconButton(string slotName, string? lockId)
    {
        var lockItem = lockId != null ? controller.GetSlotLock(lockId) : null;
        var icon = lockItem != null ? FontAwesomeIcon.Lock : FontAwesomeIcon.LockOpen;

        ImGui.PushFont(UiBuilder.IconFont);
        var clicked = ImGui.Button(icon.ToIconString() + $"##{slotName}", new Vector2(24, 24));
        ImGui.PopFont();

        if (clicked && !controller.Busy)
        {
            if (lockItem != null)
            {
                _ = controller.UnlockSlotAsync(slotName);
            }
            else
            {
                _ = controller.LockSlotAsync(slotName);
            }
        }

        if (ImGui.IsItemHovered())
        {
            ImGui.BeginTooltip();
            if (lockItem is { })
            {
                // null safety (the check for `isLocked` is literally a null check, so there's no safety issue
                ImGui.Text($"Locked by priority: {lockItem.Value.LockPriority}");
                if (lockItem.Value.Expires.HasValue)
                {
                    ImGui.Text($"Expires: {lockItem.Value.Expires}");
                }
                if (!string.IsNullOrEmpty(lockItem.Value.Password))
                {
                    ImGui.Text("Password locked");
                }
                if (lockItem.Value.CanSelfUnlock)
                {
                    ImGui.Text("Can self-unlock");
                }
            }
            else
            {
                ImGui.Text($"Click to lock {slotName}");
            }
            ImGui.EndTooltip();
        }
    }

    private static GlamourerEquipmentSlot GetSlotFromName(string slotName)
    {
        return slotName switch
        {
            "Head" => GlamourerEquipmentSlot.Head,
            "Body" => GlamourerEquipmentSlot.Body,
            "Hands" => GlamourerEquipmentSlot.Hands,
            "Legs" => GlamourerEquipmentSlot.Legs,
            "Feet" => GlamourerEquipmentSlot.Feet,
            "Ears" => GlamourerEquipmentSlot.Ears,
            "Neck" => GlamourerEquipmentSlot.Neck,
            "Wrists" => GlamourerEquipmentSlot.Wrists,
            "RFinger" => GlamourerEquipmentSlot.RFinger,
            "LFinger" => GlamourerEquipmentSlot.LFinger,
            _ => GlamourerEquipmentSlot.None,
        };
    }

    private void DrawMoodleSection(Friend friend, InteractionContext state, float width)
    {
        SharedUserInterfaces.MediumText("Moodle");

        if (!friend.HasMoodlePermission)
        {
            ImGui.TextUnformatted("  No permission");
            return;
        }

        ImGui.TextUnformatted("Apply own moodle: TBD");
        ImGui.TextUnformatted("Apply pair's moodle: TBD");
    }
}
