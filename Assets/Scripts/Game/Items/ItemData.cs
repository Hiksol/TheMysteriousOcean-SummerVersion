using System.Collections.Generic;
using Mirror;
using UnityEngine;
using UnityEngine.UI;

[CreateAssetMenu(fileName = "ItemData", menuName = "Scriptable Objects/ItemData")]
public class ItemData : ScriptableObject
{
    public Sprite itemIcon;
    public string itemName;
    public string itemDescription;
    public GameObject modelPrefab;
    public int slotCount = 1;
    public ItemFuelType itemFuelType;
    public float itemFuelAmount;
    public float holdTimeToUse = 1f;
    [SerializeReference, SubclassSelector] public List<ItemProperty> itemProperties;
}

public enum ItemFuelType {
    None,
    Fuel,
    Heat,
    Bio
}

public static class ItemDataSerializer {
    public static void WriteItemData(this NetworkWriter writer, ItemData value) {
        SONetworkSerializer<ItemData>.WriteSO(writer, value);
    }

    public static ItemData ReadItemData(this NetworkReader reader) {
        return SONetworkSerializer<ItemData>.ReadSO(reader);
    }
}
