using System;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

public static class ResourcesUtils
{
    static readonly Dictionary<Type, Dictionary<string, object>> loadedObjects = new();

    public static T LoadByName<T>(string name) where T : UnityEngine.Object {
        if (string.IsNullOrEmpty(name)) return null;
        Type t = typeof(T);
        if (loadedObjects.TryGetValue(t, out Dictionary<string, object> objects)) {
            return (T)objects[name];
        } else {
            Dictionary<string, object> newObjects = new();
            foreach (T obj in Resources.FindObjectsOfTypeAll<T>()) {
                newObjects[obj.name] = obj;
            }
            return (T)newObjects[name];
        }
    }
}

public static class SONetworkSerializer<T> where T : ScriptableObject {
    public static void WriteSO(NetworkWriter writer, T value) {
        writer.WriteString(value != null ? value.name : string.Empty);
    }

    public static T ReadSO(NetworkReader reader) {
        string name = reader.ReadString();
        return ResourcesUtils.LoadByName<T>(name);
    }
}