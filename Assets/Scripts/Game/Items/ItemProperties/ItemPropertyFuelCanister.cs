using System;
using Mirror;
using UnityEngine;

[Serializable]
public class ItemPropertyFuelCanister : ItemProperty
{
    public float maxFuel = 50;
    public float currentFuel = 50;

    [Server]
    public override void OnUse(ItemInstance item, Player player, IInteractable interactable) {
        Generator generator = (Generator)interactable;
        if (generator) {
            float fuelToAdd = Mathf.Min(generator.GetFuelMissing(), currentFuel);
            currentFuel -= fuelToAdd;
            generator.AddFuel(fuelToAdd);
        }
    }
}