using System;
using Mirror;
using UnityEngine;

[Serializable]
public class ItemPropertyEquipableContainer : ItemProperty
{
    public EquipableContainerType containerType = EquipableContainerType.Custom;
    public int capacity = 1;

    [Server]
    public override void OnUse(ItemInstance item, Player player, Interactable interactable)
    {
        if (item == null || player == null) return;
        if (!player.TryGetComponent(out Inventory inventory)) return;

        inventory.ServerEquipContainer(item);
    }
}