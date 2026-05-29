using System;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using KinkLinkClient.Domain;
using KinkLinkClient.Utils;
using KinkLinkCommon.Dependencies.Glamourer.Components;
using KinkLinkCommon.Domain.Enums;

namespace KinkLinkClient.UI.Views.Wardrobe;

public partial class WardrobeViewUi
{
    private void DrawEditorView(float columnWidth)
    {
        if (controller.EditingWardrobeItem is null)
        {
            controller.CloseEditor();
            return;
        }

        var padding = ImGui.GetStyle().WindowPadding;
        var contentWidth = columnWidth - padding.X * 2;

        SharedUserInterfaces.ContentBox(
            "EditorName",
            KinkLinkStyle.PanelBackground,
            true,
            () =>
            {
                SharedUserInterfaces.MediumText("Name");
                ImGui.SetNextItemWidth(contentWidth);
                var name = controller.EditedName;
                if (ImGui.InputText("##Name", ref name, 64))
                    controller.EditedName = name;
            }
        );

        SharedUserInterfaces.ContentBox(
            "EditorDescription",
            KinkLinkStyle.PanelBackground,
            true,
            () =>
            {
                SharedUserInterfaces.MediumText("Description");
                ImGui.SetNextItemWidth(contentWidth);
                var description = controller.EditedDescription;
                if (ImGui.InputText("##Description", ref description, 256))
                    controller.EditedDescription = description;
            }
        );

        SharedUserInterfaces.ContentBox(
            "EditorSetPriority",
            KinkLinkStyle.PanelBackground,
            true,
            () =>
            {
                SharedUserInterfaces.MediumText("Pair Access Priority");
                ImGui.SetNextItemWidth(contentWidth);
                var currentPriority = controller.EditedPriority.ToString();
                if (ImGui.BeginCombo("##SetPrioritySelector", currentPriority))
                {
                    foreach (
                        RelationshipPriority priority in Enum.GetValues<RelationshipPriority>()
                    )
                    {
                        if (ImGui.Selectable(priority.ToString()))
                            controller.EditedPriority = priority;
                    }
                    ImGui.EndCombo();
                }
            }
        );

        SharedUserInterfaces.ContentBox(
            "EditorLayer",
            KinkLinkStyle.PanelBackground,
            true,
            () =>
            {
                SharedUserInterfaces.MediumText("Layer");
                ImGui.SetNextItemWidth(contentWidth);
                var currentLayer = controller.SelectedSlotLayer.ToString();
                if (ImGui.BeginCombo("##LayerSelector", currentLayer))
                {
                    foreach (KinkLinkCommon.Domain.Wardrobe.WardrobeLayer layer in Enum.GetValues<KinkLinkCommon.Domain.Wardrobe.WardrobeLayer>())
                    {
                        if (ImGui.Selectable(layer.ToString()))
                            controller.SelectedSlotLayer = layer;
                    }
                    ImGui.EndCombo();
                }
            }
        );

        SharedUserInterfaces.ContentBox(
            "EditorDesignInfo",
            KinkLinkStyle.PanelBackground,
            true,
            () =>
            {
                SharedUserInterfaces.MediumText("Design Info");
                ImGui.Text($"Design: {controller.EditingWardrobeItem.Name}");


            }
        );

        SharedUserInterfaces.ContentBox(
            "EditorSave",
            KinkLinkStyle.PanelBackground,
            false,
            () =>
            {
                var buttonWidth = (contentWidth - padding.X) / 2;

                if (ImGui.Button("Cancel", new Vector2(buttonWidth, 40)))
                {
                    controller.CloseEditor();
                }

                ImGui.SameLine();

                if (ImGui.Button("Save", new Vector2(buttonWidth, 40)))
                {
                    _ = SaveAndHandleErrors();
                }
            }
        );
    }

    private async Task SaveAndHandleErrors()
    {
        try
        {
            if (controller.IsNewItem && !controller.HasImportedItem)
            {
                NotificationHelper.Error("Save Failed", "Please import item from player first.");
                return;
            }

            var success = await controller.SaveEditorAsync();
            if (!success)
            {
                NotificationHelper.Error("Save Failed", "Unable to save changes.");
            }
        }
        catch (Exception ex)
        {
            Plugin.Log.Error(ex, "Failed to save editor changes");
            NotificationHelper.Error(
                "Save Failed",
                "Unable to save changes. Check logs for details."
            );
        }
    }

    private async Task ImportFromPlayerWithErrorHandling()
    {
        try
        {
            await controller.ImportFromPlayerAsync();
            if (controller.HasImportedItem)
            {
                NotificationHelper.Success("Import", "Item imported successfully");
            }
            else
            {
                NotificationHelper.Error("Import", "Failed to import item. No item in that slot?");
            }
        }
        catch (Exception ex)
        {
            Plugin.Log.Error(ex, "Failed to import item from player");
            NotificationHelper.Error("Import", "Failed to import item. Check logs for details.");
        }
    }

    private async Task LoadModsWithErrorHandling()
    {
        try
        {
            await controller.LoadAvailableModsAsync();
            if (controller.AvailableMods.Count > 0)
            {
                NotificationHelper.Success(
                    "Load Mods",
                    $"Loaded {controller.AvailableMods.Count} mods"
                );
            }
            else
            {
                NotificationHelper.Error("Load Mods", "No mods found. Is Penumbra available?");
            }
        }
        catch (Exception ex)
        {
            Plugin.Log.Error(ex, "Failed to load mods");
            NotificationHelper.Error("Load Mods", "Failed to load mods. Check logs for details.");
        }
    }

    private async Task ShowAddModPopup()
    {
        if (controller.AvailableMods.Count == 0)
        {
            await LoadModsWithErrorHandling();
        }
    }
}
