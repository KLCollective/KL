using System;
using KinkLinkCommon.Dependencies.Glamourer;
using KinkLinkCommon.Domain.Enums;

namespace KinkLinkClient.Services;

public record WardrobeSet
{
    public Guid Id => Design.Identifier;
    public string Name => Design.Name;
    public string Description => Design.Description;
    public required GlamourerDesign Design { get; set; }
    public RelationshipPriority Priority { get; set; } = RelationshipPriority.Casual;
}
