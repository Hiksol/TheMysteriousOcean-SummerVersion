using UnityEngine;
using Mirror;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : NetworkBehaviour
{
    [Header("Movement")]
    public float playerSpeed = 5f;
    public float playerGravityMult = 2f;

    [Header("Camera")]
    public float cameraSensivity = 100f;
    public float cameraVertialClamp = 80f;

    float gravitySpeed = -1f;
    float cameraXRotation = 0f;

    CharacterController characterController;
    Camera cam;
    InputAction moveAction;
    InputAction lookAction;

    void Awake() {
        characterController = GetComponent<CharacterController>();
        moveAction = InputSystem.actions.FindAction("Move");
        lookAction = InputSystem.actions.FindAction("Look");
    }

    public override void OnStartClient() {
        if (!isLocalPlayer) enabled = false;
    }

    public override void OnStartLocalPlayer() {
        if (Camera.main) Camera.main.gameObject.SetActive(false);
        cam = GetComponentInChildren<Camera>(true);
        cam.gameObject.SetActive(true);
    }

    void Update() {
        if (!isLocalPlayer) return;
        HandleCamera();
        HandleMovement();
    }

    void HandleMovement() {
        Vector2 moveInput = moveAction.ReadValue<Vector2>();
        Vector3 moveInputLocal = transform.forward * moveInput.y + transform.right * moveInput.x;
        Vector3 moveVector = Vector3.ClampMagnitude(moveInputLocal, playerSpeed * Time.deltaTime);
        characterController.Move(moveVector);

        gravitySpeed += Physics.gravity.y * Time.deltaTime;
        characterController.Move(Vector3.up * (gravitySpeed * Time.deltaTime));
        if (characterController.isGrounded && gravitySpeed < -1f) gravitySpeed = -1f;

        float maxDistance = characterController.height / 2 + characterController.skinWidth;
        if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, characterController.height / 2 + characterController.skinWidth)) {
            characterController.Move(Vector3.up * (maxDistance - hit.distance));
        }
    }

    void HandleCamera() {
        Vector2 lookInput = lookAction.ReadValue<Vector2>();
        Vector2 lookVector = lookInput * (cameraSensivity * Time.deltaTime);
        transform.Rotate(transform.up, lookVector.x);

        Vector3 camEuler = cam.transform.localEulerAngles;
        cameraXRotation = Mathf.Clamp(cameraXRotation - lookVector.y, -cameraVertialClamp, cameraVertialClamp);
        camEuler.x = cameraXRotation;
        cam.transform.localEulerAngles = camEuler;
    }
}
