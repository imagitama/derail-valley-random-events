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
    // RiverFlood
}

public class Obstacle
{
    public Obstacle() { }

    // for overriding only!
    public Obstacle(Obstacle other)
    {
        // Type = other.Type;
        // Biome = other.Biome;
        FallTimeMs = other.FallTimeMs;
        SpawnCount = other.SpawnCount;
        AssetBundleName = other.AssetBundleName;
        SpawnHeightFromGround = other.SpawnHeightFromGround;
        SpawnGap = other.SpawnGap;
        MinScale = other.MinScale;
        MaxScale = other.MaxScale;
        MinMass = other.MinMass;
        MaxMass = other.MaxMass;
        Rotate = other.Rotate;
    }

    public ObstacleType Type;
    public Biome Biome;
    public int FallTimeMs;
    public int SpawnCount;
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    public string AssetBundleName;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    public float SpawnHeightFromGround;
    public float SpawnGap; // distance between spawns to avoid physics exploding
    public float MinScale;
    public float MaxScale;
    public float MinMass;
    public float MaxMass;
    public Quaternion? Rotate;
    // public string SoundName;
    // public float SoundRadius;
}

public class RandomEventsManager
{
    private UnityModManager.ModEntry.ModLogger Logger => Main.ModEntry.Logger;
    private readonly System.Random _rng = new System.Random();
    private float _nextCheckTime;
    private float _nextEligibleEventTime;
    private Dictionary<Obstacle, AssetBundle> _obstacleBundles = [];
    private GameObject? _updateDriver;
    private GameObject? _debugSphere;
    private float _nextCleanupTime;
    public bool EnableRandomEvents = true;
    public bool ShouldDrawDebugStuff = false;
    public Obstacle? OverrideObstacle = null;

    public Dictionary<ObstacleType, Obstacle> obstacles = new()
    {
        [ObstacleType.Rockslide] = new Obstacle()
        {
            Type = ObstacleType.Rockslide,
            Biome = Biome.Rock,
            FallTimeMs = 250,
            SpawnCount = 5,
            AssetBundleName = "rocks",
            SpawnHeightFromGround = 5f,
            SpawnGap = 3f,
            MinScale = 0.8f,
            MaxScale = 1.2f,
            MinMass = 300f,
            MaxMass = 500f
        },
        // [ObstacleType.CowsOnTrack] = new Obstacle()
        // {
        //     Type = ObstacleType.CowsOnTrack,
        //     FallTimeMs = 250,
        //     SpawnCount = 5,
        //     AssetBundleName = "cows",
        //     SpawnHeightFromGround = 5f
        // },
        [ObstacleType.FallenTrees] = new Obstacle()
        {
            Type = ObstacleType.FallenTrees,
            Biome = Biome.Forest,
            FallTimeMs = 250,
            SpawnCount = 1,
            AssetBundleName = "trees",
            SpawnHeightFromGround = 1f,
            SpawnGap = 3f,
            MinScale = 2f,
            MaxScale = 3f,
            MinMass = 200f,
            MaxMass = 300f,
            Rotate = Quaternion.Euler(90, 0, 0)
        },
        // [ObstacleType.RiverFlood] = new Obstacle()
        // {
        //     Type = ObstacleType.RiverFlood,
        //     FallTimeMs = 250,
        //     SpawnCount = 1,
        //     AssetBundleName = "river_flood",
        //     SpawnHeightFromGround = 5f
        // },
    };

    public enum EventCategory
    {
        Obstacle
    }

    public void Start()
    {
        Logger.Log("[RandomEventsManager] Start");

        var now = Time.time;
        _nextCheckTime = now + Main.Settings.CheckIntervalSeconds;
        _nextEligibleEventTime = now + Main.Settings.InitialMinDelay;
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

    private void DestroyUpdateDriver()
    {
        Logger.Log("[RandomEventsManager] Destroy UpdateDriver");

        if (_updateDriver != null)
            GameObject.Destroy(_updateDriver);
    }

    public void Stop()
    {
        Logger.Log("[RandomEventsManager] Stop");
        DestroyUpdateDriver();
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

            _nextCheckTime = now + Main.Settings.CheckIntervalSeconds;

            if (ShouldDrawDebugStuff)
                DrawDebugStuff();

            if (now < _nextEligibleEventTime)
                return;


            if (!GetShouldEmitRandomEvent())
                return;

            EmitRandomEvent();

            var min = Main.Settings.MinIntervalSeconds;
            var max = Main.Settings.MaxIntervalSeconds;
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

    private bool GetIsInOrOnAnyTrainCar()
    {
        return PlayerManager.Car != null;
    }

    private void DrawDebugStuff()
    {
        if (!GetIsInOrOnAnyTrainCar())
            return;

        var posLocalNullable = GetObstaclePositionFromCarLocal();

        if (posLocalNullable == null)
            return;

        var posLocal = (Vector3)posLocalNullable;

        var rockPos = new Vector3(posLocal.x, posLocal.y + 5f, posLocal.z);
        Vector3 globalPos = rockPos + OriginShift.currentMove;

        if (_debugSphere == null)
        {
            Logger.Log("[RandomEventsManager] Create debug sphere...");

            _debugSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            _debugSphere.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);

            var collider = _debugSphere.GetComponent<Collider>();
            if (collider != null)
                GameObject.Destroy(collider);

            var renderer = _debugSphere.GetComponent<MeshRenderer>();
            renderer.material.color = Color.red;
        }

        _debugSphere.transform.position = globalPos;
    }

    private bool GetShouldEmitRandomEvent()
    {
        if (!EnableRandomEvents)
            return false;

        var chance = Main.Settings.RandomChance;
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
        var values = (ObstacleType[])Enum.GetValues(typeof(ObstacleType));
        return values[_rng.Next(values.Length)];
    }

    public void EmitObstacleEventAtPos(Vector3 localPos, Obstacle incomingObstacle, GameObject prefab)
    {
        Logger.Log($"[RandomEventsManager] Emit obstacle event at position={localPos} obstacle={incomingObstacle} prefab={prefab}");

        // avoid any reference issues too
        var obstacle = new Obstacle(OverrideObstacle ?? incomingObstacle);

        var obstaclePosInSky = new Vector3(localPos.x, localPos.y + obstacle.SpawnHeightFromGround, localPos.z);

        for (var i = 0; i < obstacle.SpawnCount; i++)
        {
            Logger.Log($"[RandomEventsManager] Spawn obstacle #{i}");

            // add some tiny position changes to avoid unity stacking them up
            var jitterDistance = 0.25f;

            // cannot spawn multiple objects inside each other otherwise physics freaks out
            var gap = i * obstacle.SpawnGap;

            var thisObstaclePosInSky = new Vector3(obstaclePosInSky.x + jitterDistance, obstaclePosInSky.y + gap, obstaclePosInSky.z);

            ObstactleSpawner.Spawn(prefab, obstacle, thisObstaclePosInSky);
        }
    }

    public void EmitObstacleEventAhead(ObstacleType? overrideType = null, bool? forceForwards = null)
    {
        Logger.Log($"[RandomEventsManager] Emit obstacle event ahead override={overrideType} forceForwards={forceForwards} speed={GetTrainSpeed()} forwards={GetIsTrainMovingForwards()}");

        var obstacleLocalPos = GetObstaclePositionFromCarLocal(forceForwards);

        if (obstacleLocalPos == null)
            return;

        var currentBiome = GetCurrentBiome();

        Logger.Log($"[RandomEventsManager] Current biome: {currentBiome}");

        // TODO: decide based on biome eg. rockslide when in mountains, cow on track near farm, etc.
        var obstacleType = overrideType != null ? (ObstacleType)overrideType : GetRandomObstacleType();
        var obstacle = obstacles[obstacleType];

        Logger.Log($"[RandomEventsManager] Obstacle of type {obstacleType}: {obstacle}");

        currentBiome = Biome.Rock;

        Logger.Log($"[RandomEventsManager] Using biome: {currentBiome}");

        var prefab = GetRandomObstaclePrefab(obstacle);

        Logger.Log($"[RandomEventsManager] Using prefab: {prefab}");

        EmitObstacleEventAtPos((Vector3)obstacleLocalPos, obstacle, prefab);
    }

    private Biome GetCurrentBiome()
    {
        BiomeProvider instance = SingletonBehaviour<BiomeProvider>.Instance;
        var currentBiome = instance.CurrentBiome;
        return currentBiome;
    }

    public GameObject GetRandomObstaclePrefab(Obstacle obstacle)
    {
        AssetBundle bundle;

        if (_obstacleBundles.ContainsKey(obstacle))
        {
            bundle = _obstacleBundles[obstacle];
        }
        else
        {
            bundle = LoadObstacleBundle(obstacle);

            _obstacleBundles[obstacle] = bundle;
        }

        var all = bundle.LoadAllAssets<GameObject>();

        Logger.Log($"[RandomEventsManager] Found asset objects: {string.Join(",", all.Select(x => x.name))}");

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

    private Vector3? GetPlayerLocalPosition()
    {
        if (PlayerManager.PlayerTransform?.position == null || OriginShift.currentMove == null)
            return null;

        Vector3 worldPos = PlayerManager.PlayerTransform.position;
        return worldPos - OriginShift.currentMove;
    }

    private Vector3? GetObstaclePositionFromCarLocal(bool? forceForwards = null)
    {
        if (PlayerManager.Car == null)
            return null;

        var isMovingForward = GetIsTrainMovingForwards();

        if (isMovingForward == null && forceForwards == null)
            return null;

        if (forceForwards != null)
            isMovingForward = forceForwards;

        var playerLocalPos = GetPlayerLocalPosition();

        if (playerLocalPos == null)
            return null;

        Vector3 currentMove = OriginShift.currentMove;
        Vector3 playerGlobalPos = (Vector3)playerLocalPos + currentMove;

        Logger.Log($"[RandomEventsManager] Player is at local={playerLocalPos} global={playerGlobalPos} currentMove={currentMove}");

        var (currentTrack, currentPoint) = RailTrack.GetClosest(playerGlobalPos);

        if (currentTrack == null)
        {
            Logger.Log("[RandomEventsManager] No track near player");
            return null;
        }

        Logger.Log($"[RandomEventsManager] Closest track is {currentTrack} at {(currentPoint != null ? currentPoint.Value.position : "null")}");

        Vector3? obstacleLocalPos = ObstacleHelper.WalkAlongTrackLocal(currentTrack, (Vector3)playerLocalPos, Main.Settings.ObstacleSpawnDistance, (bool)isMovingForward!, PlayerManager.Car.transform.forward);
        return obstacleLocalPos;
    }
}
