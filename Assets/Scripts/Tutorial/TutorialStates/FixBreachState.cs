using System;

[Serializable]
public class FixBreachState : TutorialState
{
    public YachtBreach breachToFix;

    public override void OnEnter() {
        TutorialPointer.I.PointTo(breachToFix.transform);
    }

    public override void OnExit() {
        TutorialPointer.I.Hide();
    }

    public override bool IsComplete() {
        return breachToFix != null && !breachToFix.sinkingActive;
    }
}