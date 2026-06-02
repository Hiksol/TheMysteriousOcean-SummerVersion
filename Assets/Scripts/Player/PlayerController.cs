using UnityEngine;
using Mirror;
using UnityEngine.InputSystem;
using KinematicCharacterController;

[RequireComponent(typeof(KinematicCharacterMotor))]
public class PlayerController : NetworkBehaviour, ICharacterController
{
    [Header("Movement")]
    public float playerSpeed = 5f;
    public float playerGravityMult = 2f;
    public float jumpSpeed = 5f;
    public float airDrag = 0.1f;

    [Header("Camera")]
    public float cameraSensivity = 100f;
    public float cameraVertialClamp = 80f;

    [Header("Input")]
    public float jumpBuffer = 0.1f;

    [Header("Debug")]
    public float currentJumpBuffer = 0f;
    float cameraXRotation = 0f;
    Vector2 lookInput;
    Vector2 moveInput;
    bool JumpPressed => currentJumpBuffer > 0f;

    KinematicCharacterMotor characterMotor;
    Camera cam;
    InputAction moveAction;
    InputAction lookAction;
    InputAction jumpAction;

    void Awake() {
        characterMotor = GetComponent<KinematicCharacterMotor>();
        characterMotor.CharacterController = this;
        moveAction = InputSystem.actions.FindAction("Move");
        lookAction = InputSystem.actions.FindAction("Look");
        jumpAction = InputSystem.actions.FindAction("Jump");
    }

    public override void OnStartClient() {
        if (isLocalPlayer) {
            characterMotor.CharacterController = this;
        } else {
            enabled = false;
            characterMotor.enabled = false;
        }
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
        if (Keyboard.current.rKey.wasPressedThisFrame) {
            Cursor.lockState = Cursor.visible ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !Cursor.visible;
        }
    }

    public void UpdateRotation(ref Quaternion currentRotation, float deltaTime) {
        Vector2 lookVector = lookInput * (cameraSensivity * deltaTime);
        currentRotation *= Quaternion.AngleAxis(lookVector.x, characterMotor.CharacterUp);
    }

    public void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime) {
        HandleMovement(ref currentVelocity, deltaTime);
        HandleGravity(ref currentVelocity, deltaTime);
        HandleJump(ref currentVelocity, deltaTime);
    }

    void HandleCamera() {
        Vector2 lookVector = lookInput * (cameraSensivity * Time.deltaTime);
        Vector3 camEuler = cam.transform.localEulerAngles;
        cameraXRotation = Mathf.Clamp(cameraXRotation - lookVector.y, -cameraVertialClamp, cameraVertialClamp);
        camEuler.x = cameraXRotation;
        cam.transform.localEulerAngles = camEuler;
    }

    void HandleMovement(ref Vector3 currentVelocity, float deltaTime) {
        Vector3 moveInputNormal = (transform.forward * moveInput.y + transform.right * moveInput.x).normalized;
        Vector3 targetMovementVelocity;
        if (characterMotor.GroundingStatus.IsStableOnGround) {
            // Reorient velocity on slope
            currentVelocity = characterMotor.GetDirectionTangentToSurface(currentVelocity, characterMotor.GroundingStatus.GroundNormal) * currentVelocity.magnitude;

            // Calculate target velocity
            Vector3 inputRight = Vector3.Cross(moveInputNormal, characterMotor.CharacterUp);
            Vector3 reorientedInput = Vector3.Cross(characterMotor.GroundingStatus.GroundNormal, inputRight).normalized * moveInputNormal.magnitude;
            targetMovementVelocity = reorientedInput * playerSpeed;

            // Smooth movement Velocity
            currentVelocity = Vector3.Lerp(currentVelocity, targetMovementVelocity, 1 - Mathf.Exp(-15 * deltaTime));
        } else if (moveInputNormal.sqrMagnitude > 0f) {
            // Add move input
            targetMovementVelocity = moveInputNormal * playerSpeed;

            // Prevent climbing on un-stable slopes with air movement
            if (characterMotor.GroundingStatus.FoundAnyGround) {
                Vector3 perpenticularObstructionNormal = Vector3.Cross(Vector3.Cross(characterMotor.CharacterUp, characterMotor.GroundingStatus.GroundNormal), characterMotor.CharacterUp).normalized;
                targetMovementVelocity = Vector3.ProjectOnPlane(targetMovementVelocity, perpenticularObstructionNormal);
            }

            Vector3 velocity = Vector3.ProjectOnPlane(targetMovementVelocity, Physics.gravity);
            currentVelocity = velocity + new Vector3(0, currentVelocity.y, 0);
        }
    }

    void HandleGravity(ref Vector3 currentVelocity, float deltaTime) {
        if (!characterMotor.GroundingStatus.IsStableOnGround) {
            // Gravity
            currentVelocity += Physics.gravity * (playerGravityMult * deltaTime);
            // Drag
            currentVelocity.y *= 1f / (1f + (airDrag * deltaTime));
        }
    }

    void HandleJump(ref Vector3 currentVelocity, float _) {
        if (JumpPressed && characterMotor.GroundingStatus.IsStableOnGround) {
            Vector3 jumpDirection = characterMotor.CharacterUp;
            characterMotor.ForceUnground(0.1f);
            currentVelocity += (jumpDirection * jumpSpeed) - Vector3.Project(currentVelocity, characterMotor.CharacterUp);
            currentJumpBuffer = 0f;
        }
    }

    public void BeforeCharacterUpdate(float deltaTime) {}
    public void PostGroundingUpdate(float deltaTime) {}
    public void AfterCharacterUpdate(float deltaTime) {}
    public bool IsColliderValidForCollisions(Collider coll) { return true; }
    public void OnGroundHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport) {}
    public void OnMovementHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport) {}
    public void ProcessHitStabilityReport(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, Vector3 atCharacterPosition, Quaternion atCharacterRotation, ref HitStabilityReport hitStabilityReport) {}
    public void OnDiscreteCollisionDetected(Collider hitCollider) {}
}
