using System;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using KinkLinkClient.Domain.Interfaces;
using KinkLinkClient.Services;
using KinkLinkClient.Utils;
using KinkLinkCommon.Dependencies.Glamourer;
using KinkLinkCommon.Dependencies.Glamourer.Components;
using KinkLinkCommon.Domain.Enums;
using ClientWardrobeItem = KinkLinkClient.Services.WardrobeItem;

namespace KinkLinkClient.UI.Views.Wardrobe;

public partial class WardrobeViewUi(WardrobeViewUiController controller) : IDrawable
{
    private WardrobeService wardrobeService => controller.WardrobeService;

    private const float ImportButtonHeight = 40;
    private const float ListPanelWidth = 350;

    public void Draw()
    {
        var padding = ImGui.GetStyle().WindowPadding;
        ImGui.BeginChild("##WardrobeUi", Vector2.Zero, false, KinkLinkStyle.ContentFlags);
        var begin = ImGui.GetCursorPosY();

        controller.HoveredPieceId = null;
        controller.HoveredSetId = null;

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

        if (controller.CurrentView == SubView.Active)
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
                controller.CurrentView == SubView.Editor
                || controller.CurrentView == SubView.Import
                || controller.SelectedPieceId.HasValue
                || controller.SelectedSetId.HasValue
                || controller.HoveredPieceId.HasValue
                || controller.HoveredSetId.HasValue;

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
                var buttonWidth = (panelWidth - padding.X) / 3;
                var buttonHeight = 30f;

                if (controller.CurrentTab == ListTab.IndividualItems)
                {
                    ImGui.PushStyleColor(ImGuiCol.Button, KinkLinkStyle.PrimaryColor);
                    if (ImGui.Button("Items", new Vector2(buttonWidth, buttonHeight)))
                        controller.CurrentTab = ListTab.IndividualItems;
                    ImGui.PopStyleColor();
                }
                else
                {
                    if (ImGui.Button("Items", new Vector2(buttonWidth, buttonHeight)))
                        controller.CurrentTab = ListTab.IndividualItems;
                }

                ImGui.SameLine();

                if (controller.CurrentTab == ListTab.Sets)
                {
                    ImGui.PushStyleColor(ImGuiCol.Button, KinkLinkStyle.PrimaryColor);
                    if (ImGui.Button("Sets", new Vector2(buttonWidth, buttonHeight)))
                        controller.CurrentTab = ListTab.Sets;
                    ImGui.PopStyleColor();
                }
                else
                {
                    if (ImGui.Button("Sets", new Vector2(buttonWidth, buttonHeight)))
                        controller.CurrentTab = ListTab.Sets;
                }

                ImGui.SameLine();

                if (controller.CurrentView == SubView.Active)
                {
                    ImGui.PushStyleColor(ImGuiCol.Button, KinkLinkStyle.PrimaryColor);
                    if (ImGui.Button("Active", new Vector2(buttonWidth, buttonHeight)))
                        controller.CurrentView = SubView.Active;
                    ImGui.PopStyleColor();
                }
                else
                {
                    if (ImGui.Button("Active", new Vector2(buttonWidth, buttonHeight)))
                        controller.CurrentView = SubView.Active;
                }
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
                if (SharedUserInterfaces.IconButton(FontAwesomeIcon.Plus, null, "New Item/Set"))
                {
                    if (controller.CurrentTab == ListTab.IndividualItems)
                        controller.OpenEditor(null);
                    else
                        controller.CurrentView = SubView.Import;
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
                    if (!ImGui.IsItemDeactivated())
                        ImGui.EndCombo();
                }
            }
        );

        var listHeight = 300f;
        SharedUserInterfaces.ContentBox(
            "ListItems",
            KinkLinkStyle.PanelBackground,
            true,
            () =>
            {
                if (ImGui.BeginChild("##ItemList", new Vector2(0, listHeight), false))
                {
                    if (controller.CurrentTab == ListTab.IndividualItems)
                    {
                        var items = controller.FilteredItems;
                        if (items != null)
                        {
                            foreach (var item in items)
                            {
                                var isSelected = controller.SelectedPieceId == item.Id;
                                var isModSet =
                                    item.Slot == GlamourerEquipmentSlot.None && item.Item == null;

                                DrawItemListEntry(item, isSelected, isModSet);
                            }
                        }
                    }
                    else
                    {
                        var sets = controller.FilteredSets;
                        if (sets != null)
                        {
                            foreach (var set in sets)
                            {
                                var isSelected = controller.SelectedSetId == set.Id;
                                DrawSetListEntry(set, isSelected);
                            }
                        }
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

        var isEquipped = controller.IsPieceEquipped(item.Id);
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
                controller.DeletePiece(item.Id);
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
            controller.SelectedPieceId = item.Id;
            controller.OpenEditor(item);
        }

        if (ImGui.IsItemHovered(ImGuiHoveredFlags.None))
        {
            controller.HoveredPieceId = item.Id;
        }

        ImGui.SetCursorPosY(cursorStart + rowHeight * 2);
    }

    private async Task TogglePieceEquipAsync(ClientWardrobeItem item, bool isEquipped)
    {
        try
        {
            if (isEquipped)
            {
                await controller.RemoveSlotItemAsync(item.Slot.ToString());
            }
            else
            {
                await controller.ApplyPieceAsync(item);
            }
        }
        catch (Exception ex)
        {
            Plugin.Log.Error(ex, "Failed to toggle piece equip state");
            NotificationHelper.Error("Error", "Failed to update equip state.");
        }
    }

    private void DrawSetListEntry(WardrobeSet set, bool isSelected)
    {
        var padding = ImGui.GetStyle().WindowPadding;
        var rowHeight = 30f;
        var buttonSize = 24f;
        var equipButtonWidth = 50f;
        var deleteButtonWidth = 40f;

        var isEquipped = controller.IsSetEquipped(set.Id);
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
                controller.DeleteSet(set.Id);
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
            controller.SelectedSetId = set.Id;
            controller.OpenSetEditor(set);
        }

        if (ImGui.IsItemHovered(ImGuiHoveredFlags.None))
        {
            controller.HoveredSetId = set.Id;
        }

        ImGui.SetCursorPosY(cursorStart + rowHeight * 2);
    }

    private async Task ToggleSetEquipAsync(WardrobeSet set, bool isEquipped)
    {
        try
        {
            if (isEquipped)
            {
                await controller.RemoveActiveSetAsync();
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

        if (controller.CurrentView == SubView.Editor)
        {
            DrawEditorView(columnWidth);
        }
        else if (controller.CurrentView == SubView.Import)
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

        var hoveredPieceId = controller.HoveredPieceId ?? controller.SelectedPieceId;
        var hoveredSetId = controller.HoveredSetId ?? controller.SelectedSetId;

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
                        var isEquipped = controller.IsPieceEquipped(item.Id);
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
                        var isEquipped = controller.IsSetEquipped(set.Id);
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
                SharedUserInterfaces.MediumText("Active Wardrobe");

                ImGui.SameLine(width - 80);
                if (ImGui.Button("Back", new Vector2(80, 30)))
                {
                    controller.CurrentView = SubView.List;
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
                    foreach (var status in statuses)
                    {
                        DrawActiveSlotEntry(status);
                    }
                    ImGui.EndChild();
                }
            }
        );
    }

    private void DrawActiveSlotEntry(SlotStatus status)
    {
        var padding = ImGui.GetStyle().WindowPadding;
        var width = ImGui.GetWindowWidth() - padding.X * 4;

        var isLocked = controller.IsSlotLocked(status.SlotName);
        var canRemove = !isLocked || controller.CanRemoveFromSlot(status.SlotName);
        var lockInfo = controller.GetSlotLock(status.SlotName);

        SharedUserInterfaces.ContentBox(
            $"ActiveSlot_{status.SlotName}",
            KinkLinkStyle.PanelBackground,
            false,
            () =>
            {
                ImGui.Text(status.SlotName);
                ImGui.SameLine();
                ImGui.TextColored(
                    status.HasItem ? ImGuiColors.ParsedGreen : ImGuiColors.DalamudGrey,
                    status.HasItem ? status.ItemDisplay ?? "Active" : "Empty"
                );

                ImGui.SameLine(width - 200);

                if (canRemove)
                {
                    if (ImGui.Button($"Remove##{status.SlotName}", new Vector2(60, 24)))
                    {
                        _ = RemoveSlotAsync(status.SlotName);
                    }
                }
                else
                {
                    ImGui.PushStyleVar(ImGuiStyleVar.Alpha, 0.5f);
                    ImGui.Button($"Remove##{status.SlotName}", new Vector2(60, 24));
                    ImGui.PopStyleVar();
                    SharedUserInterfaces.Tooltip("Slot is locked by another user");
                }

                ImGui.SameLine(width - 130);

                if (isLocked)
                {
                    ImGui.PushStyleColor(ImGuiCol.Button, ImGuiColors.ParsedOrange);
                    if (ImGui.Button($"Unlock##{status.SlotName}", new Vector2(60, 24)))
                    {
                        _ = UnlockSlotAsync(status.SlotName);
                    }
                    ImGui.PopStyleColor();

                    var priorityText = lockInfo?.LockPriority.ToString() ?? "Unknown";
                    SharedUserInterfaces.Tooltip($"Locked ({priorityText})");
                }
                else
                {
                    if (ImGui.Button($"Lock##{status.SlotName}", new Vector2(60, 24)))
                    {
                        _ = LockSlotAsync(status.SlotName);
                    }
                }
            }
        );
    }

    private async Task RemoveSlotAsync(string slotName)
    {
        try
        {
            await controller.RemoveSlotItemAsync(slotName);
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
