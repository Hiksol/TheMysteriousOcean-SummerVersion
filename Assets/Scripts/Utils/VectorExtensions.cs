using UnityEngine;

public static class VectorExtensions
{
    public static Vector3 Invert(this Vector3 v) {
        return new(1 / v.x, 1 / v.y, 1 / v.z);
    }

    public static Vector3 Abs(this Vector3 v) {
        for (int i = 0; i < 3; i++) v[i] = Mathf.Abs(v[i]);
        return v;
    }
}