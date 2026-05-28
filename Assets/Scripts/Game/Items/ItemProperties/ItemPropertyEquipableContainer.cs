using System;
using Mirror;

[Serializable]
public class ItemPropertyEquipableContainer : ItemProperty
{
    public int capacity = 1;

    [Server]
    public override void OnUse(ItemInstance item, Player player, IInteractable interactable) {
        player.Inventory.CmdEquipContainer(item);
    }
}