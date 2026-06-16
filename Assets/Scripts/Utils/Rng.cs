using System;
using System.Collections.Generic;
using System.Linq;
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
    public float Range(float maxInc) {
        return Range(0f, maxInc);
    }

    public int RangeInt(int minInc, int maxExc) {
        return random.Next(minInc, maxExc);
    }
    public int RangeInt(int maxExc) {
        return random.Next(maxExc);
    }

    public Vector3 Vector3Abs(float x, float y, float z) {
        return new(Range(-x, x), Range(-y, y), Range(-z, z));
    }
    public Vector3 Vector3Abs(Vector3 v) {
        return Vector3Abs(v.x, v.y, v.z);
    }

    public T RandomItem<T>(IEnumerable<T> values) {
        int count = values.Count();
        if (count == 0) return default;
        return values.ElementAt(RangeInt(count));
    }

    public T RandomWeightedItem<T>(IEnumerable<T> values, Func<T, float> weightFunc) {
        IEnumerable<(T value, float weight)> weightedValues = values.Select(v => (value: v, weight: weightFunc.Invoke(v)));
        float weightsSum = weightedValues.Sum(v => v.weight);
        float selectedWeight = Range(weightsSum);
        float currentWeightsSum = 0;
        foreach (var (value, weight) in weightedValues) {
            currentWeightsSum += weight;
            if (selectedWeight <= currentWeightsSum) return value;
        }
        return default;
    }

    public List<T> Shuffle<T>(IEnumerable<T> values) {
        List<T> list = values.ToList();
        int n = list.Count;
        while (n > 1) {
            n--;
            int k = RangeInt(n + 1);
            (list[n], list[k]) = (list[k], list[n]);
        }
        return list;
    }
}