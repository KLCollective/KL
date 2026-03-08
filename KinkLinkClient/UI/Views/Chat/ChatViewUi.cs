using System.Numerics;
using Dalamud.Bindings.ImGui;
using KinkLinkClient.Domain.Interfaces;
using KinkLinkClient.Utils;

// ReSharper disable RedundantBoolCompare

namespace KinkLinkClient.UI.Views.Chat;

public class ChatViewUi(ChatViewUiController controller) : IDrawable
{
    // Const
    private const int SendChatButtonHeight = 24;

    public void Draw()
    {
        ImGui.BeginChild("ChatContent", Vector2.Zero, false, KinkLinkStyle.ContentFlags);

        var width = ImGui.GetWindowWidth();
        var padding = ImGui.GetStyle().WindowPadding;

        var begin = ImGui.GetCursorPosY();

        SharedUserInterfaces.ContentBox(
            "Global Chat",
            KinkLinkStyle.PanelBackground,
            true,
            () =>
            {
                SharedUserInterfaces.BigTextCentered("Anonymous Global Chat");
            }
        );

        var headerHeight = ImGui.GetCursorPosY() - begin;
        var chatContextBoxSize = new Vector2(
            0,
            ImGui.GetWindowHeight() - headerHeight - padding.X * 3 - SendChatButtonHeight
        );
        // Implement Chat scroll box
        if (
            ImGui.BeginChild(
                "##ChatContextBoxDisplay",
                chatContextBoxSize,
                true,
                ImGuiWindowFlags.AlwaysVerticalScrollbar
            )
        )
        {
            ImGui.Spacing();
            foreach (var message in controller.Messages())
            {
                ImGui.TextColored(new Vector4(0.7f, 0.8f, 1.0f, 1.0f), $"[{message.Alias}]");
                ImGui.SameLine();
                ImGui.Text(message.Timestamp.ToShortTimeString());
                // Render sender name in blue

                // Render message text
                ImGui.TextWrapped(message.Message);
            }

            // Auto-scroll to bottom if new messages were added
            if (controller.ScrollToBottom)
            {
                ImGui.SetScrollHereY(1.0f);
                controller.ScrollToBottom = false;
            }
            ImGui.EndChild();
        }

        ImGui.Spacing();
        SharedUserInterfaces.ContentBox(
            "ChatSend",
            KinkLinkStyle.PanelBackground,
            false,
            () =>
            {
                ImGui.AlignTextToFramePadding();
                // Input field and send button
                ImGui.SetNextItemWidth(-50); // Leave room for send button
                if (
                    ImGui.InputTextWithHint(
                        "",
                        "Type message...",
                        ref controller.InputMessage,
                        500,
                        ImGuiInputTextFlags.EnterReturnsTrue
                    )
                )
                {
                    ImGui.SetKeyboardFocusHere(-1);
                    controller.SendChat();
                }

                ImGui.SameLine();
                if (ImGui.Button("Send"))
                {
                    ImGui.SetKeyboardFocusHere(-1);
                    controller.SendChat();
                }
            }
        );
        ImGui.EndChild();
    }
}
