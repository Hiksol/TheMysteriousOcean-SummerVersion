using System.Collections.Generic;
using Mirror;
using TMPro;
using UnityEngine;

public class TutorialManager : SingletonMonoBehaviour<TutorialManager>
{
    [SerializeReference, SubclassSelector] public List<TutorialState> tutorialStates = new();
    public TMP_Text tutorialText;
    public TMP_Text additionalTutorialText;
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

    override protected void AwakeNew() {
        NetworkManager.singleton.StartHost();
    }

    void Start() {
        SetAdditionalText(null);
        ChangeState(currentStateInd);
    }

    void Update() {
        currentState?.OnUpdate();
        if (currentState != null && currentState.IsComplete()) {
            if (++currentStateInd < tutorialStates.Count) {
                ChangeState(currentStateInd);
            } else ChangeState(null);
        }
    }

    public void SetAdditionalText(string text) {
        if (!string.IsNullOrEmpty(text)) {
            additionalTutorialText.text = text;
            additionalTutorialText.enabled = true;
        } else {
            additionalTutorialText.text = "";
            additionalTutorialText.enabled = false;
        }
    }
}
