using System;
using System.Collections.Generic;
using KinkLinkCommon.Dependencies.Glamourer;
using KinkLinkCommon.Domain.Enums;
using KinkLinkCommon.Domain.Wardrobe;

namespace KinkLinkClient.Services;

public record WardrobeItem
{
    public Guid Id => Design.Identifier;
    public string Name => Design.Name;
    public string Description => Design.Description;
    public WardrobeLayer Layer = WardrobeLayer.BaseLayer;
    public required GlamourerDesign Design { get; set; }
    public RelationshipPriority Priority { get; set; } = RelationshipPriority.Casual;

    public List<GlamourerMod> Mods() => Design.Mods;
}
