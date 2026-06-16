using System;
using System.Collections.Generic;
using UnityEngine;

public static class Utils
{
    public static void Repeat(int n, Action<int> action) {
        for (int i = 0; i < n; i++) action(i);
    }

    public static IEnumerable<T> CreateItems<T> (int count) where T : new() {
        return CreateItems(count, () => new T());
    }

    public static IEnumerable<T> CreateItems<T> (int count, Func<T> creator) {
        for (int i = 0; i < count; i++) {
            yield return creator();
        }
    }

    public static int GetPhysicsLayerMask(int layer) {
        int layerMask = 0;
        for (int i = 0; i < 32; i++) {
            if (!Physics.GetIgnoreLayerCollision(layer, i)) layerMask |= 1 << i;
        }
        return layerMask;
    }

    public static Vector3 VectorAbs(Vector3 v) {
        for (int i = 0; i < 3; i++) v[i] = Mathf.Abs(v[i]);
        return v;
    }
}