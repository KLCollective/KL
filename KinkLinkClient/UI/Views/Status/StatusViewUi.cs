using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using KinkLinkClient.Domain;
using KinkLinkClient.Domain.Interfaces;
using KinkLinkClient.Services;
using KinkLinkClient.UI.Components.Input;
using KinkLinkClient.Utils;
using KinkLinkCommon.Dependencies.Glamourer;
using KinkLinkCommon.Domain;

namespace KinkLinkClient.UI.Views.Status;

public class StatusViewUi(
    StatusViewUiController controller,
    PermanentTransformationLockService permanentTransformationLockService,
    IdentityService identityService,
    TipService tipService
) : IDrawable
{
    private static readonly GlamourerEquipmentSlot[] EquipmentSlots =
    [
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
    ];

    public void Draw()
    {
        ImGui.BeginChild("SettingsContent", Vector2.Zero, false, KinkLinkStyle.ContentFlags);

        var windowWidth = ImGui.GetWindowWidth();
        var windowPadding = ImGui.GetStyle().WindowPadding;

        ImGui.AlignTextToFramePadding();

        SharedUserInterfaces.ContentBox(
            "StatusHeader",
            KinkLinkStyle.PanelBackground,
            true,
            () =>
            {
                SharedUserInterfaces.PushBigFont();

                var friendCode = identityService.FriendCode;
                var size = ImGui.CalcTextSize(friendCode);

                ImGui.SetCursorPosX((windowWidth - size.X) * 0.5f);
                if (ImGui.Selectable(friendCode, false, ImGuiSelectableFlags.None, size))
                    ImGui.SetClipboardText(friendCode);

                SharedUserInterfaces.PopBigFont();
                SharedUserInterfaces.TextCentered(
                    "(click friend code to copy)",
                    ImGuiColors.DalamudGrey
                );
            }
        );

        SharedUserInterfaces.ContentBox(
            "StatusLogout",
            KinkLinkStyle.PanelBackground,
            true,
            () =>
            {
                SharedUserInterfaces.MediumText("Welcome");
                ImGui.TextUnformatted(tipService.CurrentTip);
            }
        );

        if (SharedUserInterfaces.ContextBoxButton(FontAwesomeIcon.Plug, windowPadding, windowWidth))
            controller.Disconnect();

        SharedUserInterfaces.Tooltip("Disconnect");

        SharedUserInterfaces.ContentBox(
            "StatusButtons",
            KinkLinkStyle.PanelBackground,
            true,
            () =>
            {
                SharedUserInterfaces.MediumText("Statuses");
                ImGui.TextUnformatted(
                    "Various aspects of the plugin have lingering affects. You can find them below."
                );

                ImGui.SameLine();
                SharedUserInterfaces.Icon(FontAwesomeIcon.QuestionCircle);
                SharedUserInterfaces.Tooltip("Only active statuses will be displayed");

                SharedUserInterfaces.Tooltip([
                    "Only active statuses will be displayed. Such statuses include:",
                    "- Equipped Wardrobe Items",
                    "- Lots on slots",
                    //"- Being permanently transformed",
                    //"- Being transformed",
                    //"- Being body swapped",
                    //"- Being twinned",
                    // "- Being hypnotized"
                ]);
            }
        );

        RenderWardrobeComponent();

        SharedUserInterfaces.ContentBox(
            "OmniTool",
            KinkLinkStyle.PanelBackground,
            false,
            () =>
            {
                SharedUserInterfaces.MediumText("Plugin Misbehaving? (Temporary Solution)");
                if (ImGui.Button("Safeword"))
                    NotificationHelper.Error("Safewording", "TODO: Not yet implemented");
                if (ImGui.Button("Full Unlocks"))
                    NotificationHelper.Error("Full Unlocks", "TODO: Not yet implemented");
            }
        );

        ImGui.EndChild();
    }

    private void RenderPermanentTransformationComponent(Vector2 windowPadding, float windowWidth)
    {
        SharedUserInterfaces.ContentBox(
            "StatusLock",
            KinkLinkStyle.ElevatedBackground,
            true,
            () =>
            {
                SharedUserInterfaces.MediumText("Permanently Transformed");
                ImGui.TextUnformatted(
                    $"{identityService.Alteration?.Sender ?? "Unknown"} has locked your appearance"
                );
            }
        );

        var previousContextBoxSize = ImGui.GetItemRectSize();
        var endingCursorPosition = ImGui.GetCursorPosY();

        SharedUserInterfaces.PushBigFont();
        var cursorPositionStartX =
            windowWidth - previousContextBoxSize.Y - FourDigitInput.Width - windowPadding.Y * 2;
        var start = new Vector2(
            cursorPositionStartX,
            endingCursorPosition - previousContextBoxSize.Y - windowPadding.Y * 2
        );
        ImGui.SetCursorPos(start);

        controller.PinInput.Draw();
        SharedUserInterfaces.PopBigFont();

        ImGui.SameLine();

        if (
            SharedUserInterfaces.IconButton(
                FontAwesomeIcon.Unlock,
                new Vector2(previousContextBoxSize.Y)
            )
        )
            controller.Unlock();

        ImGui.SetCursorPosY(endingCursorPosition);
    }

    private void RenderTransformationComponent(Vector2 windowPadding, float windowWidth)
    {
        SharedUserInterfaces.ContentBox(
            "StatusTransformation",
            KinkLinkStyle.PanelBackground,
            true,
            () =>
            {
                SharedUserInterfaces.MediumText("Identity Altered");

                if (identityService.Alteration is not { } alteration)
                {
                    ImGui.TextUnformatted("An unknown friend altered your identity");
                    return;
                }

                var type = alteration.Type switch
                {
                    IdentityAlterationType.Transformation =>
                        $"{alteration.Sender} transformed you or your clothing",
                    IdentityAlterationType.Twinning => $"{alteration.Sender} twinned with you",
                    IdentityAlterationType.BodySwap => $"{alteration.Sender} swapped your body",
                    _ => $"{alteration.Sender} altered your identity",
                };

                ImGui.TextUnformatted(type);
            }
        );

        if (permanentTransformationLockService.Locked)
        {
            ImGui.BeginDisabled();
            SharedUserInterfaces.ContextBoxButton(
                FontAwesomeIcon.History,
                windowPadding,
                windowWidth
            );
            ImGui.EndDisabled();
        }
        else
        {
            if (
                SharedUserInterfaces.ContextBoxButton(
                    FontAwesomeIcon.History,
                    windowPadding,
                    windowWidth
                )
            )
                controller.ResetIdentity();
        }
    }

    private void RenderWardrobeComponent()
    {
        SharedUserInterfaces.ContentBox(
            "StatusWardrobe",
            KinkLinkStyle.PanelBackground,
            true,
            () =>
            {
                SharedUserInterfaces.MediumText("Current Wardrobe");
                ImGui.TextUnformatted("View and manage your currently equipped wardrobe items.");
            }
        );

        if (!ImGui.BeginTable("WardrobeStatusTable", 4))
            return;

        ImGui.TableSetupColumn("Slot", ImGuiTableColumnFlags.WidthFixed, 80);
        ImGui.TableSetupColumn("Item", ImGuiTableColumnFlags.WidthStretch);
        ImGui.TableSetupColumn("Remove", ImGuiTableColumnFlags.WidthFixed, 120);
        ImGui.TableSetupColumn("Unlock", ImGuiTableColumnFlags.WidthFixed, 100);

        ImGui.TableHeadersRow();

        for (var i = 0; i < EquipmentSlots.Length + 1; i++)
        {
            var slotName = i == 0 ? "Base Layer" : EquipmentSlots[i - 1].ToString();
            var slot = i == 0 ? GlamourerEquipmentSlot.None : EquipmentSlots[i - 1];
            string? itemName = null;

            LockInfoDto? lockInfo = default;

            if (i == 0)
            {
                var baseLayer = controller.BaseLayer;
                // We can skip all draw operations if the base layer is nonexistent
                if (baseLayer == null)
                    continue;

                itemName = baseLayer?.Name;
                var lockData = controller.GetLock("wardrobe-baseset");
                if (lockData.HasValue)
                {
                    // TODO: Put the locker name/alias here
                    lockInfo = lockData.Value;
                }
            }
            else
            {
                var item = controller.GetEquipmentSlot(slot);
                // We can skip all draw operations if the slot item is nonexistent
                if (item == null)
                    continue;

                itemName = item.Name;
                var lockData = controller.GetLock($"wardrobe-{slotName.ToLowerInvariant()}");
                if (lockData.HasValue)
                {
                    // TODO: Put the locker name/alias here
                    lockInfo = lockData.Value;
                }
            }

            bool isLocked = lockInfo.HasValue;
            ImGui.TableNextColumn();
            ImGui.TextUnformatted(slotName);

            ImGui.TableNextColumn();
            if (string.IsNullOrEmpty(itemName))
            {
                ImGui.TextColored(ImGuiColors.DalamudGrey, "(empty)");
            }
            else
            {
                ImGui.TextUnformatted(itemName);
            }

            ImGui.TableNextColumn();
            var hasItem = !string.IsNullOrEmpty(itemName);

            if (hasItem)
            {
                ImGui.BeginDisabled(isLocked);
                if (ImGui.Button($"Remove##{slotName}"))
                {
                    if (i == 0)
                        controller.RemoveBaseSet();
                    else
                        controller.RemoveSlotItem(slot);
                }

                if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
                {
                    ImGui.BeginTooltip();
                    if (isLocked && !lockInfo.Value.CanSelfUnlock)
                        ImGui.TextUnformatted("Locked by User.");
                    else if (isLocked)
                        ImGui.TextUnformatted("Need to be unlocked first <3");
                    else
                        ImGui.TextUnformatted($"Remove {slotName} from active wardrobe");
                    ImGui.EndTooltip();
                }

                ImGui.EndDisabled();
                ImGui.SameLine();
            }

            ImGui.TableNextColumn();
            if (hasItem)
            {
                var canUnlock = isLocked && lockInfo!.Value.CanSelfUnlock;
                ImGui.BeginDisabled(!canUnlock);
                if (ImGui.Button($"Unlock##{slotName}"))
                    controller.UnlockWardrobeSlot(slotName);

                if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
                {
                    ImGui.BeginTooltip();
                    if (!canUnlock && isLocked)
                        ImGui.TextUnformatted("You do not have permission to unlock this item.");
                    else if (!canUnlock)
                        ImGui.TextUnformatted("This item is not locked.");
                    else
                        ImGui.TextUnformatted($"Unlock {slotName} from locker's control");
                    ImGui.EndTooltip();
                }

                ImGui.EndDisabled();
            }

            ImGui.TableNextRow();
        }

        ImGui.EndTable();
    }
}
