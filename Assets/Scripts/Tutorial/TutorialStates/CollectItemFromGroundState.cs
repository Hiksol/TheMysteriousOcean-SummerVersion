using System;

[Serializable]
public class CollectItemFromGroundState : TutorialState
{
    public ItemInstance itemToCollect;

    public override void OnEnter() {
        TutorialPointer.I.PointTo(itemToCollect.transform);
    }

    public override void OnExit() {
        TutorialPointer.I.Hide();
    }

    public override bool IsComplete() {
        return itemToCollect.owner != null;
    }
}
