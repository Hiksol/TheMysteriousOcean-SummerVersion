using Mirror;
using UnityEngine;

[CreateAssetMenu(fileName = "ItemData", menuName = "Scriptable Objects/ItemData")]
public class ItemData : ScriptableObject
{
    public string itemName;
    public string itemDescription;
    public GameObject modelPrefab;
    public ItemFuelType itemFuelType;
}

public enum ItemFuelType {
    None,
    Fuel,
    Heat,
    Bio
}

public static class ItemDataSerializer {
    public static void WriteItemData(this NetworkWriter writer, ItemData value) {
        if (value) writer.WriteString(value.name);
    }

    public static ItemData ReadItemData(this NetworkReader reader) {
        return Resources.Load<ItemData>(reader.ReadString());
    }
}
