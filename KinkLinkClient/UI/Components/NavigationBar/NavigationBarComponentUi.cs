using System.Numerics;
using KinkLinkClient.Domain;
using KinkLinkClient.Managers;
using KinkLinkClient.Services;
using KinkLinkClient.Utils;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Microsoft.AspNetCore.SignalR.Client;

namespace KinkLinkClient.UI.Components.NavigationBar;

public class NavigationBarComponentUi(NetworkService networkService, ViewService viewService, SelectionManager selection)
{
    // Const
    private static readonly Vector2 AlignButtonTextLeft = new(0, 0.5f);

    public void Draw()
    {
        var spacing = ImGui.GetStyle().ItemSpacing;
        var windowPadding = ImGui.GetStyle().WindowPadding;
        var size = new Vector2(KinkLinkStyle.NavBarDimensions.X - windowPadding.X * 2, 25);
        var offset = windowPadding with { Y = (size.Y - ImGui.GetFontSize()) * 0.5f };

        ImGui.PushStyleVar(ImGuiStyleVar.ChildRounding, KinkLinkStyle.Rounding);
        ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, KinkLinkStyle.Rounding);

        if (ImGui.BeginChild("###MainWindowNavBar", KinkLinkStyle.NavBarDimensions, true, ImGuiWindowFlags.NoScrollbar))
        {
            ImGui.PushStyleVar(ImGuiStyleVar.ButtonTextAlign, AlignButtonTextLeft);

            if (networkService.Connection.State is HubConnectionState.Connected)
            {
                ImGui.TextUnformatted("General");
                NavBarButton(FontAwesomeIcon.User, "Status", View.Status, size, offset, spacing);
                NavBarButton(FontAwesomeIcon.UserCircle, "Profile", View.Profile, size, offset, spacing);
                NavBarButton(FontAwesomeIcon.UserFriends, "Pairs", View.Pairs, size, offset, spacing);
                NavBarButton(FontAwesomeIcon.Pause, "Permissions", View.Pause, size, offset, spacing);
                NavBarButton(FontAwesomeIcon.PeopleArrows, "Chat", View.Chat, size, offset, spacing);

                ImGui.TextUnformatted("Wardrobe");
                NavBarButton(FontAwesomeIcon.Kiss, "Gags [TODO]", View.Gags, size, offset, spacing);
                NavBarButton(FontAwesomeIcon.Handcuffs, "Restraints [TODO]", View.Wardrobe, size, offset, spacing);
                NavBarButton(FontAwesomeIcon.WandMagicSparkles, "Cursed Loot [TODO]", View.CursedLoot, size, offset, spacing);

                ImGui.TextUnformatted("Toybox");
                NavBarButton(FontAwesomeIcon.LockOpen, "Locks [TODO]", View.Locks, size, offset, spacing);
                NavBarButton(FontAwesomeIcon.Handshake, "Interactions", View.Interactions, size, offset, spacing);
                NavBarButton(FontAwesomeIcon.Dice, "Games [TODO]", View.Games, size, offset, spacing);

            ImGui.TextUnformatted("Configuration");
            NavBarButton(FontAwesomeIcon.UserCircle, "Profile", View.Profile, size, offset, spacing);
            NavBarButton(FontAwesomeIcon.History, "History", View.History, size, offset, spacing);
        }
            else
            {
                ImGui.TextUnformatted("General");
                NavBarButton(FontAwesomeIcon.Plug, "Login", View.Login, size, offset, spacing);

                ImGui.TextUnformatted("Configuration");
            }

            NavBarButton(FontAwesomeIcon.Wrench, "Settings", View.Settings, size, offset, spacing);

#if DEBUG
            ImGui.TextUnformatted("Testing");
            NavBarButton(FontAwesomeIcon.Bug, "Debug", View.Debug, size, offset, spacing);
#endif

            ImGui.PopStyleVar();
            ImGui.EndChild();
        }

        ImGui.PopStyleVar(2);
    }

    private void NavBarButton(FontAwesomeIcon icon, string text, View view, Vector2 size, Vector2 offset, Vector2 spacing)
    {
        var begin = ImGui.GetCursorPos();
        if (viewService.CurrentView == view)
        {
            ImGui.PushStyleColor(ImGuiCol.Button, KinkLinkStyle.PrimaryColor);
            ImGui.Button($"##{text}", size);
            ImGui.PopStyleColor();
        }
        else
        {
            if (ImGui.Button($"##{text}", size))
            {
                viewService.CurrentView = view;

                // Required to in cases where you move from things like Friends -> Speak, and the friend you were editing was offline
                selection.ClearOfflineFriends();
            }
        }

        ImGui.SetCursorPos(begin + offset);

        SharedUserInterfaces.Icon(icon);
        ImGui.SameLine();
        ImGui.TextUnformatted(text);
        ImGui.SetCursorPos(begin + new Vector2(0, size.Y + spacing.Y));
    }
}
