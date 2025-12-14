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
    public static Vector3? ScaleMultiplier = null;
    public static Vector3? RotationMultiplier = null;

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

        _spawnedObstacles.Remove(obstacleComp);
        GameObject.Destroy(obstacleComp.gameObject);
    }

    public static void CleanupObstacle(ObstacleComponent obstacleComp)
    {
        Main.ModEntry.Logger.Log($"[ObstacleSpawner] Cleanup obstacle '{obstacleComp}'");

        _spawnedObstacles.Remove(obstacleComp);
        GameObject.Destroy(obstacleComp.gameObject);
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

    public static GameObject Create(GameObject prefab, Obstacle obstacle)
    {
        // keep in the world as it loads
        // TODO: cleanup to avoid memory issues
        var parent = WorldMover.OriginShiftParent;

        var newObj = UnityEngine.Object.Instantiate(prefab, Vector3.zero, Quaternion.identity, parent);

        var obstacleComp = newObj.AddComponent<ObstacleComponent>();
        obstacleComp.obstacle = obstacle;
        obstacleComp.OnStrongImpact = (trainCar) => OnStrongImpact(trainCar, obstacle);

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

        newObj.transform.localScale = new Vector3(scale, scale, scale);

        if (obstacle.ScaleOffset.HasValue)
            newObj.transform.localScale = obstacle.ScaleOffset.Value;

        if (ScaleMultiplier != null)
            newObj.transform.localScale = Vector3.Scale(newObj.transform.localScale, ScaleMultiplier.Value);

        AddMissingCollider(newObj, obstacle);

        if (obstacle.CenterOfMass != null)
            rb.centerOfMass = obstacle.CenterOfMass.Value;

        if (Main.settings.ShowDebugStuff)
        {
            var debugSphere = RandomEventsManager.CreateDebugSphere(10f);
            debugSphere.transform.SetParent(newObj.transform);
        }

        _spawnedObstacles.Add(obstacleComp);

        Logger.Log($"[ObstacleSpawner] Created obstacle type={obstacle.Type} go={newObj} prefab={prefab} scale={newObj.transform.localScale} scaleOffset={obstacle.ScaleOffset} overrideScale={ScaleMultiplier}");

        return newObj;
    }

    private static void OnStrongImpact(TrainCar car, Obstacle obstacle)
    {
        Logger.Log($"[ObstacleSpawner] On strong impact car={car} type={obstacle.Type}");

        car.Derail();
    }
}