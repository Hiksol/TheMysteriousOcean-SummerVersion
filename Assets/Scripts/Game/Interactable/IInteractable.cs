using Mirror;

public interface IInteractable
{
    [Command]
    sealed public void CmdInteract(Player player) {
        ItemInstance item = player.Inventory.GetItemInRightHand();
        Interact(player, item);
    }

    [Server]
    public void Interact(Player player, ItemInstance item) {
        if (item == null) return;
        item.Use(player, (NetworkBehaviour)this);
    }
}
