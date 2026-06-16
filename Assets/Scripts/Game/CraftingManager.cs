using System;
using System.Collections.Generic;
using System.Linq;
using Mirror;
using UnityEngine;

public class CraftingManager : SingletonNetworkBehaviour<CraftingManager>
{
    public ItemInstance itemInstancePrefab;
    public CraftingRecipeRenderer craftingRecipeRendererPrefab;
    public Transform craftingRendererRoot;
    public Transform craftingRecipesLayoutRoot;

    public List<CraftingRecipeRenderer> craftingRecipeRenderers = new();
    public List<CraftingRecipe> craftingRecipes = new();

    [Header("Debug")]
    public bool isVisible = false;
    public bool wasCursorVisible = false;
    public Interactable interactableInitiated;

    protected override void AwakeNew() {
        craftingRendererRoot.gameObject.SetActive(false);
    }

    [Client]
    public void ShowCraftingRecipes(List<CraftingRecipe> craftingRecipes, Interactable interactable = null) {
        Player player = NetworkClient.localPlayer.GetComponent<Player>();
        player.SetPlayerState(PlayerState.Interacting);
        isVisible = true;
        this.craftingRecipes.AddRange(craftingRecipes);
        UpdateVisibility(player);
        wasCursorVisible = Cursor.visible;
        Cursor.visible = !Cursor.visible;
        Cursor.lockState = Cursor.visible ? CursorLockMode.None : CursorLockMode.Locked;
        interactableInitiated = interactable;
    }

    [Client]
    public void Hide() {
        Player player = NetworkClient.localPlayer.GetComponent<Player>();
        player.SetPlayerState(PlayerState.Default);
        isVisible = false;
        craftingRecipes.Clear();
        UpdateVisibility(player);
        Cursor.visible = wasCursorVisible;
        wasCursorVisible = false;
        Cursor.lockState = Cursor.visible ? CursorLockMode.None : CursorLockMode.Locked;
        interactableInitiated = null;
    }

    [Client]
    void UpdateVisibility(Player player) {
        craftingRendererRoot.gameObject.SetActive(false);
        craftingRecipeRenderers.ForEach(crr => Destroy(crr.gameObject));
        craftingRecipeRenderers.Clear();
        if (isVisible) {
            craftingRecipes.ForEach(craftingRecipe => {
                CraftingRecipeRenderer renderer = Instantiate(craftingRecipeRendererPrefab, craftingRecipesLayoutRoot);
                renderer.SetCraftingRecipe(craftingRecipe, player);
                craftingRecipeRenderers.Add(renderer);
            });
            craftingRendererRoot.gameObject.SetActive(true);
        }
    }

    [Command(requiresAuthority = false)]
    public void CmdTryCraft(Player player, CraftingRecipe craftingRecipe) {
        if (!CanCraft(player, craftingRecipe)) return;
        IEnumerable<(ItemContainer container, ItemInstance item, int ind)> items = player.Inventory.GetAllItemsFull();
        foreach (ItemData ingredient in craftingRecipe.ingredients) {
            (ItemContainer container, ItemInstance _, int ind) = items.Where(cii => cii.item.itemData == ingredient).First();
            container.DestroyItem(ind);
        }
        Vector3 position = interactableInitiated != null ? interactableInitiated.transform.position + Vector3.up * 2f : player.transform.position + player.transform.forward;
        Quaternion rotation = interactableInitiated != null ? interactableInitiated.transform.rotation : player.transform.rotation;
        ItemInstance item = Instantiate(itemInstancePrefab, position, rotation);
        NetworkServer.Spawn(item.gameObject);
        item.SetItemData(craftingRecipe.result);
        player.Inventory.TryPickupItem(item);
    }

    bool CanCraft(Player player, CraftingRecipe craftingRecipe) {
        if (craftingRecipe == null || craftingRecipe.result == null) return false;
        IEnumerable<ItemInstance> playerItems = player.Inventory.GetAllItems();
        return craftingRecipe.ingredients.ToHashSet().All(ingredient => playerItems.Count(item => item.itemData == ingredient) >= craftingRecipe.ingredients.Count(id => id == ingredient));
    }
}

[Serializable]
public class CraftingRecipe {
    public List<ItemData> ingredients;
    public ItemData result;
}
