using System;
using System.Collections.Generic;
using System.Linq;
using DV.OriginShift;
using DV.WorldTools;
using UnityEngine;
using UnityModManagerNet;

namespace DerailValleyRandomEvents;

// source: https://github.com/derail-valley-modding/custom-car-loader/blob/784a307592bba8afb96a05f20b92d22259f1127d/CCL.Types/MiscEnums.cs#L16
public enum DVLayer
{
    Default = 0,
    TransparentFX = 1,
    //Ignore Raycast = 2,
    //,
    Water = 4,
    UI = 5,
    //,
    //,
    Terrain = 8,
    Player = 9,
    Train_Big_Collider = 10,
    Train_Walkable = 11,
    Train_Interior = 12,
    Interactable = 13,
    Teleport_Destination = 14,
    Laser_Pointer_Target = 15,
    Camera_Dampening = 16,
    Culling_Sleepers = 17,
    Culling_Anchors = 18,
    Culling_Rails = 19,
    Render_Elements = 20,
    No_Teleport_Interaction = 21,
    Inventory = 22,
    Controller = 23,
    Hazmat = 24,
    PostProcessing = 25,
    Grabbed_Item = 26,
    World_Item = 27,
    Reflection_Probe_Only = 28,
}

public static class ObstactleSpawner
{
    private static UnityModManager.ModEntry.ModLogger Logger => Main.ModEntry.Logger;
    private static List<ObstacleComponent> _spawnedObstacles = [];
    private static PhysicMaterial? _rockPhysicMaterial;

    private static PhysicMaterial GetPhysicMaterialForBiome(Biome biome)
    {
        switch (biome)
        {
            case Biome.Rock:
                if (_rockPhysicMaterial != null)
                    return _rockPhysicMaterial;

                _rockPhysicMaterial = new PhysicMaterial("DerailValleyRandomEvents_RockMaterial");
                _rockPhysicMaterial.dynamicFriction = 0.9f;
                _rockPhysicMaterial.staticFriction = 1.0f;
                _rockPhysicMaterial.bounciness = 0f;
                _rockPhysicMaterial.frictionCombine = PhysicMaterialCombine.Maximum;
                _rockPhysicMaterial.bounceCombine = PhysicMaterialCombine.Minimum;

                return _rockPhysicMaterial;
        }

        throw new Exception($"Could not get physic material for biome: {biome}");
    }

    public static void ClearObstacle(ObstacleComponent obstacleComp)
    {
        Main.ModEntry.Logger.Log($"[ObstacleSpawner] Clear obstacle '{obstacleComp}'");

        GameObject.Destroy(obstacleComp.gameObject);
        _spawnedObstacles.Remove(obstacleComp);
    }

    public static void ClearAllObstacles()
    {
        Main.ModEntry.Logger.Log($"[ObstacleSpawner] Clear all obstacles");

        var obs = _spawnedObstacles.ToList();

        foreach (var obstacle in obs)
        {
            ClearObstacle(obstacle);
        }
    }

    public static void UnhighlightAllSpawnedObstacles()
    {
        var obs = _spawnedObstacles.ToList();

        foreach (var obstacle in obs)
        {
            obstacle.SetIsHighlighted(false);
        }
    }

    public static ObstacleComponent Spawn(GameObject prefab, Obstacle obstacle, Vector3 localPos)
    {
        Vector3 globalPos = localPos + OriginShift.currentMove;

        // keep in the world as it loads
        // TODO: cleanup to avoid memory
        var parent = WorldMover.OriginShiftParent;

        var newObj = UnityEngine.Object.Instantiate(prefab, globalPos, obstacle.Rotate != null ? (Quaternion)obstacle.Rotate : Quaternion.identity, parent);

        var obstacleComp = newObj.AddComponent<ObstacleComponent>();
        obstacleComp.Obstacle = obstacle;

        // TODO: do this help stop removing on tile load?
        // Object.DontDestroyOnLoad(newObj);

        newObj.layer = (int)DVLayer.Train_Big_Collider;

        var rb = newObj.GetComponent<Rigidbody>() ?? newObj.AddComponent<Rigidbody>();
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.mass = UnityEngine.Random.Range(obstacle.MinMass, obstacle.MaxMass);
        rb.drag = 1f;
        rb.angularDrag = 1f;

        var scale = UnityEngine.Random.Range(obstacle.MinScale, obstacle.MaxScale);

        newObj.transform.localScale = new Vector3(scale, scale, scale);

        switch (obstacle.Biome)
        {
            case Biome.Rock:
                // TODO: make setting
                rb.drag = 1f;
                rb.angularDrag = 5f;
                break;
        }

        var collider = newObj.GetComponent<Collider>();

        if (collider == null)
        {
            Logger.Log("[ObstacleSpawner] No collider - adding mesh collider...");

            MeshCollider meshCollider = newObj.AddComponent<MeshCollider>();
            meshCollider.convex = true;

            MeshFilter meshFilter = newObj.GetComponentInChildren<MeshFilter>();

            if (meshFilter == null)
                throw new Exception($"No mesh filter found inside '{newObj}'");

            meshCollider.sharedMesh = meshFilter.sharedMesh;
            collider = meshCollider;

            Logger.Log($"[ObstacleSpawner] No collider - adding mesh collider done - collider={meshCollider} mesh={meshCollider.sharedMesh}");
        }

        if (collider.material == null)
        {
            Logger.Log($"[ObstacleSpawner] No physic material - adding...");
            collider.material = GetPhysicMaterialForBiome(obstacle.Biome);
        }

        _spawnedObstacles.Add(obstacleComp);

        Logger.Log($"[ObstacleSpawner] Spawned obstacle {newObj} from prefab {prefab.name} at local={localPos} global={globalPos} into parent={parent}");

        return obstacleComp;
    }
}