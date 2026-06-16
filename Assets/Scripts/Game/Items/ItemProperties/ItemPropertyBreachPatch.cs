using System;
using Mirror;

[Serializable]
public class ItemPropertyBreachPatch : ItemProperty
{
    [Server]
    public override void OnUse(ItemInstance item, Player player, Interactable interactable) {
        if (interactable is not YachtBreach) return;
        YachtBreach breach = (YachtBreach)interactable;
        breach.Patch();
        player.Inventory.DestroyItemInRightHand();
    }
}