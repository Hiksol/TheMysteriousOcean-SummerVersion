using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class TestJitter : MonoBehaviour
{
    public float maxAngle = 30f;
    public float angleSpeed = 10f;

    public Vector3 currentEuler;
    public bool movingUp = true;

    Rigidbody rb;

    void Awake() {
        rb = GetComponent<Rigidbody>();
    }

    void FixedUpdate() {
        currentEuler = Vector3.MoveTowards(currentEuler, new(movingUp ? maxAngle : -maxAngle, 0, 0), angleSpeed * Time.fixedDeltaTime);
        if (movingUp && currentEuler.x == maxAngle || !movingUp && currentEuler.x == -maxAngle) movingUp = !movingUp;
        rb.MoveRotation(Quaternion.Euler(currentEuler));
    }
}
