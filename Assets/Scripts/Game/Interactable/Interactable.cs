using Mirror;

public class Interactable : NetworkBehaviour
{
    [SyncVar] public Player owner = null;

    [Command(requiresAuthority = false)]
    public void CmdInteract(Player player) {
        ItemInstance item = player.Inventory.GetItemInRightHand();
        Interact(player, item);
    }

    [Server]
    public virtual void Interact(Player player, ItemInstance item) {
        if (item == null) return;
        item.Use(player, this);
    }
}
