using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityModManagerNet;

namespace DerailValleyRandomEvents;

public static class ObstacleSpawner
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

    public static List<ObstacleComponent> GetAllObstacles()
    {
        return _spawnedObstacles;
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

    public static void AddMissingCollider(GameObject obj, Obstacle obstacle)
    {
        var collider = obj.GetComponent<Collider>();

        if (collider == null)
        {
            Logger.Log("[ObstacleSpawner] No collider - adding mesh collider...");

            MeshCollider meshCollider = obj.AddComponent<MeshCollider>();
            meshCollider.convex = true;

            MeshFilter meshFilter = obj.GetComponentInChildren<MeshFilter>();

            if (meshFilter == null)
                throw new Exception($"No mesh filter found inside '{obj}'");

            meshCollider.sharedMesh = meshFilter.sharedMesh;
            collider = meshCollider;

            Logger.Log($"[ObstacleSpawner] No collider - adding mesh collider done - collider={meshCollider} mesh={meshCollider.sharedMesh}");
        }

        if (collider.material == null)
        {
            Logger.Log($"[ObstacleSpawner] No physic material - adding...");
            collider.material = GetPhysicMaterialForObstacle(obstacle);
        }
    }

    public static GameObject Create(GameObject prefab, Obstacle obstacle, Quaternion? overrideRotation = null)
    {
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

        var newObj = UnityEngine.Object.Instantiate(prefab, Vector3.zero, rotation, parent);

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

        AddMissingCollider(newObj, obstacle);

        _spawnedObstacles.Add(obstacleComp);

        Logger.Log($"[ObstacleSpawner] Created obstacle type={obstacle.Type} go={newObj} prefab={prefab}");

        return newObj;
    }

    private static void OnStrongImpact(Obstacle obstacle)
    {
        Logger.Log($"[ObstacleSpawner] On strong impact (derailing) type={obstacle.Type}");

        if (PlayerManager.Car != null)
            PlayerManager.Car.Derail();
    }
}