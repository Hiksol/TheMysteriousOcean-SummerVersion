using System;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

[CreateAssetMenu(fileName = "ItemWeightedList", menuName = "Scriptable Objects/ItemWeightedList")]
public class ItemWeightedList : ScriptableObject
{
    [Serializable] public class ItemTier { public string tierName = "Tier"; public List<ItemData> itemDatas; public float weight; }
    public List<ItemTier> items;
}

public static class ItemWeightedListSerializer {
    public static void WriteItemWeightedList(this NetworkWriter writer, ItemWeightedList value) {
        SONetworkSerializer<ItemWeightedList>.WriteSO(writer, value);
    }

    public static ItemWeightedList ReadItemWeightedList(this NetworkReader reader) {
        return SONetworkSerializer<ItemWeightedList>.ReadSO(reader);
    }
}
