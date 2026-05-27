using Mirror;

public interface IInteractable
{
    [Command]
    public void CmdInteract(Player player) {
        Interact(player);
    }
    public void Interact(Player player);
}
