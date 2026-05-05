using System.Collections.Generic;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using KinkLinkClient.Domain.Interfaces;
using KinkLinkClient.Services;
using KinkLinkClient.Utils;

namespace KinkLinkClient.UI.Views.Login;

public class LoginViewUi(LoginViewUiController controller, NetworkService networkService)
    : IDrawable
{
    private const ImGuiInputTextFlags SecretInputFlags =
        ImGuiInputTextFlags.EnterReturnsTrue
        | ImGuiInputTextFlags.Password
        | ImGuiInputTextFlags.AutoSelectAll;

    public void Draw()
    {
        ImGui.BeginChild("LoginContent", Vector2.Zero, false, KinkLinkStyle.ContentFlags);

        ImGui.AlignTextToFramePadding();

        SharedUserInterfaces.ContentBox(
            "LoginHeader",
            KinkLinkStyle.PanelBackground,
            true,
            () =>
            {
                SharedUserInterfaces.BigTextCentered("Kink Link");
                SharedUserInterfaces.TextCentered(Plugin.Version.ToString());
            }
        );

        SharedUserInterfaces.ContentBox(
            "LoginSecret",
            KinkLinkStyle.PanelBackground,
            true,
            () =>
            {
                SharedUserInterfaces.MediumText("Server");
                ImGui.SetNextItemWidth(200);
                if (
                    ImGui.Combo(
                        "##ServerSelector",
                        ref controller.ServerIndex,
                        ServerOptions.Names,
                        ServerOptions.Names.Length
                    )
                )
                {
                    controller.SelectServer(controller.ServerIndex);
                }

                ImGui.Spacing();
                var has_uid = false;
                var has_secret = false;
                ImGui.BeginDisabled(controller.AvailableProfileUids.Count != 0);
                SharedUserInterfaces.MediumText("Enter Secret");
                if (
                    ImGui.InputTextWithHint(
                        "##SecretInput",
                        "Secret",
                        ref controller.Secret,
                        120,
                        SecretInputFlags
                    )
                )
                {
                    has_secret = true;
                }

                ImGui.EndDisabled();

                ImGui.SameLine();
                // Cannot connect without first querying the server.
                ImGui.BeginDisabled(controller.ServerIndex == 0 || controller.IsQuerying);
                if (ImGui.SmallButton("Get Profiles##Secret"))
                    controller.GetProfileUids();
                ImGui.EndDisabled();

                SharedUserInterfaces.MediumText("Select Profile");
                var profileIndex = 0;
                if (!string.IsNullOrEmpty(controller.SelectedProfileUID))
                {
                    for (var i = 0; i < controller.AvailableProfileUids.Count; i++)
                    {
                        if (
                            controller.AvailableProfileUids[i].Item1
                            == controller.SelectedProfileUID
                        )
                        {
                            profileIndex = i;
                            break;
                        }
                    }
                }

                var profileItems = controller.ProfilesAvailable
                    ? controller.AvailableProfileUids.ConvertAll(p => p.Item2).ToArray()
                    : ["No profiles available"];

                ImGui.SetNextItemWidth(200);
                if (
                    ImGui.Combo(
                        "##ProfileSelector",
                        ref profileIndex,
                        profileItems,
                        profileItems.Length
                    )
                )
                {
                    if (
                        controller.ProfilesAvailable
                        && profileIndex < controller.AvailableProfileUids.Count
                    )
                        controller.SelectedProfileUID = controller
                            .AvailableProfileUids[profileIndex]
                            .Item1;
                }

                if (!string.IsNullOrEmpty(controller.SelectedProfileUID))
                    has_uid = true;

                ConnectButton(has_uid, has_secret);
                ImGui.Spacing();

                ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(4, 0));

                ImGui.TextUnformatted("Need a secret? Head over to the");
                ImGui.SameLine();
                ImGui.PushStyleColor(ImGuiCol.Text, KinkLinkStyle.DiscordBlue);
                var size = ImGui.CalcTextSize("discord");
                if (ImGui.Selectable("discord", false, ImGuiSelectableFlags.None, size))
                    LoginViewUiController.OpenDiscordLink();

                ImGui.PopStyleColor();
                ImGui.SameLine();
                ImGui.TextUnformatted("to generate one.");

                ImGui.PopStyleVar();
            }
        );

        ImGui.EndChild();
    }

    private void ConnectButton(bool has_uid, bool has_secret)
    {
        // This button has two main states.
        // State 1: Only the secret key is present
        // State 2: The secret key has been validated and the profile is being selected.
        var shouldConnect = has_uid && has_secret;
        ImGui.SameLine();
        ImGui.BeginDisabled(
            !controller.CanConnect || networkService.Connecting || controller.IsQuerying
        );
        if (ImGui.Button("Connect"))
            shouldConnect = true;

        if (shouldConnect)
            controller.Connect();
        ImGui.EndDisabled();
    }
}
