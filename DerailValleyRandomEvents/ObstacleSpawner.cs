using System;
using System.Collections.Generic;
using System.Linq;
using DV.OriginShift;
using DV.WorldTools;
using UnityEngine;
using UnityModManagerNet;

namespace DerailValleyRandomEvents;

public static class ObstactleSpawner
{
    private static UnityModManager.ModEntry.ModLogger Logger => Main.ModEntry.Logger;
    private static List<ObstacleComponent> _spawnedObstacles = [];
    private static Dictionary<ObstacleType, PhysicMaterial> _physicMaterialsForObstacles = [];
    public static Vector3? OverrideScale = null;
    public static Vector3? OverrideRotation = null;

    private static PhysicMaterial GetPhysicMaterialForObstacle(Obstacle obstacle)
    {
        if (_physicMaterialsForObstacles.ContainsKey(obstacle.Type))
            return _physicMaterialsForObstacles[obstacle.Type];

        var physicMaterial = new PhysicMaterial($"DerailValleyRandomEvents_{obstacle.GetType()}");
        physicMaterial.dynamicFriction = obstacle.DynamicFriction;
        physicMaterial.staticFriction = obstacle.StaticFriction;
        physicMaterial.bounciness = obstacle.Bounciness;
        physicMaterial.frictionCombine = PhysicMaterialCombine.Maximum;
        physicMaterial.bounceCombine = PhysicMaterialCombine.Minimum;

        _physicMaterialsForObstacles[obstacle.Type] = physicMaterial;

        return physicMaterial;
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

    public static ObstacleComponent Spawn(GameObject prefab, Obstacle obstacle, Vector3 localPos, Quaternion? overrideRotation = null)
    {
        // Vector3 globalPos = localPos + OriginShift.currentMove;
        Vector3 globalPos = localPos;

        var rotation = Quaternion.identity;

        if (OverrideRotation != null)
            rotation = Quaternion.Euler((Vector3)OverrideRotation);

        if (overrideRotation != null)
            rotation = (Quaternion)overrideRotation;

        if (obstacle.RotationOffset != null)
            rotation *= (Quaternion)obstacle.RotationOffset;

        // keep in the world as it loads
        // TODO: cleanup to avoid memory issues
        var parent = WorldMover.OriginShiftParent;

        var newObj = UnityEngine.Object.Instantiate(prefab, globalPos, rotation, parent);

        var obstacleComp = newObj.AddComponent<ObstacleComponent>();
        obstacleComp.obstacle = obstacle;
        obstacleComp.OnStrongImpact = () => OnStrongImpact(obstacle);

        // TODO: do this help stop removing on tile load?
        // Object.DontDestroyOnLoad(newObj);

        newObj.layer = (int)DVLayer.Train_Big_Collider;

        var rb = newObj.GetComponent<Rigidbody>() ?? newObj.AddComponent<Rigidbody>();
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.mass = UnityEngine.Random.Range(obstacle.MinMass, obstacle.MaxMass);
        rb.drag = obstacle.Drag;
        rb.angularDrag = obstacle.AngularDrag;

        var scale = UnityEngine.Random.Range(obstacle.MinScale, obstacle.MaxScale);

        newObj.transform.localScale = OverrideScale ?? new Vector3(scale, scale, scale);

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
            collider.material = GetPhysicMaterialForObstacle(obstacle);
        }

        _spawnedObstacles.Add(obstacleComp);

        Logger.Log($"[ObstacleSpawner] Spawned obstacle {obstacle} as go={newObj} prefab={prefab} local={localPos} global={globalPos} parent={parent}");

        return obstacleComp;
    }

    private static void OnStrongImpact(Obstacle obstacle)
    {
        Logger.Log($"[ObstacleSpawner] On strong impact (derailing) obstacle={obstacle}");

        if (PlayerManager.Car != null)
            PlayerManager.Car.Derail();
    }
}