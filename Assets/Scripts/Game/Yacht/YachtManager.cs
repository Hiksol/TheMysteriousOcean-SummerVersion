using System.Collections.Generic;
using Mirror;
using UnityEngine;

[RequireComponent(typeof(WobbleWaves))]
[RequireComponent(typeof(Collider))]
public class YachtManager : SingletonNetworkBehaviour<YachtManager>
{
    public float maxSinkingProgress = 100f;
    public float startHeightOffset = 1f;
    public float endHeightOffset = 0f;

    [Header("Debug")]
    [SyncVar] public float currentSinkingProgress = 0f; // 0 - ok, maxSinkingProgress - defeat
    public List<YachtBreach> breaches = new();

    WobbleWaves wobbleWaves;
    Collider _collider;

    public float HalfWidth => _collider.bounds.extents.x;

    override protected void AwakeNew() {
        wobbleWaves = GetComponent<WobbleWaves>();
        _collider = GetComponent<Collider>();
    }

    void Update() {
        wobbleWaves.heightOffset = Mathf.Lerp(startHeightOffset, endHeightOffset, currentSinkingProgress / maxSinkingProgress);
    }

    [Server]
    public void AddSinkingProgress(float progress) {
        currentSinkingProgress = Mathf.Clamp(currentSinkingProgress + progress, 0f, maxSinkingProgress);
        if (currentSinkingProgress == maxSinkingProgress) {
            // TODO: defeat
        }
    }
}
