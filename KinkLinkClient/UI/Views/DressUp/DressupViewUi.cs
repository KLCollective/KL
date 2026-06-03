using System;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Colors;
using KinkLinkClient.Domain.Interfaces;
using KinkLinkClient.Services;
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
                if (ImGui.BeginChild("##PersonalSlotList", new Vector2(0, 0), true))
                {
                    if (
                        ImGui.BeginTable(
                            "##PersonalDressupTable",
                            4,
                            ImGuiTableFlags.RowBg | ImGuiTableFlags.BordersInnerV
                        )
                    )
                    {
                        ImGui.TableSetupColumn("Slot", ImGuiTableColumnFlags.WidthFixed, 120);
                        ImGui.TableSetupColumn("Item", ImGuiTableColumnFlags.WidthStretch);
                        ImGui.TableSetupColumn("Action", ImGuiTableColumnFlags.WidthFixed, 100);
                        ImGui.TableSetupColumn("Lock", ImGuiTableColumnFlags.WidthFixed, 90);
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
                                var pending = controller
                                    .WardrobeManager.GetItemById(pendingId.Value);
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
                            var canRemove = !isLocked || controller.CanRemoveFromSlot(layer);

                            if (controller.WardrobeManager.IsLayerActive(layer))
                            {
                                if (canRemove)
                                {
                                    if (
                                        ImGui.Button(
                                            $"Remove##personal_{status.SlotName}",
                                            new Vector2(80, 24)
                                        )
                                    )
                                    {
                                        _ = RemoveSlotAsync(layer);
                                    }
                                }
                                else
                                {
                                    ImGui.PushStyleVar(ImGuiStyleVar.Alpha, 0.5f);
                                    ImGui.Button(
                                        $"Remove##personal_{status.SlotName}",
                                        new Vector2(80, 24)
                                    );
                                    ImGui.PopStyleVar();
                                }
                            }
                            else
                            {
                                var selectedId = controller.GetSelectedForLayer(layer);
                                var canApply = selectedId.HasValue;
                                ImGui.PushStyleVar(ImGuiStyleVar.Alpha, canApply ? 1.0f : 0.5f);
                                if (
                                    ImGui.Button(
                                        $"Apply##personal_{status.SlotName}",
                                        new Vector2(80, 24)
                                    )
                                )
                                {
                                    if (canApply)
                                    {
                                        _ = controller.ApplyItemToLayerAsync(
                                            layer,
                                            selectedId.Value
                                        );
                                    }
                                }
                                ImGui.PopStyleVar();
                            }

                            ImGui.TableNextColumn();
                            if (isLocked)
                            {
                                var lockInfo = controller.GetSlotLock(layer);
                                ImGui.TextColored(ImGuiColors.ParsedOrange, "Locked");
                                if (ImGui.IsItemHovered())
                                {
                                    var priorityText =
                                        lockInfo?.LockPriority.ToString() ?? "Unknown";
                                    SharedUserInterfaces.Tooltip($"Locked ({priorityText})");
                                }
                            }
                            else
                            {
                                ImGui.Text("Open");
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
        // TODO: Reimplement with new lock assumptions
    }

    private async Task UnlockSlotAsync(WardrobeLayer layer)
    {
        // TODO:: Reimplement with new lock assumptions
    }
}
