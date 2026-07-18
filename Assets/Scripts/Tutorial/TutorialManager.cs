using System.Collections.Generic;
using Mirror;
using TMPro;
using UnityEngine;

public class TutorialManager : MonoBehaviour
{
    [SerializeReference, SubclassSelector] public List<TutorialState> tutorialStates = new();
    public TMP_Text tutorialText;
    public Transform tutorialTextRoot;

    [Header("Debug")]
    [SerializeReference] public TutorialState currentState;
    public int currentStateInd = 0;

    public void ChangeState(TutorialState newState) {
        currentState?.OnExit();
        currentState = newState;
        if (currentState != null) {
            tutorialTextRoot.gameObject.SetActive(true);
            tutorialText.text = currentState.tutorialText;
            currentState.OnEnter();
        } else tutorialTextRoot.gameObject.SetActive(false);
    }
    public void ChangeState(int ind) {
        ChangeState(tutorialStates[ind]);
    }

    void Awake() {
        NetworkManager.singleton.StartHost();
    }

    void Start() {
        ChangeState(currentStateInd);
    }

    void Update() {
        if (currentState != null && currentState.IsComplete()) {
            if (++currentStateInd < tutorialStates.Count) {
                ChangeState(currentStateInd);
            } else ChangeState(null);
        }
    }
}
