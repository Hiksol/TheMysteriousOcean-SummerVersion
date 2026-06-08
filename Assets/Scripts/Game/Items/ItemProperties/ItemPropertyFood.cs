using System;
using Mirror;

[Serializable]
public class ItemPropertyFood : ItemProperty
{
    public float saturation = 1f;

    [Server]
    public override void OnUse(ItemInstance item, Player player, Interactable _) {
        player.AddSaturation(saturation);
        player.Inventory.DestroyItemInRightHand();
    }
}