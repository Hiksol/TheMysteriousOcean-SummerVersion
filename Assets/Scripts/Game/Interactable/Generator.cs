using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class Generator : InteractableActive
{
    public float maxFuel = 100f;
    public List<ItemFuelType> acceptableFuels = new() { ItemFuelType.Fuel };
    public Battery battery;
    public float fuelConsumptionPerSecond = 1;
    public float energyGenerationPerSecond = 1;

    [Header("Debug")]
    [SyncVar] public float currentFuel = 0;

    protected override bool IsAlwaysActive => true;

    public override bool IsInteractableShouldWork() {
        return battery && currentFuel > 0;
    }

    protected override void UpdateNewServer(bool isInteractableWorking) {
        if (isInteractableWorking) {
            currentFuel = Mathf.Max(currentFuel - Time.deltaTime * fuelConsumptionPerSecond, 0);
            battery.AddCharge(energyGenerationPerSecond * Time.deltaTime);
        }
    }

    [Server]
    override public void Interact(Player player, ItemInstance item) {
        Inventory inventory = player.Inventory;
        inventory.OpenInventoryWithGenerator(this);
    }

    [Server]
    public void AddFuel(float fuel) {
        currentFuel = Mathf.Min(currentFuel + fuel, maxFuel);
    }

    public float GetFuelMissing() {
        return maxFuel - currentFuel;
    }
}
