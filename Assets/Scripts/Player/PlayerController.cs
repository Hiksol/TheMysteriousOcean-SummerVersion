using UnityEngine;
using Mirror;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : NetworkBehaviour
{
    [Header("Movement")]
    public float playerSpeed = 5f;
    public float playerGravityMult = 2f;
    public float jumpVelocity = 3f;

    [Header("Camera")]
    public float cameraSensivity = 100f;
    public float cameraVertialClamp = 80f;

    [Header("Input")]
    public float jumpBuffer = 0.1f;

    [Header("Debug")]
    public float currentYVelocity = -1f;
    float cameraXRotation = 0f;
    public bool wasGrounded = false;
    Vector2 lookInput;
    Vector2 moveInput;
    public float currentJumpBuffer = 0f;
    bool JumpPressed => currentJumpBuffer > 0f;

    CharacterController characterController;
    Camera cam;
    InputAction moveAction;
    InputAction lookAction;
    InputAction jumpAction;

    void Awake() {
        characterController = GetComponent<CharacterController>();
        moveAction = InputSystem.actions.FindAction("Move");
        lookAction = InputSystem.actions.FindAction("Look");
        jumpAction = InputSystem.actions.FindAction("Jump");
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
        lookInput = lookAction.ReadValue<Vector2>();
        HandleCamera();
        moveInput = moveAction.ReadValue<Vector2>();
        if (jumpAction.WasPressedThisFrame()) currentJumpBuffer = jumpBuffer;
        else currentJumpBuffer = Mathf.Max(currentJumpBuffer - Time.deltaTime, 0);
    }

    void FixedUpdate() {
        if (!isLocalPlayer) return;
        HandleMovement();
        HandleJump();
        HandleGravity();
    }

    void HandleCamera() {
        Vector2 lookVector = lookInput * (cameraSensivity * Time.deltaTime);
        transform.Rotate(transform.up, lookVector.x);

        Vector3 camEuler = cam.transform.localEulerAngles;
        cameraXRotation = Mathf.Clamp(cameraXRotation - lookVector.y, -cameraVertialClamp, cameraVertialClamp);
        camEuler.x = cameraXRotation;
        cam.transform.localEulerAngles = camEuler;
    }

    void HandleMovement() {
        Vector3 moveInputLocal = transform.forward * moveInput.y + transform.right * moveInput.x;
        Vector3 moveVector = Vector3.ClampMagnitude(moveInputLocal, playerSpeed * Time.fixedDeltaTime);
        characterController.Move(moveVector);
    }

    void HandleJump() {
        if (wasGrounded && JumpPressed) {
            currentYVelocity = jumpVelocity;
            currentJumpBuffer = 0f;
        }
    }

    void HandleGravity() {
        currentYVelocity += Physics.gravity.y * playerGravityMult * Time.fixedDeltaTime;
        characterController.Move(Vector3.up * (currentYVelocity * Time.fixedDeltaTime));
        if (characterController.isGrounded && currentYVelocity < -1f) currentYVelocity = -1f;
        wasGrounded = characterController.isGrounded;

        // move character up to avoid jitter with skin width
        float maxDistance = characterController.height / 2 + characterController.skinWidth;
        if (currentYVelocity < 0 && Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, characterController.height / 2 + characterController.skinWidth)) {
            characterController.Move(Vector3.up * (maxDistance - hit.distance));
        }
    }
}
