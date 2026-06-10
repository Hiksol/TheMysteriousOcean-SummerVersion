using System;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class CraftingManager : SingletonNetworkBehaviour<CraftingManager>
{
    public CraftingRecipeRenderer craftingRecipeRendererPrefab;
    public Transform craftingRendererRoot;
    public Transform craftingRecipesLayoutRoot;

    readonly List<CraftingRecipeRenderer> craftingRecipeRenderers = new();
    List<CraftingRecipe> craftingRecipes = new();

    [Header("Debug")]
    public bool isVisible = false;
    public bool wasCursorVisible = false;

    protected override void AwakeNew() {
        craftingRendererRoot.gameObject.SetActive(false);
    }

    [Client]
    public void ShowCraftingRecipes(List<CraftingRecipe> craftingRecipes) {
        isVisible = true;
        this.craftingRecipes = craftingRecipes;
        UpdateVisibility();
        wasCursorVisible = Cursor.visible;
        Cursor.visible = !Cursor.visible;
        Cursor.lockState = Cursor.visible ? CursorLockMode.None : CursorLockMode.Locked;
    }

    [Client]
    public void Hide() {
        isVisible = false;
        craftingRecipes.Clear();
        UpdateVisibility();
        Cursor.visible = wasCursorVisible;
        wasCursorVisible = false;
        Cursor.lockState = Cursor.visible ? CursorLockMode.None : CursorLockMode.Locked;
    }

    [Client]
    void UpdateVisibility() {
        craftingRendererRoot.gameObject.SetActive(false);
        craftingRecipeRenderers.ForEach(crr => Destroy(crr));
        craftingRecipeRenderers.Clear();
        if (isVisible) {
            craftingRecipes.ForEach(craftingRecipe => {
                CraftingRecipeRenderer renderer = Instantiate(craftingRecipeRendererPrefab, craftingRecipesLayoutRoot);
                renderer.SetCraftingRecipe(craftingRecipe, NetworkClient.localPlayer.GetComponent<Player>());
            });
            craftingRendererRoot.gameObject.SetActive(true);
        }
    }
}

[Serializable]
public class CraftingRecipe {
    public List<ItemData> ingredients;
    public ItemData result;
}
