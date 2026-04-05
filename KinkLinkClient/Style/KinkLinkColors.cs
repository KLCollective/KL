using System.Numerics;
using Dalamud.Bindings.ImGui;
using KinkLinkCommon.Domain.Enums;

namespace KinkLinkClient.Style;

public static class KinkLinkColors
{
    public static readonly uint PrimaryColor = ImGui.ColorConvertFloat4ToU32(new Vector4(0.9372f, 0.2862f, 0.3451f, 0.75f));
    public static readonly uint PrimaryColorAccent = ImGui.ColorConvertFloat4ToU32(new Vector4(0.9372f, 0.2862f, 0.3451f, 0.9f));
    public static readonly uint PanelColor = ImGui.ColorConvertFloat4ToU32(new Vector4(0.1294f, 0.1333f, 0.1764f, 1));
    public static readonly uint BackgroundColor = ImGui.ColorConvertFloat4ToU32(new Vector4(0.0431f, 0.0549f, 0.0588f, 0.95f));

    public static readonly uint CasualColor = ImGui.ColorConvertFloat4ToU32(new Vector4(0.478f, 0.541f, 0.600f, 1.0f));
    public static readonly uint SeriousColor = ImGui.ColorConvertFloat4ToU32(new Vector4(0.898f, 0.647f, 0.039f, 1.0f));
    public static readonly uint DevotionalColor = ImGui.ColorConvertFloat4ToU32(new Vector4(0.9372f, 0.2862f, 0.3451f, 1.0f));

    public static uint GetPriorityColor(RelationshipPriority priority) => priority switch
    {
        RelationshipPriority.Casual => CasualColor,
        RelationshipPriority.Serious => SeriousColor,
        RelationshipPriority.Devotional => DevotionalColor,
        _ => CasualColor,
    };
}
