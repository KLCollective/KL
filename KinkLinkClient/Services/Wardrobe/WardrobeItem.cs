using System;
using System.Collections.Generic;
using KinkLinkCommon.Dependencies.Glamourer;
using KinkLinkCommon.Dependencies.Glamourer.Components;
using KinkLinkCommon.Domain.Enums;

namespace KinkLinkClient.Services;

public record WardrobeItem
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public GlamourerEquipmentSlot Slot { get; set; }
    public GlamourerItem? Item { get; set; }
    public List<GlamourerMod> Mods { get; set; } = [];
    public Dictionary<string, GlamourerMaterial> Materials { get; set; } = [];
    public RelationshipPriority Priority { get; set; } = RelationshipPriority.Casual;
}
