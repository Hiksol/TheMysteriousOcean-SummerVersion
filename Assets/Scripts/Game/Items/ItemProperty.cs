using System;
using Mirror;

[Serializable]
public abstract class ItemProperty
{
    [Server]
    public virtual void OnUse(ItemInstance item, Player player, Interactable interactable) {}
}