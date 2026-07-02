using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class GeneratorWithInventory : InteractableActive
{
    public int slotsCount = 9;
    public List<ItemFuelType> acceptableFuels = new() { ItemFuelType.Fuel };
    public Battery battery;
    public float fuelConsumptionPerSecond = 1;
    public float energyGenerationPerSecond = 1;
    public ItemContainer itemContainer;

    Transform hiddenRoot;

    [Header("Debug")]
    [SyncVar] public float currentFuel = 0;

    void Awake() {
        itemContainer = new(slotsCount);
        hiddenRoot = new GameObject("HiddenRoot").transform;
        hiddenRoot.SetParent(transform);
        hiddenRoot.localPosition = Vector3.zero;
    }

    protected override bool IsAlwaysActive => true;

    public override bool IsInteractableShouldWork() {
        return battery && (currentFuel > 0 || itemContainer.Count > 0);
    }

    protected override void UpdateNewServer(bool isInteractableWorking) {
        if (isInteractableWorking) {
            if (currentFuel > 0) {
                currentFuel = Mathf.Max(currentFuel - Time.deltaTime * fuelConsumptionPerSecond, 0);
                battery.AddCharge(energyGenerationPerSecond * Time.deltaTime);
            } else if (itemContainer.Count > 0) {
                int ind = itemContainer.FirstItemInd();
                ItemInstance item = itemContainer.GetItem(ind);
                currentFuel = item.itemData.itemFuelAmount;
                itemContainer.DestroyItem(ind);
            }
        }
    }

    [Server]
    override public void Interact(Player player, ItemInstance item) {
        Inventory inventory = player.Inventory;
        inventory.OpenInventoryWithInteractable(this);
    }

    [Server]
    public void TryTransferItem(Player player, ItemInstance item) {
        Inventory inventory = player.Inventory;
        if (IsItemAcceptable(item)) {
            int ind = itemContainer.FindFreeIndex(item.itemData.slotCount);
            if (ind == -1) return;
            itemContainer.InsertItemForce(item, ind);
            inventory.DropTargetItem(item);
            ParentItem(item, true, transform.position);
            RpcParentItem(item, true, transform.position);
        }
    }

    [ClientRpc]
    void RpcParentItem(ItemInstance item, bool hide, Vector3 pos) {
        ParentItem(item, hide, pos);
    }

    void ParentItem(ItemInstance item, bool hide, Vector3 pos) {
        if (item == null) return;
        item.gameObject.SetActive(!hide);
        item.transform.position = pos;
        item.transform.SetParent(transform);
    }

    public bool IsItemAcceptable(ItemInstance item) {
        return acceptableFuels.Contains(item.itemData.itemFuelType);
    }

    [Command(requiresAuthority = false)]
    public void CmdTryTransferItem(Player player, ItemInstance item) {
        TryTransferItem(player, item);
    }
}
