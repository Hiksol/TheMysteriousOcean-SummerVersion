using Mirror;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class ItemInstance : NetworkBehaviour, IInteractable
{
    [SyncVar] public ItemData itemData;

    GameObject model;

    public override void OnStartClient() {
        UpdateModel();
    }

    [Server]
    public void SetItemData(ItemData itemData) {
        this.itemData = itemData;
        RpcUpdateModel();
    }

    [ClientRpc]
    void RpcUpdateModel() {
        UpdateModel();
    }

    [Client]
    void UpdateModel() {
        if (model) Destroy(model);
        if (itemData) model = Instantiate(itemData.modelPrefab, transform);
    }

    [Command]
    public void CmdUse(Player player, NetworkBehaviour target) {
        Use(player, target);
    }

    [Server]
    public void Use(Player player, NetworkBehaviour target) {
        IInteractable interactable = (IInteractable)target;
        itemData.itemProperties.ForEach(itemProperty => itemProperty.OnUse(this, player, interactable));
    }
}
