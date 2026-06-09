using Mirror;
using UnityEngine;

public class DropRestrictionZone : NetworkBehaviour
{
    void OnTriggerEnter(Collider other) {
        AddRestrictionDeltaSafe(other, 1);
    }

    void OnTriggerExit(Collider other) {
        AddRestrictionDeltaSafe(other, -1);
    }

    void AddRestrictionDeltaSafe(Collider collider, int delta) {
        if (!collider.CompareTag("Player")) return;
        collider.GetComponent<Player>().Inventory.AddDropRestriction(delta);
    }
}
