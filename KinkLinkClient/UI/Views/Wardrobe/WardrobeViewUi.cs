using System;
using System.Collections.Generic;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using KinkLinkClient.Domain.Interfaces;
using KinkLinkClient.Services;
using KinkLinkClient.Utils;

namespace KinkLinkClient.UI.Views.Wardrobe;

public partial class WardrobeViewUi(WardrobeViewUiController controller) : IDrawable
{
    private WardrobeManager wardrobeManager => controller.WardrobeManager;

    private const float ImportButtonHeight = 40;

    public void Draw()
    {
        var padding = ImGui.GetStyle().WindowPadding;
        var crudWidgetPadding = 500;
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

        // The main screen should display either the import view or the list view for library of wardrobe items.

        // Left column: list of sets/items with filters — stretches to fill
        ImGui.BeginChild("##WardrobeListColumn", new Vector2(0 - crudWidgetPadding, 0), true);
        SharedUserInterfaces.ContentBox(
            "Sets",
            KinkLinkStyle.PanelBackground,
            true,
            () =>
            {
                // Search + filter row
                var contentWidth = ImGui.GetContentRegionAvail().X;
                ImGui.TextUnformatted("Search");
                ImGui.SameLine();
                ImGui.SetNextItemWidth(contentWidth - 120);
                var _searchTemp = controller.SearchFilter;
                if (
                    ImGui.InputTextWithHint(
                        "###WardrobeSearch",
                        "Name or description",
                        ref _searchTemp,
                        64
                    )
                )
                {
                    controller.SearchFilter = _searchTemp;
                }

                ImGui.SameLine();
                ImGui.SetNextItemWidth(100);
                if (
                    ImGui.BeginCombo(
                        "###PairAccessFilterCombo",
                        controller.PairAccessFilter.ToString()
                    )
                )
                {
                    if (ImGui.Selectable(PairAccessFilter.All.ToString()))
                        controller.PairAccessFilter = PairAccessFilter.All;
                    if (ImGui.Selectable(PairAccessFilter.Casual.ToString()))
                        controller.PairAccessFilter = PairAccessFilter.Casual;
                    if (ImGui.Selectable(PairAccessFilter.Serious.ToString()))
                        controller.PairAccessFilter = PairAccessFilter.Serious;
                    if (ImGui.Selectable(PairAccessFilter.Devotional.ToString()))
                        controller.PairAccessFilter = PairAccessFilter.Devotional;
                    ImGui.EndCombo();
                }

                ImGui.Spacing();

                // Single unified sortable table
                if (ImGui.BeginChild("##WardrobeTableChild", new Vector2(0, 0), false))
                {
                    if (
                        ImGui.BeginTable(
                            "##WardrobeTable",
                            5,
                            ImGuiTableFlags.RowBg
                                | ImGuiTableFlags.BordersInnerV
                                | ImGuiTableFlags.ScrollY
                        )
                    )
                    {
                        // Column indices: 0=Name, 1=Layer, 2=Priority, 3=Equipped, 4=Actions
                        ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.WidthStretch);
                        ImGui.TableSetupColumn("Layer", ImGuiTableColumnFlags.WidthFixed, 60);
                        ImGui.TableSetupColumn("Priority", ImGuiTableColumnFlags.WidthFixed, 70);
                        ImGui.TableSetupColumn("Equipped", ImGuiTableColumnFlags.WidthFixed, 75);
                        ImGui.TableSetupColumn("Actions", ImGuiTableColumnFlags.WidthFixed, 130);

                        // Manual sortable header row
                        ImGui.TableNextRow(ImGuiTableRowFlags.Headers);
                        for (int col = 0; col < 5; col++)
                        {
                            ImGui.TableSetColumnIndex(col);
                            if (col == 4)
                            {
                                // Actions column - no sort
                                ImGui.TextUnformatted("Actions");
                            }
                            else
                            {
                                var label = col switch
                                {
                                    0 => "Name",
                                    1 => "Layer",
                                    2 => "Priority",
                                    3 => "Equipped",
                                    _ => "",
                                };
                                var isSorted = controller.SortColumn == col;
                                var display = isSorted
                                    ? (controller.SortAscending ? "▲ " : "▼ ") + label
                                    : label;
                                if (
                                    ImGui.Selectable(
                                        display,
                                        false,
                                        ImGuiSelectableFlags.SpanAllColumns
                                    )
                                )
                                {
                                    if (isSorted)
                                        controller.SortAscending = !controller.SortAscending;
                                    else
                                    {
                                        controller.SortColumn = col;
                                        controller.SortAscending = true;
                                    }
                                }
                            }
                        }

                        var rowMinHeight = 36f;
                        var items = controller.FilteredItems ?? new List<WardrobeItem>();
                        foreach (var item in items)
                        {
                            ImGui.TableNextRow(ImGuiTableRowFlags.None, rowMinHeight);
                            ImGui.TableNextColumn();

                            ImGui.AlignTextToFramePadding();
                            ImGui.TextUnformatted(item.Name ?? "Unnamed");

                            if (ImGui.IsItemHovered())
                            {
                                if (!string.IsNullOrEmpty(item.Description))
                                    SharedUserInterfaces.Tooltip(item.Description);
                            }

                            ImGui.TableNextColumn();
                            ImGui.AlignTextToFramePadding();
                            ImGui.TextUnformatted(item.Layer.ToString());

                            ImGui.TableNextColumn();
                            ImGui.AlignTextToFramePadding();
                            ImGui.TextUnformatted(item.Priority.ToString());

                            ImGui.TableNextColumn();
                            ImGui.AlignTextToFramePadding();
                            ImGui.TextUnformatted(
                                controller.IsItemEquipped(item.Id) ? "Yes" : "No"
                            );

                            ImGui.TableNextColumn();
                            ImGui.PushID(item.Id.ToString());
                            ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(6, 4));
                            var actionButtonSize = new Vector2(32, 28);
                            ImGui.BeginDisabled(!controller.GlamourerApiAvailable);
                            if (
                                SharedUserInterfaces.IconButton(
                                    FontAwesomeIcon.Edit,
                                    actionButtonSize,
                                    controller.GlamourerApiAvailable
                                        ? "Edit"
                                        : "Glamourer API not available"
                                )
                            )
                            {
                                controller.OpenItemEditor(item);
                            }
                            ImGui.EndDisabled();

                            ImGui.SameLine();
                            ImGui.BeginDisabled(
                                !ImGui.IsKeyDown(ImGuiKey.LeftShift)
                                    && !ImGui.IsKeyDown(ImGuiKey.RightShift)
                            );
                            if (
                                SharedUserInterfaces.IconButton(
                                    FontAwesomeIcon.Trash,
                                    actionButtonSize,
                                    "Shift+Click to Delete"
                                )
                            )
                            {
                                if (
                                    ImGui.IsKeyDown(ImGuiKey.LeftShift)
                                    || ImGui.IsKeyDown(ImGuiKey.RightShift)
                                )
                                {
                                    controller.DeleteItem(item.Id);
                                }
                            }
                            ImGui.EndDisabled();
                            ImGui.PopStyleVar();
                            ImGui.PopID();
                        }

                        ImGui.EndTable();
                    }

                    ImGui.EndChild();
                }
            }
        );
        ImGui.EndChild();

        ImGui.SameLine();

        // Right column: import view or empty/default view — fixed width
        ImGui.BeginChild("##WardrobeCRUDColumn", new Vector2(crudWidgetPadding, 0), false);
        var columnWidth = ImGui.GetContentRegionAvail().X;

        if (controller.CurrentView == SubView.Import)
        {
            DrawImportView(columnWidth);
        }
        else if (controller.CurrentView == SubView.Editor)
        {
            DrawImportView(columnWidth, true);
        }
        else
        {
            // Default right-hand content when not importing
            SharedUserInterfaces.ContentBox(
                "WardrobeEmpty",
                KinkLinkStyle.PanelBackground,
                true,
                () =>
                {
                    SharedUserInterfaces.MediumText(
                        "Select a set on left to edit, or click import to bring in new designs."
                    );
                    ImGui.Dummy(new Vector2(0, 8));
                    if (
                        SharedUserInterfaces.IconButton(
                            FontAwesomeIcon.Upload,
                            null,
                            "Open Import View"
                        )
                    )
                    {
                        controller.CurrentView = SubView.Import;
                        controller.GlamourerSearchTerm = string.Empty;
                        controller.SelectedGlamourerDesignId = Guid.Empty;
                        controller.EditedName = string.Empty;
                        controller.EditedDescription = string.Empty;
                    }
                }
            );
        }

        ImGui.EndChild();

        ImGui.EndChild();
    }

    private void DrawSetListEntry(WardrobeItem set, bool isSelected)
    {
        // The list entry should include the basic details of the wardrobe item in table compatible format.
        // It should have an edit and delete button included with it.
        // The edit button toggles the import screen from before with the fields prepopulated
        // The edit toggle should toggle the import view

        ImGui.PushID(set.Id.ToString());

        if (isSelected)
            ImGui.PushStyleColor(ImGuiCol.Header, KinkLinkStyle.PrimaryColor);

        if (
            ImGui.Selectable(
                set.Name ?? "Unnamed",
                isSelected,
                ImGuiSelectableFlags.AllowDoubleClick
            )
        )
        {
            controller.SelectedItem = set.Id;
            if (ImGui.IsMouseDoubleClicked(0))
            {
                // double click toggles import view for editing set
                controller.EditedName = set.Name ?? string.Empty;
                controller.EditedDescription = set.Design?.Description ?? string.Empty;
                controller.SelectedSlotLayer = set.Layer;
                controller.CurrentView = SubView.Import;
            }
        }

        if (isSelected)
            ImGui.PopStyleColor();

        ImGui.SameLine();

        // Edit button
        if (!controller.GlamourerApiAvailable)
        {
            ImGui.BeginDisabled();
        }
        if (
            SharedUserInterfaces.IconButton(
                FontAwesomeIcon.Edit,
                null,
                controller.GlamourerApiAvailable ? "Edit Set" : "Glamourer API not available"
            )
        )
        {
            controller.EditedName = set.Name ?? string.Empty;
            controller.EditedDescription = set.Design?.Description ?? string.Empty;
            controller.SelectedSlotLayer = set.Layer;
            controller.CurrentView = SubView.Import;
        }
        if (!controller.GlamourerApiAvailable)
        {
            ImGui.EndDisabled();
        }

        ImGui.SameLine();

        // Delete button (Shift+Click to prevent accidental deletion)
        if (SharedUserInterfaces.IconButton(FontAwesomeIcon.Trash, null, "Shift+Click to Delete"))
        {
            if (ImGui.IsKeyDown(ImGuiKey.LeftShift) || ImGui.IsKeyDown(ImGuiKey.RightShift))
            {
                controller.DeleteItem(set.Id);
            }
        }

        ImGui.PopID();
    }
}
