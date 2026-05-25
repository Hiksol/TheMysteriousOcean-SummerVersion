using Mirror;
using UnityEngine;

[RequireComponent(typeof(PlayerController))]
[RequireComponent(typeof(Inventory))]
public class Player : NetworkBehaviour
{
    public PlayerController PlayerController { get; private set; }
    public Inventory Inventory { get; private set; }

    void Awake() {
        PlayerController = GetComponent<PlayerController>();
        Inventory = GetComponent<Inventory>();
    }
}
