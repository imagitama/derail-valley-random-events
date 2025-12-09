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
    Cows,
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
    public float? VerticalSpawnGap; // distance between spawns to avoid physics exploding
    public float? HorizontalSpawnGap; // distance between spawns to avoid physics exploding
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
    public float DerailThreshold = 150000; // newton-seconds (light bump in DE2 is 130,000~)
    // exploding!
    public float? ExplodeThreshold; // newton-seconds (light bump in DE2 is 130,000~)
    public float? ExplodeForce = 10;
    public float? ExplodeRadius = 5;
    public float? ExplodeUpwards = 5;
    public bool LookAtPlayer = false;
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
            VerticalSpawnGap = VerticalSpawnGap,
            HorizontalSpawnGap = HorizontalSpawnGap,
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
            DerailThreshold = DerailThreshold,
            // exploding
            ExplodeThreshold = ExplodeThreshold,
            ExplodeForce = ExplodeForce,
            ExplodeRadius = ExplodeRadius,
            ExplodeUpwards = ExplodeUpwards,
            // other
            LookAtPlayer = LookAtPlayer
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
$"VerticalSpawnGap={VerticalSpawnGap}," +
$"HorizontalSpawnGap={HorizontalSpawnGap}," +
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
$"DerailThreshold={DerailThreshold})";
    }
}

public class SpawnEvent
{
    public ObstacleType? obstacleType;
    public float distance;
    // optional
    public bool ignoreNearbyCheck = false;
    public bool flipDirection = false;
    // populate later
    public Vector3? intendedPos;
    public bool? isForward;
    public RailTrack? intendedTrack;
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

    private bool GetIsInOrOnAnyTrainCar()
    {
        return PlayerManager.Car != null;
    }

    private void DrawDebugStuff()
    {
        if (!GetIsInOrOnAnyTrainCar())
            return;

        var (track, obstacleLocalPos) = GetObstaclePositionFromCarLocal();

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

        var newSpherePos = new Vector3(obstacleLocalPos.x, obstacleLocalPos.y + 5f, obstacleLocalPos.z);
        _debugSphere.transform.position = newSpherePos;
    }

    private bool GetShouldEmitRandomEvent()
    {
        if (!Main.settings.RandomSpawningEnabled)
            return false;

        if (PlayerManager.Car == null || PlayerManager.Car.GetAbsSpeed() == 0)
            return false;

        var chance = Main.settings.RandomChance;
        return _rng.NextDouble() < chance;
    }

    public void EmitRandomEvent()
    {
        var category = GetRandomCategory();

        Logger.Log($"[RandomEventsManager] Emit random event category={category}");

        var spawnEvent = new SpawnEvent();

        switch (category)
        {
            case EventCategory.Obstacle:
                EmitObstacleEventAhead(spawnEvent);
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
        var poolable = ObstacleRegistry.Obstacles
            .Where(x => x.InPool != false)
            .Select(x => x.Type)
            .ToList();

        return poolable[_rng.Next(poolable.Count)];
    }

    public void EmitObstacleEventAtPos(SpawnEvent spawnEvent, Obstacle incomingObstacle, GameObject prefab, Quaternion? overrideRotation = null)
    {
        // avoid any reference issues too
        var obstacle = OverrideObstacle != null ? OverrideObstacle.Clone() : incomingObstacle.Clone();

        var spawnCount = obstacle.MaxSpawnCount == null ? obstacle.MinSpawnCount : UnityEngine.Random.Range(obstacle.MinSpawnCount, (int)obstacle.MaxSpawnCount);

        var localPos = spawnEvent.intendedPos;

        if (localPos == null)
            throw new Exception("Cannot emit obstacle event without a position");

        Logger.Log($"[RandomEventsManager] Emit obstacle event at position={localPos} type={incomingObstacle.Type} prefab={prefab} count={spawnCount} ({obstacle.MinSpawnCount} -> {obstacle.MaxSpawnCount})");

        var obstaclePosInSky = new Vector3(localPos.Value.x, localPos.Value.y + obstacle.SpawnHeightFromGround, localPos.Value.z);

        var objects = new List<GameObject>();

        for (var i = 0; i < spawnCount; i++)
        {
            Logger.Log($"[RandomEventsManager] Spawn obstacle #{i}");

            var obj = ObstacleSpawner.Create(prefab, obstacle, overrideRotation);

            objects.Add(obj);

            var jitterDistance = 0.25f;

            Vector3 spawnPos = obstaclePosInSky;
            spawnPos.x += jitterDistance;

            // cannot spawn multiple objects inside each other otherwise physics freaks out
            if (obstacle.VerticalSpawnGap != null)
            {
                var gap = i * obstacle.VerticalSpawnGap.Value;
                spawnPos.y += gap;
            }
            else if (obstacle.HorizontalSpawnGap != null)
            {
                var track = spawnEvent.intendedTrack;

                if (track == null)
                    throw new Exception("Need a track");

                float offset = i - (spawnCount - 1) / 2f;
                var gap = offset * obstacle.HorizontalSpawnGap.Value;

                spawnPos.z += gap;
            }

            spawnPos = AddJitter(spawnPos, 1f);

            obj.transform.position = spawnPos;
        }
    }

    Vector3 AddJitter(Vector3 pos, float maxJitterAmount)
    {
        float x = UnityEngine.Random.Range(-maxJitterAmount, maxJitterAmount);
        float z = UnityEngine.Random.Range(-maxJitterAmount, maxJitterAmount);
        return new Vector3(pos.x + x, pos.y, pos.z + z);
    }

    public bool GetIsObstacleNearby(Vector3 localPos)
    {
        var obstacles = ObstacleSpawner.GetAllObstacles();
        var threshold = 250f;

        if (obstacles.Count == 0)
            return false;

        foreach (var obstacle in obstacles)
            if (Vector3.Distance(obstacle.transform.position, localPos) < threshold)
                return true;

        return false;
    }

    public bool EmitObstacleEventAhead(SpawnEvent spawnEvent)
    {
        Logger.Log($"[RandomEventsManager] Emit obstacle event ahead type={spawnEvent.obstacleType}");

        var (track, obstacleLocalPos) = GetObstaclePositionFromCarLocal(spawnEvent.distance, spawnEvent.flipDirection);

        spawnEvent.intendedTrack = track;
        spawnEvent.intendedPos = obstacleLocalPos;

        if (GetIsObstacleNearby(obstacleLocalPos) && spawnEvent.ignoreNearbyCheck != true)
        {
            Logger.Log($"[RandomEventsManager] Another obstacle is within area - skipping");
            return false;
        }

        var currentBiome = GetCurrentBiome();

        Logger.Log($"[RandomEventsManager] Current biome: {currentBiome}");

        // TODO: decide based on biome eg. rockslide when in mountains, cow on track near farm, etc.
        var obstacleType = spawnEvent.obstacleType != null ? spawnEvent.obstacleType.Value : GetRandomObstacleType();
        var obstacle = ObstacleRegistry.GetObstacleByType(obstacleType);

        if (obstacle == null)
            throw new Exception($"Could not find obstacle from type '{obstacleType}'");

        Logger.Log($"[RandomEventsManager] Obstacle of type {obstacleType}: {obstacle}");

        currentBiome = Biome.Rock;

        Logger.Log($"[RandomEventsManager] Using biome: {currentBiome}");

        var prefab = GetObstaclePrefab(obstacle);

        Logger.Log($"[RandomEventsManager] Using prefab: {prefab}");

        EmitObstacleEventAtPos(spawnEvent, obstacle, prefab);

        return true;
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

    private (RailTrack, Vector3) GetObstaclePositionFromCarLocal(float? overrideDistance = null, bool? flipDirection = false)
    {
        Logger.Log($"[RandomEventsManager] Choosing obstacle position...");

        if (PlayerManager.Car == null)
            throw new Exception("Need a car");

        var car = PlayerManager.Car!;

        var (currentTrack, currentPoint) = RailTrack.GetClosest(car.transform.position);

        if (!currentPoint.HasValue)
            throw new Exception("Failed to get closest point");

        var startLocalPos = car.transform.position;

        Logger.Log($"[RandomEventsManager] Starting at track '{currentTrack.name}' at position {startLocalPos}");

        var distance = overrideDistance != null ? overrideDistance.Value : Main.settings.ObstacleSpawnDistance;

        var (resultTrack, resultLocalPos) =
            PlayerManager.Car.GetAbsSpeed() == 0 ?
                TrackWalking.GetAheadTrack(currentTrack, startLocalPos, flipDirection == true ? -currentPoint.Value.forward : currentPoint.Value.forward, distance) :
                TrackWalking.GetAheadTrack(currentTrack, startLocalPos, flipDirection == true ? -car.rb.velocity : car.rb.velocity, distance);

        Logger.Log($"[RandomEventsManager] Chosen position {resultLocalPos} on track '{resultTrack.name}'");

        return (resultTrack, resultLocalPos);
    }

    public void Reset()
    {
        Logger.Log($"[RandomEventsManager] Reset");

        // trigger re-create
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
