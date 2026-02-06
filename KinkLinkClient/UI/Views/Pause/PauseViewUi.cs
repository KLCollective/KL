using System.Linq;
using System.Numerics;
using KinkLinkClient.Domain;
using KinkLinkClient.Domain.Interfaces;
using KinkLinkClient.Services;
using KinkLinkClient.Utils;
using KinkLinkCommon.Domain.Enums.Permissions;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;

// ReSharper disable ForCanBeConvertedToForeach

namespace KinkLinkClient.UI.Views.Pause;

public class PauseViewUi(
    PauseViewUiController controller,
    FriendsListService friendsListService,
    PauseService pauseService) : IDrawable
{
    public void Draw()
    {
        ImGui.BeginChild("OverridesContent", Vector2.Zero, false, KinkLinkStyle.ContentFlags);
        ImGui.PushStyleVar(ImGuiStyleVar.ChildRounding, KinkLinkStyle.Rounding);

        var width = new Vector2(ImGui.GetWindowWidth() - KinkLinkStyle.NavBarDimensions.X, 0);
        if (ImGui.BeginChild("PauseFeatureHeader", width, false, KinkLinkStyle.ContentFlags))
        {
            SharedUserInterfaces.ContentBox("PauseHeader", KinkLinkStyle.PanelBackground, true, () =>
            {
                ImGui.TextUnformatted("Pausing a feature disables all incoming requests of that feature");
            });

            SharedUserInterfaces.ContentBox("PauseSpeakPermissions", KinkLinkStyle.PanelBackground, true, () =>
            {
                ImGui.TextUnformatted("Speak Permissions");
                if (ImGui.BeginTable("GeneralSpeakPermissions", 4) is false)
                    return;

                BuildPauseButtonForSpeakFeature(GarblerChannels.None);

                ImGui.TableNextColumn();
                BuildPauseButtonForSpeakFeature(GarblerChannels.Say);
                ImGui.TableNextColumn();
                BuildPauseButtonForSpeakFeature(GarblerChannels.Yell);
                ImGui.TableNextColumn();
                BuildPauseButtonForSpeakFeature(GarblerChannels.Shout);
                ImGui.TableNextColumn();
                BuildPauseButtonForSpeakFeature(GarblerChannels.Tell);
                ImGui.TableNextColumn();
                BuildPauseButtonForSpeakFeature(GarblerChannels.Party);
                ImGui.TableNextColumn();
                BuildPauseButtonForSpeakFeature(GarblerChannels.Alliance);
                ImGui.TableNextColumn();
                BuildPauseButtonForSpeakFeature(GarblerChannels.FreeCompany);
                ImGui.TableNextColumn();
                BuildPauseButtonForSpeakFeature(GarblerChannels.PvPTeam);
                ImGui.TableNextColumn();
                BuildPauseButtonForSpeakFeature(GarblerChannels.Echo);
                ImGui.TableNextColumn();
                BuildPauseButtonForSpeakFeature(GarblerChannels.Roleplay);

                ImGui.EndTable();
            });

            SharedUserInterfaces.ContentBox("PauseLinkshellPermissions", KinkLinkStyle.PanelBackground, true, () =>
            {
                ImGui.TextUnformatted("Linkshell Permissions");
                if (ImGui.BeginTable("LinkshellSpeakPermissions", 4) is false)
                    return;

                ImGui.TableNextColumn();
                BuildPauseButtonForSpeakFeature(GarblerChannels.Ls1);
                ImGui.TableNextColumn();
                BuildPauseButtonForSpeakFeature(GarblerChannels.Ls2);
                ImGui.TableNextColumn();
                BuildPauseButtonForSpeakFeature(GarblerChannels.Ls3);
                ImGui.TableNextColumn();
                BuildPauseButtonForSpeakFeature(GarblerChannels.Ls4);
                ImGui.TableNextColumn();
                BuildPauseButtonForSpeakFeature(GarblerChannels.Ls5);
                ImGui.TableNextColumn();
                BuildPauseButtonForSpeakFeature(GarblerChannels.Ls6);
                ImGui.TableNextColumn();
                BuildPauseButtonForSpeakFeature(GarblerChannels.Ls7);
                ImGui.TableNextColumn();
                BuildPauseButtonForSpeakFeature(GarblerChannels.Ls8);

                ImGui.EndTable();
            });

            SharedUserInterfaces.ContentBox("PauseCrossWorldPermissions", KinkLinkStyle.PanelBackground, true, () =>
            {
                ImGui.TextUnformatted("Cross-world Linkshell Permissions");
                if (ImGui.BeginTable("Cross-worldLinkshellPermissions", 4) is false)
                    return;

                ImGui.TableNextColumn();
                BuildPauseButtonForSpeakFeature(GarblerChannels.Cwl1);
                ImGui.TableNextColumn();
                BuildPauseButtonForSpeakFeature(GarblerChannels.Cwl2);
                ImGui.TableNextColumn();
                BuildPauseButtonForSpeakFeature(GarblerChannels.Cwl3);
                ImGui.TableNextColumn();
                BuildPauseButtonForSpeakFeature(GarblerChannels.Cwl4);
                ImGui.TableNextColumn();
                BuildPauseButtonForSpeakFeature(GarblerChannels.Cwl5);
                ImGui.TableNextColumn();
                BuildPauseButtonForSpeakFeature(GarblerChannels.Cwl6);
                ImGui.TableNextColumn();
                BuildPauseButtonForSpeakFeature(GarblerChannels.Cwl7);
                ImGui.TableNextColumn();
                BuildPauseButtonForSpeakFeature(GarblerChannels.Cwl8);

                ImGui.EndTable();
            });

            SharedUserInterfaces.ContentBox("PauseGeneralPermissions", KinkLinkStyle.PanelBackground, true, () =>
            {
                ImGui.TextUnformatted("General Permissions");
                if (ImGui.BeginTable("GeneralPermissions", 2) is false)
                    return;

                ImGui.TableNextColumn();
                BuildPauseButtonForPrimaryFeature(PrimaryPermissions2.Emote);
                ImGui.TableNextColumn();
                BuildPauseButtonForPrimaryFeature(PrimaryPermissions2.Hypnosis);

                ImGui.EndTable();
            });

            // TODO: Change includeEndPadding once elevated permissions are back
            SharedUserInterfaces.ContentBox("PauseAttributes", KinkLinkStyle.PanelBackground, false, () =>
            {
                ImGui.TextUnformatted("Character Attributes");
                if (ImGui.BeginTable("CharacterAttributes", 2))
                {
                    ImGui.TableNextColumn();
                    BuildPauseButtonForPrimaryFeature(PrimaryPermissions2.GlamourerCustomization);
                    ImGui.TableNextColumn();
                    BuildPauseButtonForPrimaryFeature(PrimaryPermissions2.GlamourerEquipment);
                    ImGui.TableNextColumn();
                    BuildPauseButtonForPrimaryFeature(PrimaryPermissions2.Mods);
                    ImGui.TableNextColumn();
                    BuildPauseButtonForPrimaryFeature(PrimaryPermissions2.BodySwap);
                    ImGui.TableNextColumn();
                    BuildPauseButtonForPrimaryFeature(PrimaryPermissions2.Twinning);
                    ImGui.TableNextColumn();
                    BuildPauseButtonForPrimaryFeature(PrimaryPermissions2.Moodles);
                    ImGui.TableNextColumn();
                    BuildPauseButtonForPrimaryFeature(PrimaryPermissions2.CustomizePlus);
                    ImGui.TableNextColumn();
                    BuildPauseButtonForPrimaryFeature(PrimaryPermissions2.Honorific);

                    ImGui.EndTable();
                }
            });

            /*
            SharedUserInterfaces.ContentBox("TransformationElevatedPermissions", KinkLinkStyle.ElevatedBackground, false, () =>
            {
                ImGui.TextUnformatted("Character Attributes");
                if (ImGui.BeginTable("CharacterAttributes", 2) is false)
                    return;

                ImGui.TableNextColumn();
                BuildPauseButtonForElevatedFeature(ElevatedPermissions.PermanentTransformation);

                ImGui.EndTable();
            });
            */

            ImGui.EndChild();
        }

        ImGui.SameLine();

        if (ImGui.BeginChild("PauseFriendHeader", Vector2.Zero, false, KinkLinkStyle.ContentFlags))
        {
            SharedUserInterfaces.ContentBox("PauseFriendList", KinkLinkStyle.PanelBackground, true, () =>
            {
                ImGui.TextUnformatted("Pause Friend");

                ImGui.SetNextItemWidth(ImGui.GetWindowWidth() - ImGui.GetCursorPos().X - ImGui.GetStyle().WindowPadding.X);
                ImGui.InputTextWithHint("##SearchFriendInputText", "Search", ref controller.SearchString, 1000);
            });

            if (ImGui.BeginChild("PauseFriendBody", Vector2.Zero, true))
            {
                var sorted = friendsListService.Friends.OrderBy(f => f.NoteOrFriendCode).ToList();
                for (var i = 0; i < sorted.Count; i++)
                    BuildPauseButtonForFriend(sorted[i]);

                ImGui.EndChild();
            }

            ImGui.EndChild();
        }

        ImGui.PopStyleVar();
        ImGui.EndChild();
    }

    private void BuildPauseButtonForFriend(Friend friend)
    {
        if (pauseService.IsFriendPaused(friend.FriendCode))
        {
            ImGui.PushStyleColor(ImGuiCol.Button, KinkLinkStyle.PrimaryColor);
            if (SharedUserInterfaces.IconButton(FontAwesomeIcon.Play, null, null, friend.FriendCode))
                pauseService.ToggleFriend(friend.FriendCode);
            ImGui.PopStyleColor();
        }
        else
        {
            if (SharedUserInterfaces.IconButton(FontAwesomeIcon.Pause, null, null, friend.FriendCode))
                pauseService.ToggleFriend(friend.FriendCode);
        }

        ImGui.SameLine();
        ImGui.TextUnformatted(friend.NoteOrFriendCode);
    }

    private void BuildPauseButtonForSpeakFeature(GarblerChannels permissions)
    {
        if (pauseService.IsFeaturePaused(permissions))
        {
            ImGui.PushStyleColor(ImGuiCol.Button, KinkLinkStyle.PrimaryColor);
            if (SharedUserInterfaces.IconButton(FontAwesomeIcon.Play, null, null, permissions.ToString()))
                pauseService.ToggleFeature(permissions);
            ImGui.PopStyleColor();
        }
        else
        {
            if (SharedUserInterfaces.IconButton(FontAwesomeIcon.Pause, null, null, permissions.ToString()))
                pauseService.ToggleFeature(permissions);
        }

        ImGui.SameLine();
        ImGui.TextUnformatted(permissions.ToString());
    }

    private void BuildPauseButtonForPrimaryFeature(PrimaryPermissions2 permissions)
    {
        if (pauseService.IsFeaturePaused(permissions))
        {
            ImGui.PushStyleColor(ImGuiCol.Button, KinkLinkStyle.PrimaryColor);
            if (SharedUserInterfaces.IconButton(FontAwesomeIcon.Play, null, null, permissions.ToString()))
                pauseService.ToggleFeature(permissions);
            ImGui.PopStyleColor();
        }
        else
        {
            if (SharedUserInterfaces.IconButton(FontAwesomeIcon.Pause, null, null, permissions.ToString()))
                pauseService.ToggleFeature(permissions);
        }

        ImGui.SameLine();
        ImGui.TextUnformatted(permissions.ToString());
    }

    // TODO: Re-Enable when a new Mare solution is made
    /*
    private void BuildPauseButtonForElevatedFeature(ElevatedPermissions permissions)
    {
        if (pauseService.IsFeaturePaused(permissions))
        {
            ImGui.PushStyleColor(ImGuiCol.Button, KinkLinkStyle.PrimaryColor);
            if (SharedUserInterfaces.IconButton(FontAwesomeIcon.Play, null, null, permissions.ToString()))
                pauseService.ToggleFeature(permissions);
            ImGui.PopStyleColor();
        }
        else
        {
            if (SharedUserInterfaces.IconButton(FontAwesomeIcon.Pause, null, null, permissions.ToString()))
                pauseService.ToggleFeature(permissions);
        }

        ImGui.SameLine();
        ImGui.TextUnformatted(permissions.ToString());
    }
    */
}
