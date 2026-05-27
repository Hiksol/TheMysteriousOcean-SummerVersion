using Mirror;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Island : NetworkBehaviour
{
    public Vector3 velocity;
    public float timeToLive = 60f;

    [Header("Debug")]
    public float currentTimeLiving = 0f;

    Rigidbody rb;

    void Awake() {
        rb = GetComponent<Rigidbody>();
    }

    void FixedUpdate() {
        if (!isServer) return;
        rb.MovePosition(transform.position + velocity * Time.fixedDeltaTime);
        currentTimeLiving += Time.deltaTime;
        if (currentTimeLiving >= timeToLive) {
            NetworkServer.UnSpawn(gameObject);
            Destroy(gameObject);
        }
    }

    void OnCollisionEnter(Collision collision) {
        print(collision.gameObject);
    }
}
