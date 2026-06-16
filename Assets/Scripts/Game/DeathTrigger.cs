using Mirror;
using UnityEngine;

public class DeathTrigger : NetworkBehaviour
{
    void OnTriggerEnter(Collider other) {
        if (!isServer) return;
        if (!other.CompareTag("Player")) return;
        other.GetComponent<Player>().Die();
    }
}
