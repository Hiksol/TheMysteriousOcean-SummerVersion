using System;
using Mirror;

[Serializable]
public class ItemPropertyChangeStaminaUseMult : ItemProperty
{
    public float staminaUseMult = 0.5f;
    public float remainingTime = 5f;

    [Server]
    public override void OnUse(ItemInstance item, Player player, Interactable interactable) {
        player.PlayerController.staminaUseMults.Add(new() {
            staminaUseMult = staminaUseMult,
            remainingTime = remainingTime
        });
        player.Inventory.DestroyItemInRightHand();
    }
}

public struct StaminaUseMult {
    public float staminaUseMult;
    public float remainingTime;

    public bool OnUndate(float deltaTime) {
        remainingTime -= deltaTime;
        return remainingTime <= 0;
    }
}