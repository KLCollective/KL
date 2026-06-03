using System;
using System.Linq;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using KinkLinkClient.Services;
using KinkLinkClient.Utils;

namespace KinkLinkClient.UI.Views.Wardrobe;

public partial class WardrobeViewUi
{
    private void DrawImportView(float columnWidth, bool edit = false)
    {
        var padding = ImGui.GetStyle().WindowPadding;
        var contentWidth = columnWidth - padding.X * 2;

        SharedUserInterfaces.ContentBox(
            "ImportName",
            KinkLinkStyle.PanelBackground,
            true,
            () =>
            {
                SharedUserInterfaces.MediumText("Name");
                ImGui.SetNextItemWidth(contentWidth - padding.X * 2);
                var name = controller.EditedName;
                if (ImGui.InputText("##ImportName", ref name, 64))
                    controller.EditedName = name;
            }
        );

        SharedUserInterfaces.ContentBox(
            "ImportDescription",
            KinkLinkStyle.PanelBackground,
            true,
            () =>
            {
                SharedUserInterfaces.MediumText("Description");
                ImGui.SetNextItemWidth(contentWidth - padding.X * 2);
                var description = controller.EditedDescription;
                if (ImGui.InputText("##ImportDescription", ref description, 256))
                    controller.EditedDescription = description;
            }
        );

        SharedUserInterfaces.ContentBox(
            "ImportLayer",
            KinkLinkStyle.PanelBackground,
            true,
            () =>
            {
                SharedUserInterfaces.MediumText("Layer");
                ImGui.SetNextItemWidth(contentWidth);
                var currentLayer = controller.SelectedSlotLayer.ToString();
                if (ImGui.BeginCombo("##ImportLayerSelector", currentLayer))
                {
                    foreach (
                        KinkLinkCommon.Domain.Wardrobe.WardrobeLayer layer in Enum.GetValues<KinkLinkCommon.Domain.Wardrobe.WardrobeLayer>()
                    )
                    {
                        if (ImGui.Selectable(layer.ToString()))
                            controller.SelectedSlotLayer = layer;
                    }
                    ImGui.EndCombo();
                }
            }
        );

        var windowHeight = ImGui.GetWindowHeight();
        var designBoxHeight = (windowHeight - padding.Y * 16 - ImportButtonHeight - 140) * 0.5f;

        SharedUserInterfaces.ContentBox(
            "ImportDesignSearch",
            KinkLinkStyle.PanelBackground,
            true,
            () =>
            {
                if (edit)
                    SharedUserInterfaces.MediumText("Change Design");
                else
                    SharedUserInterfaces.MediumText("Search Design to Import");

                ImGui.SetNextItemWidth(contentWidth - padding.X * 4 - ImGui.GetFontSize());
                var searchTerm = controller.GlamourerSearchTerm;
                if (ImGui.InputTextWithHint("##ImportSearchBar", "Search", ref searchTerm, 32))
                {
                    controller.GlamourerSearchTerm = searchTerm;
                    controller.FilterDesigns();
                }

                ImGui.SameLine();

                if (SharedUserInterfaces.IconButton(FontAwesomeIcon.Sync, null, "Refresh Designs"))
                    controller.RefreshDesigns();
            }
        );

        SharedUserInterfaces.ContentBox(
            "ImportDesignList",
            KinkLinkStyle.PanelBackground,
            true,
            () =>
            {
                if (
                    ImGui.BeginChild(
                        "##ImportDesignsList",
                        new Vector2(0, designBoxHeight),
                        true,
                        ImGuiWindowFlags.AlwaysVerticalScrollbar
                    )
                )
                {
                    var designs = controller.FilteredGlamourerDesigns;
                    if (designs != null)
                    {
                        foreach (var design in designs)
                        {
                            var isSelected = controller.SelectedGlamourerDesignId == design.Id;

                            if (isSelected)
                            {
                                ImGui.PushStyleColor(ImGuiCol.Header, KinkLinkStyle.PrimaryColor);
                                if (ImGui.Selectable($"{design.Path}##{design.Id}", true))
                                {
                                    controller.SelectedGlamourerDesignId = design.Id;
                                }
                                ImGui.PopStyleColor();
                            }
                            else
                            {
                                if (ImGui.Selectable($"{design.Path}##{design.Id}"))
                                {
                                    controller.SelectedGlamourerDesignId = design.Id;
                                }
                            }
                        }
                    }

                    if (designs == null || designs.Count == 0)
                    {
                        ImGui.TextColored(ImGuiColors.DalamudGrey, "No designs found.");
                        ImGui.TextColored(
                            ImGuiColors.DalamudGrey,
                            "Make sure Glamourer is installed and has designs."
                        );
                    }

                    ImGui.EndChild();
                }
            }
        );

        SharedUserInterfaces.ContentBox(
            "ImportButton",
            KinkLinkStyle.PanelBackground,
            false,
            () =>
            {
                var canImport = controller.SelectedGlamourerDesignId != Guid.Empty;
                var buttonWidth = (contentWidth - 6) * 0.5f;

                if (!canImport)
                {
                    ImGui.BeginDisabled();
                    ImGui.Button(
                        edit ? "Select a design to change" : "Select a design to import",
                        new Vector2(buttonWidth, ImportButtonHeight)
                    );
                    ImGui.EndDisabled();
                }
                else
                {
                    if (
                        ImGui.Button(
                            edit ? "Save" : "Import Design",
                            new Vector2(buttonWidth, ImportButtonHeight)
                        )
                    )
                    {
                        ImportSelectedDesign();
                    }
                }

                ImGui.SameLine();

                if (ImGui.Button("Cancel", new Vector2(buttonWidth, ImportButtonHeight)))
                {
                    controller.CloseImport();
                }
            }
        );
    }

    private async void ImportSelectedDesign()
    {
        if (controller.SelectedGlamourerDesignId == Guid.Empty)
            return;

        var designs = controller.FilteredGlamourerDesigns;
        var design = designs?.FirstOrDefault(d => d.Id == controller.SelectedGlamourerDesignId);
        if (design == null)
            return;

        var name = string.IsNullOrWhiteSpace(controller.EditedName)
            ? design.Name
            : controller.EditedName;
        var description = controller.EditedDescription ?? string.Empty;

        var glamourerDesign = await wardrobeManager.GetDesignAsync(design.Id);
        if (glamourerDesign == null)
        {
            NotificationHelper.Error("Import", "Failed to import design.");
            return;
        }

        glamourerDesign.Name = name;
        glamourerDesign.Description = description;
        var newItem = new WardrobeItem
        {
            Id = Guid.NewGuid(),
            Design = glamourerDesign,
            Layer = controller.SelectedSlotLayer,
        };
        wardrobeManager.AddDesign(newItem);

        controller.EditedName = string.Empty;
        controller.EditedDescription = string.Empty;

        NotificationHelper.Success("Import", $"Imported {name} successfully");
    }
}
