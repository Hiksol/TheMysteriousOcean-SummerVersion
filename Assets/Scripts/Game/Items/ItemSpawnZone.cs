using System.Collections.Generic;
using System.Linq;
using Mirror;
using UnityEngine;

public class ItemSpawnZone : NetworkBehaviour
{
    public List<Transform> spawnPlaces = new();
    public int numberOfItems = 1;
    public ItemInstance itemPrefab;
    public ItemWeightedList possibleItems;
    public bool drawGizmosOnSelected;

    public override void OnStartServer() {
        foreach (Transform t in GameManager.I.Rng.Shuffle(spawnPlaces).Take(numberOfItems))
            SpawnRandomItem(t.position);
    }

    [Server]
    public void SpawnRandomItem(Vector3 pos) {
        ItemWeightedList.ItemTier itemTier = GameManager.I.Rng.RandomWeightedItem(possibleItems.items, tier => tier.weight);
        ItemData itemData = GameManager.I.Rng.RandomItem(itemTier.itemDatas);
        ItemInstance item = Instantiate(itemPrefab);
        item.transform.SetPositionAndRotation(pos, transform.rotation);
        // StickToGround(item.gameObject);
        NetworkServer.Spawn(item.gameObject);
        ParentGameObjectToTransform(item.gameObject);
        RpcParentGameObjectToTransform(item.gameObject);
        item.SetItemData(itemData);
    }

    [ClientRpc]
    void RpcParentGameObjectToTransform(GameObject go) {
        ParentGameObjectToTransform(go);
    }

    void ParentGameObjectToTransform(GameObject go) {
        go.transform.SetParent(transform);
    }

    void StickToGround(GameObject go) {
        Collider collider = go.GetComponent<Collider>();
        Vector3 rayStart = go.transform.position;
        // rayStart.y = transform.position.y + spawnAreaSize.y / 2;
        if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hitInfo, 10f)) {
            Vector3 newPosition = hitInfo.point;
            newPosition.y += collider.bounds.size.y / 2 + 0.1f;
            go.transform.position = newPosition;
        }
    }
}
