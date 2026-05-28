using System.Numerics;
using Dalamud.Bindings.ImGui;
using System.Collections.Generic;
using KinkLinkClient.Domain;
using KinkLinkClient.Domain.Interfaces;
using KinkLinkClient.Services;
using KinkLinkClient.Utils;
using KinkLinkCommon.Domain;

namespace KinkLinkClient.UI.Views.Debug;

public class DebugViewUi(
    FriendsListService friendsListService,
    NetworkService networkService,
    IdentityService identityService,
    LockService lockService,
    WardrobeManager wardrobeManager
) : IDrawable
{
    public void Draw()
    {
        ImGui.BeginChild("DebugContent", Vector2.Zero, false, KinkLinkStyle.ContentFlags);

        SharedUserInterfaces.ContentBox(
            "DebugLocalData",
            KinkLinkStyle.PanelBackground,
            true,
            () =>
            {
                DrawConfiguration();
            }
        );

        SharedUserInterfaces.ContentBox(
            "DebugRuntimeState",
            KinkLinkStyle.PanelBackground,
            true,
            () =>
            {
                DrawRuntimeState();
            }
        );

        SharedUserInterfaces.ContentBox(
            "DebugLocks",
            KinkLinkStyle.PanelBackground,
            true,
            () =>
            {
                DrawLocks();
            }
        );

        SharedUserInterfaces.ContentBox(
            "DebugWardrobe",
            KinkLinkStyle.PanelBackground,
            true,
            () =>
            {
                DrawWardrobe();
            }
        );

        SharedUserInterfaces.ContentBox(
            "DebugPairs",
            KinkLinkStyle.PanelBackground,
            true,
            () =>
            {
                DrawPairs();
            }
        );

        ImGui.EndChild();
    }

    private void DrawConfiguration()
    {
        var config = Plugin.Configuration;
        if (config != null)
        {
            ImGui.Text($"Version: {config.Version}");
            ImGui.Text($"ServerBaseUrl: {config.ServerBaseUrl}");
            ImGui.Text($"SafeMode: {config.SafeMode}");
            ImGui.Text(
                $"SecretKey: {(string.IsNullOrEmpty(config.SecretKey) ? "(empty)" : "***")}"
            );
            ImGui.Text($"Notes count: {config.Notes.Count}");
        }

        var charConfig = Plugin.CharacterConfiguration;
        if (charConfig != null)
        {
            ImGui.Text($"Char Name: {charConfig.Name}");
            ImGui.Text($"Char World: {charConfig.World}");
            ImGui.Text($"AutoLogin: {charConfig.AutoLogin}");
            ImGui.Text($"ProfileUID: {charConfig.ProfileUID}");
            ImGui.Text($"ChatTitle: {charConfig.ChatTitle}");
        }
        else
        {
            ImGui.TextUnformatted("No CharacterConfiguration loaded");
        }
    }

    private void DrawRuntimeState()
    {
        ImGui.Text($"My FriendCode: {identityService.FriendCode}");
        ImGui.Text($"Connection State: {networkService.Connection.State}");
        ImGui.Text($"Is Altered: {identityService.IsAltered}");

        if (identityService.Alteration != null)
        {
            ImGui.Text($"  Alteration Type: {identityService.Alteration.Type}");
            ImGui.Text($"  Alteration Sender: {identityService.Alteration.Sender}");
        }
    }

    private void DrawLocks()
    {
        var locks = lockService.GetAllLocks();
        ImGui.Text($"Active locks: {locks.Count}");

        foreach (var kvp in locks)
        {
            if (ImGui.TreeNode(kvp.Key))
            {
                ImGui.Text($"LockID: {kvp.Value.LockID}");
                ImGui.Text($"LockeeID: {kvp.Value.LockeeID}");
                ImGui.Text($"LockerID: {kvp.Value.LockerID}");
                ImGui.Text($"LockPriority: {kvp.Value.LockPriority}");
                ImGui.Text($"CanSelfUnlock: {kvp.Value.CanSelfUnlock}");
                ImGui.Text($"Expires: {kvp.Value.Expires}");
                ImGui.TreePop();
            }
        }
    }

    private void DrawWardrobe()
    {
        var activeSet = wardrobeManager.ActiveSet;
        ImGui.Text($"ActiveSet IsActive: {activeSet.IsActive()}");
        ImGui.Text($"WardrobeLibrary: {wardrobeManager.WardrobeLibrary.Count}");

        if (ImGui.TreeNode("SlotStatuses"))
        {
            foreach (var layer in wardrobeManager.ActiveSet.Layers)
            {
                ImGui.Text(
                    $"Layer: {layer.Key} Id: {layer.Value.Id} Name: {layer.Value.Name} Description: {layer.Value.Description} Layer: {layer.Value.Layer} Priority {layer.Value.Priority} Mods: {layer.Value.Mods} "
                );
            }
            ImGui.TreePop();
        }

        var mods = activeSet.GetMods();
        ImGui.Text($"Total mods: {mods.Count}");
    }

    private void DrawPairs()
    {
        var friends = friendsListService.Friends;
        ImGui.Text($"Total pairs: {friends.Count}");

        foreach (var friend in friends)
        {
            var label = friend.NoteOrFriendCode;
            if (ImGui.TreeNode(label))
            {
                ImGui.Text($"FriendCode: {friend.FriendCode}");
                ImGui.Text($"Status: {friend.Status}");
                ImGui.Text($"Note: {friend.Note ?? "(none)"}");
                ImGui.Text($"LastInteractedWith: {friend.LastInteractedWith}");
                ImGui.Text(
                    $"PermissionsGrantedToFriend: {friend.PermissionsGrantedToFriend.Perms}"
                );
                ImGui.Text(
                    $"PermissionsGrantedByFriend: {friend.PermissionsGrantedByFriend.Perms}"
                );

                DrawInteractionState(friend);
                ImGui.TreePop();
            }
        }
    }

    private void DrawPairsSlotLocks()
    {
        var friends = friendsListService.Friends;
        ImGui.Text($"Total pairs: {friends.Count}");

        foreach (var friend in friends)
        {
            var label = friend.NoteOrFriendCode;
            if (ImGui.TreeNode(label))
            {
                DrawInteractionSlotLocks(friend);
                ImGui.TreePop();
            }
        }
    }

    private void DrawInteractionSlotLocks(Friend friend)
    {
        if (friend.WardrobeState == null)
        {
            ImGui.TextUnformatted("WardrobeState: null");
            return;
        }

        var state = friend.WardrobeState;
        var locks = new List<LockInfoDto?>();
        foreach (var kv in state.Layers)
        {
            if (kv.Value?.LockId != null)
                locks.Add(kv.Value.LockId);
        }

        ImGui.Text($"SlotLocks count: {locks.Count}");

        if (locks.Count == 0)
        {
            ImGui.TextUnformatted("(no locks)");
        }
        else
        {
            foreach (var lockInfo in locks)
            {
                if (lockInfo is { })
                {
                    ImGui.Text(
                        $"LockID={lockInfo.LockID} LockeeID={lockInfo.LockeeID} LockerID={lockInfo.LockerID} LockPriority={lockInfo.LockPriority} CanSelfUnlock={lockInfo.CanSelfUnlock} Expires={lockInfo.Expires} Password={lockInfo.Password}"
                    );
                }
            }
        }
    }

    private void DrawInteractionState(Friend friend)
    {
        if (friend.WardrobeState == null)
        {
            ImGui.TextUnformatted("WardrobeState: null");
            return;
        }

        if (ImGui.TreeNode("WardrobeState"))
        {
            var state = friend.WardrobeState;

            if (ImGui.TreeNode("WardrobeItems"))
            {
                if (state.Layers.Count == 0)
                {
                    ImGui.TextUnformatted("(none)");
                }
                else
                {
                    foreach (var item in state.Layers)
                    {
                        if (ImGui.TreeNode(item.Value.Name))
                        {
                            ImGui.Text($"Id: {item.Value.Id}");
                            ImGui.Text($"Name: {item.Value.Name}");
                            ImGui.Text($"Layer: {item.Value.Layer}");
                            ImGui.Text($"Priority: {item.Value.Priority}");
                            ImGui.Text($"Description: {item.Value.Description}");
                            ImGui.Text($"LockId: {item.Value.LockId}");
                            ImGui.TreePop();
                        }
                    }
                }
                ImGui.TreePop();
            }

            if (ImGui.TreeNode("SlotLocks"))
            {
                var locks = new List<LockInfoDto?>();
                foreach (var kv in state.Layers)
                {
                    if (kv.Value?.LockId != null)
                        locks.Add(kv.Value.LockId);
                }

                if (locks.Count == 0)
                {
                    ImGui.TextUnformatted("(none)");
                }
                else
                {
                    foreach (var lockInfo in locks)
                    {
                        if (lockInfo is { })
                        {
                            ImGui.Text(
                                $"LockID={lockInfo.LockID}, LockeeID={lockInfo.LockeeID}, LockerID={lockInfo.LockerID}, LockPriority={lockInfo.LockPriority}, CanSelfUnlock={lockInfo.CanSelfUnlock}, Expires={lockInfo.Expires}, Password={lockInfo.Password}"
                            );
                        }
                    }
                }
                ImGui.TreePop();
            }

            ImGui.TreePop();
        }
    }
}
