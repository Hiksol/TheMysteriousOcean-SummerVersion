using Mirror;
using UnityEngine;

[RequireComponent(typeof(Animator))]
public class PlayerAnimations : NetworkBehaviour
{
    Player player;
    Animator animator;

    static readonly int IsMovingHash = Animator.StringToHash("IsMoving");
    static readonly int IsSwimmingHash = Animator.StringToHash("IsSwimming");

    public override void OnStartClient() {
        if (!isLocalPlayer) {
            enabled = false;
            return;
        }
        GetComponentInChildren<SkinnedMeshRenderer>().enabled = false;
        player = GetComponentInParent<Player>();
        animator = GetComponent<Animator>();
    }

    void Update() {
        animator.SetBool(IsMovingHash, player.PlayerController.MoveInput.sqrMagnitude >= 0.01f);
        animator.SetBool(IsSwimmingHash, player.PlayerController.InWater);
    }
}
