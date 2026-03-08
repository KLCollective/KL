using System.Collections.Generic;
using MessagePack;

namespace KinkLinkCommon.Dependencies.Penumbra.Domain;

[MessagePackObject]
public readonly record struct Mod([property: Key(0)] string Name, [property: Key(1)] string DirectoryName) : IComparable<Mod>
{
    public int CompareTo(Mod other)
    {
        var nameComparison = string.Compare(Name, other.Name, System.StringComparison.Ordinal);
        if (nameComparison != 0)
            return nameComparison;

        return string.Compare(DirectoryName, other.DirectoryName, System.StringComparison.Ordinal);
    }
}

[MessagePackObject]
public readonly record struct ModSettings(
    [property: Key(0)] Dictionary<string, List<string>> Settings,
    [property: Key(1)] int Priority,
    [property: Key(2)] bool Enabled,
    [property: Key(3)] bool ForceInherit = false,
    [property: Key(4)] bool Remove = false
)
{
    public ModSettings()
        : this(new Dictionary<string, List<string>>(), 0, false, false, false) { }

    public static ModSettings Empty => new();
}
