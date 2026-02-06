using System.Collections.Generic;
using System.Threading.Tasks;
using KinkLinkClient.Domain.Dependencies.Glamourer;
using KinkLinkClient.Domain.Dependencies.Glamourer.Components;

public enum Slots {
    Head, Chest, Hands, Feet, Legs, Earring, Necklace, Wrists, RingR, RingL, Bonus
}
public enum LayerLocks {
    InnerGag, Gag, OuterGag, Blindfold, Collar,
    Head, Chest, Hands, Feet, Legs, Earring, Necklace, Wrists, RingR, RingL,
    Bonus,
    SetLayer
}


public class RestraintInfo {
    public GlamourerItem? Item;
    // public ModSettings
    public Dictionary<string, GlamourerMaterial> Materials = [];
    // Need to add in the specific mods associated with the restraint for penumbra
}

public class WardrobeService {
    public RestraintInfo? InnerGag, Gag, OuterGag, Blindfold, Collar;
    // Individual layers
    public RestraintInfo? Head, Chest, Hands, Feet, Legs, Earring, Necklace, Wrists, RingR, RingL, Bonus;
    public GlamourerBonus? BonusItem;
    public GlamourerDesign? SetLayer;


    public async Task SetRestraint(Slots slot, RestraintInfo restraint) {
        // TODO: Implement this, 
    }

    public async Task SetBonusItem(GlamourerBonus restraint) {
        // TODO: Implement the set item
    }

    public async Task SetDesign(GlamourerDesign restraint) {
        // TODO: Implement the set item
    }

    public async Task ClearRestraint(Slots slot){
        // TODO: Implement the set item
    }

    public async Task ClearBonusItem() {
        // TODO: Implement the set item
    }

    public async Task ClearDesign() {
        // TODO: Implement the set item
    }
}
