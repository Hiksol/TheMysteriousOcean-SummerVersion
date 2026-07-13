using KinematicCharacterController;
using Mirror;
using UnityEngine;

[RequireComponent(typeof(PhysicsMover))]
[RequireComponent(typeof(Collider))]
public class Island : NetworkBehaviour, IMoverController
{
    public Vector3 velocity;
    // public float timeToLive = 60f;
    public float halfDiagonal;
    public float maxAngle = 60f;
    public float angleChangeSpeed = 30f;
    public float downSpeed = 5f;

    [Header("Debug")]
    public float currentTimeLiving = 0f;
    [SyncVar] public Vector3 targetPosition;
    [SyncVar] public Quaternion targetRotation;
    public bool isDrowning = false;

    PhysicsMover mover;
    WobbleWaves wobbleWaves;


    void Awake() {
        mover = GetComponent<PhysicsMover>();
        mover.SetPosition(transform.position);
        mover.MoverController = this;
        TryGetComponent(out wobbleWaves);
        targetPosition = transform.position;
        targetRotation = transform.rotation;
    }

    void Update() {
        if (!isServer) return;
        currentTimeLiving += Time.deltaTime;
        if (wobbleWaves && !isDrowning && currentTimeLiving >= 1f && !wobbleWaves.hasWater) isDrowning = true;
    }

    void FixedUpdate() {
        if (!isServer) return;
        if (currentTimeLiving <= 0.1f || !wobbleWaves) {
            targetPosition = transform.position;
            targetRotation = transform.rotation;
            return;
        }
        Vector3 position = !isDrowning ? wobbleWaves.targetSmoothPosition : transform.position;
        targetPosition = position + (!isDrowning ? velocity : Vector3.down * downSpeed) * Time.fixedDeltaTime;
        Quaternion rotation;
        if (!isDrowning) rotation = Quaternion.Euler(wobbleWaves.targetRotation);
        else {
            rotation = Quaternion.Euler(-angleChangeSpeed * Time.deltaTime, 0, 0) * transform.rotation;
            rotation.DecomposeSwingTwist(Vector3.right, out Quaternion _, out Quaternion twist);
            float xAngle = (twist.eulerAngles.x + 180) % 360 - 180;
            if (xAngle < -maxAngle) rotation = transform.rotation;
        }
        targetRotation = rotation;
    }

    public void UpdateMovement(out Vector3 goalPosition, out Quaternion goalRotation, float deltaTime) {
        goalPosition = targetPosition;
        goalRotation = targetRotation;
    }

    [Server]
    public void Remove() {
        NetworkServer.UnSpawn(gameObject);
        Destroy(gameObject);
    }
}
