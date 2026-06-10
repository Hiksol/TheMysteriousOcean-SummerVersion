using System.Collections.Generic;
using System.Linq;
using Mirror;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CraftingRecipeRenderer : MonoBehaviour
{
    public GameObject craftingObjectRendererPrefab;
    public GameObject craftingResultRenderer;
    public Transform craftingIngredientsRoot;
    public Color hasIngredientColor = Color.green;
    public Color notHasIngredientColor = Color.softRed;

    readonly List<GameObject> craftingIngredientRenderers = new();

    [Header("Debug")]
    public CraftingRecipe craftingRecipe;

    public void SetCraftingRecipe(CraftingRecipe craftingRecipe, Player player) {
        this.craftingRecipe = craftingRecipe;
        craftingResultRenderer.GetComponentInChildren<TMP_Text>().text = craftingRecipe.result.itemName;
        craftingIngredientRenderers.ForEach(cir => Destroy(cir));
        craftingIngredientRenderers.Clear();
        Dictionary<ItemData, int> requiredIngredients = new();
        craftingRecipe.ingredients.ForEach(ci => {
            if (!requiredIngredients.ContainsKey(ci)) requiredIngredients[ci] = 0;
            requiredIngredients[ci] += 1;
            GameObject craftingIngredientRenderer = Instantiate(craftingObjectRendererPrefab, craftingIngredientsRoot);
            craftingIngredientRenderer.GetComponent<Image>().color =
                player.Inventory.GetAllItems().Count(item => item.itemData == ci) >= requiredIngredients[ci] ? hasIngredientColor : notHasIngredientColor;
            craftingIngredientRenderer.GetComponentInChildren<TMP_Text>().text = ci.itemName;
            craftingIngredientRenderers.Add(craftingIngredientRenderer);
        });
    }

    [Client]
    public void TryCraft() {
        Player player = NetworkClient.localPlayer.GetComponent<Player>();
        CraftingManager.I.CmdTryCraft(player, craftingRecipe);
    }
}
