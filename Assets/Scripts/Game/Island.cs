using KinematicCharacterController;
using Mirror;
using UnityEngine;

[RequireComponent(typeof(PhysicsMover))]
public class Island : NetworkBehaviour, IMoverController
{
    public Vector3 velocity;
    public float timeToLive = 60f;

    [Header("Debug")]
    public float currentTimeLiving = 0f;
    [SyncVar] public Vector3 targetPosition;
    [SyncVar] public Quaternion targetRotation;

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
        if (currentTimeLiving >= timeToLive) {
            NetworkServer.UnSpawn(gameObject);
            Destroy(gameObject);
        }
    }

    void FixedUpdate() {
        if (!isServer) return;
        Vector3 position = wobbleWaves && currentTimeLiving > 0.1f ? wobbleWaves.targetSmoothPosition : transform.position;
        targetPosition = position + velocity * Time.fixedDeltaTime;
        Quaternion rotation = wobbleWaves && currentTimeLiving > 0.1f ? Quaternion.Euler(wobbleWaves.targetRotation) : transform.rotation;
        targetRotation = rotation;
    }

    public void UpdateMovement(out Vector3 goalPosition, out Quaternion goalRotation, float deltaTime) {
        goalPosition = targetPosition;
        goalRotation = targetRotation;
    }
}
