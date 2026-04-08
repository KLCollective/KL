using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using KinkLinkClient.Domain.Interfaces;
using KinkLinkClient.Services;
using KinkLinkClient.Utils;
using KinkLinkCommon.Domain;
using KinkLinkCommon.Domain.Network;

namespace KinkLinkClient.UI.Views.UserProfile;

public class ProfileViewUi(ProfileViewUiController controller, IdentityService identityService)
    : IDrawable
{
    public void Draw()
    {
        ImGui.BeginChild("ProfileContent", Vector2.Zero, false, KinkLinkStyle.ContentFlags);

        var windowWidth = ImGui.GetWindowWidth();
        var windowPadding = ImGui.GetStyle().WindowPadding;

        ImGui.AlignTextToFramePadding();

        SharedUserInterfaces.ContentBox(
            "ProfileHeader",
            KinkLinkStyle.PanelBackground,
            true,
            () =>
            {
                SharedUserInterfaces.PushBigFont();

                var friendCode = identityService.FriendCode;
                var size = ImGui.CalcTextSize(friendCode);

                ImGui.SetCursorPosX((windowWidth - size.X) * 0.5f);
                ImGui.TextUnformatted(friendCode);

                SharedUserInterfaces.PopBigFont();
            }
        );

        SharedUserInterfaces.ContentBox(
            "ProfileInfo",
            KinkLinkStyle.PanelBackground,
            true,
            () =>
            {
                SharedUserInterfaces.MediumText("Profile Information");
                ImGui.TextUnformatted("Update your profile details");

                ImGui.Spacing();
                ImGui.Separator();
                ImGui.Spacing();

                ImGui.TextUnformatted("Alias");
                ImGui.InputText("##Alias", ref controller.Alias, 64);

                ImGui.TextUnformatted("Title");
                // Implement an ImGui select combo box for the `controller.Title` parameter.
                // It should only be able to select the discrete enum values of `KinkLinkCommon.Domain.Network.Title``
                if (ImGui.BeginCombo("Title", controller.Title.ToString()))
                {
                    foreach (var title in Enum.GetValues<Title>())
                    {
                        var isSelected = controller.Title == title;
                        if (ImGui.Selectable(title.ToString(), isSelected))
                        {
                            controller.Title = title;
                        }
                        if (isSelected)
                        {
                            ImGui.SetItemDefaultFocus();
                        }
                    }
                    ImGui.EndCombo();
                }

                ImGui.TextUnformatted("Description");
                ImGui.InputTextMultiline(
                    "##Description",
                    ref controller.Description,
                    256,
                    new Vector2(0, 60)
                );

                // ImGui.TextUnformatted("Chat Role");
                // ImGui.InputText("##ChatRole", ref _chatRole, 64);

                ImGui.Spacing();

                if (ImGui.Button("Save Profile"))
                {
                    controller.SaveProfile();
                }
            }
        );

        SharedUserInterfaces.ContentBox(
            "ProfileConfig",
            KinkLinkStyle.PanelBackground,
            true,
            () =>
            {
                SharedUserInterfaces.MediumText("Profile Configuration");
                ImGui.TextUnformatted("Configure which features are enabled for you");

                ImGui.Spacing();
                ImGui.Separator();
                ImGui.Spacing();

                ImGui.Checkbox("Enable Glamours", ref controller.EnableGlamours);
                ImGui.Checkbox("Enable Garbler", ref controller.EnableGarbler);
                ImGui.Checkbox("Enable Garbler Channels", ref controller.EnableGarblerChannels);
                ImGui.Checkbox("Enable Moodles", ref controller.EnableMoodles);

                ImGui.Spacing();

                if (ImGui.Button("Save Configuration"))
                {
                    controller.SaveConfig();
                }
            }
        );

        ImGui.EndChild();
    }
}
