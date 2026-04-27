using System.Numerics;
using KinkLinkClient.Domain.Interfaces;
using KinkLinkClient.Utils;
using Dalamud.Bindings.ImGui;

namespace KinkLinkClient.UI.Views.Locks;

// TODO: This class needs to be implemented
public class LocksViewUi(LocksViewUiController controller) : IDrawable
{
    // Const
    private const int ActionButtonHeight = 40;

    public void Draw()
    {
        ImGui.BeginChild("LocksContent", Vector2.Zero, false, KinkLinkStyle.ContentFlags);

        var width = ImGui.GetWindowWidth();
        var padding = ImGui.GetStyle().WindowPadding;

        var begin = ImGui.GetCursorPosY();

        SharedUserInterfaces.ContentBox("#locks", KinkLinkStyle.PanelBackground, true, () =>
        {
            SharedUserInterfaces.BigTextCentered("Locks [TODO]");
            ImGui.TextUnformatted("This will be the menu with locks Configuration");
        });

        var headerHeight = ImGui.GetCursorPosY() - begin;
        var contextBoxSize = new Vector2(0, ImGui.GetWindowHeight() - headerHeight - padding.X * 3 - ActionButtonHeight);
        // TODO: Implement the locks configuration menu below
        ImGui.EndChild();
    }
}
