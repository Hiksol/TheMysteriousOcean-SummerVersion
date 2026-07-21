using System;
using Mirror;

[Serializable]
public class GetInventoryContainersState : TutorialState
{
    public int containersToHave;

    public override bool IsComplete() {
        return NetworkClient.localPlayer && NetworkClient.localPlayer.GetComponent<Player>().Inventory.inventoryContainers.Count >= containersToHave;
    }
}
