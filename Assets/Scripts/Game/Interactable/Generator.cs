using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class Generator : Interactable
{
    public float maxFuel = 100f;
    public List<ItemFuelType> acceptableFuels = new() { ItemFuelType.Fuel };
    public Battery battery;
    public float fuelConsumptionPerSecond = 1;
    public float energyGenerationPerSecond = 1;

    [Header("Debug")]
    [SyncVar] public float currentFuel = 0;

    void Update() {
        if (!isServer) return;
        if (currentFuel > 0 && battery) {
            currentFuel = Mathf.Max(currentFuel - Time.deltaTime * fuelConsumptionPerSecond, 0);
            battery.AddCharge(energyGenerationPerSecond * Time.deltaTime);
        }
    }

    [Server]
    override public void Interact(Player player, ItemInstance item) {
        Inventory inventory = player.Inventory;
        if (item == null) return;
        if (acceptableFuels.Contains(item.itemData.itemFuelType)) {
            AddFuel(item.itemData.itemFuelAmount);
            inventory.DestroyItemInRightHand();
        } else {
            item.Use(player, this);
        }
    }

    [Server]
    public void AddFuel(float fuel) {
        currentFuel = Mathf.Min(currentFuel + fuel, maxFuel);
    }

    public float GetFuelMissing() {
        return maxFuel - currentFuel;
    }
}
