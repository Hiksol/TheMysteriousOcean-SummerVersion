using System;
using Mirror;

[Serializable]
public class ItemPropertyEquipableContainer : ItemProperty
{
    public int capacity = 1;

    [Client]
    public override void OnUse(ItemInstance item, Player player) {
        player.Inventory.CmdEquipContainer(item);
    }
}