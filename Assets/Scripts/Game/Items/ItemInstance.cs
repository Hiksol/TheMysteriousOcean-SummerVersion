using Mirror;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class ItemInstance : NetworkBehaviour
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
}
