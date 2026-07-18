using System;
using System.Linq;
using Mirror;
using UnityEngine;

[Serializable]
public class GetItemInInventoryState : TutorialState
{
    public ItemData itemDataToGet;
    public Transform pointerTarget;

    public override void OnEnter() {
        if (pointerTarget != null) TutorialPointer.I.PointTo(pointerTarget);
    }

    public override void OnExit() {
        TutorialPointer.I.Hide();
    }

    public override bool IsComplete() {
        return NetworkClient.localPlayer && NetworkClient.localPlayer.GetComponent<Player>().Inventory.GetAllItems().Any(item => item.itemData == itemDataToGet);
    }
}
