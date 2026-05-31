using System;
using System.Collections.Generic;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using KinkLinkClient.Domain.Interfaces;
using KinkLinkClient.Services;
using KinkLinkClient.Utils;

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

        // The main screen should display either the import view or the list view for library of wardrobe items.

        // Left column: list of sets/items with filters
        ImGui.BeginChild("##WardrobeListColumn", new Vector2(ListPanelWidth, 0), true);
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
                if (ImGui.InputTextWithHint("###WardrobeSearch", "Name or description", ref _searchTemp, 64))
                {
                    controller.SearchFilter = _searchTemp;
                }

                ImGui.SameLine();
                ImGui.SetNextItemWidth(100);
                if (ImGui.BeginCombo("###PairAccessFilterCombo", controller.PairAccessFilter.ToString()))
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

                // Tabs for Sets / Items
                if (ImGui.BeginTabBar("##WardrobeListTabs"))
                {
                    if (ImGui.BeginTabItem("Sets"))
                    {
                        var sets = controller.FilteredSets ?? new List<WardrobeItem>();
                        if (ImGui.BeginChild("##SetsTableChild", new Vector2(0, 0), false))
                        {
                            if (
                                ImGui.BeginTable(
                                    "##WardrobeSetsTable",
                                    4,
                                    ImGuiTableFlags.RowBg | ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.ScrollY
                                )
                            )
                            {
                                ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.WidthStretch);
                                ImGui.TableSetupColumn("Layer", ImGuiTableColumnFlags.WidthFixed, 80);
                                ImGui.TableSetupColumn("Priority", ImGuiTableColumnFlags.WidthFixed, 90);
                                ImGui.TableSetupColumn("Actions", ImGuiTableColumnFlags.WidthFixed, 90);
                                ImGui.TableHeadersRow();

                                foreach (var set in sets)
                                {
                                    ImGui.TableNextRow();
                                    ImGui.TableNextColumn();

                                    var isSelected =
                                        controller.SelectedItem.HasValue && controller.SelectedItem.Value == set.Id;

                                    if (
                                        ImGui.Selectable(
                                            set.Name ?? "Unnamed",
                                            isSelected,
                                            ImGuiSelectableFlags.AllowDoubleClick | ImGuiSelectableFlags.SpanAllColumns
                                        )
                                    )
                                    {
                                        controller.SelectedItem = set.Id;
                                        if (ImGui.IsMouseDoubleClicked(0))
                                        {
                                            controller.EditedName = set.Name ?? string.Empty;
                                            controller.EditedDescription = set.Design?.Description ?? string.Empty;
                                            controller.SelectedSlotLayer = set.Layer;
                                            controller.CurrentView = SubView.Import;
                                        }
                                    }

                                    if (ImGui.IsItemHovered())
                                    {
                                        if (!string.IsNullOrEmpty(set.Description))
                                            SharedUserInterfaces.Tooltip(set.Description);
                                    }

                                    ImGui.TableNextColumn();
                                    ImGui.TextUnformatted(set.Layer.ToString());

                                    ImGui.TableNextColumn();
                                    ImGui.TextUnformatted(set.Priority.ToString());

                                    ImGui.TableNextColumn();
                                    ImGui.PushID(set.Id.ToString());
                                    ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(4, 4));
                                    if (SharedUserInterfaces.IconButton(FontAwesomeIcon.Edit, null, "Edit Set"))
                                    {
                                        controller.EditedName = set.Name ?? string.Empty;
                                        controller.EditedDescription = set.Design?.Description ?? string.Empty;
                                        controller.SelectedSlotLayer = set.Layer;
                                        controller.CurrentView = SubView.Import;
                                    }
                                    ImGui.SameLine();
                                    if (SharedUserInterfaces.IconButton(FontAwesomeIcon.Trash, null, "Delete Set"))
                                    {
                                        controller.DeleteItem(set.Id);
                                    }
                                    ImGui.PopStyleVar();
                                    ImGui.PopID();
                                }

                                ImGui.EndTable();
                            }

                            ImGui.EndChild();
                        }

                        ImGui.EndTabItem();
                    }

                    if (ImGui.BeginTabItem("Items"))
                    {
                        var items = controller.FilteredItems ?? new List<WardrobeItem>();
                        if (ImGui.BeginChild("##ItemsTableChild", new Vector2(0, 0), false))
                        {
                            if (
                                ImGui.BeginTable(
                                    "##WardrobeItemsTable",
                                    5,
                                    ImGuiTableFlags.RowBg | ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.ScrollY
                                )
                            )
                            {
                                ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.WidthStretch);
                                ImGui.TableSetupColumn("Slot", ImGuiTableColumnFlags.WidthFixed, 90);
                                ImGui.TableSetupColumn("Equipped", ImGuiTableColumnFlags.WidthFixed, 80);
                                ImGui.TableSetupColumn("Priority", ImGuiTableColumnFlags.WidthFixed, 90);
                                ImGui.TableSetupColumn("Actions", ImGuiTableColumnFlags.WidthFixed, 90);
                                ImGui.TableHeadersRow();

                                foreach (var item in items)
                                {
                                    ImGui.TableNextRow();
                                    ImGui.TableNextColumn();

                                    var isSelected =
                                        controller.SelectedItem.HasValue && controller.SelectedItem.Value == item.Id;

                                    if (
                                        ImGui.Selectable(
                                            item.Name ?? "Unnamed",
                                            isSelected,
                                            ImGuiSelectableFlags.AllowDoubleClick | ImGuiSelectableFlags.SpanAllColumns
                                        )
                                    )
                                    {
                                        controller.SelectedItem = item.Id;
                                        if (ImGui.IsMouseDoubleClicked(0))
                                        {
                                            controller.EditedName = item.Name ?? string.Empty;
                                            controller.EditedDescription = item.Design?.Description ?? string.Empty;
                                            controller.SelectedSlotLayer = item.Layer;
                                            controller.CurrentView = SubView.Import;
                                        }
                                    }

                                    if (ImGui.IsItemHovered())
                                    {
                                        if (!string.IsNullOrEmpty(item.Description))
                                            SharedUserInterfaces.Tooltip(item.Description);
                                    }

                                    ImGui.TableNextColumn();
                                    ImGui.TextUnformatted(item.Layer.ToString());

                                    ImGui.TableNextColumn();
                                    ImGui.TextUnformatted(controller.IsItemEquipped(item.Id) ? "Yes" : "No");

                                    ImGui.TableNextColumn();
                                    ImGui.TextUnformatted(item.Priority.ToString());

                                    ImGui.TableNextColumn();
                                    ImGui.PushID(item.Id.ToString());
                                    ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(4, 4));
                                    if (SharedUserInterfaces.IconButton(FontAwesomeIcon.Edit, null, "Edit Item"))
                                    {
                                        controller.EditedName = item.Name ?? string.Empty;
                                        controller.EditedDescription = item.Design?.Description ?? string.Empty;
                                        controller.SelectedSlotLayer = item.Layer;
                                        controller.CurrentView = SubView.Import;
                                    }
                                    ImGui.SameLine();
                                    if (SharedUserInterfaces.IconButton(FontAwesomeIcon.Trash, null, "Delete Item"))
                                    {
                                        controller.DeleteItem(item.Id);
                                    }
                                    ImGui.PopStyleVar();
                                    ImGui.PopID();
                                }

                                ImGui.EndTable();
                            }

                            ImGui.EndChild();
                        }

                        ImGui.EndTabItem();
                    }

                    ImGui.EndTabBar();
                }
            }
        );
        ImGui.EndChild();

        ImGui.SameLine();

        // Right column: import view or empty/default view
        ImGui.BeginChild("##WardrobeRightColumn", new Vector2(0, 0), false);
        var columnWidth = ImGui.GetContentRegionAvail().X;

        if (controller.CurrentView == SubView.Import)
        {
            DrawImportView(columnWidth);
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
        if (SharedUserInterfaces.IconButton(FontAwesomeIcon.Edit, null, "Edit Set"))
        {
            controller.EditedName = set.Name ?? string.Empty;
            controller.EditedDescription = set.Design?.Description ?? string.Empty;
            controller.SelectedSlotLayer = set.Layer;
            controller.CurrentView = SubView.Import;
        }

        ImGui.SameLine();

        // Delete button
        if (SharedUserInterfaces.IconButton(FontAwesomeIcon.Trash, null, "Delete Set"))
        {
            controller.DeleteItem(set.Id);
        }

        ImGui.PopID();
    }
}
