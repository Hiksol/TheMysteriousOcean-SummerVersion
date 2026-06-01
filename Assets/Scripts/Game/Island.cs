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

    PhysicsMover mover;
    WobbleWaves wobbleWaves;

    void Awake() {
        mover = GetComponent<PhysicsMover>();
        mover.SetPosition(transform.position);
        mover.MoverController = this;
        TryGetComponent(out wobbleWaves);
    }

    public override void OnStartClient() {
        if (!isServer) {
            enabled = false;
            mover.enabled = false;
        }
    }

    void Update() {
        if (!isServer) return;
        currentTimeLiving += Time.deltaTime;
        if (currentTimeLiving >= timeToLive) {
            NetworkServer.UnSpawn(gameObject);
            Destroy(gameObject);
        }
    }

    public void UpdateMovement(out Vector3 goalPosition, out Quaternion goalRotation, float deltaTime) {
        Vector3 position = wobbleWaves && currentTimeLiving > 0.1f ? wobbleWaves.targetSmoothPosition : transform.position;
        goalPosition = position + velocity * deltaTime;
        Quaternion rotation = wobbleWaves && currentTimeLiving > 0.1f ? Quaternion.Euler(wobbleWaves.targetRotation) : transform.rotation;
        goalRotation = rotation;
    }
}
