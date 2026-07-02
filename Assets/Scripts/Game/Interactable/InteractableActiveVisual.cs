using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(InteractableActive))]
public class InteractableActiveVisual : MonoBehaviour
{
    public List<ParticleSystem> particleSystems = new();
    public List<AudioSource> audioSources = new();
    public UnityEvent onWorkingOn = new();
    public UnityEvent onWorkingOff = new();

    InteractableActive interactableActive;

    void Awake() {
        interactableActive = GetComponent<InteractableActive>();
    }

    void OnEnable() { interactableActive.onInteractableWorkingChanged.AddListener(OnInteractableWorkingChanged); }
    void OnDisable() { interactableActive.onInteractableWorkingChanged.RemoveListener(OnInteractableWorkingChanged); }

    void OnInteractableWorkingChanged(bool isWorking) {
        if (particleSystems.Count > 0) {
            if (isWorking) particleSystems.ForEach(ps => ps.Play());
            else particleSystems.ForEach(ps => ps.Stop());
        }
        if (audioSources.Count > 0) {
            if (isWorking) audioSources.ForEach(ps => ps.Play());
            else audioSources.ForEach(ps => ps.Stop());
        }
        if (isWorking) onWorkingOn.Invoke();
        else onWorkingOff.Invoke();
    }
}
