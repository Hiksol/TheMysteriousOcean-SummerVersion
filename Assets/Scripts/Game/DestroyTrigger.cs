using Mirror;
using UnityEngine;

public class DestroyTrigger : NetworkBehaviour
{
    void OnTriggerEnter(Collider other) {
        if (!isServer) return;
        if (other.CompareTag("Player")) {
            other.GetComponent<Player>().Die();
        } else if (other.CompareTag("Island")) {
            other.GetComponent<Island>().Remove();
        } else if (other.CompareTag("Item")) {
            other.GetComponent<ItemInstance>().Remove();
        }
    }
}
