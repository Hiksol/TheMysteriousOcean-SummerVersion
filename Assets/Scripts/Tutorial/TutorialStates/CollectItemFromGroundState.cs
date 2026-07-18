using System;
using System.Linq;
using UnityEngine;

[Serializable]
public class CollectItemFromGroundState : TutorialState
{
    public ItemInstance itemToCollect;
    public Transform pointToCheck;
    public float radiusToCheck = 3f;

    public override void OnEnter() {
        if (itemToCollect != null) TutorialPointer.I.PointTo(itemToCollect.transform);
    }

    public override void OnExit() {
        TutorialPointer.I.Hide();
    }

    public override void OnUpdate() {
        if (itemToCollect == null) {
            Collider[] colliders = new Collider[8];
            Physics.OverlapSphereNonAlloc(pointToCheck.position, radiusToCheck, colliders);
            Collider col = colliders.FirstOrDefault(col => col.TryGetComponent(out ItemInstance _));
            if (col != null) itemToCollect = col.GetComponent<ItemInstance>();
            TutorialPointer.I.PointTo(itemToCollect.transform);
        }
    }

    public override bool IsComplete() {
        return itemToCollect != null && itemToCollect.owner != null;
    }
}
