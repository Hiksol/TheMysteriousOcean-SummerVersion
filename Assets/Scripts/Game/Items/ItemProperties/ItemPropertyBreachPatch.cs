using System;
using Mirror;

[Serializable]
public class ItemPropertyBreachPatch : ItemProperty
{
    public float maxFuel = 50;
    public float currentFuel = 50;

    [Server]
    public override void OnUse(ItemInstance item, Player player, IInteractable interactable) {
        YachtBreach breach = (YachtBreach)interactable;
        if (breach) {
            breach.Patch();
            player.Inventory.DestroyItemInRightHand();
        }
    }
}