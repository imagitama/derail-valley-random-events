using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DV.WorldTools;
using DV.Utils;
using UnityEngine;
using UnityModManagerNet;
using DV.OriginShift;

namespace DerailValleyRandomEvents;

public enum ObstacleType
{
    Rockslide,
    // CowsOnTrack,
    FallenTrees,
    FunRamp
    // RiverFlood
}

public class Obstacle
{
    // basics
    public ObstacleType Type;
    public Biome? Biome;
    public string AssetBundleName;
    public string? PrefabName;
    public bool? InPool;
    // spawning
    public int MinSpawnCount = 1;
    public int? MaxSpawnCount = 1;
    public float SpawnHeightFromGround = 1f;
    public float SpawnGap; // distance between spawns to avoid physics exploding
    // rigidbody
    public float MinScale = 1f;
    public float MaxScale = 1f;
    public float MinMass = 1000f; // at least 1000 for trains to "crash"
    public float MaxMass = 1000f;
    public float Drag = 0f; // how much air/liquid holds it back (no effect on fall speed)
    public float AngularDrag = 0f; // how much it spins in the air (???)
    // physicmaterial
    public float DynamicFriction = 1.0f;
    public float StaticFriction = 1.0f;
    public float Bounciness = 0f;
    // physics
    public float Gravity = 1f; // multiplier to increase fall speed
    public Quaternion? RotationOffset;
    public float ImpulseThreshold = 150000; // newton-seconds (light bump in DE2 is 130,000~)
    // public string SoundName;
    // public float SoundRadius;

    public Obstacle Clone()
    {
        return new Obstacle()
        {
            // basics
            Type = Type,
            Biome = Biome,
            AssetBundleName = AssetBundleName,
            PrefabName = PrefabName,
            InPool = InPool,
            // spawning
            MinSpawnCount = MinSpawnCount,
            MaxSpawnCount = MaxSpawnCount,
            SpawnHeightFromGround = SpawnHeightFromGround,
            SpawnGap = SpawnGap,
            // rigidbody
            MinScale = MinScale,
            MaxScale = MaxScale,
            MinMass = MinMass,
            MaxMass = MaxMass,
            Drag = Drag,
            AngularDrag = AngularDrag,
            // physics
            DynamicFriction = DynamicFriction,
            StaticFriction = StaticFriction,
            Bounciness = Bounciness,
            RotationOffset = RotationOffset,
            // more physics
            Gravity = Gravity,
            ImpulseThreshold = ImpulseThreshold
        };
    }

    public override string ToString()
    {
        return "Obstacle(" +
$"Type={Type}," +
$"Biome={Biome}," +
$"MinSpawnCount={MinSpawnCount}," +
$"MaxSpawnCount={MaxSpawnCount}," +
$"SpawnHeightFromGround={SpawnHeightFromGround}," +
$"SpawnGap={SpawnGap}," +
$"MinScale={MinScale}," +
$"MaxScale={MaxScale}," +
$"MinMass={MinMass}," +
$"MaxMass={MaxMass}," +
$"Drag={Drag}," +
$"AngularDrag={AngularDrag}," +
$"DynamicFriction={DynamicFriction}," +
$"StaticFriction={StaticFriction}," +
$"Bounciness={Bounciness}," +
$"RotationOffset={RotationOffset}," +
$"Gravity={Gravity}," +
$"ImpulseThreshold={ImpulseThreshold})";
    }
}

public class RandomEventsManager
{
    private UnityModManager.ModEntry.ModLogger Logger => Main.ModEntry.Logger;
    private readonly System.Random _rng = new System.Random();
    private float _nextCheckTime;
    private float _nextEligibleEventTime;
    private Dictionary<ObstacleType, AssetBundle> _obstacleBundles = [];
    private GameObject? _updateDriver;
    private GameObject? _debugSphere;
    private float _nextCleanupTime;
    public Obstacle? OverrideObstacle = null;

    public Dictionary<ObstacleType, Obstacle> Obstacles = new()
    {
        [ObstacleType.Rockslide] = new Obstacle()
        {
            Type = ObstacleType.Rockslide,
            Biome = Biome.Rock,
            MinSpawnCount = 5,
            MaxSpawnCount = 10,
            AssetBundleName = "rocks",
            SpawnHeightFromGround = 5f,
            SpawnGap = 3f,
            MinScale = 1.5f,
            MaxScale = 2f,
            MinMass = 5000f,
            MaxMass = 7500f,
            Drag = 0,
            AngularDrag = 1f,
            DynamicFriction = 0.9f,
            StaticFriction = 0.9f,
            Bounciness = 0,
            Gravity = 2
        },
        [ObstacleType.FallenTrees] = new Obstacle()
        {
            Type = ObstacleType.FallenTrees,
            Biome = Biome.Forest,
            MinSpawnCount = 1,
            MaxSpawnCount = 1,
            AssetBundleName = "trees",
            SpawnHeightFromGround = 2f,
            SpawnGap = 3f,
            MinScale = 4f,
            MaxScale = 6f,
            MinMass = 15000f,
            MaxMass = 25000f,
            Drag = 0,
            AngularDrag = 3f,
            DynamicFriction = 0.9f,
            StaticFriction = 0.9f,
            Bounciness = 0,
            RotationOffset = Quaternion.Euler(90, 0, 0), // lay across track
            Gravity = 2
        },
        [ObstacleType.FunRamp] = new Obstacle()
        {
            InPool = false,
            Type = ObstacleType.FunRamp,
            MinSpawnCount = 1,
            MaxSpawnCount = 1,
            AssetBundleName = "fun",
            PrefabName = "ObstacleRamp",
            SpawnHeightFromGround = 2f,
            SpawnGap = 3f,
            MinScale = 1f,
            MaxScale = 1f,
            MinMass = 999999f,
            MaxMass = 999999f,
            Drag = 0,
            AngularDrag = 0,
            DynamicFriction = 0,
            StaticFriction = 0,
            Bounciness = 0,
            ImpulseThreshold = 50000 // very low for launch!
        },
    };

    public enum EventCategory
    {
        Obstacle
    }

    public void Start()
    {
        Logger.Log("[RandomEventsManager] Start");

        var now = Time.time;
        _nextCheckTime = now + Main.settings.CheckIntervalSeconds;
        _nextEligibleEventTime = now + Main.settings.InitialMinDelay;
        _nextCleanupTime = Time.time + 30f;

        CreateUpdateDriver();
    }

    private void CreateUpdateDriver()
    {
        Logger.Log("[RandomEventsManager] Create UpdateDriver");

        _updateDriver = new GameObject("DerailValleyRandomEvents_UpdateDriver");
        UnityEngine.Object.DontDestroyOnLoad(_updateDriver);
        var comp = _updateDriver.AddComponent<UpdateDriver>();
        comp.OnFrame = OnFrame;
    }

    public void OnFrame()
    {
        try
        {

            var now = Time.time;

            if (now >= _nextCleanupTime)
            {
                Cleanup();
                _nextCleanupTime = now + 30f;
            }

            if (now < _nextCheckTime)
                return;

            _nextCheckTime = now + Main.settings.CheckIntervalSeconds;

            if (Main.settings.ShowDebugStuff)
                DrawDebugStuff();

            if (now < _nextEligibleEventTime)
                return;

            if (!GetShouldEmitRandomEvent())
                return;

            EmitRandomEvent();

            var min = Main.settings.MinIntervalSeconds;
            var max = Main.settings.MaxIntervalSeconds;
            var interval = (float)(_rng.NextDouble() * (max - min) + min);

            _nextEligibleEventTime = now + interval;
        }
        catch (Exception ex)
        {
            Logger.Log($"OnFrame failed: {ex}");
        }
    }

    private void Cleanup()
    {
        Logger.Log("[RandomEventsManager] Cleanup");
        // TODO: this
    }

    public void RegisterObstacle(Obstacle obstacle)
    {
        Obstacles[obstacle.Type] = obstacle;
    }

    private bool GetIsInOrOnAnyTrainCar()
    {
        return PlayerManager.Car != null;
    }

    private void DrawDebugStuff()
    {
        if (!GetIsInOrOnAnyTrainCar())
            return;

        var obstacleLocalPos = GetObstaclePositionFromCarLocal();

        if (obstacleLocalPos == null)
            return;

        if (_debugSphere == null)
        {
            Logger.Log("[RandomEventsManager] Create debug sphere...");

            _debugSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            _debugSphere.transform.localScale = new Vector3(2f, 2f, 2f);

            var parent = WorldMover.OriginShiftParent;
            _debugSphere.transform.SetParent(parent);

            var collider = _debugSphere.GetComponent<Collider>();
            if (collider != null)
                GameObject.Destroy(collider);

            var renderer = _debugSphere.GetComponent<MeshRenderer>();
            renderer.material.color = Color.red;
        }

        var newSpherePos = new Vector3(obstacleLocalPos.Value.x, obstacleLocalPos.Value.y + 5f, obstacleLocalPos.Value.z);
        _debugSphere.transform.position = newSpherePos;
    }

    private bool GetShouldEmitRandomEvent()
    {
        if (!Main.settings.RandomSpawningEnabled)
            return false;

        var chance = Main.settings.RandomChance;
        return _rng.NextDouble() < chance;
    }

    public void EmitRandomEvent()
    {
        var category = GetRandomCategory();

        Logger.Log($"[RandomEventsManager] Emit random event category={category}");

        switch (category)
        {
            case EventCategory.Obstacle:
                EmitObstacleEventAhead();
                break;
        }
    }

    private EventCategory GetRandomCategory()
    {
        var values = (EventCategory[])Enum.GetValues(typeof(EventCategory));
        return values[_rng.Next(values.Length)];
    }

    private ObstacleType GetRandomObstacleType()
    {
        var poolable = Obstacles
            .Where(x => x.Value.InPool != false)
            .Select(x => x.Key)
            .ToList();

        return poolable[_rng.Next(poolable.Count)];
    }

    public void EmitObstacleEventAtPos(Vector3 localPos, Obstacle incomingObstacle, GameObject prefab, Quaternion? overrideRotation = null)
    {
        // avoid any reference issues too
        var obstacle = OverrideObstacle != null ? OverrideObstacle.Clone() : incomingObstacle.Clone();

        var obstaclePosInSky = new Vector3(localPos.x, localPos.y + obstacle.SpawnHeightFromGround, localPos.z);

        var spawnCount = obstacle.MaxSpawnCount == null ? obstacle.MinSpawnCount : UnityEngine.Random.Range(obstacle.MinSpawnCount, (int)obstacle.MaxSpawnCount);

        Logger.Log($"[RandomEventsManager] Emit obstacle event at position={localPos} obstacle={incomingObstacle} prefab={prefab} count={spawnCount} ({obstacle.MinSpawnCount} -> {obstacle.MaxSpawnCount})");

        for (var i = 0; i < obstacle.MinSpawnCount; i++)
        {
            Logger.Log($"[RandomEventsManager] Spawn obstacle #{i}");

            // add some tiny position changes to avoid unity stacking them up
            var jitterDistance = 0.25f;

            // cannot spawn multiple objects inside each other otherwise physics freaks out
            var gap = i * obstacle.SpawnGap;

            var thisObstaclePosInSky = new Vector3(obstaclePosInSky.x + jitterDistance, obstaclePosInSky.y + gap, obstaclePosInSky.z);

            ObstactleSpawner.Spawn(prefab, obstacle, thisObstaclePosInSky, overrideRotation);
        }
    }

    public void EmitObstacleEventAhead(ObstacleType? overrideType = null, float? distance = null)
    {
        Logger.Log($"[RandomEventsManager] Emit obstacle event ahead override={overrideType} speed={GetTrainSpeed()} forwards={GetIsTrainMovingForwards()}");

        var obstacleLocalPos = GetObstaclePositionFromCarLocal(distance);

        if (obstacleLocalPos == null)
            return;

        var currentBiome = GetCurrentBiome();

        Logger.Log($"[RandomEventsManager] Current biome: {currentBiome}");

        // TODO: decide based on biome eg. rockslide when in mountains, cow on track near farm, etc.
        var obstacleType = overrideType != null ? (ObstacleType)overrideType : GetRandomObstacleType();
        var obstacle = Obstacles[obstacleType];

        if (obstacle == null)
            throw new Exception($"Could not find obstacle from type '{obstacleType}'");

        Logger.Log($"[RandomEventsManager] Obstacle of type {obstacleType}: {obstacle}");

        currentBiome = Biome.Rock;

        Logger.Log($"[RandomEventsManager] Using biome: {currentBiome}");

        var prefab = GetObstaclePrefab(obstacle);

        Logger.Log($"[RandomEventsManager] Using prefab: {prefab}");

        EmitObstacleEventAtPos((Vector3)obstacleLocalPos, obstacle, prefab);
    }

    private Biome GetCurrentBiome()
    {
        BiomeProvider instance = SingletonBehaviour<BiomeProvider>.Instance;
        var currentBiome = instance.CurrentBiome;
        return currentBiome;
    }

    public GameObject GetObstaclePrefab(Obstacle obstacle)
    {
        AssetBundle bundle;

        if (_obstacleBundles.ContainsKey(obstacle.Type))
        {
            bundle = _obstacleBundles[obstacle.Type];
        }
        else
        {
            bundle = LoadObstacleBundle(obstacle);

            _obstacleBundles[obstacle.Type] = bundle;
        }

        var all = bundle.LoadAllAssets<GameObject>();

        Logger.Log($"[RandomEventsManager] Found asset objects: {string.Join(",", all.Select(x => x.name))}");

        if (obstacle.PrefabName != null)
        {
            var match = all.First(x => x.name == obstacle.PrefabName) ?? throw new Exception($"Prefab '{obstacle.PrefabName}' not found in assetbundle");
            return match;
        }

        return all[_rng.Next(all.Length)];
    }

    private AssetBundle LoadBundle(string pathInsideAssetBundles)
    {
        var bundlePath = Path.Combine(Main.ModEntry.Path, "Dependencies/AssetBundles", pathInsideAssetBundles);

        Logger.Log($"[RandomEventsManager] Loading bundle from: {bundlePath}");

        if (!File.Exists(bundlePath))
            throw new Exception($"Asset bundle not found at {bundlePath}");

        return AssetBundle.LoadFromFile(bundlePath);
    }

    private AssetBundle LoadObstacleBundle(Obstacle obstacle)
    {
        return LoadBundle(obstacle.AssetBundleName);
    }

    private float? GetTrainSpeed()
    {
        if (PlayerManager.Car == null)
            return null;

        return PlayerManager.Car.GetForwardSpeed();
    }

    public bool? GetIsTrainMovingForwards()
    {
        var speed = GetTrainSpeed();

        if (speed < 0.01 && speed > -0.01)
            return null;

        var movingFowards = speed > 0;

        return movingFowards;
    }

    private Vector3? GetObstaclePositionFromCarLocal(float? overrideDistance = null)
    {
        Logger.Log($"[RandomEventsManager] Choosing obstacle position...");

        if (PlayerManager.Car == null)
            return null;

        var car = PlayerManager.Car!;

        var (currentTrack, currentPoint) = RailTrack.GetClosest(car.transform.position);

        if (!currentPoint.HasValue)
            throw new Exception("Failed to get closest point");

        var startLocalPos = car.transform.position;

        Logger.Log($"[RandomEventsManager] Starting at track '{currentTrack.name}' at position {startLocalPos}");

        var distance = overrideDistance != null ? overrideDistance.Value : Main.settings.ObstacleSpawnDistance;

        var (resultTrack, resultLocalPos) = TrackWalking.GetAheadTrack(currentTrack, startLocalPos, car.rb.velocity, distance);

        Logger.Log($"[RandomEventsManager] Chosen position {resultLocalPos} on track '{resultTrack.name}'");

        return resultLocalPos;
    }

    public void Reset()
    {
        Logger.Log($"[RandomEventsManager] Reset");

        GameObject.Destroy(_debugSphere);
    }

    public void Stop()
    {
        Logger.Log("[RandomEventsManager] Stop");
        GameObject.Destroy(_updateDriver);
        GameObject.Destroy(_debugSphere);

        foreach (var kv in _obstacleBundles)
        {
            var assetBundle = kv.Value;

            Logger.Log($"[RandomEventsManager] Unload '{assetBundle.name}'");

            assetBundle.Unload(true);
        }
    }
}
