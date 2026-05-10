using System.Numerics;
using KinkLinkClient.Dependencies.CustomizePlus.Services;
using KinkLinkClient.Dependencies.Glamourer.Services;
using KinkLinkClient.Dependencies.Honorific.Services;
using KinkLinkClient.Dependencies.Moodles.Services;
using KinkLinkClient.Dependencies.Penumbra.Services;
using KinkLinkClient.Domain.Interfaces;
using KinkLinkClient.Utils;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Colors;

namespace KinkLinkClient.UI.Views.Settings;

public class SettingsViewUi(
    SettingsViewUiController controller,
    PenumbraService penumbraService,
    GlamourerService glamourerService,
    MoodlesService moodlesService,
    CustomizePlusService customizePlusService,
    HonorificService honorificService) : IDrawable
{
    private static readonly Vector2 CheckboxPadding = new(8, 0);

    public void Draw()
    {
        ImGui.BeginChild("SettingsContent", Vector2.Zero, false, KinkLinkStyle.ContentFlags);
        ImGui.PushStyleVar(ImGuiStyleVar.ItemInnerSpacing, CheckboxPadding);

        SharedUserInterfaces.ContentBox("", KinkLinkStyle.PanelBackground, true, () =>
        {
            SharedUserInterfaces.MediumText("Emergency Actions");
            ImGui.AlignTextToFramePadding();
            if (ImGui.Checkbox("Safe mode is", ref Plugin.Configuration.SafeMode))
                controller.EnterSafeMode(Plugin.Configuration.SafeMode);

            SharedUserInterfaces.Tooltip(
            [
                "Enabling safe mode will cancel any commands sent to you and",
                " prevent further ones from being processed"
            ]);

            ImGui.SameLine();
            if (Plugin.Configuration.SafeMode)
                ImGui.TextColored(ImGuiColors.HealerGreen, "ON");
            else
                ImGui.TextColored(ImGuiColors.DalamudRed, "OFF");
        });

        SharedUserInterfaces.ContentBox("SettingsGeneral", KinkLinkStyle.PanelBackground, true, () =>
        {
            SharedUserInterfaces.MediumText("General");

            // Only draw the remaining UI elements if the character configuration value is set
            if (Plugin.CharacterConfiguration is null)
                return;

            if (ImGui.Checkbox("Auto Connect", ref Plugin.CharacterConfiguration.AutoLogin))
                controller.SaveConfiguration();
        });

        SharedUserInterfaces.ContentBox("SettingsDependencies", KinkLinkStyle.PanelBackground, true, () =>
        {
            SharedUserInterfaces.MediumText("Dependencies");
            ImGui.TextColored(ImGuiColors.DalamudGrey,
                "Install these additional plugins for a more complete experience");

            ImGui.Spacing();

            DrawCheckmarkOrCrossOut(penumbraService.ApiAvailable);
            ImGui.SameLine();
            ImGui.TextUnformatted("Penumbra");

            DrawCheckmarkOrCrossOut(glamourerService.ApiAvailable);
            ImGui.SameLine();
            ImGui.TextUnformatted("Glamourer");

            DrawCheckmarkOrCrossOut(moodlesService.ApiAvailable);
            ImGui.SameLine();
            ImGui.TextUnformatted("Moodles");

            DrawCheckmarkOrCrossOut(customizePlusService.ApiAvailable);
            ImGui.SameLine();
            ImGui.TextUnformatted("Customize+");

            DrawCheckmarkOrCrossOut(honorificService.ApiAvailable);
            ImGui.SameLine();
            ImGui.TextUnformatted("Honorific");
        });

        SharedUserInterfaces.ContentBox("SettingsAbout", KinkLinkStyle.PanelBackground, true, () =>
        {
            SharedUserInterfaces.MediumText("About");
            ImGui.TextColored(ImGuiColors.DalamudGrey, $"Plugin Version: {Plugin.Version}");
        });

        ImGui.PopStyleVar();
        ImGui.EndChild();
    }

    private static void DrawCheckmarkOrCrossOut(bool apiAvailable)
    {
        if (apiAvailable)
            SharedUserInterfaces.Icon(FontAwesomeIcon.Check, ImGuiColors.HealerGreen);
        else
            SharedUserInterfaces.Icon(FontAwesomeIcon.Times, ImGuiColors.DalamudRed);
    }

}
