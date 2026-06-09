using Mirror;
using UnityEngine;

[RequireComponent(typeof(PlayerController))]
[RequireComponent(typeof(Inventory))]
public class Player : NetworkBehaviour
{
    public float maxSaturation = 10f;
    [SyncVar] public float currentSaturation = 10f;
    public float saturationConsumtionPerSecond = 0.1f;

    public float Hunger => maxSaturation - currentSaturation;

    public PlayerController PlayerController { get; private set; }
    public Inventory Inventory { get; private set; }

    void Awake() {
        PlayerController = GetComponent<PlayerController>();
        Inventory = GetComponent<Inventory>();
    }

    void Update() {
        if (!isLocalPlayer) return;
        AddSaturation(-saturationConsumtionPerSecond * Time.deltaTime);
    }

    public void AddSaturation(float saturation) {
        currentSaturation = Mathf.Clamp(currentSaturation + saturation, 0f, maxSaturation);
    }

    [Server]
    public void Die() {
        KinematicCharacterController.KinematicCharacterMotorState state = PlayerController.CharacterMotor.GetState();
        state.Position = YachtManager.I.transform.position + Vector3.up * 3f;
        state.BaseVelocity = Vector3.zero;
        PlayerController.CharacterMotor.ApplyState(state);
    }
}
