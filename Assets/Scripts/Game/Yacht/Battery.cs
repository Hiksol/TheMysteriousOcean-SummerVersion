using Mirror;
using TMPro;
using UnityEngine;

public class Battery : NetworkBehaviour
{
    public float maxCharge = 300f;
    public TMP_Text chargeText;

    [Header("Debug")]
    [SyncVar] public float currentCharge;

    void Update() {
        chargeText.text = ((int)currentCharge).ToString();
    }

    [Server]
    public void AddCharge(float charge) {
        currentCharge = Mathf.Min(currentCharge + charge, maxCharge);
    }

    public bool TryConsumeCharge(float charge) {
        if (currentCharge >= charge) {
            currentCharge -= charge;
            return true;
        }
        return false;
    }
}
