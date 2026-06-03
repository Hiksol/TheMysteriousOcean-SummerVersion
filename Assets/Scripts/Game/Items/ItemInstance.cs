using System.Collections.Generic;
using System.Linq;
using Mirror;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class ItemInstance : Interactable
{
    [SyncVar(hook = nameof(OnItemDataChanged))] public ItemData itemData;
    [SerializeReference] public List<ItemProperty> itemProperties;

    GameObject model;

    public override void OnStartClient() {
        OnItemDataChanged(null, itemData);
    }

    [Server]
    public void SetItemData(ItemData itemData) {
        this.itemData = itemData;
    }

    void OnItemDataChanged(ItemData _, ItemData newItemData) {
        UpdateModel(newItemData);
        // itemProperties = new(newItemData.itemProperties);
        itemProperties = newItemData.itemProperties.Clone().ToList();
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
        itemProperties.ForEach(itemProperty => itemProperty.OnUse(this, player, interactable));
    }
}
