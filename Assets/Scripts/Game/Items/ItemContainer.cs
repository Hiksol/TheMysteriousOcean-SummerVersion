using System;
using System.Collections.Generic;
using System.Linq;
using Mirror;

[Serializable]
public class ItemContainer {
    public ItemInstance containerItem;
    public int capacity;
    public List<ItemSlotInfo> containerSlots;

    public int Count => containerSlots.Count(slot => slot.item != null);

    public ItemContainer() {}
    public ItemContainer(int capacity) {
        containerItem = null;
        this.capacity = capacity;
        containerSlots = Utils.CreateItems<ItemSlotInfo>(capacity).ToList();
    }
    public ItemContainer(ItemInstance containerItem, int capacity) {
        this.containerItem = containerItem;
        this.capacity = capacity;
        containerSlots = Utils.CreateItems<ItemSlotInfo>(capacity).ToList();
    }

    public int FindFreeIndex(int itemSlotCount) {
        int freeSlot = -1;
        if (itemSlotCount < 1) return freeSlot;
        for (int i = 0; i < capacity - itemSlotCount + 1; i++) {
            for (int j = i; j < i + itemSlotCount; j++) {
                if (containerSlots[j].IsOccupied) {
                    i = j;
                    goto slotsOccupied;
                }
            }
            freeSlot = i;
            break;
            slotsOccupied: continue;
        }
        return freeSlot;
    }

    public void InsertItemForce(ItemInstance item, int ind) {
        if (ind < 0 || ind > capacity) return;
        for (int i = 0; i < item.itemData.slotCount; i++) {
            if (i == 0) containerSlots[ind + i].item = item;
            containerSlots[ind + i].occupiedByDelta = i;
        }
    }

    public ItemInstance GetItem(int ind) {
        if (ind < 0 || ind > capacity) return null;
        ItemSlotInfo itemSlotInfo = containerSlots[ind];
        return itemSlotInfo.occupiedByDelta switch {
            -1 => null,
            _ => containerSlots[ind - itemSlotInfo.occupiedByDelta].item
        };
    }

    public ItemInstance FreeSlot(int ind) {
        if (ind < 0 || ind > capacity) return null;
        ItemInstance item = GetItem(ind);
        if (item == null) return null;
        int startInd = ind - containerSlots[ind].occupiedByDelta;
        for (int i = startInd; i < startInd + item.itemData.slotCount; i++) containerSlots[i].Reset();
        return item;
    }

    public void DestroyItem(int ind) {
        if (ind < 0 || ind > capacity) return;
        ItemInstance item = FreeSlot(ind);
        if (item == null) return;
        NetworkServer.UnSpawn(item.gameObject);
        UnityEngine.Object.Destroy(item.gameObject);
    }

    public bool IsSlotFreeForPotentialItem(int ind, ItemInstance potentialItem) {
        if (potentialItem == null) return true;
        int itemSlotCount = potentialItem.itemData.slotCount;
        if (ind + itemSlotCount > capacity) return false;
        ItemInstance startItem = GetItem(ind);
        for (int i = ind + 1; i < ind + itemSlotCount; i++) {
            ItemInstance item = GetItem(i);
            if (item != null && item != startItem) return false;
        }
        return true;
    }

    public int FirstItemInd() {
        for (int i = 0; i < capacity; i++)
            if (containerSlots[i].item != null) return i;
        return -1;
    }

    public IEnumerable<ItemInstance> GetAllItems() {
        foreach (ItemSlotInfo itemSlotInfo in containerSlots)
            if (itemSlotInfo.item != null) yield return itemSlotInfo.item;
    }
}

[Serializable]
public class ItemSlotInfo {
    public ItemInstance item;
    public int occupiedByDelta = -1;

    public bool IsOccupied => occupiedByDelta >= 0;
    public bool IsOccupiedByItself => occupiedByDelta == 0;
    public bool IsOccupiedByAnotherSlot => occupiedByDelta >= 1;

    public ItemSlotInfo() {}

    public void Reset() {
        item = null;
        occupiedByDelta = -1;
    }
}