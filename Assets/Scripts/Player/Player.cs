using Mirror;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(PlayerController))]
[RequireComponent(typeof(Inventory))]
public class Player : NetworkBehaviour
{
    public float maxSaturation = 10f;
    [SyncVar] public float currentSaturation = 10f;
    public float saturationConsumtionPerSecond = 0.2f;

    [Header("Hunger UI")]
    public Image[] hungerIcons;
    public Sprite fullFood;
    public Sprite halfFood;
    public Sprite emptyFood;

    [Header("Hunger Warning UI")]
    public Image hungerWarningImage;   // the image that we are changing the color of
    public Color normalColor = Color.green;
    public Color midColor = Color.yellow;
    public Color lowColor = Color.red;

    [Header("Debug")]
    public PlayerState playerState;

    public float Hunger => maxSaturation - currentSaturation;

    public PlayerController PlayerController { get; private set; }
    public Inventory Inventory { get; private set; }

    void Awake() {
        PlayerController = GetComponent<PlayerController>();
        Inventory = GetComponent<Inventory>();
        normalColor = hungerWarningImage.color;
    }

    void Update() {
        if (!isLocalPlayer) return;
        AddSaturation(-saturationConsumtionPerSecond * Time.deltaTime);
        UpdateHungerUI(currentSaturation + 0.3f);
        UpdateStaminaColorUI(currentSaturation);
    }

    public void AddSaturation(float saturation) {
        currentSaturation = Mathf.Clamp(currentSaturation + saturation, 0f, maxSaturation);
    }

    [Server]
    public void Die() {
        RpcDie(connectionToClient);
    }

    [TargetRpc]
    void RpcDie(NetworkConnectionToClient _) {
        SetPlayerState(PlayerState.Dead);
        NotificationManager.I.PrintNotification("Respawn in 5 seconds");
        Invoke(nameof(Respawn), 5f);
    }

    [Client]
    void Respawn() {
        SetPlayerState(PlayerState.Default);
        PlayerController.currentStamina = PlayerController.maxStamina;
        KinematicCharacterController.KinematicCharacterMotorState state = PlayerController.CharacterMotor.GetState();
        state.Position = YachtManager.I.transform.position + Vector3.up * 3f;
        state.BaseVelocity = Vector3.zero;
        PlayerController.CharacterMotor.ApplyState(state);
    }

    public void UpdateHungerUI(float saturation)
    {
        float hungerPoints = Mathf.RoundToInt(saturation / (maxSaturation / 20));

        for (int i = 0; i < hungerIcons.Length; i++)
        {
            int iconValue = i * 2;
            if (hungerPoints >= iconValue + 2)
                hungerIcons[i].sprite = fullFood;
            else if (hungerPoints == iconValue + 1)
                hungerIcons[i].sprite = halfFood;
            else
                hungerIcons[i].sprite = emptyFood;
        }
    }

    public void UpdateStaminaColorUI(float saturation)
    {
        if (saturation < maxSaturation / 4)
            hungerWarningImage.color = lowColor;
        else if (saturation < maxSaturation / 2)
            hungerWarningImage.color = midColor;
        else
            hungerWarningImage.color = normalColor;
    }

    public void SetPlayerState(PlayerState newPlayerState) {
        playerState = newPlayerState;
        bool isDead = newPlayerState == PlayerState.Dead;
        PlayerController.CharacterMotor.enabled = !isDead;
        PlayerController.enabled = !isDead;
        Inventory.enabled = !isDead;
    }
}

public enum PlayerState {
    Default,
    Interacting,
    Dead
}
