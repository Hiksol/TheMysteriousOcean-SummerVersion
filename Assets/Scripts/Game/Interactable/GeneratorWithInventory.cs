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
        if (item == null) return;
        if (acceptableFuels.Contains(item.itemData.itemFuelType)) {
            int ind = itemContainer.FindFreeIndex(item.itemData.slotCount);
            if (ind == -1) return;
            itemContainer.InsertItemForce(item, ind);
            inventory.DropItemInRightHand();
            ParentItem(item, true, transform.position);
            RpcParentItem(item, true, transform.position);
        } else {
            item.Use(player, this);
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
}
