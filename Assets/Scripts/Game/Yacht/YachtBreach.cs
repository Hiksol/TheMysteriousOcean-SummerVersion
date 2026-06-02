using Mirror;
using UnityEngine;

public class YachtBreach : Interactable
{
    public float sinkingSpeed = 1f;
    public bool sinkingActive = true;

    void Update() {
        if (!isServer) return;
        if (sinkingActive) YachtManager.I.AddSinkingProgress(sinkingSpeed * Time.deltaTime);
    }

    [Server]
    public void Patch() {
        sinkingActive = false;
    }
}
