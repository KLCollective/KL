using MessagePack;

namespace KinkLinkCommon.Dependencies.Glamourer.Components;

[MessagePackObject]
public class GlamourerShow
{
    [Key(0)]
    public bool Apply;

    [Key(1)]
    public bool Show;

    public GlamourerShow Clone() => (GlamourerShow)MemberwiseClone();

    public override string ToString()
    {
        return $"Show: {Show}, Apply: {Apply}, Show: {Show}";
    }

    public bool IsEqualTo(GlamourerShow other)
    {
        if (Apply != other.Apply) return false;
        if (Show != other.Show) return false;
        return true;
    }
}
