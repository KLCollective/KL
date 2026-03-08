// Helper strutures
using System;
using System.Collections.Generic;

namespace KinkLinkClient.Dependencies.Penumbra.Services;

// Source: github.com/ottermandias/Glamourer/Glamourer/Interop/Penumbra/PenumbraService.cs
// Used as an intermediate format
public readonly record struct Mod(string Name, string DirectoryName) : IComparable<Mod>
{
    public int CompareTo(Mod other)
    {
        var nameComparison = string.Compare(Name, other.Name, StringComparison.Ordinal);
        if (nameComparison != 0)
            return nameComparison;

        return string.Compare(DirectoryName, other.DirectoryName, StringComparison.Ordinal);
    }
}

public readonly record struct ModSettings(
    Dictionary<string, List<string>> Settings,
    int Priority,
    bool Enabled,
    bool ForceInherit = false,
    bool Remove = false
)
{
    public ModSettings()
        : this(new Dictionary<string, List<string>>(), 0, false, false, false) { }

    public static ModSettings Empty => new();
}
