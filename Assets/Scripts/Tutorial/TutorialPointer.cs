using UnityEngine;
using UnityEngine.UIElements.Experimental;

[RequireComponent(typeof(MeshRenderer))]
public class TutorialPointer : SingletonMonoBehaviour<TutorialPointer>
{
    public float heightDiff = 0.25f;
    public float heightOffset = 1f;
    public float timeToReachEnd = 1f;

    [Header("Debug")]
    public bool isActive = false;
    public Transform point;
    public bool goingUp = true;
    public float currentT = 0f;

    MeshRenderer meshRenderer;

    protected override void AwakeNew() {
        meshRenderer = GetComponent<MeshRenderer>();
        meshRenderer.enabled = false;
    }

    public void PointTo(Transform point) {
        this.point = point;
        isActive = true;
        ResetPointer();
        meshRenderer.enabled = true;
    }

    public void Hide() {
        isActive = false;
        meshRenderer.enabled = false;
    }

    public void ResetPointer() {
        goingUp = true;
        currentT = 0f;
        SetPosition(F(currentT));
    }

    void SetPosition(float t) {
        Vector3 heightVector = Vector3.up * heightDiff;
        transform.position = Vector3.Lerp(point.position - heightVector, point.position + heightVector, t) + Vector3.up * heightOffset;
    }

    float F(float t) {
        t = t / (2 * timeToReachEnd) + 0.5f;
        // return t < 0.5 ? 2 * Mathf.Pow(t, 2) : 1 - Mathf.Pow(-2 * t + 2, 2) / 2;
        return Easing.InOutQuad(t);
    }

    void Update() {
        if (isActive) {
            currentT += goingUp ? Time.deltaTime : -Time.deltaTime;
            currentT = Mathf.Clamp(currentT, -timeToReachEnd, timeToReachEnd);
            if (goingUp && currentT == timeToReachEnd || !goingUp && currentT == -timeToReachEnd) goingUp = !goingUp;
            SetPosition(F(currentT));
        }
    }
}
