using System;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class IslandSpawnManager : NetworkBehaviour
{
    public List<IslandSpawnCategory> islandCategories;
    public float spawnInterval = 10f;
    public Transform startSpawnTransform, endSpawnTransform;
    public Vector3 islandsVelocity;
    public LayerMask layersToExlude = 1 << 4;

    [Header("Debug")]
    public float currentInterval = 0f;

    void Update() {
        if (!isServer) return;
        islandCategories.ForEach(ic => ic.OnUpdate());
        currentInterval += Time.deltaTime;
        if (currentInterval >= spawnInterval) {
            currentInterval -= spawnInterval;
            SpawnIsland();
        }
    }

    Island TryGetIsland() {
        foreach (IslandSpawnCategory islandCategory in islandCategories) {
            Island island = islandCategory.TryGetIsland();
            if (island != null) return island;
        }
        return null;
    }

    Vector3 GetSpawnPosition(float islandRadius) {
        float xSpawnLen = endSpawnTransform.position.x - startSpawnTransform.position.x;
        float yachtPosDeltaFromStart = YachtManager.I.transform.position.x - startSpawnTransform.position.x;
        float endForStartT = (yachtPosDeltaFromStart - YachtManager.I.HalfWidth - islandRadius) / xSpawnLen,
              startForEndT = (yachtPosDeltaFromStart + YachtManager.I.HalfWidth + islandRadius) / xSpawnLen;
        // final t should be in [0, endForStartT] or [startForndT, 1]
        float selectedPreT = GameManager.I.Rng.Range(endForStartT + (1 - startForEndT));
        float selectedT = selectedPreT <= endForStartT ? selectedPreT : selectedPreT + (startForEndT - endForStartT);
        return Vector3.Lerp(startSpawnTransform.position, endSpawnTransform.position, selectedT);
    }

    [Server]
    void SpawnIsland() {
        Island island = TryGetIsland();
        if (island == null) return;
        float radius = island.halfDiagonal;
        Vector3 spawnPosition = GetSpawnPosition(radius);
        Collider[] colliders = new Collider[1];
        if (Physics.OverlapSphereNonAlloc(spawnPosition, radius, colliders, ~layersToExlude) > 0) return;
        Island islandInstance = Instantiate(island, spawnPosition, Quaternion.Euler(new(0, GameManager.I.Rng.Range(360), 0)));
        islandInstance.velocity = islandsVelocity;
        NetworkServer.Spawn(islandInstance.gameObject);
    }

    [Serializable]
    public class IslandSpawnCategory {
        public string categoryName;
        public List<IslandSpawnSubcategory> subcategories;
        public float categoryTimer = 15f;
        public float currentCategoryTimer = 0f;

        public void OnUpdate() {
            currentCategoryTimer = Mathf.Min(currentCategoryTimer + Time.deltaTime, categoryTimer);
        }

        public Island TryGetIsland() {
            if (currentCategoryTimer == categoryTimer) {
                currentCategoryTimer = 0;
                IslandSpawnSubcategory islandSpawnSubcategory = GameManager.I.Rng.RandomWeightedItem(subcategories, sc => sc.scWeight);
                return GameManager.I.Rng.RandomItem(islandSpawnSubcategory.islandPrefabs);
            }
            return null;
        }

        [Serializable]
        public class IslandSpawnSubcategory {
            public string scName;
            public float scWeight = 1f;
            public List<Island> islandPrefabs;
        }
    }
}
