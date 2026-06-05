using System.Numerics;
using Dalamud.Bindings.ImGui;
using System.Collections.Generic;
using KinkLinkClient.Domain;
using KinkLinkClient.Domain.Interfaces;
using KinkLinkClient.Services;
using KinkLinkClient.Utils;
using KinkLinkCommon.Domain;
using KinkLinkCommon.Domain.Enums;
using KinkLinkCommon.Domain.Wardrobe;
using KinkLinkCommon.Dependencies.Glamourer;
using KinkLinkCommon.Dependencies.Glamourer.Components;

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

        foreach (var lockInfo in locks)
        {
            if (ImGui.TreeNode(lockInfo.LockID.ToString()))
            {
                ImGui.Text($"LockID: {lockInfo.LockID}");
                ImGui.Text($"LockeeID: {lockInfo.LockeeID}");
                ImGui.Text($"LockerID: {lockInfo.LockerID}");
                ImGui.Text($"LockPriority: {lockInfo.LockPriority}");
                ImGui.Text($"CanSelfUnlock: {lockInfo.CanSelfUnlock}");
                ImGui.Text($"Expires: {lockInfo.Expires}");
                ImGui.Text($"Password: {lockInfo.Password}");
                ImGui.TreePop();
            }
        }
    }

    // ─── Wardrobe ───────────────────────────────────────────────────────

    private void DrawWardrobe()
    {
        var activeSet = wardrobeManager.ActiveSet;

        ImGui.Text($"Glamourer ApiAvailable: {wardrobeManager.GlamourerApiAvailable}");
        ImGui.Separator();
        ImGui.Text($"ActiveSet IsActive: {activeSet.IsActive()}");
        ImGui.Text($"ActiveSet Layer Count: {activeSet.Layers.Count}");
        ImGui.Text($"WardrobeLibrary Count: {wardrobeManager.WardrobeLibrary.Count}");

        // ── ActiveSet Layers ──
        if (ImGui.TreeNode($"ActiveSet Layers ({activeSet.Layers.Count})"))
        {
            foreach (var kvp in activeSet.Layers)
            {
                DrawActiveLayerTreeNode(kvp.Key, kvp.Value);
            }
            ImGui.TreePop();
        }

        // ── Merged Design ──
        if (activeSet.IsActive())
        {
            if (ImGui.TreeNode("Merged Design (GetCurrentState)"))
            {
                var merged = activeSet.GetCurrentState();
                DrawGlamourerDesignTree(merged);
                ImGui.TreePop();
            }
        }

        // ── Active Mods ──
        var mods = activeSet.GetMods();
        if (ImGui.TreeNode($"Active Mods ({mods.Count})"))
        {
            foreach (var m in mods)
            {
                if (ImGui.TreeNode($"{m.Name}##mod_{m.Name}"))
                {
                    ImGui.Text($"Directory: {m.Directory}");
                    ImGui.Text($"Enabled: {m.Enabled}");
                    ImGui.Text($"Priority: {m.Priority}");
                    ImGui.Text($"ForceInherit: {m.ForceInherit}");
                    ImGui.Text($"Remove: {m.Remove}");
                    if (m.Settings != null && m.Settings.Count > 0)
                    {
                        if (ImGui.TreeNode($"Settings ({m.Settings.Count})"))
                        {
                            foreach (var skvp in m.Settings)
                            {
                                ImGui.Text($"{skvp.Key}: [{string.Join(", ", skvp.Value)}]");
                            }
                            ImGui.TreePop();
                        }
                    }
                    ImGui.TreePop();
                }
            }
            ImGui.TreePop();
        }

        // ── WardrobeLibrary ──
        if (ImGui.TreeNode($"WardrobeLibrary ({wardrobeManager.WardrobeLibrary.Count})"))
        {
            foreach (var libItem in wardrobeManager.WardrobeLibrary)
            {
                if (ImGui.TreeNode($"{libItem.Name}##lib_{libItem.Id}"))
                {
                    DrawWardrobeItemInfo(libItem);
                    if (ImGui.TreeNode("Design"))
                    {
                        DrawGlamourerDesignTree(libItem.Design);
                        ImGui.TreePop();
                    }
                    ImGui.TreePop();
                }
            }
            ImGui.TreePop();
        }

        // ── Layer Lock Status Summary ──
        if (ImGui.TreeNode("Layer Lock Status (per slot)"))
        {
            foreach (WardrobeLayer layer in System.Enum.GetValues(typeof(WardrobeLayer)))
            {
                var lockId = GetWardrobeLockId(layer);
                var layerLock = lockService.GetLock(lockId);
                var hasLayer = activeSet.HasLayer(layer);
                var locked = layerLock.HasValue;

                var icon = locked ? "\U0001f512" : "\U0001f513";
                var status = hasLayer ? "active" : "inactive";
                var label = $"{icon} {layer} [{status}]##lock_{layer}";

                if (ImGui.TreeNode(label))
                {
                    ImGui.Text($"LockId key: {lockId}");
                    ImGui.Text($"Has layer in ActiveSet: {hasLayer}");
                    ImGui.Text($"IsLayerLocked: {wardrobeManager.IsLayerLocked(layer)}");
                    if (locked)
                    {
                        var li = layerLock.Value;
                        ImGui.Text($"LockID: {li.LockID}");
                        ImGui.Text($"LockeeID: {li.LockeeID}");
                        ImGui.Text($"LockerID: {li.LockerID}");
                        ImGui.Text($"LockPriority: {li.LockPriority}");
                        ImGui.Text($"CanSelfUnlock: {li.CanSelfUnlock}");
                        ImGui.Text($"Expires: {li.Expires}");
                        ImGui.Text($"Password: {li.Password}");
                    }
                    else
                    {
                        ImGui.Text("(no lock)");
                    }
                    ImGui.TreePop();
                }
            }
            ImGui.TreePop();
        }
    }

    private void DrawActiveLayerTreeNode(WardrobeLayer layer, WardrobeItem item)
    {
        var lockId = GetWardrobeLockId(layer);
        var layerLock = lockService.GetLock(lockId);
        var lockIcon = layerLock.HasValue ? "\U0001f512" : "";
        var label = $"{layer}{lockIcon}##layer_{layer}";

        if (ImGui.TreeNode(label))
        {
            DrawWardrobeItemInfo(item);

            ImGui.Separator();
            ImGui.Text($"LockId key: {lockId}");
            ImGui.Text($"IsLayerLocked: {wardrobeManager.IsLayerLocked(layer)}");
            if (layerLock.HasValue)
            {
                if (ImGui.TreeNode("Lock Details"))
                {
                    var li = layerLock.Value;
                    var expiresStr = li.Expires?.ToString("O") ?? "(none)";
                    ImGui.Text($"LockID: {li.LockID}");
                    ImGui.Text($"LockeeID: {li.LockeeID}");
                    ImGui.Text($"LockerID: {li.LockerID}");
                    ImGui.Text($"LockPriority: {li.LockPriority}");
                    ImGui.Text($"CanSelfUnlock: {li.CanSelfUnlock}");
                    ImGui.Text($"Expires: {expiresStr}");
                    ImGui.Text($"Password: {li.Password ?? "(none)"}");
                    ImGui.TreePop();
                }
            }
            else
            {
                ImGui.Text("Lock: (none)");
            }

            ImGui.Separator();
            if (ImGui.TreeNode("Design"))
            {
                DrawGlamourerDesignTree(item.Design);
                ImGui.TreePop();
            }

            if (item.Item != null)
            {
                if (ImGui.TreeNode("Slot Equipment Item"))
                {
                    DrawGlamourerItemInfo(item.Item);
                    ImGui.TreePop();
                }
            }

            ImGui.TreePop();
        }
    }

    private void DrawWardrobeItemInfo(WardrobeItem item)
    {
        ImGui.Text($"Id: {item.Id}");
        ImGui.Text($"Name: {item.Name}");
        ImGui.Text($"Description: {item.Description}");
        ImGui.Text($"Layer: {item.Layer}");
        ImGui.Text($"Priority: {item.Priority}");
        ImGui.Text($"Slot: {item.Slot}");
        ImGui.Text($"Mods count: {item.Mods.Count}");
    }

    // ─── Glamourer Design Tree ─────────────────────────────────────────

    private void DrawGlamourerDesignTree(GlamourerDesign design)
    {
        if (design == null)
        {
            ImGui.Text("(null design)");
            return;
        }

        ImGui.Text($"FileVersion: {design.FileVersion}");
        ImGui.Text($"Identifier: {design.Identifier}");
        ImGui.Text($"CreationDate: {design.CreationDate:O}");
        ImGui.Text($"LastEdit: {design.LastEdit:O}");
        ImGui.Text($"Name: {design.Name}");
        ImGui.Text($"Description: {design.Description}");
        ImGui.Text($"ForcedRedraw: {design.ForcedRedraw}");
        ImGui.Text($"ResetAdvancedDyes: {design.ResetAdvancedDyes}");
        ImGui.Text($"ResetTemporarySettings: {design.ResetTemporarySettings}");
        ImGui.Text($"Color: {design.Color}");
        ImGui.Text($"QuickDesign: {design.QuickDesign}");
        ImGui.Text($"WriteProtected: {design.WriteProtected}");
        ImGui.Text($"Tags: [{string.Join(", ", design.Tags)}]");

        if (ImGui.TreeNode("Equipment"))
        {
            DrawGlamourerEquipmentTree(design.Equipment);
            ImGui.TreePop();
        }

        if (ImGui.TreeNode($"Mods ({design.Mods.Count})"))
        {
            foreach (var m in design.Mods)
            {
                ImGui.Text($"{m.Name} Dir={m.Directory} Enabled={m.Enabled} Priority={m.Priority} ForceInherit={m.ForceInherit} Remove={m.Remove}");
            }
            ImGui.TreePop();
        }

        if (ImGui.TreeNode($"Customize"))
        {
            ImGui.Text($"ModelId: {design.Customize.ModelId}");
            ImGui.Text($"BodyType.Apply={design.Customize.BodyType.Apply} Value={design.Customize.BodyType.Value}");
            ImGui.Text($"Gender.Apply={design.Customize.Gender.Apply} Value={design.Customize.Gender.Value}");
            ImGui.Text($"Race.Apply={design.Customize.Race.Apply} Value={design.Customize.Race.Value}");
            ImGui.Text($"Clan.Apply={design.Customize.Clan.Apply} Value={design.Customize.Clan.Value}");
            ImGui.Text($"Height.Apply={design.Customize.Height.Apply} Value={design.Customize.Height.Value}");
            ImGui.Text($"Face.Apply={design.Customize.Face.Apply} Value={design.Customize.Face.Value}");
            ImGui.Text($"Hairstyle.Apply={design.Customize.Hairstyle.Apply} Value={design.Customize.Hairstyle.Value}");
            ImGui.Text($"SkinColor.Apply={design.Customize.SkinColor.Apply} Value={design.Customize.SkinColor.Value}");
            ImGui.Text($"EyeShape.Apply={design.Customize.EyeShape.Apply} Value={design.Customize.EyeShape.Value}");
            ImGui.Text($"EyeColorLeft.Apply={design.Customize.EyeColorLeft.Apply} Value={design.Customize.EyeColorLeft.Value}");
            ImGui.Text($"EyeColorRight.Apply={design.Customize.EyeColorRight.Apply} Value={design.Customize.EyeColorRight.Value}");
            ImGui.Text($"HairColor.Apply={design.Customize.HairColor.Apply} Value={design.Customize.HairColor.Value}");
            ImGui.Text($"Highlights.Apply={design.Customize.Highlights.Apply} Value={design.Customize.Highlights.Value}");
            ImGui.Text($"HighlightsColor.Apply={design.Customize.HighlightsColor.Apply} Value={design.Customize.HighlightsColor.Value}");
            ImGui.Text($"FacialFeature1.Apply={design.Customize.FacialFeature1.Apply} Value={design.Customize.FacialFeature1.Value}");
            ImGui.Text($"FacePaint.Apply={design.Customize.FacePaint.Apply} Value={design.Customize.FacePaint.Value}");
            ImGui.Text($"FacePaintColor.Apply={design.Customize.FacePaintColor.Apply} Value={design.Customize.FacePaintColor.Value}");
            ImGui.Text($"FacePaintReversed.Apply={design.Customize.FacePaintReversed.Apply} Value={design.Customize.FacePaintReversed.Value}");
            ImGui.Text($"Jaw.Apply={design.Customize.Jaw.Apply} Value={design.Customize.Jaw.Value}");
            ImGui.Text($"Mouth.Apply={design.Customize.Mouth.Apply} Value={design.Customize.Mouth.Value}");
            ImGui.Text($"Nose.Apply={design.Customize.Nose.Apply} Value={design.Customize.Nose.Value}");
            ImGui.Text($"Eyebrows.Apply={design.Customize.Eyebrows.Apply} Value={design.Customize.Eyebrows.Value}");
            ImGui.Text($"Lipstick.Apply={design.Customize.Lipstick.Apply} Value={design.Customize.Lipstick.Value}");
            ImGui.Text($"LipColor.Apply={design.Customize.LipColor.Apply} Value={design.Customize.LipColor.Value}");
            ImGui.Text($"BustSize.Apply={design.Customize.BustSize.Apply} Value={design.Customize.BustSize.Value}");
            ImGui.Text($"MuscleMass.Apply={design.Customize.MuscleMass.Apply} Value={design.Customize.MuscleMass.Value}");
            ImGui.Text($"TailShape.Apply={design.Customize.TailShape.Apply} Value={design.Customize.TailShape.Value}");
            ImGui.Text($"TattooColor.Apply={design.Customize.TattooColor.Apply} Value={design.Customize.TattooColor.Value}");
            ImGui.Text($"LegacyTattoo.Apply={design.Customize.LegacyTattoo.Apply} Value={design.Customize.LegacyTattoo.Value}");
            ImGui.Text($"SmallIris.Apply={design.Customize.SmallIris.Apply} Value={design.Customize.SmallIris.Value}");
            ImGui.Text($"Wetness.Apply={design.Customize.Wetness.Apply} Value={design.Customize.Wetness.Value}");
            ImGui.TreePop();
        }

        if (ImGui.TreeNode($"Parameters"))
        {
            DrawGlamourerColor("FeatureColor", design.Parameters.FeatureColor);
            DrawGlamourerColor("HairDiffuse", design.Parameters.HairDiffuse);
            DrawGlamourerColor("HairHighlight", design.Parameters.HairHighlight);
            DrawGlamourerColor("LeftEye", design.Parameters.LeftEye);
            DrawGlamourerColor("RightEye", design.Parameters.RightEye);
            DrawGlamourerColor("SkinDiffuse", design.Parameters.SkinDiffuse);
            DrawGlamourerColorAlpha("DecalColor", design.Parameters.DecalColor);
            DrawGlamourerColorAlpha("LipDiffuse", design.Parameters.LipDiffuse);
            DrawGlamourerPercentage("FacePaintUvMultiplier", design.Parameters.FacePaintUvMultiplier);
            DrawGlamourerPercentage("FacePaintUvOffset", design.Parameters.FacePaintUvOffset);
            DrawGlamourerPercentage("LeftLimbalIntensity", design.Parameters.LeftLimbalIntensity);
            DrawGlamourerPercentage("RightLimbalIntensity", design.Parameters.RightLimbalIntensity);
            DrawGlamourerPercentage("MuscleTone", design.Parameters.MuscleTone);
            ImGui.TreePop();
        }

        if (ImGui.TreeNode($"Bonus"))
        {
            ImGui.Text($"Apply: {design.Bonus.Apply}");
            ImGui.Text($"BonusId: {design.Bonus.BonusId}");
            ImGui.TreePop();
        }

        if (design.Materials != null && design.Materials.Count > 0)
        {
            if (ImGui.TreeNode($"Materials ({design.Materials.Count})"))
            {
                foreach (var mkvp in design.Materials)
                {
                    if (ImGui.TreeNode(mkvp.Key))
                    {
                        var mat = mkvp.Value;
                        ImGui.Text($"Enabled: {mat.Enabled}");
                        ImGui.Text($"Revert: {mat.Revert}");
                        ImGui.Text($"Gloss: {mat.Gloss:F5}");
                        ImGui.Text($"DiffuseRGB: ({mat.DiffuseR:F5}, {mat.DiffuseG:F5}, {mat.DiffuseB:F5})");
                        ImGui.Text($"EmissiveRGB: ({mat.EmissiveR:F5}, {mat.EmissiveG:F5}, {mat.EmissiveB:F5})");
                        ImGui.Text($"SpecularRGBA: ({mat.SpecularR:F5}, {mat.SpecularG:F5}, {mat.SpecularB:F5}, {mat.SpecularA:F5})");
                        ImGui.TreePop();
                    }
                }
                ImGui.TreePop();
            }
        }
    }

    private void DrawGlamourerEquipmentTree(GlamourerEquipment equip)
    {
        if (equip == null)
        {
            ImGui.Text("(null equipment)");
            return;
        }

        DrawGlamourerItemInfoSlot("MainHand", equip.MainHand);
        DrawGlamourerItemInfoSlot("OffHand", equip.OffHand);
        DrawGlamourerItemInfoSlot("Head", equip.Head);
        DrawGlamourerItemInfoSlot("Body", equip.Body);
        DrawGlamourerItemInfoSlot("Hands", equip.Hands);
        DrawGlamourerItemInfoSlot("Legs", equip.Legs);
        DrawGlamourerItemInfoSlot("Feet", equip.Feet);
        DrawGlamourerItemInfoSlot("Ears", equip.Ears);
        DrawGlamourerItemInfoSlot("Neck", equip.Neck);
        DrawGlamourerItemInfoSlot("Wrists", equip.Wrists);
        DrawGlamourerItemInfoSlot("RFinger", equip.RFinger);
        DrawGlamourerItemInfoSlot("LFinger", equip.LFinger);
        DrawGlamourerShow("Hat", equip.Hat);
        DrawGlamourerShow("VieraEars", equip.VieraEars);
        DrawGlamourerShow("Weapon", equip.Weapon);
        DrawGlamourerIsToggled("Visor", equip.Visor);
    }

    private void DrawGlamourerItemInfoSlot(string label, GlamourerItem item)
    {
        if (ImGui.TreeNode($"{label}##equip_{label}"))
        {
            DrawGlamourerItemInfo(item);
            ImGui.TreePop();
        }
    }

    private void DrawGlamourerItemInfo(GlamourerItem item)
    {
        if (item == null)
        {
            ImGui.Text("(null)");
            return;
        }
        ImGui.Text($"Apply: {item.Apply}");
        ImGui.Text($"ItemId: {item.ItemId}");
        ImGui.Text($"Stain: {item.Stain}");
        ImGui.Text($"Stain2: {item.Stain2}");
        ImGui.Text($"ApplyStain: {item.ApplyStain}");
        ImGui.Text($"Crest: {item.Crest}");
        ImGui.Text($"ApplyCrest: {item.ApplyCrest}");
    }

    private void DrawGlamourerShow(string label, GlamourerShow show)
    {
        ImGui.Text($"{label}: Apply={show.Apply} Show={show.Show}");
    }

    private void DrawGlamourerIsToggled(string label, GlamourerIsToggled tog)
    {
        ImGui.Text($"{label}: Apply={tog.Apply} IsToggled={tog.IsToggled}");
    }

    private void DrawGlamourerColor(string label, GlamourerColor c)
    {
        ImGui.Text($"{label}: Apply={c.Apply} ({c.Red:F5}, {c.Green:F5}, {c.Blue:F5})");
    }

    private void DrawGlamourerColorAlpha(string label, GlamourerColorAlpha c)
    {
        ImGui.Text($"{label}: Apply={c.Apply} ({c.Red:F5}, {c.Green:F5}, {c.Blue:F5}, {c.Alpha:F5})");
    }

    private void DrawGlamourerPercentage(string label, GlamourerPercentage p)
    {
        ImGui.Text($"{label}: Apply={p.Apply} Value={p.Percentage:F5}");
    }

    private static LockKind GetWardrobeLockId(WardrobeLayer layer)
    {
        return LockKindExtensions.From(layer);
    }

    // ─── Pairs ──────────────────────────────────────────────────────────

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
                ImGui.Text($"HasWardrobePermission: {friend.HasWardrobePermission}");
                ImGui.Text($"HasGagPermission: {friend.HasGagPermission}");
                ImGui.Text($"HasGarblerPermission: {friend.HasGarblerPermission}");
                ImGui.Text($"HasMoodlePermission: {friend.HasMoodlePermission}");

                if (ImGui.TreeNode("Permissions Granted To Friend"))
                {
                    DrawUserPermissions(friend.PermissionsGrantedToFriend);
                    ImGui.TreePop();
                }

                if (ImGui.TreeNode("Permissions Granted By Friend"))
                {
                    DrawUserPermissions(friend.PermissionsGrantedByFriend);
                    ImGui.TreePop();
                }

                DrawInteractionState(friend);
                ImGui.TreePop();
            }
        }
    }

    private void DrawUserPermissions(UserPermissions perms)
    {
        if (perms == null)
        {
            ImGui.Text("(null)");
            return;
        }
        ImGui.Text($"Id: {perms.Id}");
        ImGui.Text($"PairId: {perms.PairId}");
        ImGui.Text($"PairUid: {perms.PairUid ?? "(none)"}");
        ImGui.Text($"Priority: {perms.Priority}");
        ImGui.Text($"Expires: {perms.Expires?.ToString("O") ?? "(none)"}");
        ImGui.Text($"ControlsPerm: {perms.ControlsPerm}");
        ImGui.Text($"ControlsConfig: {perms.ControlsConfig}");
        ImGui.Text($"DisableSafeword: {perms.DisableSafeword}");
        ImGui.Text($"Perms: {perms.Perms}");
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

            if (ImGui.TreeNode($"Layers ({state.Layers.Count})"))
            {
                if (state.Layers.Count == 0)
                {
                    ImGui.TextUnformatted("(none)");
                }
                else
                {
                    foreach (var kvp in state.Layers)
                    {
                        var layerItem = kvp.Value;
                        var lockIcon = layerItem.LockId.HasValue ? "\U0001f512" : "";
                        if (ImGui.TreeNode($"{kvp.Key}{lockIcon}##pair_{friend.FriendCode}_{kvp.Key}"))
                        {
                            ImGui.Text($"Id: {layerItem.Id}");
                            ImGui.Text($"Name: {layerItem.Name}");
                            ImGui.Text($"Description: {layerItem.Description}");
                            ImGui.Text($"Layer: {layerItem.Layer}");
                            ImGui.Text($"Priority: {layerItem.Priority}");

                            if (layerItem.LockId.HasValue)
                            {
                                if (ImGui.TreeNode("Lock"))
                                {
                                    var li = layerItem.LockId.Value;
                                    ImGui.Text($"LockID: {li.LockID}");
                                    ImGui.Text($"LockeeID: {li.LockeeID}");
                                    ImGui.Text($"LockerID: {li.LockerID}");
                                    ImGui.Text($"LockPriority: {li.LockPriority}");
                                    ImGui.Text($"CanSelfUnlock: {li.CanSelfUnlock}");
                                    ImGui.Text($"Expires: {li.Expires?.ToString("O") ?? "(none)"}");
                                    ImGui.Text($"Password: {li.Password ?? "(none)"}");
                                    ImGui.TreePop();
                                }
                            }
                            else
                            {
                                ImGui.Text("LockId: (none)");
                            }
                            ImGui.TreePop();
                        }
                    }
                }
                ImGui.TreePop();
            }

            ImGui.TreePop();
        }
    }
}
