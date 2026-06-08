using System.Collections.Generic;
using System.Linq;
using Mirror;
using UnityEngine;

public class InventoryRenderer : NetworkBehaviour
{
    public InventoryRendererCell inventoryRendererCellPrefab;
    public float inventoryRendererCellDelta = 50f;
    public Transform inventoryRendererRoot;

    Inventory inventory;
    readonly List<InventoryRendererCell> handsCells = new();
    readonly List<InventoryRendererCell> inventoryCells = new();

    void Awake() {
        inventory = transform.parent.GetComponent<Inventory>();
    }

    void Start() {
        if (!isLocalPlayer) {
            gameObject.SetActive(false);
            return;
        }
        Utils.Repeat(Inventory.HANDS_COUNT, i => handsCells.Add(
            Instantiate(inventoryRendererCellPrefab, inventoryRendererRoot.position + Vector3.right * (i * inventoryRendererCellDelta), Quaternion.identity, inventoryRendererRoot)
        ));
        OnInventoryCapacityChange(inventory.GetInventoryCapacity());
    }

    void OnEnable() {
        inventory.onHandsChange.AddListener(OnHandsChange);
        inventory.onInventoryChange.AddListener(OnInventoryChange);
        inventory.onInventoryCapacityChange.AddListener(OnInventoryCapacityChange);
    }
    void OnDisable() {
        inventory.onHandsChange.RemoveListener(OnHandsChange);
        inventory.onInventoryChange.RemoveListener(OnInventoryChange);
        inventory.onInventoryCapacityChange.RemoveListener(OnInventoryCapacityChange);
    }

    void OnHandsChange(ItemContainer hands) {
        OnBaseInventoryChange(hands.containerSlots, handsCells);
    }

    void OnInventoryChange(List<ItemContainer> itemContainers) {
        List<ItemSlotInfo> itemContainersFlatten = itemContainers.SelectMany(ic => ic.containerSlots).ToList();
        OnBaseInventoryChange(itemContainersFlatten, inventoryCells);
    }

    void OnBaseInventoryChange(List<ItemSlotInfo> itemSlots, List<InventoryRendererCell> cells) {
        for (int i = 0; i < cells.Count; i++) {
            ItemSlotInfo itemSlotInfo = itemSlots[i];
            cells[i].SetCellText(itemSlotInfo.IsOccupiedByItself ? itemSlotInfo.item.itemData.itemName :
                                            itemSlotInfo.IsOccupiedByAnotherSlot ? "---" : "");
        }
    }

    void OnInventoryCapacityChange(int capacity) {
        while (inventoryCells.Count < capacity)
            inventoryCells.Add(Instantiate(inventoryRendererCellPrefab, inventoryRendererRoot.position + Vector3.right * ((Inventory.HANDS_COUNT + inventoryCells.Count) * inventoryRendererCellDelta), Quaternion.identity, inventoryRendererRoot));
        if (inventoryCells.Count > capacity) {
            for (int i = capacity; i < inventoryCells.Count; i++) Destroy(inventoryCells[i].gameObject);
            inventoryCells.RemoveRange(capacity, inventoryCells.Count - capacity);
        }
    }
}
