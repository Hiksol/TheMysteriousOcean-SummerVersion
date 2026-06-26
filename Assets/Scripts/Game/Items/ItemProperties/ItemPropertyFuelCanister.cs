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
        TryTransferFuelToGenerator(generator, currentFuel);
    }

    [Server]
    public override void OnStart(ItemInstance item) {
        currentFuel = GameManager.I.Rng.Range(minInitFuel, maxInitFuel);
    }

    [Server]
    public void TryTransferFuelToGenerator(Generator generator, float fuelAmount) {
        float fuelToAdd = Mathf.Min(generator.GetFuelMissing(), currentFuel, fuelAmount);
        currentFuel -= fuelToAdd;
        generator.AddFuel(fuelToAdd);
    }

    [Command]
    public void CmdTryTransferFuelToGenerator(Generator generator, float fuelAmount) {
        TryTransferFuelToGenerator(generator, fuelAmount);
    }
}