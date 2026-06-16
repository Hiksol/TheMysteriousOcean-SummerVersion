using UnityEngine;
using UnityEngine.UI;
using Mirror;
using UnityEngine.InputSystem;
using KinematicCharacterController;

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
    public bool inWater = false;
    public Vector3 waterRaycastPos;

    float cameraXRotation = 0f;
    Vector2 lookInput;
    Vector2 moveInput;
    bool JumpPressed => currentJumpBuffer > 0f;

    Player player;
    public KinematicCharacterMotor CharacterMotor { get; private set; }
    Camera cam;
    InputAction moveAction;
    InputAction lookAction;
    InputAction jumpAction;
    InputAction sprintAction;

    void Awake() {
        player = GetComponent<Player>();
        CharacterMotor = GetComponent<KinematicCharacterMotor>();
        CharacterMotor.CharacterController = this;
        moveAction = InputSystem.actions.FindAction("Move");
        lookAction = InputSystem.actions.FindAction("Look");
        jumpAction = InputSystem.actions.FindAction("Jump");
        sprintAction = InputSystem.actions.FindAction("Sprint");
    }

    public override void OnStartClient() {
        if (isLocalPlayer) {
            CharacterMotor.CharacterController = this;
        } else {
            enabled = false;
            CharacterMotor.enabled = false;
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
        moveInput = player.playerState == PlayerState.Default ? moveAction.ReadValue<Vector2>() : Vector2.zero;
        if (player.playerState == PlayerState.Default && jumpAction.WasPressedThisFrame()) currentJumpBuffer = jumpBuffer;
        else currentJumpBuffer = Mathf.Max(currentJumpBuffer - Time.deltaTime, 0);
        if (Keyboard.current.rKey.wasPressedThisFrame) {
            Cursor.visible = !Cursor.visible;
            Cursor.lockState = Cursor.visible ? CursorLockMode.None : CursorLockMode.Locked;
        }
        isSprinting = player.playerState == PlayerState.Default && sprintAction.IsPressed();
        CheckWater();
        UpdateStamina();
        UpdateStaminaIcon();
    }

    void UpdateStamina() {
        if (inWater) AddStamina(-staminaSwimmingPerSecond * Time.deltaTime);
        else if (isSprinting && moveInput.sqrMagnitude != 0) AddStamina(-staminaSprintigPerSecond * Time.deltaTime);
        else AddStamina(staminaRegenPerSecond / GetStaminaRegenDenominator(player.Hunger) * Time.deltaTime);
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
            if (!inWater || CharacterMotor.MustUnground() || (inWater && currentStamina == 0)) currentVelocity += Physics.gravity * (playerGravityMult * deltaTime);
            else {
                float targetSpeed = targetVerticalSpeedInWater * (transform.position.y > waterRaycastPos.y ? -1 : 1);
                // currentVelocity.y = Mathf.Lerp(currentVelocity.y, targetSpeed, 1 - Mathf.Exp(-5 * deltaTime));
                currentVelocity.y = Mathf.Clamp(currentVelocity.y, -maxVerticalSpeedInWater, maxVerticalSpeedInWater);
                currentVelocity.y = Mathf.MoveTowards(currentVelocity.y, targetSpeed, tdVerticalSpeedInWater * deltaTime + Mathf.Abs(currentVelocity.y) * csVerticalSpeedInWater * deltaTime);
            }
            // Drag
            // currentVelocity.y *= 1f / (1f + (airDrag * deltaTime));
        }
    }

    void HandleJump(ref Vector3 currentVelocity, float _) {
        if (JumpPressed && (CharacterMotor.GroundingStatus.IsStableOnGround || inWater)) {
            Vector3 jumpDirection = CharacterMotor.CharacterUp;
            CharacterMotor.ForceUnground(0.1f);
            currentVelocity += (jumpDirection * jumpSpeed) - Vector3.Project(currentVelocity, CharacterMotor.CharacterUp);
            currentJumpBuffer = 0f;
        }
    }

    void CheckWater() {
        inWater = Physics.Raycast(transform.position + Vector3.up * (CharacterMotor.Capsule.height / 2f + 2f), Vector3.down, out RaycastHit hit, CharacterMotor.Capsule.height + 2f, waterLayer);
        if (inWater) {
            waterRaycastPos = hit.point;
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
