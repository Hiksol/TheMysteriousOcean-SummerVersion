using System.Collections.Generic;
using System.Linq;
using Mirror;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

[RequireComponent(typeof(Player))]
public class Inventory : NetworkBehaviour
{
    public float interactionRange = 3f;
    const int RIGHT_HAND_IND = 0;
    const int LEFT_HAND_IND = 1;
    public const int HANDS_COUNT = 2;
    [SyncVar(hook = nameof(OnHandsChanged))] public ItemContainer hands;
    public List<Transform> handPoints;
    public int baseInventorySize = 2;
    // [SyncVar(hook = nameof(OnInventoryContainersChanged))] public List<ItemContainer> inventoryContainers = new();
    readonly public SyncList<ItemContainer> inventoryContainers = new();
    public LayerMask waterLayer = 1 << 4;

    [Header("Debug")]
    public int dropRestriction = 0;
    public bool CanDrop => dropRestriction == 0;

    Player player;
    Transform hiddenRoot;

    InputAction interactAction;
    InputAction useAction;
    InputAction dropAction;
    List<KeyControl> inventoryKeys;
    int lastItemCount;

    public UnityEvent<ItemContainer> onHandsChange;
    // public UnityEvent<List<ItemContainer>> onInventoryChange;
    public UnityEvent<List<ItemContainer>> onInventoryChange;
    public UnityEvent<int> onInventoryCapacityChange;

    void Awake() {
        player = GetComponent<Player>();
        hands = new(HANDS_COUNT);
        // inventoryContainers = new() { new(baseInventorySize) };
        inventoryContainers.Add(new(baseInventorySize));
        inventoryContainers.OnSet += OnInventoryContainersChanged;
        hiddenRoot = new GameObject("HiddenRoot").transform;
        hiddenRoot.SetParent(transform);
        hiddenRoot.localPosition = Vector3.zero;
        interactAction = InputSystem.actions.FindAction("Interact");
        useAction = InputSystem.actions.FindAction("Attack");
        dropAction = InputSystem.actions.FindAction("Drop");
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
        CheckItemCount();
        if (player.playerState == PlayerState.Default) {
            HandleInteract();
            HandleSwitchItems();
            HandleDropItem();
        }
    }

    [Client]
    void CheckItemCount() {
        int itemCount = hands.Count + inventoryContainers.Sum(ic => ic.Count);
        if (itemCount != lastItemCount) {
            onHandsChange.Invoke(hands);
            onInventoryChange.Invoke(inventoryContainers.ToList());
        }
        lastItemCount = itemCount;
    }

    [Client]
    void HandleInteract() {
        bool interactWasPressed = interactAction.WasPressedThisFrame();
        bool useWasPressed = useAction.WasPressedThisFrame();
        if (!interactWasPressed && !useWasPressed) return;
        bool wasHit = Physics.Raycast(Camera.main.transform.position, Camera.main.transform.forward, out RaycastHit hit, interactionRange, Physics.DefaultRaycastLayers & ~waterLayer);
        GameObject go = wasHit ? hit.collider.gameObject : null;
        if (wasHit && interactWasPressed) {
            if (go.TryGetComponent(out ItemInstance item)) CmdTryPickupItem(item);
            else if (go.TryGetComponent(out Interactable interactable)) interactable.CmdInteract(player);
        } else if (useWasPressed) {
            if (GetItemInRightHand() is ItemInstance rightHandItem && rightHandItem != null) rightHandItem.Use(player, null);
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

    [Client]
    void HandleDropItem() {
        if (dropAction.WasPressedThisFrame()) CmdDropItemInRightHand();
    }

    [Command]
    void CmdDropItemInRightHand() {
        if (!CanDrop) {
            RpcSendNotification(connectionToClient, "You can't throw objects away here", NotificationInstance.NotificationType.Info);
            return;
        }
        DropItemInRightHand();
    }

    [TargetRpc]
    void RpcSendNotification(NetworkConnectionToClient _, string text, NotificationInstance.NotificationType notificationType) {
        NotificationManager.I.PrintNotification(text, notificationType);
    }

    [Command]
    void CmdTryPickupItem(ItemInstance item) {
        TryPickupItem(item);
    }

    [Server]
    public void TryPickupItem(ItemInstance item) {
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
        if (container1.FreeSlot(slotIndex1) != null) ForceUpdateSync(container1);
        if (container2.FreeSlot(slotIndex2) != null) ForceUpdateSync(container2);
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
            // item.netIdentity.AssignClientAuthority(connectionToClient);
            ForceUpdateSync(itemContainer);
            ParentItemToPlayer(item, !isHands, pos);
            RpcParentItemToPlayer(item, !isHands, pos);
        }
        if (isLocalPlayer) {
            OnHandsChanged(null, hands);
            OnInventoryContainersChanged(-1, null);
        }
    }

    [Command]
    public void CmdEquipContainer(ItemInstance item) {
        ItemPropertyEquipableContainer equipable = item.itemData.itemProperties.OfType<ItemPropertyEquipableContainer>().FirstOrDefault();
        if (equipable == null) return;
        inventoryContainers.Add(new(item, equipable.capacity));
        item.transform.SetParent(hiddenRoot);
        // item.netIdentity.AssignClientAuthority(connectionToClient);
        RpcParentItemToPlayer(item, true, Vector3.zero);
        onInventoryCapacityChange.Invoke(GetInventoryCapacity());
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
        if (item.TryGetComponent(out Rigidbody rb)) rb.isKinematic = true;
        item.GetComponent<Collider>().enabled = false;
    }

    [Server]
    public ItemInstance DropItemInRightHand() {
        ItemInstance item = hands.FreeSlot(0);
        if (item == null) return null;
        DropItem(item, handPoints[RIGHT_HAND_IND].position);
        ForceUpdateSync(hands);
        if (isLocalPlayer) OnHandsChanged(null, hands);
        RpcDropItem(item, handPoints[RIGHT_HAND_IND].position);
        return item;
    }

    [ClientRpc]
    void RpcDropItem(ItemInstance item, Vector3 pos) {
        DropItem(item, pos);
    }

    void DropItem(ItemInstance item, Vector3 pos) {
        if (item == null) return;
        item.transform.SetParent(null);
        item.transform.position = pos;
        item.gameObject.SetActive(true);
        if (item.TryGetComponent(out Rigidbody rb)) rb.isKinematic = false;
        item.GetComponent<Collider>().enabled = true;
    }

    public ItemInstance GetItemInRightHand() {
        return hands.GetItem(RIGHT_HAND_IND);
    }

    [Server]
    public void DestroyItemInRightHand() {
        hands.DestroyItem(RIGHT_HAND_IND);
        ForceUpdateSync(hands);
    }

    void OnHandsChanged(ItemContainer _, ItemContainer newValue) {
        onHandsChange.Invoke(newValue);
    }

    void OnInventoryContainersChanged(int i, ItemContainer newValue) {
        onInventoryChange.Invoke(inventoryContainers.ToList());
    }

    public int GetInventoryCapacity() {
        return inventoryContainers.Sum(ic => ic.capacity);
    }

    public void AddDropRestriction(int dropRestrictionDelta) {
        dropRestriction = Mathf.Max(dropRestriction + dropRestrictionDelta, 0);
    }

    void ForceUpdateSync(ItemContainer itemContainer) {
        bool isHands = hands == itemContainer;
        if (isHands) hands = hands.Clone();
        else {
            int i = inventoryContainers.FindIndex(ic => ic == itemContainer);
            if (i < 0 || i >= inventoryContainers.Count) return;
            inventoryContainers[i] = inventoryContainers[i].Clone();
        }
    }

    public IEnumerable<ItemInstance> GetAllItems() {
        foreach (ItemInstance itemInstance in hands.GetAllItems()) yield return itemInstance;
        foreach (ItemContainer itemContainer in inventoryContainers)
            foreach (ItemInstance itemInstance in itemContainer.GetAllItems()) yield return itemInstance;
    }

    public IEnumerable<(ItemContainer container, ItemInstance item, int ind)> GetAllItemsFull() {
        foreach ((ItemInstance item, int ind) in hands.GetAllItemsFull()) yield return (container: hands, item, ind);
        foreach (ItemContainer container in inventoryContainers)
            foreach ((ItemInstance item, int ind) in container.GetAllItemsFull()) yield return (container, item, ind);
    }
}
