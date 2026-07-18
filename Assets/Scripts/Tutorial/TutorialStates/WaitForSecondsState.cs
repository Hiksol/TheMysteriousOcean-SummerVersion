using System;
using UnityEngine;

[Serializable]
public class WaitForSecondsState : TutorialState
{
    public float secondsToWait;
    public Transform pointerTarget;

    float secondsWaited = 0f;

    public override void OnEnter() {
        if (pointerTarget != null) TutorialPointer.I.PointTo(pointerTarget);
    }

    public override void OnExit() {
        TutorialPointer.I.Hide();
    }

    public override void OnUpdate() {
        secondsWaited += Time.deltaTime;
    }

    public override bool IsComplete() {
        return secondsWaited >= secondsToWait;
    }
}