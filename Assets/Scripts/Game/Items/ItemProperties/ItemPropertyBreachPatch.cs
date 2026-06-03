using System;
using Mirror;

[Serializable]
public class ItemPropertyBreachPatch : ItemProperty
{
    [Server]
    public override void OnUse(ItemInstance item, Player player, Interactable interactable) {
        YachtBreach breach = (YachtBreach)interactable;
        if (breach) {
            breach.Patch();
            player.Inventory.DestroyItemInRightHand();
        }
    }
}