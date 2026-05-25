using System;
using System.Collections.Generic;

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
}