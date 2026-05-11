using System;
using System.Collections.Generic;
using KinkLinkCommon.Dependencies.Glamourer;
using KinkLinkCommon.Dependencies.Glamourer.Components;
using KinkLinkCommon.Domain.Enums;

namespace KinkLinkClient.Services;

public record WardrobeItem
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public GlamourerEquipmentSlot Slot { get; init; }
    public GlamourerItem? Item { get; init; }
    public List<GlamourerMod> Mods { get; init; } = [];
    public Dictionary<string, GlamourerMaterial> Materials { get; init; } = [];
    public RelationshipPriority Priority { get; init; } = RelationshipPriority.Casual;
}
