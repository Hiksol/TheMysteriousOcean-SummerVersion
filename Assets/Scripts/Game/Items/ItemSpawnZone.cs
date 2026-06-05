using Mirror;
using UnityEngine;

public class ItemSpawnZone : NetworkBehaviour
{
    public Vector3 spawnAreaSize = new(1, 1, 1);
    public ItemInstance itemPrefab;
    public ItemWeightedList possibleItems;
    public bool drawGizmosOnSelected;

    public override void OnStartServer() {
        SpawnRandomItem();
    }

    [Server]
    [ContextMenu(nameof(SpawnRandomItem))]
    public void SpawnRandomItem() {
        ItemData itemData = possibleItems.items[0].itemDatas[0];
        ItemInstance item = Instantiate(itemPrefab);
        item.transform.position = transform.position + GameManager.I.Rng.Vector3Abs(spawnAreaSize / 2);
        StickToGround(item.gameObject);
        NetworkServer.Spawn(item.gameObject);
        item.SetItemData(itemData);
    }

    void StickToGround(GameObject go) {
        Collider collider = go.GetComponent<Collider>();
        Vector3 rayStart = go.transform.position;
        rayStart.y = transform.position.y + spawnAreaSize.y / 2;
        if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hitInfo, spawnAreaSize.y)) {
            Vector3 newPosition = hitInfo.point;
            newPosition.y += collider.bounds.size.y / 2 + 0.1f;
            go.transform.position = newPosition;
        }
    }

    void BaseDrawGizmos() {
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(transform.position, spawnAreaSize);
    }

    void OnDrawGizmos() { if (!drawGizmosOnSelected) BaseDrawGizmos(); }
    void OnDrawGizmosSelected() { if (drawGizmosOnSelected) BaseDrawGizmos(); }
}
