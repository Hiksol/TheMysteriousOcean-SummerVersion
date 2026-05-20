using UnityEngine;
using Random = System.Random;

public class Rng {
    readonly Random random;

    public Rng(int seed) {
        random = new(seed);
    }

    public float Next() {
        return (float)random.NextDouble();
    }

    public float Range(float minInc, float maxInc) {
        return Next() * (maxInc - minInc) + minInc;
    }

    public int RangeInt(int minInc, int maxExc) {
        return random.Next(minInc, maxExc);
    }

    public Vector3 Vector3Abs(float x, float y, float z) {
        return new(Range(-x, x), Range(-y, y), Range(-z, z));
    }
    public Vector3 Vector3Abs(Vector3 v) {
        return Vector3Abs(v.x, v.y, v.z);
    }
}