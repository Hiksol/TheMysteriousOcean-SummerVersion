using System.Collections;
using Mirror;
using UnityEngine;

public class ParentOnCollision : NetworkBehaviour
{
    void OnCollisionEnter(Collision collision) {
        // if (!isServer) return;
        if (collision.collider.CompareTag("Player") || collision.collider.TryGetComponent(out ItemInstance _)) return;
        if (TryGetComponent(out ItemInstance item) && item.itemData == null) return;
        if (transform.parent == null) {
            transform.GetLocalPositionAndRotation(out Vector3 localPosition, out Quaternion localRotation);
            Vector3 newPosition = Vector3.Scale(localPosition - collision.transform.position, collision.transform.lossyScale.Invert());
            print(transform.localScale);
            Vector3 localScale = transform.localScale;
            transform.SetParent(collision.transform);
            transform.SetLocalPositionAndRotation(newPosition, localRotation);
            transform.localScale = Vector3.Scale(localScale, collision.transform.lossyScale.Invert());
            Vector3 localEuler = transform.localEulerAngles;
            localEuler.y = 0;
            transform.Rotate(-localEuler);
            if (isServer) {
                if (TryGetComponent(out Rigidbody rb)) rb.isKinematic = true;
                // RpcParentThisToTransform(collision.transform);
                RpcParentThisToTransform(collision.transform, transform.localPosition, transform.localRotation, transform.localScale);
                // StartCoroutine(ResetRotation());
            }
        }
    }

    [ClientRpc]
    void RpcParentThisToTransform(Transform tr, Vector3 localPosition, Quaternion localRotation, Vector3 localScale) {
        transform.SetParent(tr);
        transform.SetLocalPositionAndRotation(localPosition, localRotation);
        transform.localScale = localScale;
    }

    [ClientRpc]
    void RpcParentThisToTransform(Transform tr) {
        transform.SetParent(tr);
    }

    IEnumerator ResetRotation() {
        yield return null;
        Vector3 localEuler = transform.localEulerAngles;
        // localEuler.x = localEuler.z = 0;
        localEuler.y = 0;
        // transform.localEulerAngles = localEuler;
        transform.Rotate(-localEuler);
    }
}
