using System.Collections;
using Mirror;
using UnityEngine;

public class ParentOnCollision : NetworkBehaviour
{
    void OnCollisionEnter(Collision collision) {
        if (!isServer) return;
        if (collision.collider.CompareTag("Player")) return;
        if (TryGetComponent(out ItemInstance item) && item.itemData == null) return;
        if (transform.parent == null) {
            if (TryGetComponent(out Rigidbody rb)) rb.isKinematic = true;
            transform.SetParent(collision.transform);
            RpcParentThisToTransform(collision.transform);
            StartCoroutine(ResetRotation());
        }
    }

    [ClientRpc]
    void RpcParentThisToTransform(Transform tr) {
        transform.SetParent(tr);
    }

    IEnumerator ResetRotation() {
        Vector3 localEuler = transform.localEulerAngles;
        localEuler.x = localEuler.z = 0;
        transform.localEulerAngles = localEuler;
        yield return null;
    }
}
