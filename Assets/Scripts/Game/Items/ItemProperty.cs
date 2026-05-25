using System;

[Serializable]
public abstract class ItemProperty
{
    public virtual void OnUse(ItemInstance item, Player player) {}
}