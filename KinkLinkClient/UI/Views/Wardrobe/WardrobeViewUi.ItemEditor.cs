using System;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using KinkLinkClient.Domain;
using KinkLinkCommon.Dependencies.Glamourer.Components;
using KinkLinkClient.Utils;
using KinkLinkCommon.Domain.Enums;

namespace KinkLinkClient.UI.Views.Wardrobe;

public partial class WardrobeViewUi
{
    private void DrawEditorView(float columnWidth)
    {
        if (controller.EditingPiece is null && controller.EditingSet is null)
        {
            controller.CloseEditor();
            return;
        }

        var padding = ImGui.GetStyle().WindowPadding;
        var contentWidth = columnWidth - padding.X * 2;

        if (controller.EditingPiece is not null)
        {
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
                "EditorPriority",
                KinkLinkStyle.PanelBackground,
                true,
                () =>
                {
                    SharedUserInterfaces.MediumText("Pair Access Priority");
                    ImGui.SetNextItemWidth(contentWidth);
                    var currentPriority = controller.EditedPriority.ToString();
                    if (ImGui.BeginCombo("##PrioritySelector", currentPriority))
                    {
                        foreach (RelationshipPriority priority in Enum.GetValues<RelationshipPriority>())
                        {
                            if (ImGui.Selectable(priority.ToString()))
                                controller.EditedPriority = priority;
                        }
                        ImGui.EndCombo();
                    }
                }
            );

            if (controller.IsNewItem)
            {
                SharedUserInterfaces.ContentBox(
                    "EditorImportFromPlayer",
                    KinkLinkStyle.PanelBackground,
                    true,
                    () =>
                    {
                        SharedUserInterfaces.MediumText("Import from Player");

                        SharedUserInterfaces.MediumText("Slot");
                        ImGui.SetNextItemWidth(contentWidth);
                        if (
                            ImGui.BeginCombo(
                                "##ImportSlotSelector",
                                WardrobeViewUiController.GetSlotDisplayName(
                                    controller.ImportSlotName
                                )
                            )
                        )
                        {
                            foreach (var slotName in WardrobeViewUiController.AllSlotNames)
                            {
                                if (ImGui.Selectable(slotName))
                                {
                                    controller.ImportSlotName = slotName;
                                }
                            }
                            ImGui.EndCombo();
                        }

                        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 5);

                        if (!controller.HasImportedItem)
                        {
                            if (ImGui.Button("Import from Player", new Vector2(contentWidth, 35)))
                            {
                                _ = ImportFromPlayerWithErrorHandling();
                            }
                        }
                        else
                        {
                            ImGui.Separator();
                            ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 5);
                            SharedUserInterfaces.MediumText("Imported Item Details");

                            ImGui.Text(
                                $"Slot: {WardrobeViewUiController.GetSlotDisplayName(controller.SelectedSlotName)}"
                            );
                            ImGui.Text($"Item ID: {controller.EditedItem.ItemId}");
                            ImGui.Text($"Dye 1: {controller.EditedDye1}");
                            ImGui.Text($"Dye 2: {controller.EditedDye2}");
                            ImGui.Text($"Apply: {(controller.EditedItem.Apply ? "Yes" : "No")}");
                            ImGui.Text(
                                $"Apply Stain: {(controller.EditedItem.ApplyStain ? "Yes" : "No")}"
                            );
                            ImGui.Text(
                                $"Apply Crest: {(controller.EditedItem.ApplyCrest ? "Yes" : "No")}"
                            );
                            ImGui.Text($"Crest: {(controller.EditedItem.Crest ? "Yes" : "No")}");

                            ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 5);

                            if (
                                ImGui.Button("Import Different Item", new Vector2(contentWidth, 30))
                            )
                            {
                                controller.HasImportedItem = false;
                                controller.EditedItem = new GlamourerItem();
                                controller.EditedDye1 = 0;
                                controller.EditedDye2 = 0;
                            }
                        }
                    }
                );
            }
            else
            {
                SharedUserInterfaces.ContentBox(
                    "EditorSlot",
                    KinkLinkStyle.PanelBackground,
                    true,
                    () =>
                    {
                        SharedUserInterfaces.MediumText("Slot");
                        ImGui.SetNextItemWidth(contentWidth);
                        if (
                            ImGui.BeginCombo(
                                "##SlotSelector",
                                WardrobeViewUiController.GetSlotDisplayName(
                                    controller.SelectedSlotName
                                )
                            )
                        )
                        {
                            foreach (var slotName in WardrobeViewUiController.AllSlotNames)
                            {
                                if (ImGui.Selectable(slotName))
                                {
                                    controller.SelectedSlotName = slotName;
                                }
                            }
                            ImGui.EndCombo();
                        }
                    }
                );

                if (controller.EditingPiece?.Item != null)
                {
                    SharedUserInterfaces.ContentBox(
                        "EditorImportedItem",
                        KinkLinkStyle.PanelBackground,
                        true,
                        () =>
                        {
                            SharedUserInterfaces.MediumText("Imported Item Details");
                            ImGui.Text(
                                $"Slot: {WardrobeViewUiController.GetSlotDisplayName(controller.SelectedSlotName)}"
                            );
                            ImGui.Text($"Item ID: {controller.EditingPiece.Item.ItemId}");
                            ImGui.Text($"Dye 1: {controller.EditingPiece.Item.Stain}");
                            ImGui.Text($"Dye 2: {controller.EditingPiece.Item.Stain2}");
                            ImGui.Text($"Apply: {(controller.EditingPiece.Item.Apply ? "Yes" : "No")}");
                            ImGui.Text(
                                $"Apply Stain: {(controller.EditingPiece.Item.ApplyStain ? "Yes" : "No")}"
                            );
                            ImGui.Text(
                                $"Apply Crest: {(controller.EditingPiece.Item.ApplyCrest ? "Yes" : "No")}"
                            );
                            ImGui.Text($"Crest: {(controller.EditingPiece.Item.Crest ? "Yes" : "No")}");

                            ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 5);

                            SharedUserInterfaces.MediumText("Re-import from Player");

                            ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 5);

                            SharedUserInterfaces.MediumText("Slot");
                            ImGui.SetNextItemWidth(contentWidth);
                            if (
                                ImGui.BeginCombo(
                                    "##ReImportSlotSelector",
                                    WardrobeViewUiController.GetSlotDisplayName(
                                        controller.ImportSlotName
                                    )
                                )
                            )
                            {
                                foreach (var slotName in WardrobeViewUiController.AllSlotNames)
                                {
                                    if (ImGui.Selectable(slotName))
                                    {
                                        controller.ImportSlotName = slotName;
                                    }
                                }
                                ImGui.EndCombo();
                            }

                            ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 5);

                            if (ImGui.Button("Import from Player", new Vector2(contentWidth, 35)))
                            {
                                _ = ImportFromPlayerWithErrorHandling();
                            }
                        }
                    );
                }
            }

            SharedUserInterfaces.ContentBox(
                "EditorMods",
                KinkLinkStyle.PanelBackground,
                true,
                () =>
                {
                    var selectedCount = controller.GetSelectedModCount();
                    SharedUserInterfaces.MediumText($"Mods ({selectedCount} added)");

                    ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 5);

                    var buttonWidth = 100f;
                    var iconButtonWidth = 26f;

                    ImGui.SetCursorPosX(contentWidth - buttonWidth - iconButtonWidth - padding.X);
                    if (SharedUserInterfaces.IconButton(FontAwesomeIcon.Sync, new Vector2(iconButtonWidth, 24), "Refresh Mods"))
                    {
                        _ = LoadModsWithErrorHandling();
                    }

                    ImGui.SameLine(contentWidth - buttonWidth);

                    if (controller.AvailableMods.Count == 0)
                    {
                        if (ImGui.Button("Add Mod", new Vector2(buttonWidth, 24)))
                        {
                            _ = LoadModsWithErrorHandling();
                        }
                    }
                    else
                    {
                        if (ImGui.Button("Add Mod##open", new Vector2(buttonWidth, 24)))
                        {
                            controller.ModFilter = string.Empty;
                            ImGui.OpenPopup("##AddModPopup");
                        }

                        if (ImGui.BeginPopup("##AddModPopup"))
                        {
                            ImGui.SetNextItemWidth(contentWidth - padding.X * 2);
                            var filter = controller.ModFilter;
                            if (ImGui.InputTextWithHint("##ModFilter", "Filter mods...", ref filter, 64))
                            {
                                controller.ModFilter = filter;
                            }

                            var filteredMods = controller.AvailableMods
                                .Where(m => !controller.IsModSelected(m.Item1.DirectoryName))
                                .Where(m => string.IsNullOrEmpty(controller.ModFilter) ||
                                           m.Item1.Name.Contains(controller.ModFilter, StringComparison.OrdinalIgnoreCase))
                                .ToList();

                            if (filteredMods.Count == 0)
                            {
                                ImGui.TextColored(ImGuiColors.DalamudGrey, "No mods available.");
                            }
                            else
                            {
                                var listHeight = Math.Min(200f, filteredMods.Count * 20f + 20f);
                                if (ImGui.BeginChild("##ModSelectList", new Vector2(contentWidth - padding.X * 2, listHeight), true))
                                {
                                    foreach (var mod in filteredMods)
                                    {
                                        if (ImGui.Selectable($"{mod.Item1.Name}##{mod.Item1.DirectoryName}"))
                                        {
                                            controller.AddMod(mod.Item1.DirectoryName);
                                            ImGui.CloseCurrentPopup();
                                        }
                                    }
                                    ImGui.EndChild();
                                }
                            }

                            ImGui.EndPopup();
                        }
                    }

                    ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 5);

                    if (selectedCount == 0)
                    {
                        ImGui.TextColored(ImGuiColors.DalamudGrey, "No mods added. Click 'Add Mod' to add mods.");
                    }
                    else
                    {
                        var listHeight = 150f;
                        if (ImGui.BeginChild("##ModsList", new Vector2(contentWidth, listHeight), true))
                        {
                            foreach (var kvp in controller.SelectedModSettings)
                            {
                                var modName = controller.GetModName(kvp.Key);
                                var settings = kvp.Value;

                                var checkboxLabel = $"##mod_{kvp.Key}";
                                var checkboxValue = settings.Enabled;

                                ImGui.SetCursorPosX(padding.X);
                                if (ImGui.Checkbox(checkboxLabel, ref checkboxValue))
                                {
                                    controller.UpdateModSelection(kvp.Key, checkboxValue, settings.Priority);
                                }

                                ImGui.SameLine(padding.X + 20);
                                ImGui.Text(modName ?? kvp.Key);

                                ImGui.SameLine(contentWidth - 130);

                                var priorityStr = settings.Priority.ToString();
                                ImGui.SetNextItemWidth(60);
                                if (ImGui.InputText($"##priority_{kvp.Key}", ref priorityStr, 10))
                                {
                                    if (int.TryParse(priorityStr, out var newPriority))
                                    {
                                        controller.UpdateModSelection(kvp.Key, settings.Enabled, newPriority);
                                    }
                                }

                                ImGui.SameLine(contentWidth - 60);
                                ImGui.Text("Prio");

                                ImGui.SameLine(contentWidth - 30);
                                if (ImGui.Button($"X##{kvp.Key}", new Vector2(20, 20)))
                                {
                                    controller.RemoveMod(kvp.Key);
                                }

                                ImGui.Spacing();
                            }
                            ImGui.EndChild();
                        }
                    }
                }
            );
        }
        else if (controller.EditingSet != null)
        {
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
                        foreach (RelationshipPriority priority in Enum.GetValues<RelationshipPriority>())
                        {
                            if (ImGui.Selectable(priority.ToString()))
                                controller.EditedPriority = priority;
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
                    ImGui.Text($"Design: {controller.EditingSet.Name}");
                }
            );
        }

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
                NotificationHelper.Success("Load Mods", $"Loaded {controller.AvailableMods.Count} mods");
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
