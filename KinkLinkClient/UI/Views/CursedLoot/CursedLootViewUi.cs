using System.Numerics;
using KinkLinkClient.Domain.Interfaces;
using KinkLinkClient.Utils;
using Dalamud.Bindings.ImGui;

namespace KinkLinkClient.UI.Views.CursedLoot;

// TODO: This class needs to be implemented
public class CursedLootViewUi(CursedLootViewUiController controller) : IDrawable
{
    // Const
    private const int ActionButtonHeight = 40;

    public void Draw()
    {
        ImGui.BeginChild("CursedLootContent", Vector2.Zero, false, KinkLinkStyle.ContentFlags);

        var width = ImGui.GetWindowWidth();
        var padding = ImGui.GetStyle().WindowPadding;

        var begin = ImGui.GetCursorPosY();

        SharedUserInterfaces.ContentBox("#cursedloot", KinkLinkStyle.PanelBackground, true, () =>
        {
            SharedUserInterfaces.BigTextCentered("Cursed Loot [TODO]");
            ImGui.TextUnformatted("This will be the menu with Cursed Loot Configuration");
        });

        var headerHeight = ImGui.GetCursorPosY() - begin;
        var contextBoxSize = new Vector2(0, ImGui.GetWindowHeight() - headerHeight - padding.X * 3 - ActionButtonHeight);
        // TODO: Implement the cursed loot configuration menu below
        ImGui.EndChild();
    }
}
