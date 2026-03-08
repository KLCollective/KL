using KinkLinkCommon.Domain.Enums;
using MessagePack;

namespace KinkLinkCommon.Domain.Network;

[MessagePackObject]
public record ActionResult<T>(
    [property: Key(0)] ActionResultEc Result,
    [property: Key(1)] T? Value
)
{
    public ActionResult() : this(ActionResultEc.Unknown, default) { }
}
