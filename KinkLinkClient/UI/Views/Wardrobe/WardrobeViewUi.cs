using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using KinkLinkClient.Domain.Interfaces;
using KinkLinkClient.Services;
using KinkLinkClient.Utils;
using ClientWardrobeItem = KinkLinkClient.Services.WardrobeItem;

namespace KinkLinkClient.UI.Views.Wardrobe;

public partial class WardrobeViewUi(WardrobeViewUiController controller) : IDrawable
{
    private WardrobeManager wardrobeManager => controller.WardrobeManager;

    private const float ImportButtonHeight = 40;
    private const float ListPanelWidth = 350;

    public void Draw()
    {
        var padding = ImGui.GetStyle().WindowPadding;
        ImGui.BeginChild("##WardrobeUi", Vector2.Zero, false, KinkLinkStyle.ContentFlags);
        var begin = ImGui.GetCursorPosY();

        controller.HoveredItemId = null;

        SharedUserInterfaces.ContentBox(
            "Wardrobe",
            KinkLinkStyle.PanelBackground,
            true,
            () =>
            {
                SharedUserInterfaces.BigTextCentered("Restraints");
            }
        );

        var headerHeight = ImGui.GetCursorPosY() - begin;

        if (controller.CurrentView == KinkLinkClient.UI.Views.Wardrobe.SubView.Active)
        {
            DrawActiveView();
        }
        else
        {
            var width = ImGui.GetWindowWidth();
            var windowHeight = ImGui.GetWindowHeight();

            ImGui.Columns(2, "WardrobeColumns", true);
            ImGui.SetColumnWidth(0, ListPanelWidth);

            DrawListPanel();

            ImGui.NextColumn();

            var showRightPanel =
                controller.CurrentView == KinkLinkClient.UI.Views.Wardrobe.SubView.Editor
                || controller.CurrentView == KinkLinkClient.UI.Views.Wardrobe.SubView.Import
                || controller.SelectedItem.HasValue
                || controller.HoveredItemId.HasValue;

            if (showRightPanel)
            {
                DrawRightPanel();
            }
            else
            {
                SharedUserInterfaces.ContentBox(
                    "EmptyRightPanel",
                    KinkLinkStyle.PanelBackground,
                    true,
                    () =>
                    {
                        ImGui.TextColored(
                            ImGuiColors.DalamudGrey,
                            "Hover over or select an item to view details"
                        );
                    }
                );
            }

            ImGui.Columns(1);
        }

        ImGui.EndChild();
    }

    private void DrawListPanel()
    {
        var padding = ImGui.GetStyle().WindowPadding;
        var panelWidth = ListPanelWidth - padding.X * 2;

        SharedUserInterfaces.ContentBox(
            "ListTabs",
            KinkLinkStyle.PanelBackground,
            false,
            () =>
            {
                SharedUserInterfaces.MediumText("Library");
            }
        );

        SharedUserInterfaces.ContentBox(
            "ListActions",
            KinkLinkStyle.PanelBackground,
            false,
            () =>
            {
                var newButtonWidth = 40f;
                var newButtonX = panelWidth - newButtonWidth - padding.X;
                ImGui.SetCursorPosX(newButtonX);
                if (SharedUserInterfaces.IconButton(FontAwesomeIcon.Plus, null, "New Item"))
                {
                    controller.ResetEditorFields();
                    controller.OpenItemEditor(
                        new KinkLinkClient.Services.WardrobeItem { Id = Guid.Empty }
                    );
                }
            }
        );

        SharedUserInterfaces.ContentBox(
            "ListSearchPairAccess",
            KinkLinkStyle.PanelBackground,
            false,
            () =>
            {
                var labelWidth = 60f;
                var searchWidth = (panelWidth - padding.X * 2 - labelWidth - 50) * 0.6f;
                var filterWidth = searchWidth - padding.X;
                var comboWidth =
                    panelWidth - padding.X * 2 - labelWidth - searchWidth - padding.X * 2;

                ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 5);

                ImGui.Text("Search");
                ImGui.SameLine(labelWidth);
                ImGui.SetNextItemWidth(filterWidth);
                var searchTerm = controller.SearchFilter;
                if (ImGui.InputTextWithHint("##SearchFilter", "Filter...", ref searchTerm, 32))
                    controller.SearchFilter = searchTerm;

                ImGui.SameLine(labelWidth + searchWidth + padding.X);
                ImGui.Text("Access");
                ImGui.SameLine(labelWidth + searchWidth + padding.X + 50);
                ImGui.SetNextItemWidth(comboWidth);
                var currentFilter = controller.PairAccessFilter.ToString();
                if (ImGui.BeginCombo("##PairAccessFilter", currentFilter))
                {
                    foreach (PairAccessFilter filter in Enum.GetValues<PairAccessFilter>())
                    {
                        if (ImGui.Selectable(filter.ToString()))
                            controller.PairAccessFilter = filter;
                    }
                    ImGui.EndCombo();
                }
            }
        );

        var listHeight = 400f;
        SharedUserInterfaces.ContentBox(
            "ListItems",
            KinkLinkStyle.PanelBackground,
            true,
            () =>
            {
                if (ImGui.BeginChild("##ItemList", new Vector2(0, listHeight), false))
                {
                    if (
                        ImGui.BeginTable(
                            "##WardrobeTable",
                            4,
                            ImGuiTableFlags.RowBg | ImGuiTableFlags.BordersInnerV
                        )
                    )
                    {
                        ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.WidthStretch);
                        ImGui.TableSetupColumn("Layer", ImGuiTableColumnFlags.WidthFixed, 80);
                        ImGui.TableSetupColumn("Priority", ImGuiTableColumnFlags.WidthFixed, 90);
                        ImGui.TableSetupColumn("Actions", ImGuiTableColumnFlags.WidthFixed, 120);
                        ImGui.TableHeadersRow();

                        var items = controller.FilteredItems ?? new List<WardrobeItem>();
                        var sets = controller.FilteredSets ?? new List<WardrobeItem>();

                        foreach (var entry in items.Concat(sets))
                        {
                            ImGui.TableNextRow();
                            ImGui.TableNextColumn();

                            var name = entry.Name;
                            if (
                                ImGui.Selectable(
                                    $"{name}##sel_{entry.Id}",
                                    controller.SelectedItem == entry.Id
                                )
                            )
                            {
                                controller.SelectedItem = entry.Id;
                            }

                            if (ImGui.IsItemHovered())
                                controller.HoveredItemId = entry.Id;

                            ImGui.TableNextColumn();
                            ImGui.Text(entry.Layer.ToString());

                            ImGui.TableNextColumn();
                            ImGui.Text(entry.Priority.ToString());

                            ImGui.TableNextColumn();
                            ImGui.PushID(entry.Id.ToString());
                            if (ImGui.Button("Edit"))
                            {
                                controller.OpenItemEditor(entry);
                            }
                            ImGui.SameLine();
                            var keyShift = ImGui.GetIO().KeyShift;
                            ImGui.PushStyleVar(ImGuiStyleVar.Alpha, keyShift ? 1.0f : 0.5f);
                            if (ImGui.Button("Del"))
                            {
                                if (keyShift)
                                {
                                    controller.DeleteItem(entry.Id);
                                }
                            }
                            ImGui.PopStyleVar();
                            ImGui.PopID();
                        }

                        ImGui.EndTable();
                    }

                    ImGui.EndChild();
                }
            }
        );
    }

    private void DrawItemListEntry(ClientWardrobeItem item, bool isSelected, bool isModSet)
    {
        var padding = ImGui.GetStyle().WindowPadding;
        var rowHeight = 30f;
        var buttonSize = 24f;
        var equipButtonWidth = 50f;
        var deleteButtonWidth = 40f;

        var isEquipped = controller.IsItemEquipped(item.Id);
        var slotName = isModSet ? "BaseSet" : item.Slot.ToString();
        var slotLocked = !isModSet && controller.IsSlotLocked(slotName);
        var canEquip = !slotLocked || isEquipped;

        ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(4, 2));

        var cursorStart = ImGui.GetCursorPosY();
        var textAreaWidth =
            ListPanelWidth - padding.X * 3 - equipButtonWidth - deleteButtonWidth - 60;

        ImGui.SetCursorPosX(padding.X);
        ImGui.SetCursorPosY(cursorStart);
        ImGui.Text(item.Name);

        ImGui.SetCursorPosX(padding.X);
        ImGui.SetCursorPosY(cursorStart + rowHeight * 0.5f);
        var descColor = ImGuiColors.DalamudGrey;
        ImGui.TextColored(descColor, item.Description);

        var slotText = isModSet ? "Mod Set" : item.Slot.ToString();
        ImGui.SetCursorPosY(cursorStart);
        ImGui.SameLine();
        ImGui.SetCursorPosX(
            ListPanelWidth - padding.X * 3 - equipButtonWidth - deleteButtonWidth - 60
        );
        if (slotLocked && !isEquipped)
        {
            ImGui.TextColored(ImGuiColors.ParsedOrange, $"🔒 {slotText}");
        }
        else
        {
            ImGui.TextColored(descColor, slotText);
        }

        ImGui.SetCursorPosY(cursorStart);
        ImGui.SameLine();
        ImGui.SetCursorPosX(ListPanelWidth - padding.X * 3 - equipButtonWidth - deleteButtonWidth);

        var equipLabel = isEquipped ? $"Remove##Rem_{item.Id}" : $"Equip##Equip_{item.Id}";
        if (canEquip)
        {
            if (ImGui.Button(equipLabel, new Vector2(equipButtonWidth, buttonSize)))
            {
                _ = TogglePieceEquipAsync(item, isEquipped);
            }
        }
        else
        {
            ImGui.PushStyleVar(ImGuiStyleVar.Alpha, 0.5f);
            ImGui.Button(equipLabel, new Vector2(equipButtonWidth, buttonSize));
            ImGui.PopStyleVar();
            SharedUserInterfaces.Tooltip("Slot is locked by another user");
        }

        ImGui.SameLine();
        ImGui.SetCursorPosX(ListPanelWidth - padding.X * 3 - deleteButtonWidth);

        var keyShift = ImGui.GetIO().KeyShift;
        ImGui.PushStyleVar(ImGuiStyleVar.Alpha, keyShift ? 1.0f : 0.5f);
        if (ImGui.Button($"Del##Del_{item.Id}", new Vector2(deleteButtonWidth, buttonSize)))
        {
            if (keyShift)
            {
                controller.DeleteItem(item.Id);
            }
        }
        ImGui.PopStyleVar();

        ImGui.PopStyleVar();

        ImGui.SetCursorPosY(cursorStart);
        ImGui.SetCursorPosX(padding.X);
        ImGui.SetCursorPosY(ImGui.GetCursorPosY() - rowHeight * 2 + rowHeight);

        if (
            ImGui.InvisibleButton(
                $"##ItemEntry_{item.Id}",
                new Vector2(textAreaWidth, rowHeight * 2)
            )
        )
        {
            controller.SelectedItem = item.Id;
            controller.OpenItemEditor(item);
        }

        if (ImGui.IsItemHovered(ImGuiHoveredFlags.None))
        {
            controller.HoveredItemId = item.Id;
        }

        ImGui.SetCursorPosY(cursorStart + rowHeight * 2);
    }

    private async Task TogglePieceEquipAsync(ClientWardrobeItem item, bool isEquipped)
    {
        try
        {
            if (isEquipped)
            {
                await controller.RemoveActiveItemAsync(item.Layer);
            }
            else
            {
                await controller.ApplyItemToLayerAsync(item.Layer, item.Id);
            }
        }
        catch (Exception ex)
        {
            Plugin.Log.Error(ex, "Failed to toggle piece equip state");
            NotificationHelper.Error("Error", "Failed to update equip state.");
        }
    }

    private void DrawSetListEntry(WardrobeItem set, bool isSelected)
    {
        var padding = ImGui.GetStyle().WindowPadding;
        var rowHeight = 30f;
        var buttonSize = 24f;
        var equipButtonWidth = 50f;
        var deleteButtonWidth = 40f;

        var isEquipped = controller.IsItemEquipped(set.Id);
        var baseSetLocked = controller.IsSlotLocked("BaseSet");
        var canEquip = !baseSetLocked || isEquipped;

        ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(4, 2));

        var cursorStart = ImGui.GetCursorPosY();
        var textAreaWidth = ListPanelWidth - padding.X * 3 - equipButtonWidth - deleteButtonWidth;

        ImGui.SetCursorPosX(padding.X);
        ImGui.SetCursorPosY(cursorStart);
        ImGui.Text(set.Name);

        ImGui.SetCursorPosX(padding.X);
        ImGui.SetCursorPosY(cursorStart + rowHeight * 0.5f);
        var descColor = ImGuiColors.DalamudGrey;
        ImGui.TextColored(descColor, set.Description);

        ImGui.SetCursorPosY(cursorStart);
        ImGui.SameLine();
        ImGui.SetCursorPosX(ListPanelWidth - padding.X * 3 - equipButtonWidth - deleteButtonWidth);

        var equipLabel = isEquipped ? $"Remove##Rem_{set.Id}" : $"Equip##Equip_{set.Id}";
        if (canEquip)
        {
            if (ImGui.Button(equipLabel, new Vector2(equipButtonWidth, buttonSize)))
            {
                _ = ToggleSetEquipAsync(set, isEquipped);
            }
        }
        else
        {
            ImGui.PushStyleVar(ImGuiStyleVar.Alpha, 0.5f);
            ImGui.Button(equipLabel, new Vector2(equipButtonWidth, buttonSize));
            ImGui.PopStyleVar();
            SharedUserInterfaces.Tooltip("BaseSet is locked by another user");
        }

        ImGui.SameLine();
        ImGui.SetCursorPosX(ListPanelWidth - padding.X * 3 - deleteButtonWidth);

        var keyShift = ImGui.GetIO().KeyShift;
        ImGui.PushStyleVar(ImGuiStyleVar.Alpha, keyShift ? 1.0f : 0.5f);
        if (ImGui.Button($"Del_{set.Id}", new Vector2(deleteButtonWidth, buttonSize)))
        {
            if (keyShift)
            {
                controller.DeleteItem(set.Id);
            }
        }
        ImGui.PopStyleVar();

        ImGui.PopStyleVar();

        ImGui.SetCursorPosY(cursorStart);
        ImGui.SetCursorPosX(padding.X);
        ImGui.SetCursorPosY(ImGui.GetCursorPosY() - rowHeight * 2 + rowHeight);

        if (
            ImGui.InvisibleButton($"##SetEntry_{set.Id}", new Vector2(textAreaWidth, rowHeight * 2))
        )
        {
            controller.SelectedItem = set.Id;
            controller.OpenItemEditor(set);
        }

        if (ImGui.IsItemHovered(ImGuiHoveredFlags.None))
        {
            controller.HoveredItemId = set.Id;
        }

        ImGui.SetCursorPosY(cursorStart + rowHeight * 2);
    }

    private async Task ToggleSetEquipAsync(WardrobeItem set, bool isEquipped)
    {
        try
        {
            if (isEquipped)
            {
                await controller.RemoveActiveItemAsync(
                    KinkLinkCommon.Domain.Wardrobe.WardrobeLayer.Outfit
                );
            }
            else
            {
                await controller.ApplySetAsync(set.Name);
            }
        }
        catch (Exception ex)
        {
            Plugin.Log.Error(ex, "Failed to toggle set equip state");
            NotificationHelper.Error("Error", "Failed to update equip state.");
        }
    }

    private void DrawRightPanel()
    {
        var padding = ImGui.GetStyle().WindowPadding;
        var totalWidth = ImGui.GetContentRegionAvail().X;
        var columnWidth = totalWidth - padding.X;

        if (controller.CurrentView == KinkLinkClient.UI.Views.Wardrobe.SubView.Editor)
        {
            DrawEditorView(columnWidth);
        }
        else if (controller.CurrentView == KinkLinkClient.UI.Views.Wardrobe.SubView.Import)
        {
            DrawImportView(columnWidth);
        }
        else
        {
            DrawDetailView(columnWidth);
        }
    }

    private void DrawDetailView(float columnWidth)
    {
        var padding = ImGui.GetStyle().WindowPadding;
        var contentWidth = columnWidth - padding.X * 2;

        var hoveredPieceId = controller.HoveredItemId ?? controller.SelectedItem;
        var hoveredSetId = controller.HoveredItemId ?? controller.SelectedItem;

        if (hoveredPieceId.HasValue)
        {
            var item = controller.FilteredItems?.FirstOrDefault(i => i.Id == hoveredPieceId.Value);
            if (item != null)
            {
                SharedUserInterfaces.ContentBox(
                    "DetailName",
                    KinkLinkStyle.PanelBackground,
                    true,
                    () => SharedUserInterfaces.MediumText(item.Name)
                );

                SharedUserInterfaces.ContentBox(
                    "DetailDescription",
                    KinkLinkStyle.PanelBackground,
                    true,
                    () => ImGui.Text(item.Description)
                );

                SharedUserInterfaces.ContentBox(
                    "DetailSlot",
                    KinkLinkStyle.PanelBackground,
                    true,
                    () =>
                    {
                        ImGui.Text($"Slot: {item.Slot}");
                        if (item.Item != null)
                        {
                            ImGui.Text($"Item ID: {item.Item.ItemId}");
                            ImGui.Text($"Dye 1: {item.Item.Stain}");
                            ImGui.Text($"Dye 2: {item.Item.Stain2}");
                        }
                    }
                );

                SharedUserInterfaces.ContentBox(
                    "DetailPriority",
                    KinkLinkStyle.PanelBackground,
                    true,
                    () => ImGui.Text($"Priority: {item.Priority}")
                );

                SharedUserInterfaces.ContentBox(
                    "DetailActions",
                    KinkLinkStyle.PanelBackground,
                    false,
                    () =>
                    {
                        var isEquipped = controller.IsItemEquipped(item.Id);
                        var buttonWidth = contentWidth;

                        if (
                            ImGui.Button(
                                isEquipped ? "Remove" : "Equip",
                                new Vector2(buttonWidth, 35)
                            )
                        )
                        {
                            _ = TogglePieceEquipAsync(item, isEquipped);
                        }
                    }
                );
            }
        }
        else if (hoveredSetId.HasValue)
        {
            var set = controller.FilteredSets?.FirstOrDefault(s => s.Id == hoveredSetId.Value);
            if (set != null)
            {
                SharedUserInterfaces.ContentBox(
                    "DetailName",
                    KinkLinkStyle.PanelBackground,
                    true,
                    () => SharedUserInterfaces.MediumText(set.Name)
                );

                SharedUserInterfaces.ContentBox(
                    "DetailDescription",
                    KinkLinkStyle.PanelBackground,
                    true,
                    () => ImGui.Text(set.Description)
                );

                SharedUserInterfaces.ContentBox(
                    "DetailPriority",
                    KinkLinkStyle.PanelBackground,
                    true,
                    () => ImGui.Text($"Priority: {set.Priority}")
                );

                SharedUserInterfaces.ContentBox(
                    "DetailActions",
                    KinkLinkStyle.PanelBackground,
                    false,
                    () =>
                    {
                        var isEquipped = controller.IsItemEquipped(set.Id);
                        var buttonWidth = contentWidth;

                        if (
                            ImGui.Button(
                                isEquipped ? "Remove" : "Equip",
                                new Vector2(buttonWidth, 35)
                            )
                        )
                        {
                            _ = ToggleSetEquipAsync(set, isEquipped);
                        }
                    }
                );
            }
        }
    }

    private void DrawActiveView()
    {
        var padding = ImGui.GetStyle().WindowPadding;
        var width = ImGui.GetWindowWidth() - padding.X * 2;

        SharedUserInterfaces.ContentBox(
            "ActiveHeader",
            KinkLinkStyle.PanelBackground,
            true,
            () =>
            {
                SharedUserInterfaces.MediumText("Dressup");

                // Randomize button left of Back
                ImGui.SameLine(width - 170);
                if (ImGui.Button("Randomize", new Vector2(80, 30)))
                {
                    _ = controller.WardrobeManager.RandomizeActiveAsync();
                }

                ImGui.SameLine();
                if (ImGui.Button("Back", new Vector2(80, 30)))
                {
                    controller.CurrentView = KinkLinkClient.UI.Views.Wardrobe.SubView.List;
                }
            }
        );

        var statuses = controller.GetActiveSlotStatuses();

        SharedUserInterfaces.ContentBox(
            "ActiveSlots",
            KinkLinkStyle.PanelBackground,
            true,
            () =>
            {
                if (ImGui.BeginChild("##ActiveSlotList", new Vector2(0, 0), true))
                {
                    if (
                        ImGui.BeginTable(
                            "##DressupTable",
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
                            var names = candidates.Select(i => i.Name).ToArray();

                            // build preview string
                            var currentSelection = controller.GetSelectedForLayer(layer);
                            var preview = currentSelection.HasValue
                                ? (
                                    controller
                                        .WardrobeManager.GetItemById(currentSelection.Value)
                                        ?.Name
                                    ?? "None"
                                )
                                : "None";

                            if (ImGui.BeginCombo($"##combo_{status.SlotName}", preview))
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

                            ImGui.TableNextColumn();
                            var isLocked = controller.IsSlotLocked(status.SlotName);
                            var canRemove =
                                !isLocked || controller.CanRemoveFromSlot(status.SlotName);

                            if (controller.WardrobeManager.IsLayerActive(layer))
                            {
                                if (canRemove)
                                {
                                    if (ImGui.Button($"Remove##{status.SlotName}"))
                                    {
                                        _ = controller.RemoveActiveItemAsync(layer);
                                    }
                                }
                                else
                                {
                                    ImGui.PushStyleVar(ImGuiStyleVar.Alpha, 0.5f);
                                    ImGui.Button($"Remove##{status.SlotName}", new Vector2(80, 24));
                                    ImGui.PopStyleVar();
                                }
                            }
                            else
                            {
                                var selectedId = controller.GetSelectedForLayer(layer);
                                var canApply = selectedId.HasValue;
                                ImGui.PushStyleVar(ImGuiStyleVar.Alpha, canApply ? 1.0f : 0.5f);
                                if (ImGui.Button("Apply", new Vector2(80, 24)))
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
                                var lockInfo = controller.GetSlotLock(status.SlotName);
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
    }

    private async Task RemoveSlotAsync(string slotName)
    {
        try
        {
            var layer = KinkLinkClient.Services.WardrobeSlotHelper.GetLayerFromName(slotName);
            await controller.RemoveActiveItemAsync(layer);
        }
        catch (Exception ex)
        {
            Plugin.Log.Error(ex, "Failed to remove slot item");
            NotificationHelper.Error("Error", "Failed to remove item.");
        }
    }

    private async Task LockSlotAsync(string slotName)
    {
        // TODO: Reimplement with new lock assumptions
    }

    private async Task UnlockSlotAsync(string slotName)
    {
        // TODO:: Reimplement with new lock assumptions
    }
}
