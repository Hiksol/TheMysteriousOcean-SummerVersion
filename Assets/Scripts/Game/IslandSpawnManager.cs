using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class IslandSpawnManager : NetworkBehaviour
{
    public List<Island> islandPrefabs;
    public float spawnInterval = 10f;
    public Vector3 islandsVelocity;

    [Header("Debug")]
    public float currentInterval = 0f;

    void Update() {
        if (!isServer) return;
        currentInterval += Time.deltaTime;
        if (currentInterval >= spawnInterval) {
            currentInterval -= spawnInterval;
            SpawnIsland();
        }
    }

    [Server]
    void SpawnIsland() {
        Island island = Instantiate(GameManager.I.Rng.RandomItem(islandPrefabs), transform.position, Quaternion.identity);
        island.velocity = islandsVelocity;
        NetworkServer.Spawn(island.gameObject);
    }
}
