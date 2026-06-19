using Mirror;
using UnityEngine;

public class CameraShake : NetworkBehaviour
{
    public float shakeFrequency = 15f;
    public float shakeAmount = 0.1f;
    public float goBackSpeed = 10f;

    Vector3 startPos;
    float timeElapsed = 0f;

    Player player;
    PlayerController playerController;

    void Awake() {
        player = GetComponentInParent<Player>();
        playerController = player.PlayerController;
    }

    void Start() {
        if (!isLocalPlayer) {
            enabled = false;
            return;
        }
        startPos = transform.localPosition;
    }

    void Update() {
        bool isMoving = playerController.IsDefault && playerController.MoveInput.magnitude > 0;
        if (isMoving) {
            float shakeX = Mathf.Sin(timeElapsed * shakeFrequency / 2) * shakeAmount;
            float shakeY = Mathf.Sin(timeElapsed * shakeFrequency) * shakeAmount;
            transform.localPosition = new(startPos.x + shakeX, startPos.y + shakeY, transform.localPosition.z);
            timeElapsed += Time.deltaTime;
        } else {
            transform.localPosition = Vector3.Lerp(transform.localPosition, startPos, goBackSpeed * Time.deltaTime);
            timeElapsed = 0f;
        }
    }
}
