using Mirror;
using UnityEngine;

public class YachtBreach : Interactable
{
    public float sinkingSpeed = 1f;
    public bool sinkingActive = true;

    public ParticleSystem _particleSystem;

    public override void OnStartServer() {
        YachtManager.I.breaches.Add(this);
    }

    void Update() {
        if (!isServer) return;
        if (sinkingActive) YachtManager.I.AddSinkingProgress(sinkingSpeed * Time.deltaTime);
    }

    [Server]
    public void Patch() {
        sinkingActive = false;
        YachtManager.I.breaches.Remove(this);
        if (_particleSystem != null) {
            _particleSystem.Stop();
            RpcDisableParticleSystem();
        }
    }

    [ClientRpc]
    void RpcDisableParticleSystem() {
        _particleSystem.Stop();
    }
}
