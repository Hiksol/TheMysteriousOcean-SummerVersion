using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(InteractableActive))]
public class InteractableActiveVisual : MonoBehaviour
{
    public List<ParticleSystem> particleSystems;

    InteractableActive interactableActive;

    void Awake() {
        interactableActive = GetComponent<InteractableActive>();
    }

    void OnEnable() { interactableActive.onInteractableWorkingChanged.AddListener(OnInteractableWorkingChanged); }
    void OnDisable() { interactableActive.onInteractableWorkingChanged.RemoveListener(OnInteractableWorkingChanged); }

    void OnInteractableWorkingChanged(bool isWorking) {
        if (isWorking) particleSystems.ForEach(ps => ps.Play());
        else particleSystems.ForEach(ps => ps.Stop());
    }
}
