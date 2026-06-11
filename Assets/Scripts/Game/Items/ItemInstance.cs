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
    public struct NetworkTransformStruct { public NetworkIdentity ni; public string childName; }
    [SyncVar(hook = nameof(OnTransformParentChangedHook))] NetworkTransformStruct transformRoot;

    void Awake() {
        _collider = GetComponent<BoxCollider>();
    }

    public override void OnStartServer() {
        if (transform.parent != null) OnTransformParentChanged();
    }

    public override void OnStartClient() {
        if (itemData != null) OnItemDataChanged(null, itemData);
    }

    void OnTransformParentChanged() {
        if (!isServer) return;
        if (transform.parent != null) transformRoot = new() {
            ni = transform.parent.GetComponentInParent<NetworkIdentity>(),
            childName = transform.parent.gameObject.name
        }; else transformRoot = new() {
            ni = null,
            childName = ""
        };
    }

    void OnTransformParentChangedHook(NetworkTransformStruct _, NetworkTransformStruct newValue) {
        string currentParentName = transform.parent != null ? transform.parent.gameObject.name : "";
        print($"{newValue.childName} --- {currentParentName} --- {newValue.childName == currentParentName}");
        if (newValue.childName == currentParentName) return;
        // if (newValue.ni == null) transform.SetParent(null, false);
        // else if (newValue.ni.gameObject.name == newValue.childName) transform.SetParent(newValue.ni.transform, false);
        // else transform.SetParent(newValue.ni.transform.FindRecursive(newValue.childName), false);
        if (newValue.ni == null) transform.parent = null;
        else if (newValue.ni.gameObject.name == newValue.childName) transform.parent = newValue.ni.transform;
        else transform.parent = newValue.ni.transform.FindRecursive(newValue.childName);
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
