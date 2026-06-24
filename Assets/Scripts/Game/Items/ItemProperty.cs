using System;
using Mirror;

[Serializable]
public abstract class ItemProperty : ICloneable
{
    [Server]
    public virtual void OnUse(ItemInstance item, Player player, Interactable interactable) {}

    [Server]
    public virtual void OnStart(ItemInstance item) {}

    virtual public object Clone() { return MemberwiseClone(); }
}