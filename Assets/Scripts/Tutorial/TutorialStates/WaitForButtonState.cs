using System;
using Mirror;
using UnityEngine;
using UnityEngine.InputSystem;

[Serializable]
public class WaitForButtonState : TutorialState
{
    public InputActionReference inputActionRef;
    public Transform pointerTarget;

    public override void OnEnter() {
        if (pointerTarget != null) TutorialPointer.I.PointTo(pointerTarget);
        TutorialManager.I.SetAdditionalText(inputActionRef.action.GetBindingDisplayString(
            InputBinding.MaskByGroup("Keyboard&Mouse"),
            InputBinding.DisplayStringOptions.DontUseShortDisplayNames
        ));
    }

    public override void OnExit() {
        TutorialPointer.I.Hide();
        TutorialManager.I.SetAdditionalText(null);
    }

    public override bool IsComplete() {
        return NetworkClient.localPlayer && NetworkClient.localPlayer.GetComponent<Player>().playerState == PlayerState.Default && inputActionRef.action.WasPressedThisFrame();
    }
}