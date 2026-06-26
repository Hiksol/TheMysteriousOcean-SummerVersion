using System;
using System.Collections.Generic;
using System.Linq;

public static class IEnumarableExtensions
{
    public static IEnumerable<T> Clone<T>(this IEnumerable<T> collection) where T : ICloneable {
        return collection.Select(item => (T)item.Clone());
    }

    public static void ForEachI<T>(this IEnumerable<T> collection, Action<T, int> action) {
        int i = 0;
        foreach (T elem in collection) action.Invoke(elem, i++);
    }
}
