using System.Collections.Generic;
using KinematicCharacterController;
using Mirror;
using UnityEngine;

[RequireComponent(typeof(WobbleWaves))]
public class YachtManager : SingletonNetworkBehaviour<YachtManager>, IMoverController
{
    public float maxSinkingProgress = 100f;
    public float startHeightOffset = 1f;
    public float endHeightOffset = 0f;

    [Header("Debug")]
    [SyncVar] public float currentSinkingProgress = 0f; // 0 - ok, maxSinkingProgress - defeat
    public List<YachtBreach> breaches = new();
    [SyncVar] public Vector3 targetPosition;
    [SyncVar] public Quaternion targetRotation;

    WobbleWaves wobbleWaves;
    Collider _collider;
    PhysicsMover mover;

    public float HalfWidth => _collider.bounds.extents.x;

    override protected void AwakeNew() {
        wobbleWaves = GetComponent<WobbleWaves>();
        _collider = GetComponentInChildren<Collider>();
        if (TryGetComponent(out mover)) mover.MoverController = this;
        targetPosition = transform.position;
        targetRotation = transform.rotation;
    }

    void Update() {
        if (!isServer) return;
        wobbleWaves.heightOffset = Mathf.Lerp(startHeightOffset, endHeightOffset, currentSinkingProgress / maxSinkingProgress);
    }

    void FixedUpdate() {
        if (!isServer) return;
        targetPosition = wobbleWaves.targetSmoothPosition;
        targetRotation = Quaternion.Euler(wobbleWaves.targetRotation);
    }

    [Server]
    public void AddSinkingProgress(float progress) {
        currentSinkingProgress = Mathf.Clamp(currentSinkingProgress + progress, 0f, maxSinkingProgress);
    }

    public void UpdateMovement(out Vector3 goalPosition, out Quaternion goalRotation, float deltaTime) {
        goalPosition = targetPosition;
        goalRotation = targetRotation;
    }
}
