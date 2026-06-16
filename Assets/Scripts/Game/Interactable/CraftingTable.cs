using System.Collections.Generic;
using Mirror;

public class CraftingTable : Interactable
{
    public List<CraftingRecipe> craftingRecipes;

    [Server]
    override public void Interact(Player player, ItemInstance item) {
        RpcShowCraftingRecipes(player.connectionToClient);
    }

    [TargetRpc]
    void RpcShowCraftingRecipes(NetworkConnectionToClient _) {
        CraftingManager.I.ShowCraftingRecipes(craftingRecipes);
    }
}
