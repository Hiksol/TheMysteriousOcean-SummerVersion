using Mirror;
using UnityEngine.Events;

public abstract class InteractableActive : Interactable
{
    public bool isInteractableActive;
    public UnityEvent<bool> onInteractableActiveChanged = new();
    public UnityEvent<bool> onInteractableWorkingChanged = new();

    protected virtual bool IsAlwaysActive => false;
    public bool IsInteractableWorking => isInteractableActive && IsInteractableShouldWork();
    bool isInteractableWorkingLast = false;

    public override void OnStartServer() {
        if (IsAlwaysActive) SetInteractableActive(true);
    }

    protected virtual void Update() {
        if (isServer) {
            bool isInteractableWorkingNew = IsInteractableWorking;
            if (isInteractableWorkingNew != isInteractableWorkingLast) {
                onInteractableWorkingChanged.Invoke(isInteractableWorkingNew);
            }
            UpdateNewServer(isInteractableWorkingNew);
            isInteractableWorkingLast = isInteractableWorkingNew;
        }
    }

    [Server]
    protected virtual void UpdateNewServer(bool isInteractableWorking) {}

    public virtual bool IsInteractableShouldWork() => false;

    [Server]
    public override void Interact(Player player, ItemInstance item) {
        if (!IsAlwaysActive) SetInteractableActive(!isInteractableActive);
    }

    [Server]
    public void SetInteractableActive(bool isActive) {
        isInteractableActive = isActive;
        OnInteractableActiveChangedInternal(isActive);
        onInteractableActiveChanged.Invoke(isActive);
        RpcInvokeActiveChanged(isActive);
    }

    [ClientRpc]
    public void RpcInvokeActiveChanged(bool isActive) {
        onInteractableActiveChanged.Invoke(isActive);
    }

    [Server]
    protected virtual void OnInteractableActiveChangedInternal(bool isActive) {}

    [ClientRpc]
    public void RpcInvokeWorkingChanged(bool isActive) {
        onInteractableWorkingChanged.Invoke(isActive);
    }
}
