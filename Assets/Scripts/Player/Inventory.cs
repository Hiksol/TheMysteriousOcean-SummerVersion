using System;
using System.Collections.Generic;
using System.Linq;
using Mirror;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

[RequireComponent(typeof(Player))]
public class Inventory : NetworkBehaviour
{
    public float interactionRange = 3f;
    const int RIGHT_HAND_IND = 0;
    const int LEFT_HAND_IND = 1;
    const int HANDS_COUNT = 2;
    [SyncVar] public ItemContainer hands;
    public List<Transform> handPoints;
    public int baseInventorySize = 2;
    [SyncVar] public List<ItemContainer> inventoryContainers = new();

    Player player;
    Transform hiddenRoot;

    InputAction interactAction;
    InputAction useAction;
    List<KeyControl> inventoryKeys;

    void Awake() {
        player = GetComponent<Player>();
        hands = new(HANDS_COUNT);
        inventoryContainers = new() { new(baseInventorySize) }; 
        hiddenRoot = new GameObject("HiddenRoot").transform;
        hiddenRoot.SetParent(transform);
        hiddenRoot.localPosition = Vector3.zero;
        interactAction = InputSystem.actions.FindAction("Interact");
        useAction = InputSystem.actions.FindAction("Attack");
        inventoryKeys = new() {
            Keyboard.current.digit1Key, Keyboard.current.digit2Key,
            Keyboard.current.digit3Key, Keyboard.current.digit4Key,
            Keyboard.current.digit5Key, Keyboard.current.digit6Key,
            Keyboard.current.digit7Key, Keyboard.current.digit8Key,
            Keyboard.current.digit9Key, Keyboard.current.digit0Key
        };
    }

    void Update() {
        if (!isLocalPlayer) return;
        HandleInteract();
        HandleSwitchItems();
    }

    [Client]
    void HandleInteract() {
        bool interactWasPressed = interactAction.WasPressedThisFrame();
        bool useWasPressed = useAction.WasPressedThisFrame();
        if (!interactWasPressed && !useWasPressed) return;
        bool wasHit = Physics.Raycast(Camera.main.transform.position, Camera.main.transform.forward, out RaycastHit hit, interactionRange);
        GameObject go = wasHit ? hit.collider.gameObject : null;
        if (wasHit && interactWasPressed && go.TryGetComponent(out ItemInstance item)) CmdTryPickupItem(item);
        else if (useWasPressed) {
            if (wasHit && go.TryGetComponent(out IInteractable interactable)) interactable.CmdInteract(player);
            else if (GetItemInRightHand() is ItemInstance rightHandItem && rightHandItem != null) rightHandItem.Use(player, null);
        }
    }

    [Client]
    void HandleSwitchItems() {
        int keyInd = inventoryKeys.FindIndex(key => key.wasPressedThisFrame);
        if (keyInd == -1) return;
        int slotsChecked = 0, inventoryIndex, slotIndex = -1;
        for (inventoryIndex = 0; inventoryIndex < inventoryContainers.Count; inventoryIndex++) {
            if (keyInd - slotsChecked < inventoryContainers[inventoryIndex].capacity) {
                slotIndex = keyInd - slotsChecked;
                break;
            }
            slotsChecked += inventoryContainers[inventoryIndex].capacity;
        }
        if (slotIndex == -1) return;
        CmdTrySwapItemsHandsInventory(RIGHT_HAND_IND, inventoryIndex, slotIndex);
    }

    [Command]
    void CmdTryPickupItem(ItemInstance item) {
        // item.netIdentity.AssignClientAuthority(connectionToClient);
        ItemData itemData = item.itemData;
        int ind = hands.FindFreeIndex(itemData.slotCount);
        if (ind == RIGHT_HAND_IND) {
            InsertItemIntoContainer(item, hands, ind);
        } else {
            foreach (ItemContainer itemContainer in inventoryContainers) {
                ind = itemContainer.FindFreeIndex(itemData.slotCount);
                if (ind != -1) {
                    InsertItemIntoContainer(item, itemContainer, ind);
                    break;
                }
            }
        }
    }

    [Command]
    void CmdTrySwapItemsHandsInventory(int handSlotIndex, int inventoryIndex, int inventorySlotIndex) {
        TrySwapItems(hands, handSlotIndex, inventoryContainers[inventoryIndex], inventorySlotIndex);
    }

    [Server]
    void TrySwapItems(ItemContainer container1, int slotIndex1, ItemContainer container2, int slotIndex2) {
        ItemInstance item1 = container1.GetItem(slotIndex1);
        ItemInstance item2 = container2.GetItem(slotIndex2);
        if (!container1.IsSlotFreeForPotentialItem(slotIndex1, item2) ||
            !container2.IsSlotFreeForPotentialItem(slotIndex2, item1)) return;
        container1.FreeSlot(slotIndex1);
        container2.FreeSlot(slotIndex2);
        if (item1 != null) InsertItemIntoContainer(item1, container2, slotIndex2);
        if (item2 != null) InsertItemIntoContainer(item2, container1, slotIndex1);
    }

    [Server]
    void InsertItemIntoContainer(ItemInstance item, ItemContainer itemContainer, int slotIndex) {
        if (itemContainer.IsSlotFreeForPotentialItem(slotIndex, item)) {
            bool isHands = itemContainer == hands;
            item.transform.SetParent(null);
            itemContainer.InsertItemForce(item, slotIndex);
            Vector3 pos = isHands ? handPoints[slotIndex].position : Vector3.zero;
            item.netIdentity.AssignClientAuthority(connectionToClient);
            ParentItemToPlayer(item, !isHands, pos);
            RpcParentItemToPlayer(item, !isHands, pos);
        }
    }

    [Command]
    public void CmdEquipContainer(ItemInstance item) {
        ItemPropertyEquipableContainer equipable = item.itemData.itemProperties.OfType<ItemPropertyEquipableContainer>().FirstOrDefault();
        if (equipable == null) return;
        inventoryContainers.Add(new(item, equipable.capacity));
        item.transform.SetParent(hiddenRoot);
        item.netIdentity.AssignClientAuthority(connectionToClient);
        RpcParentItemToPlayer(item, true, Vector3.zero);
    }

    [ClientRpc]
    void RpcParentItemToPlayer(ItemInstance item, bool hide, Vector3 pos) {
        ParentItemToPlayer(item, hide, pos);
    }

    void ParentItemToPlayer(ItemInstance item, bool hide, Vector3 pos) {
        item.transform.SetParent(hiddenRoot);
        item.transform.position = hide ? Vector3.zero : pos;
        item.transform.localRotation = Quaternion.identity;
        item.gameObject.SetActive(!hide);
        item.GetComponent<Rigidbody>().isKinematic = true;
        item.GetComponent<Collider>().enabled = false;
    }

    public ItemInstance GetItemInRightHand() {
        return hands.GetItem(RIGHT_HAND_IND);
    }

    [Server]
    public void DestroyItemInRightHand() {
        hands.DestroyItem(RIGHT_HAND_IND);
    } 

    [Serializable] public class ItemContainer {
        public ItemInstance containerItem;
        public int capacity;
        public List<ItemSlotInfo> containerSlots;

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
            Destroy(item.gameObject);
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
    }

    [Serializable] public class ItemSlotInfo {
        public ItemInstance item;
        public int occupiedByDelta = -1;
        public bool IsOccupied => occupiedByDelta >= 0;

        public ItemSlotInfo() {}

        public void Reset() {
            item = null;
            occupiedByDelta = -1;
        }
    }
}
