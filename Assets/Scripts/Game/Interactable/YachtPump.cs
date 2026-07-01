using Mirror;
using UnityEngine;

public class YachtPump : InteractableActive
{
    public Battery battery;
    public float energyConsumptionPerSecond = 5;
    public float sinkingProgressDecreasePerSecond = 5;

    float EnergyConsumptionDt => energyConsumptionPerSecond * Time.deltaTime;

    public override bool IsInteractableShouldWork() {
        return battery && battery.currentCharge >= EnergyConsumptionDt;
    }

    [Server]
    protected override void UpdateNewServer(bool isInteractableWorking) {
        if (isInteractableActive && isInteractableWorking) {
            battery.TryConsumeCharge(EnergyConsumptionDt);
            YachtManager.I.AddSinkingProgress(-sinkingProgressDecreasePerSecond * Time.deltaTime);
        }
    }
}
