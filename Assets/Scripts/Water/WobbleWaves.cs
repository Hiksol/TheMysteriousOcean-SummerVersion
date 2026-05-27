using System.Collections.Generic;
using System.Linq;
using Mirror;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class WobbleWaves : NetworkBehaviour
{
    [Header("Wave Detection")]
    public float sampleDistance = 1f;
    public List<Transform> samplePoints;
    public LayerMask waterLayer = 1 << 4;
    public float raycastDistance = 10f;

    [Header("Rotation Settings")]
    public float maxTiltAngle = 15f;
    public float rotationSpeed = 2f;

    [Header("Height Settings")]
    public float heightOffset = 1f;
    public float heightSmoothness = 0.1f; 

    Rigidbody rb;

    private List<float> currentSampleHeights;
    private Vector3 targetEulerAngles;
    private float targetHeight;
    private Vector3 velocity;
    float len;

    void Awake() {
        rb = GetComponent<Rigidbody>();
    }

    void Start() {
        CreateSamplePoints();
        currentSampleHeights = new(new float[samplePoints.Count]);
        targetEulerAngles = transform.eulerAngles;
        len = sampleDistance * 2;
    }

    void Update() {
        SampleWaveHeights();
        CalculateTilt();
        ApplyRotationAndHeight();
    }

    void CreateSamplePoints() {
        // Создаем 4 точки вокруг корабля: вперед, назад, влево, вправо
        Vector3[] directions = {
            Vector3.forward,
            Vector3.back,
            Vector3.left,
            Vector3.right
        };
        samplePoints = new();
        for (int i = 0; i < directions.Length; i++) {
            GameObject point = new($"SamplePoint_{i}");
            point.transform.SetParent(transform);
            point.transform.localPosition = directions[i] * sampleDistance;
            samplePoints.Add(point.transform);
        }
    }

    void SampleWaveHeights() {
        float maxHeight = samplePoints.Max(tr => tr.position.y);
        for (int i = 0; i < samplePoints.Count; i++) {
            Vector3 worldPos = samplePoints[i].position;
            Vector3 rayStart = new(worldPos.x, maxHeight, worldPos.z);

            if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, raycastDistance, waterLayer)) currentSampleHeights[i] = hit.point.y;
            else currentSampleHeights[i] = 0;
        }
    }

    float AsinSafe(float f) {
        return Mathf.Asin(Mathf.Clamp(f, -1f, 1f));
    }

    void CalculateTilt() {
        if (currentSampleHeights.Count < 3) return;

        // Вычисляем наклон по оси X (вперед-назад)
        float frontHeight = currentSampleHeights[0];
        float backHeight = currentSampleHeights[1];
        float frontBackDiff = backHeight - frontHeight;
        float pitchTilt = AsinSafe(frontBackDiff / len) * Mathf.Rad2Deg;
        float pitchTiltClamped = Mathf.Clamp(pitchTilt, -maxTiltAngle, maxTiltAngle);

        // Вычисляем наклон по оси Z (влево-вправо)
        float leftHeight = currentSampleHeights[2];
        float rightHeight = currentSampleHeights[3];
        float leftRightDiff = rightHeight - leftHeight;
        float rollTilt = AsinSafe(leftRightDiff / len) * Mathf.Rad2Deg;
        float rollTiltClamped = Mathf.Clamp(rollTilt, -maxTiltAngle, maxTiltAngle);

        // Устанавливаем целевые углы поворота
        targetEulerAngles = new Vector3(pitchTiltClamped, transform.localEulerAngles.y, rollTiltClamped);

        // Вычисляем среднюю высоту волн под кораблем для определения высоты корабля
        float averageHeight = currentSampleHeights.Average();
        targetHeight = averageHeight + heightOffset;
    }

    void ApplyRotationAndHeight() {
        // Плавно поворачиваем корабль
        Vector3 currentEuler = transform.localEulerAngles;
        if (currentEuler.x > 180) currentEuler.x -= 360;
        if (currentEuler.z > 180) currentEuler.z -= 360;
        Vector3 targetRotation = Vector3.MoveTowards(currentEuler, targetEulerAngles, rotationSpeed * Time.deltaTime);
        // print($"{currentEuler} {targetEulerAngles} {rotationSpeed * Time.deltaTime}");

        // Плавно изменяем высоту корабля
        Vector3 targetPosition = new(transform.position.x, targetHeight, transform.position.z);
        Vector3 smoothPosition = Vector3.SmoothDamp(transform.position, targetPosition, ref velocity, heightSmoothness);
        rb.Move(smoothPosition, Quaternion.Euler(targetRotation));
    }

    // Визуализация точек измерения в редакторе
    void OnDrawGizmosSelected() {
        if (samplePoints != null) {
            Gizmos.color = Color.yellow;
            foreach (Transform point in samplePoints)
                if (point != null) {
                    Gizmos.DrawSphere(point.position, 0.2f);
                    Gizmos.DrawLine(transform.position, point.position);
                }
        }
    }
}