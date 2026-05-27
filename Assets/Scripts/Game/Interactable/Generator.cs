using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class Generator : NetworkBehaviour, IInteractable
{
    public float baseMaxCharge = 10f;
    public List<ItemFuelType> acceptableFuels = new() { ItemFuelType.Fuel };

    [Header("Debug")]
    public float currentCharge;

    void Awake() {
        currentCharge = baseMaxCharge;
    }

    public void Interact(Player player) {
        Inventory inventory = player.Inventory;
        ItemInstance item = inventory.GetItemInRightHand();
        if (item != null && acceptableFuels.Contains(item.itemData.itemFuelType)) {
            AddCharge(item.itemData.itemFuelAmount);
            inventory.DestroyItemInRightHand();
        }
    }

    void AddCharge(float charge) {
        currentCharge = Mathf.Min(currentCharge + charge, EvaluateMaxCharge());
    }

    float EvaluateMaxCharge() {
        // TODO: implement battaries
        return baseMaxCharge;
    }
}
