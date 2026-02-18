using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using FFXIVClientStructs.FFXIV.Client.UI.Info;
using KinkLinkClient.Domain.Interfaces;
using KinkLinkClient.Managers;
using KinkLinkClient.Style;
using KinkLinkClient.UI.Components.Friends;
using KinkLinkClient.Utils;

namespace KinkLinkClient.UI.Views.Friends;

/// <summary>
///     Handles UI elements for the Friends tab
/// </summary>
public partial class PairsViewUi(
    PairsListComponentUi friendsList,
    PairsViewUiController controller,
    SelectionManager selectionManager
) : IDrawable
{
    private const string UnsavedChangesText = "You have unsaved changes";
    private static readonly Vector2 Half = new(0.5f);
    private static readonly Vector2 IconSize = new(40);
    private static readonly Vector2 SmallIconSize = new(24, 0);

    public void Draw()
    {
        ImGui.BeginChild(
            "PermissionContent",
            KinkLinkStyle.ContentSize,
            false,
            KinkLinkStyle.ContentFlags
        );

        var width = ImGui.GetWindowWidth();

        SharedUserInterfaces.ContentBox(
            "PermissionsModeSelect",
            KinkLinkStyle.PanelBackground,
            true,
            () =>
            {
                var buttonWidth = (width - 3 * KinkLinkImGui.WindowPadding.X) * 0.3f;
                var buttonDimensions = new Vector2(
                    buttonWidth,
                    KinkLinkDimensions.SendCommandButtonHeight
                );
                // Header
                SharedUserInterfaces.PushMediumFont();
                SharedUserInterfaces.TextCentered("Pair Permissions");
                SharedUserInterfaces.PopMediumFont();
                // Draw the tooltip/tutorial
                ImGui.SameLine(width - ImGui.GetFontSize() - KinkLinkImGui.WindowPadding.X * 2);
                SharedUserInterfaces.Icon(FontAwesomeIcon.QuestionCircle);

                if (ImGui.IsItemHovered())
                {
                    ImGui.SetNextWindowSize(KinkLinkDimensions.Tooltip);
                    ImGui.BeginTooltip();

                    SharedUserInterfaces.MediumText("Tutorial");
                    // TODO: Separate permissions based on view
                    ImGui.Separator();
                    ImGui.TextWrapped(
                        "Add pairs via their pair codes on the left side of the screen."
                    );
                    ImGui.TextWrapped("Permissions do not update until they are saved.");
                    ImGui.TextWrapped("Default Permissions are applied to all **new** pairs");
                    ImGui.TextWrapped("Default Permissions are applied to all **new** pairs");
                    ImGui.EndTooltip();
                }

                headerButton(
                    PairsViewUiController.SubView.DefaultPerms,
                    "Defaults",
                    buttonDimensions
                );
                ImGui.SameLine();
                headerButton(
                    PairsViewUiController.SubView.PairPerms,
                    "Granted To Pair",
                    buttonDimensions
                );
                ImGui.SameLine();
                headerButton(
                    PairsViewUiController.SubView.ViewPairPerms,
                    "Granted By Pair",
                    buttonDimensions
                );
            }
        );

        bool pendingChanges = false;

        switch (controller.View)
        {
            case PairsViewUiController.SubView.DefaultPerms:
                // Swap to default permissions
                DrawPermissions("DefaultPermissions", false, width);
                break;
            case PairsViewUiController.SubView.PairPerms:
                // Ensure that it is looking at the pair perms
                DrawPermissions("PairPermissions", false, width);
                break;
            case PairsViewUiController.SubView.ViewPairPerms:
                // Ensure that it is looking at the pair perms
                DrawPermissions("ViewPairPerms", true, width);
                break;
        }

        pendingChanges = controller.PendingChanges();
        if (pendingChanges)
        {
            var drawList = ImGui.GetWindowDrawList();
            drawList.ChannelsSplit(2);

            drawList.ChannelsSetCurrent(1);
            var textSize = ImGui.CalcTextSize("Unsaved Changes");
            var pos = ImGui.GetWindowPos();
            var size = ImGui.GetWindowSize();
            var final = new Vector2(
                pos.X + (size.X - textSize.X) * 0.5f,
                pos.Y + textSize.Y + KinkLinkImGui.WindowPadding.Y
            );
            drawList.AddText(final, ImGui.ColorConvertFloat4ToU32(Vector4.One), "Unsaved Changes");

            drawList.ChannelsSetCurrent(0);
            var min = final - KinkLinkImGui.WindowPadding;
            var max = final + textSize + KinkLinkImGui.WindowPadding;
            drawList.AddRectFilled(
                min,
                max,
                ImGui.ColorConvertFloat4ToU32(new Vector4(0, 0, 0, 1)),
                KinkLinkStyle.Rounding
            );
            drawList.AddRect(
                min,
                max,
                ImGui.ColorConvertFloat4ToU32(ImGuiColors.DalamudOrange),
                KinkLinkStyle.Rounding
            );
            drawList.AddRect(
                pos,
                pos + size,
                ImGui.ColorConvertFloat4ToU32(ImGuiColors.DalamudOrange),
                KinkLinkStyle.Rounding
            );

            drawList.ChannelsMerge();
        }

        ImGui.EndChild();
        ImGui.SameLine();
        friendsList.Draw(true, true);
    }

    private void headerButton(
        PairsViewUiController.SubView nextSubview,
        string label,
        Vector2 dimensions
    )
    {
        // This is for stylings primary buttons
        if (nextSubview == controller.View)
        {
            ImGui.PushStyleColor(ImGuiCol.Button, KinkLinkStyle.PrimaryColor);
            if (ImGui.Button(label, dimensions))
            {
                controller.View = nextSubview;
                controller.RefreshViewData();
            }
            ImGui.PopStyleColor();
        }
        else
        {
            if (ImGui.Button(label, dimensions))
            {
                controller.View = nextSubview;
                controller.RefreshViewData();
            }
        }
    }

    private static unsafe string? GetLinkshellName(uint index)
    {
        var instance = InfoProxyLinkshell.Instance();
        var info = instance->GetLinkshellInfo(index);
        return info == null ? null : instance->GetLinkshellName(info->Id).ToString();
    }

    private static unsafe string GetCrossWorldLinkshellName(uint index)
    {
        var instance = InfoProxyCrossWorldLinkshell.Instance();
        var info = instance->GetCrossworldLinkshellName(index);
        return info->ToString();
    }
}
