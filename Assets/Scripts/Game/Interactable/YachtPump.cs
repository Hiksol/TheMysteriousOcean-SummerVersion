using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class YachtPump : NetworkBehaviour, IInteractable
{
    public Battery battery;
    public float energyConsumptionPerSecond = 5;
    public float sinkingProgressDecreasePerSecond = 5;

    [Header("Debug")]
    public bool pumpEnabled = false;

    void Update() {
        if (!isServer) return;
        if (battery && battery.TryConsumeCharge(energyConsumptionPerSecond * Time.deltaTime)) {
            YachtManager.I.AddSinkingProgress(-sinkingProgressDecreasePerSecond * Time.deltaTime);
        }
    }

    [Server]
    public void Interact(Player player, ItemInstance item) {
        pumpEnabled = !pumpEnabled;
    }
}
