using Mirror;
using UnityEngine;

public class YachtPump : Interactable
{
    public Battery battery;
    public float energyConsumptionPerSecond = 5;
    public float sinkingProgressDecreasePerSecond = 5;
    public ParticleSystem _particleSystem;

    [Header("Debug")]
    public bool pumpEnabled = false;

    [SyncVar] bool particlesActive = false;

    void Update() {
        if (isServer) {
            if (pumpEnabled && battery && battery.TryConsumeCharge(energyConsumptionPerSecond * Time.deltaTime)) {
                YachtManager.I.AddSinkingProgress(-sinkingProgressDecreasePerSecond * Time.deltaTime);
                particlesActive = true;
            } else particlesActive = false;
        }
        if (_particleSystem) {
            if (particlesActive && !_particleSystem.isPlaying) _particleSystem.Play();
            else if (!particlesActive && _particleSystem.isPlaying) _particleSystem.Stop();
        }
    }

    [Server]
    override public void Interact(Player player, ItemInstance item) {
        pumpEnabled = !pumpEnabled;
    }
}
