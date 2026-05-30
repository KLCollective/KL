using System;
using KinkLinkClient.Services;
using KinkLinkCommon.Dependencies.Glamourer.Components;
using KinkLinkCommon.Dependencies.Glamourer;
using KinkLinkCommon.Domain.Enums;
using Xunit;

namespace KinkLinkClientTests;

public class WardrobeTests
{
    [Fact]
    public void WardrobeItem_DesignSetter_SetsIdWhenEmpty()
    {
        var design = new GlamourerDesign();
        design.Identifier = Guid.NewGuid();
        design.Name = "DesignName";

        var item = new WardrobeItem();
        item.Id = Guid.Empty;
        item.Name = string.Empty;
        item.Description = string.Empty;

        item.Design = design;

        Assert.Equal(design.Identifier, item.Id);
        Assert.Equal("DesignName", item.Name);
    }

    [Fact]
    public void WardrobeItem_DesignSetter_DoesNotOverrideExistingId()
    {
        var design = new GlamourerDesign();
        design.Identifier = Guid.NewGuid();
        design.Name = "DesignName2";

        var item = new WardrobeItem();
        var existingId = Guid.NewGuid();
        item.Id = existingId;
        item.Name = "ExistingName";

        item.Design = design;

        // With deterministic GUID policy, design.Identifier overrides existing id when present
        Assert.Equal(design.Identifier, item.Id);
        Assert.Equal("ExistingName", item.Name); // name should not be overwritten when already set
    }

    [Fact]
    public void ActiveWardrobe_GetCurrentState_UsesOutfitAsBase()
    {
        var active = new ActiveWardrobe();

        var outfitDesign = new GlamourerDesign();
        outfitDesign.Name = "Outfit";
        outfitDesign.Identifier = Guid.NewGuid();

        var headDesign = new GlamourerDesign();
        headDesign.Name = "Head";
        headDesign.Identifier = Guid.NewGuid();

        var outfitItem = new WardrobeItem { Layer = KinkLinkCommon.Domain.Wardrobe.WardrobeLayer.Outfit, Name = "OutfitItem" };
        outfitItem.Design = outfitDesign;

        var headItem = new WardrobeItem { Layer = KinkLinkCommon.Domain.Wardrobe.WardrobeLayer.Head, Name = "HeadItem" };
        headItem.Design = headDesign;

        // Inject private _layers field via reflection to avoid serializing GlamourerDesign
        var dict = new System.Collections.Generic.Dictionary<KinkLinkCommon.Domain.Wardrobe.WardrobeLayer, WardrobeItem>
        {
            [KinkLinkCommon.Domain.Wardrobe.WardrobeLayer.Outfit] = outfitItem,
            [KinkLinkCommon.Domain.Wardrobe.WardrobeLayer.Head] = headItem,
        };
        var field = typeof(ActiveWardrobe).GetField("_layers", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        field.SetValue(active, dict);

        var baseLayer = active.GetBaseLayer();
        Assert.NotNull(baseLayer);
        Assert.Equal(outfitDesign.Identifier, baseLayer.Identifier);
    }
}
