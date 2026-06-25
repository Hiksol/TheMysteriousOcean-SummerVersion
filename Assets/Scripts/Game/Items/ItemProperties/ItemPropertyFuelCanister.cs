using System;
using Mirror;
using UnityEngine;

[Serializable]
public class ItemPropertyFuelCanister : ItemProperty
{
    public float maxFuel = 50;
    public float currentFuel = 50;
    public float minInitFuel = 20, maxInitFuel = 50;

    [Server]
    public override void OnUse(ItemInstance item, Player player, Interactable interactable)
    {
        if (interactable is not Generator) return;
        Generator generator = (Generator)interactable;
        float fuelToAdd = Mathf.Min(generator.GetFuelMissing(), currentFuel);
        currentFuel -= fuelToAdd;
        generator.AddFuel(fuelToAdd);
    }

    [Server]
    public override void OnStart(ItemInstance item) {
        currentFuel = GameManager.I.Rng.Range(minInitFuel, maxInitFuel);
    }
}