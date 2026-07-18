using System;

[Serializable]
public class StartInteractableActiveState : TutorialState
{
    public InteractableActive interactableActive;

    public override void OnEnter() {
        TutorialPointer.I.PointTo(interactableActive.transform);
    }

    public override void OnExit() {
        TutorialPointer.I.Hide();
    }

    public override bool IsComplete() {
        return interactableActive != null && interactableActive.IsInteractableWorking;
    }
}