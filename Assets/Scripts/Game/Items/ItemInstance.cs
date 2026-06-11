using System.Collections.Generic;
using System.Linq;
using Mirror;
using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class ItemInstance : Interactable
{
    [SyncVar(hook = nameof(OnItemDataChanged))] public ItemData itemData;
    [SerializeReference] public List<ItemProperty> itemProperties;

    GameObject model;
    BoxCollider _collider;
    [SyncVar(hook = nameof(OnTransformParentChangedHook))] Transform transformParent;

    void Awake() {
        _collider = GetComponent<BoxCollider>();
    }

    public override void OnStartClient() {
        if (itemData != null) OnItemDataChanged(null, itemData);
    }

    void OnTransformParentChanged() {
        if (!isServer) return;
        transformParent = transform.parent;
    }

    void OnTransformParentChangedHook(Transform _, Transform newValue) {
        transform.SetParent(newValue, false);
    }

    [Server]
    public void SetItemData(ItemData itemData) {
        this.itemData = itemData;
        UpdateModel(itemData);
    }

    void OnItemDataChanged(ItemData _, ItemData newItemData) {
        UpdateModel(newItemData);
        itemProperties = newItemData.itemProperties.Clone().ToList();
    }

    void UpdateModel(ItemData itemData) {
        if (model) Destroy(model);
        if (itemData) {
            model = Instantiate(itemData.modelPrefab, transform);
            if (model.TryGetComponent(out BoxCollider collider)) {
                _collider.size = Vector3.Scale(Utils.VectorAbs(model.transform.localRotation * collider.size ), model.transform.localScale);
                collider.enabled = false;
            }
        }
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
