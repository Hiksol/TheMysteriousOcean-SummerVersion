using Mirror;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class ItemInstance : Interactable
{
    [SyncVar(hook = nameof(OnItemDataChanged))] public ItemData itemData;

    GameObject model;

    public override void OnStartClient() {
        UpdateModel(itemData);
    }

    [Server]
    public void SetItemData(ItemData itemData) {
        this.itemData = itemData;
    }

    void OnItemDataChanged(ItemData _, ItemData newItemData) {
        UpdateModel(newItemData);
    }

    [Client]
    void UpdateModel(ItemData itemData) {
        if (model) Destroy(model);
        if (itemData) model = Instantiate(itemData.modelPrefab, transform);
    }

    [Command]
    public void CmdUse(Player player, NetworkBehaviour target) {
        Use(player, target);
    }

    [Server]
    public void Use(Player player, NetworkBehaviour target) {
        Interactable interactable = (Interactable)target;
        itemData.itemProperties.ForEach(itemProperty => itemProperty.OnUse(this, player, interactable));
    }
}
