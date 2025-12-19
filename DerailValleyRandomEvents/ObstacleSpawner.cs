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
    private static Dictionary<string, PhysicMaterial> _physicMaterialsForObstacles = [];
    public static Vector3? ScaleMultiplier = null;
    public static Vector3? RotationMultiplier = null;
    public static int lastObstacleId = -1;

    static ObstacleSpawner()
    {
        CleanupHelper.Add(typeof(ObstacleSpawner), () =>
        {
            ClearAllObstacles();
        });
    }

    public static int GetNewObstacleId()
    {
        lastObstacleId++;
        return lastObstacleId;
    }

    public static PhysicMaterial GetPhysicMaterialForObstacle(Obstacle obstacle)
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
        Main.ModEntry.Logger.Log($"[ObstacleSpawner] Clear {_spawnedObstacles.Count} obstacles");

        var obs = _spawnedObstacles.ToList();

        foreach (var obstacle in obs)
        {
            ClearObstacle(obstacle);
        }

        _spawnedObstacles.Clear();
        lastObstacleId = -1;
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
            // skinned renderers so more complex so just use box
            if (obstacle.AnimalType != null)
            {
                Logger.Log("[ObstacleSpawner] Is an animal so creating new collider");

                collider = MeshUtils.AddOrUpdateCombinedBoxCollider(obj);
            }
            else
            {
                Logger.Log("[ObstacleSpawner] Is NOT an animal so creating a mesh collider");

                MeshCollider meshCollider = obj.AddComponent<MeshCollider>();
                meshCollider.convex = true;

                var meshFilter = obj.GetComponentInChildren<MeshFilter>();
                var skinned = obj.GetComponentInChildren<SkinnedMeshRenderer>();

                if (meshFilter == null && skinned == null)
                    throw new Exception($"No mesh found inside '{obj}'");

                Mesh meshToUse;

                if (meshFilter != null)
                {
                    meshToUse = meshFilter.sharedMesh;
                }
                else
                {
                    var bakedMesh = new Mesh();
                    skinned.BakeMesh(bakedMesh);
                    meshToUse = bakedMesh;
                }

                meshCollider.sharedMesh = meshToUse;
                collider = meshCollider;
            }
        }

        if (collider.material == null)
        {
            Logger.Log($"[ObstacleSpawner] No physic material, adding...");
            collider.material = GetPhysicMaterialForObstacle(obstacle);
        }
    }

    public static Vector3 GetObstacleScale(Obstacle obstacle)
    {
        var scale = UnityEngine.Random.Range(obstacle.MinScale, obstacle.MaxScale);

        var localScale = new Vector3(scale, scale, scale);

        if (obstacle.ScaleOffset.HasValue)
            localScale = obstacle.ScaleOffset.Value;

        if (ScaleMultiplier != null)
            localScale = Vector3.Scale(localScale, ScaleMultiplier.Value);

        return localScale;
    }

    public static GameObject Create(GameObject prefab, Obstacle obstacleTemplate)
    {
        // keep in the world as it loads
        // TODO: cleanup to avoid memory issues
        var parent = WorldMover.OriginShiftParent;

        var newObj = UnityEngine.Object.Instantiate(prefab, Vector3.zero, Quaternion.identity, parent);

        var obstacleComp = newObj.AddComponent<ObstacleComponent>();

        var obstacle = obstacleTemplate.Clone();
        obstacle.Id = GetNewObstacleId();

        obstacleComp.obstacle = obstacle;

        // TODO: do this help stop removing on tile load?
        // Object.DontDestroyOnLoad(newObj);

        newObj.layer = (int)DVLayer.Train_Big_Collider;

        if (obstacle.AnimalType != null)
        {
            var animalPrefab = AnimalHelper.GetRandomPrefab(obstacle.AnimalType.Value);

            var explodingBase = newObj.transform.Find(ObstacleComponent.baseTransformName);

            var animalParent = explodingBase ?? newObj.transform;

            GameObject obj = (GameObject)GameObject.Instantiate(animalPrefab, animalParent, instantiateInWorldSpace: false);

            // without this the cats go craaaaaazy
            obj.transform.localPosition = new Vector3(0, 0, 0);

            Logger.Log($"Added animal '{obstacle.AnimalType}' into {animalParent} = {obj}");
        }

        var rb = newObj.GetComponent<Rigidbody>() ?? newObj.AddComponent<Rigidbody>();
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic; // old: Continuous
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.solverIterations = 10;
        rb.solverVelocityIterations = 10;
        rb.mass = UnityEngine.Random.Range(obstacle.MinMass, obstacle.MaxMass);
        rb.drag = obstacle.Drag;
        rb.angularDrag = obstacle.AngularDrag;
        // rb.sleepThreshold = 0.01f; // default ~0.005

        AddMissingCollider(newObj, obstacle);

        if (obstacle.CenterOfMass != null)
            rb.centerOfMass = obstacle.CenterOfMass.Value;

        // if (Main.settings.ShowDebugStuff)
        // {
        //     var debugSphere = RandomEventsManager.CreateDebugSphere(10f);
        //     debugSphere.transform.SetParent(newObj.transform);
        // }

        _spawnedObstacles.Add(obstacleComp);

        // Logger.Log($"[ObstacleSpawner] Created obstacle type={obstacle.Type} go={newObj} prefab={prefab} scale={newObj.transform.localScale} scaleOffset={obstacle.ScaleOffset} overrideScale={ScaleMultiplier}");

        return newObj;
    }

    public static GameObject GetRandomObstaclePrefab(Obstacle obstacle)
    {
        return AssetBundleHelper.GetRandomObstaclePrefab(obstacle);
    }
}