using System.Collections.Generic;
using System.Numerics;
using KinkLinkClient.Domain;
using KinkLinkClient.Domain.Enums;
using KinkLinkClient.Managers;
using KinkLinkClient.Style;
using KinkLinkClient.Utils;
using KinkLinkCommon.Domain.Enums;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Colors;

// ReSharper disable once RedundantBoolCompare

namespace KinkLinkClient.UI.Components.Friends;

public class PairsListComponentUi(FriendsListComponentUiController controller, SelectionManager selectionManager)
{
    public void Draw(bool displayAddFriendsBox = false, bool displayOfflineFriends = false)
    {
        controller.Filter.Refresh();

        if (ImGui.BeginChild("FriendsListComponent", new Vector2(KinkLinkDimensions.NavBar.X - KinkLinkImGui.WindowPadding.X, 0), false, KinkLinkStyle.ContentFlags) is false)
        {
            ImGui.EndChild();
            return;
        }

        var width = ImGui.GetWindowWidth() - ImGui.GetCursorPosX() - KinkLinkImGui.WindowPadding.X * 2;

        SharedUserInterfaces.ContentBox("FriendsListSearch", KinkLinkStyle.PanelBackground, true, () =>
        {
            ImGui.TextUnformatted("Search");
            ImGui.SetNextItemWidth(width - KinkLinkImGui.WindowPadding.X - KinkLinkDimensions.IconButton.X);
            if (ImGui.InputTextWithHint("###SearchFriendInputText", "Friend", ref controller.SearchText, 24))
                controller.Filter.UpdateSearchTerm(controller.SearchText);

            ImGui.SameLine();

            if (controller.Filter.SortMode is FilterSortMode.Alphabetically)
            {
                if (SharedUserInterfaces.IconButton(FontAwesomeIcon.SortAlphaDown, KinkLinkDimensions.IconButton, "Filtering by Alphabetical"))
                    controller.ToggleSortMode();
            }
            else
            {
                if (SharedUserInterfaces.IconButton(FontAwesomeIcon.SortAmountUp, KinkLinkDimensions.IconButton, "Filtering by most recent interaction"))
                    controller.ToggleSortMode();
            }
        });

        var height = displayAddFriendsBox
            ? KinkLinkImGui.WindowPadding.Y * 3 + KinkLinkImGui.FramePadding.Y * 4 + ImGui.GetFontSize() * 2 + KinkLinkImGui.ItemSpacing.Y * 2
            : 0;

        if (ImGui.BeginChild("###PermissionViewFriendsList", new Vector2(0, -height), true))
        {
            var pending = new List<Friend>();
            var online = new List<Friend>();
            var offline = new List<Friend>();

            foreach (var friend in controller.Filter.List)
            {
                var list = friend.Status switch
                {
                    FriendOnlineStatus.Pending => pending,
                    FriendOnlineStatus.Online => online,
                    _ => offline
                };

                list.Add(friend);
            }

            if (displayOfflineFriends && pending.Count > 0)
            {
                ImGui.TextColored(ImGuiColors.ParsedPink, "Pending");
                foreach (var friend in pending)
                {
                    if (ImGui.Selectable($"{friend.NoteOrFriendCode}##{friend.FriendCode}", selectionManager.Contains(friend)))
                        selectionManager.Select(friend, ImGui.GetIO().KeyCtrl);
                }
            }

            ImGui.TextColored(ImGuiColors.HealerGreen, "Online");
            foreach (var friend in online)
            {
                if (ImGui.Selectable($"{friend.NoteOrFriendCode}##{friend.FriendCode}", selectionManager.Contains(friend)))
                    selectionManager.Select(friend, ImGui.GetIO().KeyCtrl);
            }

            if (displayOfflineFriends)
            {
                ImGui.TextColored(ImGuiColors.DalamudRed, "Offline");
                foreach (var friend in offline)
                {
                    if (ImGui.Selectable($"{friend.NoteOrFriendCode}###{friend.FriendCode}", selectionManager.Contains(friend)))
                        selectionManager.Select(friend, ImGui.GetIO().KeyCtrl);
                }
            }

            ImGui.EndChild();
        }

        if (displayAddFriendsBox)
        {
            ImGui.Spacing();

            SharedUserInterfaces.ContentBox("AddFriendContentBox", KinkLinkStyle.PanelBackground, false, () =>
            {
                ImGui.SetNextItemWidth(width);
                ImGui.InputTextWithHint("###AddFriendInputText", "Friend code", ref controller.FriendCodeToAdd, 128);

                if (ImGui.IsItemHovered())
                    ImGui.SetTooltip("Friend codes are case sensitive");

                ImGui.Spacing();
                if (ImGui.Button("Add Friend", new Vector2(width, 0)))
                    _ = controller.Add();
            });
        }

        ImGui.EndChild();
    }
}
