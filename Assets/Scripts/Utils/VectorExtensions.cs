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

    public static Vector3 WithX(this Vector3 v, float x) {
        return new(x, v.y, v.z);
    }
    public static Vector3 WithX(this Vector3 v, Vector3 v1) {
        return v.WithX(v1.x);
    }

    public static Vector3 WithY(this Vector3 v, float y) {
        return new(v.x, y, v.z);
    }
    public static Vector3 WithY(this Vector3 v, Vector3 v1) {
        return v.WithY(v1.y);
    }

    public static Vector3 WithZ(this Vector3 v, float z) {
        return new(v.x, v.y, z);
    }
    public static Vector3 WithZ(this Vector3 v, Vector3 v1) {
        return v.WithZ(v1.z);
    }

    public static Vector3 WithXY(this Vector3 v, float x, float y) {
        return new(x, y, v.z);
    }
    public static Vector3 WithXY(this Vector3 v, Vector3 v1) {
        return v.WithXY(v1.x, v1.y);
    }

    public static Vector3 WithXZ(this Vector3 v, float x, float z) {
        return new(x, v.y, z);
    }
    public static Vector3 WithXZ(this Vector3 v, Vector3 v1) {
        return v.WithXZ(v1.x, v1.z);
    }

    public static Vector3 WithYZ(this Vector3 v, float y, float z) {
        return new(v.x, y, z);
    }
    public static Vector3 WithYZ(this Vector3 v, Vector3 v1) {
        return v.WithYZ(v1.y, v1.z);
    }
}