using System.Numerics;
using KinkLinkCommon.Domain.Enums;
using Dalamud.Bindings.ImGui;
using KinkLinkClient.Style;
using KinkLinkClient.Utils;

namespace KinkLinkClient.UI.Views.Friends;
public partial class FriendsViewUi {
    private void DrawPermissions(string view,  bool readOnly, float width)
    {
        switch (selectionManager.Selected.Count) {
            case 0:
                SharedUserInterfaces.ContentBox("DrawPermissionsSelectOne", KinkLinkStyle.PanelBackground, true, () => {
                    ImGui.TextUnformatted("No pair selected. Please select a pair to continue");
                });
                return;
            case >1:
                SharedUserInterfaces.ContentBox("DrawPermissionsSelectOnlyOne", KinkLinkStyle.PanelBackground, true, () => {
                    ImGui.TextUnformatted("Can only set one pair permissions at a time.");
                });
                return;
        }
        // Early return in the event that the pairs are not currently selected. No need to draw it.

        var half = width * 0.5f;

        ImGui.BeginDisabled(readOnly);
        if (controller.View != FriendsViewUiController.SubView.DefaultPerms)
        {
            SharedUserInterfaces.ContentBox("FriendsSettings", KinkLinkStyle.PanelBackground, true, () => {
                ImGui.TextUnformatted("Priority");
                if (ImGui.RadioButton("Casual", controller.EditingPermissions.Priority == RelationshipPriority.Casual)) {
                    controller.EditingPermissions.Priority = RelationshipPriority.Casual;
                }
                if (ImGui.RadioButton("Serious", controller.EditingPermissions.Priority == RelationshipPriority.Serious)) {
                    controller.EditingPermissions.Priority = RelationshipPriority.Serious;
                }
                if (ImGui.RadioButton("Devotional", controller.EditingPermissions.Priority == RelationshipPriority.Devotional)) {
                    controller.EditingPermissions.Priority = RelationshipPriority.Devotional;
                }
            });
        }

        SharedUserInterfaces.ContentBox("GagPermissions", KinkLinkStyle.PanelBackground, true, () =>
        {
            /// Gag perms
            ImGui.TextUnformatted("Gag Permissions");
            ImGui.Checkbox("Can Apply", ref controller.EditingPermissions.CanApplyGag);
            ImGui.SameLine(half);
            ImGui.Checkbox("Can Lock", ref controller.EditingPermissions.CanLockGag);
            ImGui.Checkbox("Can Unlock", ref controller.EditingPermissions.CanUnlockGag);
            ImGui.SameLine(half);
            ImGui.Checkbox("Can Remove", ref controller.EditingPermissions.CanRemoveGag);
            ImGui.Checkbox("Can Force Enable Glamour", ref controller.EditingPermissions.CanForceEnableGagGlamour);
            ImGui.SameLine(half);
            ImGui.Checkbox("Can Enable Garbler", ref controller.EditingPermissions.CanEnableGarbler);
            ImGui.Checkbox("Can Lock Garbler", ref controller.EditingPermissions.CanLockGarbler);
            ImGui.SameLine(half);
            ImGui.Checkbox("Can Set Garbler Channels", ref controller.EditingPermissions.CanSetGarlblerChannels);
            ImGui.Checkbox("Can Lock Garbler Channels", ref controller.EditingPermissions.CanLockGarlbleChannels);
        });
        
        SharedUserInterfaces.ContentBox("WardrobePermissions", KinkLinkStyle.PanelBackground, true, () =>
        {
            ImGui.TextUnformatted("Wardrobe Permissions");
            /// Wardrobe permsThese permissions are specifically related to the wardrobe section of the interaction menu
            ImGui.Checkbox("Apply Wardrobe", ref controller.EditingPermissions.CanApplyWardrobe); 
            ImGui.SameLine(half);
            ImGui.Checkbox("Can Lock Wardrobe", ref controller.EditingPermissions.CanLockWardrobe);
            ImGui.Checkbox("Can Unlock Wardrobe", ref controller.EditingPermissions.CanUnlockWardrobe);
            ImGui.SameLine(half);
            ImGui.Checkbox("Can Remove Wardrobe", ref controller.EditingPermissions.CanRemoveWardrobe);
            ImGui.Checkbox("Can Force Enable Wardrobe Glamour", ref controller.EditingPermissions.CanForceEnableWardrobeGlamour);
        });

        SharedUserInterfaces.ContentBox("MoodlesPermissions", KinkLinkStyle.PanelBackground, true, () =>
        {
            ImGui.TextUnformatted("Moodles Permissions");
            ImGui.Checkbox("Can Apply My", ref controller.EditingPermissions.CanApplyOwn);
            ImGui.SameLine(half);
            ImGui.Checkbox("Can Apply Pairs", ref controller.EditingPermissions.CanApplyPairs);
            ImGui.Checkbox("Can Lock Moodles", ref controller.EditingPermissions.CanLockMoodles);
            ImGui.SameLine(half);
            ImGui.Checkbox("Can Unlock Moodles", ref controller.EditingPermissions.CanUnlockMoodles);
            ImGui.Checkbox("Can Remove Moodles", ref controller.EditingPermissions.CanRemoveMoodles);
        });
        
        if (!readOnly) {
            SharedUserInterfaces.ContentBox("SaveChanges", KinkLinkStyle.PanelBackground, false, () =>
            {
                if (ImGui.Button("Save Changes", new Vector2(width - KinkLinkImGui.WindowPadding.X * 2, KinkLinkDimensions.SendCommandButtonHeight)))
                    _ = controller.Save().ConfigureAwait(false);
                
                if (ImGui.IsItemHovered())
                    ImGui.SetTooltip("Save permissions");
            });
        }
        ImGui.EndDisabled();
    }
}
