using UnityEngine;
using UnityEngine.UI;
using Mirror;
using UnityEngine.InputSystem;
using KinematicCharacterController;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.TextCore.Text;

[RequireComponent(typeof(Player))]
[RequireComponent(typeof(KinematicCharacterMotor))]
public class PlayerController : NetworkBehaviour, ICharacterController
{
    [Header("Movement")]
    public float playerSpeed = 5f;
    public float playerGravityMult = 2f;
    public float jumpSpeed = 5f;
    public float airDrag = 0.1f;
    public float maxStamina = 10f;
    public float staminaSprintigPerSecond = 2f;
    public float staminaRegenPerSecond = 1.5f;
    public float speedMultSprinting = 1.5f;

    [Header("Swimming")]
    public LayerMask waterLayer = 1 << 4;
    public float staminaSwimmingPerSecond = 2f;
    public float heightWaterOffset = 0f;
    public float targetVerticalSpeedInWater = 1.5f;
    public float tdVerticalSpeedInWater = 3f;
    public float csVerticalSpeedInWater = 1f;
    public float maxVerticalSpeedInWater = 2f;

    [Header("Climbing")]
    public float climbingSpeed = 3f;
    public float anchoringDuration = 1f;

    [Header("Camera")]
    public float cameraSensivity = 100f;
    public float cameraVertialClamp = 80f;

    [Header("Input")]
    public float jumpBuffer = 0.1f;

    //Stamina
    [Header("UI")]
    public Slider staminaSlider;
    public RectTransform staminaIcon;          // the image that will rotate (RectTransform)
    public float staminaIconRotSpeed = 360f;   // rotation speed (deg/seconds) at full rate of change

    private float targetRotSpeed = 0f;
    private float currentRotSpeed = 0f;

    public float rotAcceleration = 8f;
    public float rotDeceleration = 6f;



    // For smooth rotation
    private float currentStaminaDelta = 0f;    // the sign and magnitude of the change in stamina in the current frame

    [Header("Debug")]
    public float currentJumpBuffer = 0f;
    public float currentStamina = 10f;
    public bool isSprinting = false;
    public PlayerControllerState state = PlayerControllerState.Default;
    public ClimbingState climbingState = ClimbingState.Climbing;
    public Vector3 waterRaycastPos;
    public List<StaminaUseMult> staminaUseMults = new();

    float cameraXRotation = 0f;
    Vector2 lookInput;
    Vector2 moveInput;
    public Vector2 MoveInput => moveInput;
    bool JumpPressed => currentJumpBuffer > 0f;
    public bool IsDefault => state == PlayerControllerState.Default;
    bool InWater => state == PlayerControllerState.Swimming;
    bool IsClimbing => state == PlayerControllerState.Climbing;
    Ladder activeLadder;
    float onLadderSegmentState;
    float anchoringTimer = 0f;
    Vector3 anchoringStartPosition;

    Player player;
    public KinematicCharacterMotor CharacterMotor { get; private set; }
    Camera cam;
    InputAction moveAction;
    InputAction lookAction;
    InputAction jumpAction;
    InputAction sprintAction;
    InputAction interactAction;
    readonly Collider[] probedColliders = new Collider[8];
    Vector3 ladderTargetPosition;

    void Awake() {
        player = GetComponent<Player>();
        CharacterMotor = GetComponent<KinematicCharacterMotor>();
        CharacterMotor.CharacterController = this;
        moveAction = InputSystem.actions.FindAction("Move");
        lookAction = InputSystem.actions.FindAction("Look");
        jumpAction = InputSystem.actions.FindAction("Jump");
        sprintAction = InputSystem.actions.FindAction("Sprint");
        interactAction = InputSystem.actions.FindAction("Interact");
    }

    public override void OnStartClient() {
        if (isLocalPlayer) {
            CharacterMotor.CharacterController = this;
        } else {
            enabled = false;
            CharacterMotor.enabled = false;
            return;
        }

        //Stamina
        if (staminaSlider != null)
        {
            staminaSlider.maxValue = maxStamina;
            staminaSlider.value = currentStamina;
        }
    }

    public override void OnStartLocalPlayer() {
        if (Camera.main) Camera.main.gameObject.SetActive(false);
        cam = GetComponentInChildren<Camera>(true);
        cam.gameObject.SetActive(true);
    }

    void Update() {
        if (!isLocalPlayer) return;
        lookInput = player.playerState == PlayerState.Default ? lookAction.ReadValue<Vector2>() : Vector2.zero;
        HandleCamera();
        HandleMovementInputs();
        CheckLadder();
        CheckWater();
        UpdateStamina();
        UpdateStaminaIcon();
    }

    void ChangeState(PlayerControllerState newState) {
        switch (state) {
            case PlayerControllerState.Climbing:
                CharacterMotor.SetMovementCollisionsSolvingActivation(true);
                CharacterMotor.SetGroundSolvingActivation(true);
                break;
        }
        state = newState;
        switch (state) {
            case PlayerControllerState.Climbing:
                CharacterMotor.SetMovementCollisionsSolvingActivation(false);
                CharacterMotor.SetGroundSolvingActivation(false);
                ladderTargetPosition = activeLadder.ClosestPointOnLadderSegment(CharacterMotor.TransientPosition, out onLadderSegmentState);
                ChangeClimbingState(ClimbingState.Anchoring);
                break;
        }
    }

    void ChangeClimbingState(ClimbingState newClimbingState) {
        climbingState = newClimbingState;
        anchoringTimer = 0f;
        anchoringStartPosition = CharacterMotor.TransientPosition;
    }

    void UpdateStamina() {
        float staminaUseMult = staminaUseMults.Aggregate(1f, (mult, sum) => mult * sum.staminaUseMult);
        if (InWater) AddStamina(-staminaSwimmingPerSecond * staminaUseMult * Time.deltaTime);
        else if (IsDefault && isSprinting && moveInput.sqrMagnitude != 0) AddStamina(-staminaSprintigPerSecond * staminaUseMult * Time.deltaTime);
        else AddStamina(staminaRegenPerSecond / GetStaminaRegenDenominator(player.Hunger) * Time.deltaTime);
        staminaUseMults.ToList().ForEach(sum => { if (sum.OnUndate(Time.deltaTime)) staminaUseMults.Remove(sum); });
    }

    void UpdateStaminaIcon() {
        if (staminaIcon == null) return;
        float direction = Mathf.Sign(currentStaminaDelta);

        // enhance the effect of changing stamina
        float magnitude = Mathf.Clamp01(Mathf.Abs(currentStaminaDelta) * 50f);

        targetRotSpeed = direction * staminaIconRotSpeed * magnitude;

        if (Mathf.Abs(targetRotSpeed) > 0.01f)
            currentRotSpeed = Mathf.Lerp(currentRotSpeed, targetRotSpeed, rotAcceleration * Time.deltaTime);
        else
            currentRotSpeed = Mathf.Lerp(currentRotSpeed, 0f, rotDeceleration * Time.deltaTime);

        staminaIcon.Rotate(0f, 0f, currentRotSpeed * Time.deltaTime);
    }

    void AddStamina(float staminaDelta)
    {
        float oldStamina = currentStamina;
        currentStamina = Mathf.Clamp(currentStamina + staminaDelta, 0f, maxStamina);
        float realDelta = currentStamina - oldStamina; // clamping accounting

        // We memorize the sign and magnitude of the change for the rotation of the image
        currentStaminaDelta = realDelta;

        // Updating the slider
        if (staminaSlider != null)
            staminaSlider.value = currentStamina;
    }

    float GetStaminaRegenDenominator(float hunger) {
        if (hunger <= 0) return 1;
        return 9 * Mathf.Pow(hunger / 10, 5) + 1;
        // 0 => 1, 5 => ~1.28, 8 => ~3.95, 10 => 10
    }

    public void UpdateRotation(ref Quaternion currentRotation, float deltaTime) {
        Vector2 lookVector = lookInput * (cameraSensivity * deltaTime);
        currentRotation *= Quaternion.AngleAxis(lookVector.x, CharacterMotor.CharacterUp);
    }

    public void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime) {
        HandleMovement(ref currentVelocity, deltaTime);
        HandleGravity(ref currentVelocity, deltaTime);
        HandleJump(ref currentVelocity, deltaTime);
        HandleLadder(ref currentVelocity, deltaTime);
    }

    public void AfterCharacterUpdate(float deltaTime) {
        if (!IsClimbing) return;
        switch (climbingState) {
            case ClimbingState.Climbing:
                activeLadder.ClosestPointOnLadderSegment(CharacterMotor.TransientPosition, out onLadderSegmentState);
                if (Mathf.Abs(onLadderSegmentState) > 0.05f) {
                    ChangeClimbingState(ClimbingState.DeAnchoring);
                    // If we're higher than the ladder top point
                    if (onLadderSegmentState > 0) ladderTargetPosition = activeLadder.topReleasePoint.position;
                    // If we're lower than the ladder bottom point
                    else if (onLadderSegmentState < 0) ladderTargetPosition = activeLadder.bottomReleasePoint.position;
                }
                break;
            case ClimbingState.Anchoring:
            case ClimbingState.DeAnchoring:
                // Detect transitioning out from anchoring states
                if (anchoringTimer >= anchoringDuration) {
                    if (climbingState == ClimbingState.Anchoring) ChangeClimbingState(ClimbingState.Climbing);
                    else if (climbingState == ClimbingState.DeAnchoring) ChangeState(PlayerControllerState.Default);
                }
                anchoringTimer += deltaTime;
                break;
        }
    }

    void HandleCamera() {
        Vector2 lookVector = lookInput * (cameraSensivity * Time.deltaTime);
        Vector3 camEuler = cam.transform.localEulerAngles;
        cameraXRotation = Mathf.Clamp(cameraXRotation - lookVector.y, -cameraVertialClamp, cameraVertialClamp);
        camEuler.x = cameraXRotation;
        cam.transform.localEulerAngles = camEuler;
    }

    void HandleMovementInputs() {
        moveInput = player.playerState == PlayerState.Default ? moveAction.ReadValue<Vector2>() : Vector2.zero;
        if (player.playerState == PlayerState.Default && jumpAction.WasPressedThisFrame()) currentJumpBuffer = jumpBuffer;
        else currentJumpBuffer = Mathf.Max(currentJumpBuffer - Time.deltaTime, 0);
        if (Keyboard.current.rKey.wasPressedThisFrame) {
            Cursor.visible = !Cursor.visible;
            Cursor.lockState = Cursor.visible ? CursorLockMode.None : CursorLockMode.Locked;
        }
        isSprinting = player.playerState == PlayerState.Default && sprintAction.IsPressed();
    }

    void HandleMovement(ref Vector3 currentVelocity, float deltaTime) {
        if (!IsDefault && !InWater) {
            currentVelocity.x = currentVelocity.z = 0;
            return;
        }
        Vector3 moveInputNormal = (transform.forward * moveInput.y + transform.right * moveInput.x).normalized;
        float currentPlayerSpeed = playerSpeed * (isSprinting && currentStamina > 0 ? speedMultSprinting : 1f);
        Vector3 targetMovementVelocity;
        if (CharacterMotor.GroundingStatus.IsStableOnGround) {
            // Reorient velocity on slope
            currentVelocity = CharacterMotor.GetDirectionTangentToSurface(currentVelocity, CharacterMotor.GroundingStatus.GroundNormal) * currentVelocity.magnitude;

            // Calculate target velocity
            Vector3 inputRight = Vector3.Cross(moveInputNormal, CharacterMotor.CharacterUp);
            Vector3 reorientedInput = Vector3.Cross(CharacterMotor.GroundingStatus.GroundNormal, inputRight).normalized * moveInputNormal.magnitude;
            targetMovementVelocity = reorientedInput * currentPlayerSpeed;
        } else {
            // Add move input
            targetMovementVelocity = moveInputNormal * currentPlayerSpeed;

            // Prevent climbing on un-stable slopes with air movement
            if (CharacterMotor.GroundingStatus.FoundAnyGround) {
                Vector3 perpenticularObstructionNormal = Vector3.Cross(Vector3.Cross(CharacterMotor.CharacterUp, CharacterMotor.GroundingStatus.GroundNormal), CharacterMotor.CharacterUp).normalized;
                targetMovementVelocity = Vector3.ProjectOnPlane(targetMovementVelocity, perpenticularObstructionNormal);
            }

            targetMovementVelocity = Vector3.ProjectOnPlane(targetMovementVelocity, Physics.gravity);
        }

        // Smooth movement Velocity
        float y = currentVelocity.y;
        currentVelocity = Vector3.Lerp(currentVelocity, targetMovementVelocity, 1 - Mathf.Exp(-15 * deltaTime));
        currentVelocity.y = y;
    }

    void HandleGravity(ref Vector3 currentVelocity, float deltaTime) {
        if (!CharacterMotor.GroundingStatus.IsStableOnGround) {
            // Gravity
            if (IsDefault || CharacterMotor.MustUnground() || (InWater && currentStamina == 0)) currentVelocity += Physics.gravity * (playerGravityMult * deltaTime);
            else if (InWater) {
                float targetSpeed = targetVerticalSpeedInWater * (transform.position.y > waterRaycastPos.y ? -1 : 1);
                // currentVelocity.y = Mathf.Lerp(currentVelocity.y, targetSpeed, 1 - Mathf.Exp(-5 * deltaTime));
                currentVelocity.y = Mathf.Clamp(currentVelocity.y, -maxVerticalSpeedInWater, maxVerticalSpeedInWater);
                currentVelocity.y = Mathf.MoveTowards(currentVelocity.y, targetSpeed, tdVerticalSpeedInWater * deltaTime + Mathf.Abs(currentVelocity.y) * csVerticalSpeedInWater * deltaTime);
            } else if (IsClimbing) currentVelocity.y = 0;
            // Drag
            currentVelocity.y *= 1f / (1f + (airDrag * deltaTime));
        }
    }

    void HandleJump(ref Vector3 currentVelocity, float _) {
        if (JumpPressed && (CharacterMotor.GroundingStatus.IsStableOnGround || InWater)) {
            Vector3 jumpDirection = CharacterMotor.CharacterUp;
            CharacterMotor.ForceUnground(0.1f);
            currentVelocity += (jumpDirection * jumpSpeed) - Vector3.Project(currentVelocity, CharacterMotor.CharacterUp);
            currentJumpBuffer = 0f;
        }
    }

    void CheckWater() {
        bool inWater = Physics.Raycast(transform.position + Vector3.up * (CharacterMotor.Capsule.height / 2f + 2f), Vector3.down, out RaycastHit hit, CharacterMotor.Capsule.height + 2f, waterLayer) &&
            !Physics.Raycast(transform.position + Vector3.up * CharacterMotor.Capsule.height / 2f, Vector3.down, CharacterMotor.Capsule.height + 2f, Physics.DefaultRaycastLayers & ~waterLayer);
        if (inWater) {
            if (IsDefault) ChangeState(PlayerControllerState.Swimming);
            waterRaycastPos = hit.point;
        } else {
            if (InWater) ChangeState(PlayerControllerState.Default);
        }
    }

    void CheckLadder() {
        if (!interactAction.WasPressedThisFrame()) return;
        if (CharacterMotor.CharacterOverlap(CharacterMotor.TransientPosition, CharacterMotor.TransientRotation, probedColliders, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide) > 0) {
            if (probedColliders[0] == null) return;
            Collider collider = probedColliders.FirstOrDefault(col => col && col.TryGetComponent(out Ladder _));
            if (collider != null && collider.TryGetComponent(out Ladder ladder)) {
                if (IsDefault) {
                    activeLadder = ladder;
                    ChangeState(PlayerControllerState.Climbing);
                } else if (IsClimbing) {
                    ChangeClimbingState(ClimbingState.DeAnchoring);
                    ladderTargetPosition = CharacterMotor.TransientPosition;
                }
            }
        }
    }

    void HandleLadder(ref Vector3 currentVelocity, float deltaTime) {
        if (!IsClimbing) return;
        currentVelocity = Vector3.zero;
        switch (climbingState) {
            case ClimbingState.Climbing:
                float ladderInput = moveInput.y * moveInput.magnitude;
                currentVelocity = activeLadder.transform.up * (ladderInput * climbingSpeed);
                break;
            case ClimbingState.Anchoring:
            case ClimbingState.DeAnchoring:
                Vector3 tmpPosition = Vector3.Lerp(anchoringStartPosition, ladderTargetPosition, anchoringTimer / anchoringDuration);
                currentVelocity = CharacterMotor.GetVelocityForMovePosition(CharacterMotor.TransientPosition, tmpPosition, deltaTime);
                break;
        }
    }

    public void BeforeCharacterUpdate(float deltaTime) {}
    public void PostGroundingUpdate(float deltaTime) {}
    public bool IsColliderValidForCollisions(Collider coll) { return true; }
    public void OnGroundHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport) {}
    public void OnMovementHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport) {}
    public void ProcessHitStabilityReport(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, Vector3 atCharacterPosition, Quaternion atCharacterRotation, ref HitStabilityReport hitStabilityReport) {}
    public void OnDiscreteCollisionDetected(Collider hitCollider) {}

    public enum PlayerControllerState {
        Default,
        Swimming,
        Climbing
    }

    public enum ClimbingState {
        Anchoring,
        Climbing,
        DeAnchoring
    }
}
