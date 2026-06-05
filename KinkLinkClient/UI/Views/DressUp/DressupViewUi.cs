using System;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using KinkLinkClient.Domain.Interfaces;
using KinkLinkClient.Services;
using KinkLinkClient.Style;
using KinkLinkClient.Utils;
using KinkLinkCommon.Domain.Wardrobe;

namespace KinkLinkClient.UI.Views.DressUp;

public partial class DressupViewUi(DressupViewUiController controller) : IDrawable
{
    private WardrobeManager wardrobeManager => controller.WardrobeManager;

    public void Draw()
    {
        var padding = ImGui.GetStyle().WindowPadding;
        ImGui.BeginChild("##WardrobeUi", Vector2.Zero, false, KinkLinkStyle.ContentFlags);
        var begin = ImGui.GetCursorPosY();
        var width = ImGui.GetWindowWidth() - padding.X * 2;

        SharedUserInterfaces.ContentBox(
            "PersonalHeader",
            KinkLinkStyle.PanelBackground,
            true,
            () =>
            {
                SharedUserInterfaces.MediumText("Personal Dressup");

                // Randomize button left of Back (reuse existing functionality)
                ImGui.SameLine(width - 170);
                if (ImGui.Button("Randomize", new Vector2(80, 30)))
                {
                    _ = controller.WardrobeManager.RandomizeActiveAsync();
                }

                ImGui.SameLine();
            }
        );

        var statuses = controller.GetActiveSlotStatuses();

        SharedUserInterfaces.ContentBox(
            "PersonalSlots",
            KinkLinkStyle.PanelBackground,
            true,
            () =>
            {
                if (ImGui.BeginChild("##PersonalSlotList", new Vector2(0, 0), false))
                {
                    if (
                        ImGui.BeginTable(
                            "##PersonalDressupTable",
                            4,
                            ImGuiTableFlags.RowBg | ImGuiTableFlags.BordersInnerV
                        )
                    )
                    {
                        ImGui.TableSetupColumn("Slot", ImGuiTableColumnFlags.WidthFixed, 85);
                        ImGui.TableSetupColumn("Item", ImGuiTableColumnFlags.WidthStretch);
                        ImGui.TableSetupColumn("Action", ImGuiTableColumnFlags.WidthFixed, 36);
                        ImGui.TableSetupColumn("Lock", ImGuiTableColumnFlags.WidthFixed, 36);
                        ImGui.TableHeadersRow();

                        foreach (var status in statuses)
                        {
                            ImGui.TableNextRow();
                            ImGui.TableNextColumn();

                            ImGui.Text(status.SlotName);

                            // Item dropdown
                            ImGui.TableNextColumn();
                            var layer = KinkLinkClient.Services.WardrobeSlotHelper.GetLayerFromName(
                                status.SlotName
                            );
                            var candidates = controller
                                .WardrobeManager.WardrobeLibrary.Where(i => i.Layer == layer)
                                .ToList();

                            // build preview string
                            // 1. Show pending selection name if user picked from combo
                            // 2. Otherwise show active WardrobeItem name from slot status
                            // 3. Fall back to "None"
                            string? preview = null;
                            var pendingId = controller.GetSelectedForLayer(layer);
                            if (pendingId.HasValue)
                            {
                                var pending = controller.WardrobeManager.GetItemById(
                                    pendingId.Value
                                );
                                preview = pending?.Name;
                            }
                            if (string.IsNullOrEmpty(preview) && status.HasItem)
                            {
                                preview = status.ItemDisplay ?? "Active";
                            }
                            if (string.IsNullOrEmpty(preview))
                                preview = "None";

                            var slotActive = controller.WardrobeManager.IsLayerActive(layer);
                            ImGui.BeginDisabled(slotActive);

                            if (ImGui.BeginCombo($"##personal_combo_{layer}", preview))
                            {
                                if (ImGui.Selectable("None"))
                                {
                                    controller.SetSelectedForLayer(layer, null);
                                }

                                for (int i = 0; i < candidates.Count; i++)
                                {
                                    var item = candidates[i];
                                    if (ImGui.Selectable(item.Name))
                                    {
                                        controller.SetSelectedForLayer(layer, item.Id);
                                    }
                                }

                                ImGui.EndCombo();
                            }

                            ImGui.EndDisabled();

                            ImGui.TableNextColumn();
                            var isLocked = controller.IsSlotLocked(layer);

                            if (controller.WardrobeManager.IsLayerActive(layer))
                            {
                                var canRemove = controller.CanRemoveFromSlot(layer);
                                ImGui.BeginDisabled(!canRemove);
                                if (
                                    SharedUserInterfaces.IconButton(
                                        FontAwesomeIcon.Reply,
                                        KinkLinkDimensions.IconButton,
                                        "Remove",
                                        $"personal_remove_{status.SlotName}"
                                    )
                                )
                                {
                                    _ = RemoveSlotAsync(layer);
                                }
                                ImGui.EndDisabled();
                            }
                            else
                            {
                                var selectedId = controller.GetSelectedForLayer(layer);
                                var canApply = selectedId.HasValue;
                                ImGui.BeginDisabled(!canApply);
                                if (
                                    SharedUserInterfaces.IconButton(
                                        FontAwesomeIcon.Tshirt,
                                        KinkLinkDimensions.IconButton,
                                        "Apply",
                                        $"personal_apply_{status.SlotName}"
                                    )
                                )
                                {
                                    _ = controller.ApplyItemToLayerAsync(layer, selectedId!.Value);
                                }
                                ImGui.EndDisabled();
                            }

                            ImGui.TableNextColumn();
                            if (isLocked)
                            {
                                var slotLock = controller.GetSlotLock(layer);
                                if (controller.CanUnlockSlot(layer))
                                {
                                    if (
                                        SharedUserInterfaces.IconButton(
                                            FontAwesomeIcon.Lock,
                                            KinkLinkDimensions.IconButton,
                                            "Unlock",
                                            $"personal_unlock_{status.SlotName}"
                                        )
                                    )
                                    {
                                        _ = UnlockSlotAsync(layer);
                                    }
                                }
                                else
                                {
                                    ImGui.BeginDisabled(true);
                                    SharedUserInterfaces.IconButton(
                                        FontAwesomeIcon.Lock,
                                        KinkLinkDimensions.IconButton,
                                        $"Locked ({slotLock?.LockPriority.ToString() ?? "?"})",
                                        $"personal_locked_{status.SlotName}"
                                    );
                                    ImGui.EndDisabled();
                                }
                            }
                            else
                            {
                                bool hasSomethingInSlot = controller
                                    .GetSelectedForLayer(layer)
                                    .HasValue;
                                ImGui.BeginDisabled(!hasSomethingInSlot);
                                if (
                                    SharedUserInterfaces.IconButton(
                                        FontAwesomeIcon.LockOpen,
                                        KinkLinkDimensions.IconButton,
                                        "Lock this slot",
                                        $"personal_lock_{status.SlotName}"
                                    )
                                )
                                {
                                    _ = LockSlotAsync(layer);
                                }
                                ImGui.EndDisabled();
                            }
                        }

                        ImGui.EndTable();
                    }

                    ImGui.EndChild();
                }
            }
        );
        ImGui.EndChild();
    }

    private async Task RemoveSlotAsync(WardrobeLayer layer)
    {
        try
        {
            await controller.RemoveActiveItemAsync(layer);
        }
        catch (Exception ex)
        {
            Plugin.Log.Error(ex, "Failed to remove slot item");
            NotificationHelper.Error("Error", "Failed to remove item.");
        }
    }

    private async Task LockSlotAsync(WardrobeLayer layer)
    {
        try
        {
            await controller.LockSlotAsync(layer);
        }
        catch (Exception ex)
        {
            Plugin.Log.Error(ex, "Failed to lock slot");
            NotificationHelper.Error("Error", "Failed to lock slot.");
        }
    }

    private async Task UnlockSlotAsync(WardrobeLayer layer)
    {
        try
        {
            await controller.UnlockSlotAsync(layer);
        }
        catch (Exception ex)
        {
            Plugin.Log.Error(ex, "Failed to unlock slot");
            NotificationHelper.Error("Error", "Failed to unlock slot.");
        }
    }
}
