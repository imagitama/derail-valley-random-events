using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DV.WorldTools;
using DV.Utils;
using UnityEngine;
using UnityModManagerNet;
using System.Threading.Tasks;

namespace DerailValleyRandomEvents;

public class RandomEventsManager
{
    private UnityModManager.ModEntry.ModLogger Logger => Main.ModEntry.Logger;
    private readonly System.Random _rng = new System.Random();
    private float _nextCheckTime;
    private float _nextEligibleEventTime;
    private Dictionary<ObstacleType, AssetBundle> _obstacleBundles = [];
    private GameObject? _updateDriver;
    private GameObject? _debugSphere;
    private float? _nextCleanupTime;
    public Obstacle? OverrideObstacle = null;
    public bool PreventAllObstacleDerailment = false;
    public static bool IsCleanupEnabled = true;

    public enum EventCategory
    {
        Obstacle
    }

    public void Start()
    {
        Logger.Log("[RandomEventsManager] Start");

        var now = Time.time;
        _nextCheckTime = now + Main.settings.CheckIntervalSeconds;
        _nextEligibleEventTime = now + Main.settings.InitialDelay;
        _nextCleanupTime = Time.time + 30f;

        CreateUpdateDriver();
    }

    public void CreateUpdateDriver()
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
            SpawnerHelper.OnFrame();

            var now = Time.time;

            if (_nextCleanupTime != null && now >= _nextCleanupTime)
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

    public void Cleanup()
    {
        if (!IsCleanupEnabled)
        {
            return;
        }

        var playerPos = PlayerManager.PlayerTransform.transform.position;

        var obstacles = ObstacleSpawner.GetAllObstacles().ToList();

        Logger.Log($"[RandomEventsManager] Cleanup ({obstacles.Count})");

        var cleanupCount = 0;

        foreach (var obstacle in obstacles)
        {
            var distance = Vector3.Distance(playerPos, obstacle.transform.position);
            var maxDistance = Main.settings.ObstacleSpawnDistance + 50f;
            if (distance > maxDistance)
            {
                Logger.Log($"[RandomEventsManager] Cleaning up '{obstacle}' (distance {distance} > {maxDistance})");
                ObstacleSpawner.CleanupObstacle(obstacle);
                cleanupCount++;
            }
        }

        Logger.Log($"[RandomEventsManager] Cleaned up {cleanupCount} obstacles");
    }

    public bool GetIsInOrOnAnyTrainCar()
    {
        return PlayerManager.Car != null;
    }

    private void DrawDebugStuff()
    {
        if (!GetIsInOrOnAnyTrainCar())
            return;

        var (track, obstacleLocalPos, rotation) = GetObstaclePositionFromCarLocal(Main.settings.ObstacleSpawnDistance);

        if (_debugSphere == null)
        {
            Logger.Log("[RandomEventsManager] Create debug sphere...");

            _debugSphere = CreateDebugSphere();

            var parent = WorldMover.OriginShiftParent;
            _debugSphere.transform.SetParent(parent);
        }

        var newSpherePos = new Vector3(obstacleLocalPos.x, obstacleLocalPos.y + 5f, obstacleLocalPos.z);
        _debugSphere.transform.position = newSpherePos;
    }

    public static GameObject CreateDebugSphere(float scale = 2f)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.transform.localScale = new Vector3(scale, scale, scale);

        var collider = go.GetComponent<Collider>();
        if (collider != null)
            GameObject.Destroy(collider);

        var renderer = go.GetComponent<MeshRenderer>();
        var mat = new Material(Shader.Find("Standard"));
        mat.color = new Color(1f, 0f, 0f, 0.35f);
        mat.SetFloat("_Mode", 3);                       // Transparent
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);
        mat.DisableKeyword("_ALPHATEST_ON");
        mat.EnableKeyword("_ALPHABLEND_ON");
        mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        mat.renderQueue = 3000;

        renderer.material = mat;

        return go;
    }


    public bool GetShouldEmitRandomEvent()
    {
        if (!Main.settings.RandomSpawningEnabled)
            return false;

        if (PlayerManager.Car == null || PlayerManager.Car.GetAbsSpeed() < 1f)
            return false;

        var chance = Main.settings.RandomChance;
        return _rng.NextDouble() < chance;
    }

    public void EmitRandomEvent()
    {
        var category = GetRandomCategory();

        Logger.Log($"[RandomEventsManager] Emit random event category={category}");

        var eventRequest = new EventRequest();

        switch (category)
        {
            case EventCategory.Obstacle:
                EmitObstacleEventAhead(eventRequest);
                break;
        }
    }

    public EventCategory GetRandomCategory()
    {
        var values = (EventCategory[])Enum.GetValues(typeof(EventCategory));
        return values[_rng.Next(values.Length)];
    }

    public Obstacle? GetRandomObstacleForBiome(Biome biome)
    {
        var poolable = ObstacleRegistry.Obstacles
            .Where(x => x.InPool != false && x.Biomes.Contains(biome))
            .ToList();

        if (poolable.Count == 0)
            return null;

        return poolable[_rng.Next(poolable.Count)];
    }

    public Obstacle GetRandomObstacle()
    {
        var poolable = ObstacleRegistry.Obstacles
            .Where(x => x.InPool != false)
            .ToList();

        return poolable[_rng.Next(poolable.Count)];
    }

    public Obstacle GetRandomObstacleForType(ObstacleType type, bool forceEverythingInPool = false)
    {
        var poolable = ObstacleRegistry.Obstacles
            .Where(x => x.Type == type && (forceEverythingInPool || x.InPool != false))
            .ToList();

        return poolable[_rng.Next(poolable.Count)];
    }

    public SpawnedEvent EmitObstacleEvent(EventRequest eventRequest, Obstacle incomingObstacle, GameObject prefab)
    {
        bool showWarning = Main.settings.WarningChance == 0 ? false : UnityEngine.Random.value < Main.settings.WarningChance;
        if (showWarning)
            NotificationHelper.ShowNotificationViaRadio($"An obstacle has been reported ahead! Be careful!");

        // avoid any reference issues too
        var obstacle = OverrideObstacle != null ? OverrideObstacle.Clone() : incomingObstacle.Clone();

        var spawnCount =
            obstacle.MaxSpawnCount != null && obstacle.MinSpawnCount != null ?
                UnityEngine.Random.Range(obstacle.MinSpawnCount, obstacle.MaxSpawnCount.Value) :
                obstacle.MaxSpawnCount != null ?
                    UnityEngine.Random.Range(1, obstacle.MaxSpawnCount.Value) :
                    obstacle.MinSpawnCount != null ?
                        obstacle.MinSpawnCount :
                        1;

        var localPos = eventRequest.intendedPos;

        if (localPos == null)
            throw new Exception("Cannot emit obstacle event without a position");

        var rotation = eventRequest.intendedRot != null ? eventRequest.intendedRot.Value : Quaternion.identity;

        Logger.Log($"[RandomEventsManager] Emit obstacle event at position={localPos} rotation={rotation} type={obstacle.Type} prefab={prefab} count={spawnCount} ({obstacle.MinSpawnCount} -> {obstacle.MaxSpawnCount}) warn={showWarning}");

        var obstaclePosInSky = new Vector3(localPos.Value.x, localPos.Value.y + obstacle.SpawnHeightFromGround, localPos.Value.z);

        if (obstacle.TranslateOffset != null)
        {
            var offset = (Vector3)obstacle.TranslateOffset;
            var transform = PlayerManager.Car.transform;

            // apply relative to forward/right of the spawner
            obstaclePosInSky +=
                transform.forward * offset.z +   // forward/back
                transform.right * offset.x +   // left/right
                transform.up * offset.y;    // up/down
        }

        if (PreventAllObstacleDerailment)
            obstacle.DerailThreshold = 0;

        var objects = new List<GameObject>();

        if (obstacle.MaxRadius != null)
        {
            SpawnerHelper.Create = ObstacleSpawner.Create;

            // SpawnerHelper.SpawnAround(
            //     localPos.Value,
            //     prefab,
            //     spawnCount,
            //     maxRadius: 1000,
            //     minScale: obstacle.MinScale,
            //     maxScale: obstacle.MaxScale,
            //     obstacle: obstacle
            // );

            AsyncHelper.StartCoroutine(
                SpawnerHelper.SpawnAroundCoroutine(
                    localPos.Value,
                    prefab,
                    spawnCount,
                    maxRadius: 1000,
                    minScale: obstacle.MinScale,
                    maxScale: obstacle.MaxScale,
                    obstacle: obstacle
                )
            );
        }
        else
        {
            for (var i = 0; i < spawnCount; i++)
            {
                Logger.Log($"[RandomEventsManager] Spawn obstacle #{i}");

                var obj = ObstacleSpawner.Create(prefab, obstacle);
                objects.Add(obj);

                obj.transform.localScale = ObstacleSpawner.GetObstacleScale(obstacle);

                obj.transform.rotation = rotation;

                if (ObstacleSpawner.RotationMultiplier != null)
                    obj.transform.rotation *= Quaternion.Euler(ObstacleSpawner.RotationMultiplier.Value);

                var jitterDistance = 0.25f;

                Vector3 spawnPos = obstaclePosInSky;
                spawnPos.x += jitterDistance;

                // cannot spawn multiple objects inside each other otherwise physics freaks out
                if (obstacle.VerticalSpawnGap != null)
                {
                    var gap = i * obstacle.VerticalSpawnGap.Value;
                    spawnPos.y += gap;
                }
                // TODO: combine with vertical?
                else if (obstacle.HorizontalSpawnGap != null)
                {
                    var track = eventRequest.intendedTrack;

                    if (track == null)
                        throw new Exception("Need a track");

                    float offset = i - (spawnCount - 1) / 2f;
                    var gap = offset * obstacle.HorizontalSpawnGap.Value;

                    spawnPos.z += gap;
                }

                if (obstacle.JitterAmount > 0)
                    spawnPos = GetJitteredPos(spawnPos, obstacle.JitterAmount);

                obj.transform.position = spawnPos;

                if (obstacle.RotationOffset != null)
                    obj.transform.rotation = obj.transform.rotation * obstacle.RotationOffset.Value;
            }
        }

        return new SpawnedEvent()
        {
            position = localPos.Value,
            obstacle = obstacle,
            distance = eventRequest.distance!.Value, // trusting it
            count = spawnCount
        };

    }

    Vector3 GetJitteredPos(Vector3 pos, float maxJitterAmount)
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

    public SpawnedEvent? EmitObstacleEventAhead(EventRequest eventRequest)
    {
        if (eventRequest.distance == null)
            eventRequest.distance = Main.settings.ObstacleSpawnDistance;

        Logger.Log($"[RandomEventsManager] Emit obstacle event ahead type={eventRequest.obstacleType} distance={eventRequest.distance}");

        var (track, obstacleLocalPos, rotationAlongTrack) = GetObstaclePositionFromCarLocal(eventRequest.distance.Value, eventRequest.flipDirection);

        eventRequest.intendedTrack = track;
        eventRequest.intendedPos = obstacleLocalPos;
        eventRequest.intendedRot = rotationAlongTrack;

        if (GetIsObstacleNearby(obstacleLocalPos) && eventRequest.ignoreNearbyCheck != true)
        {
            Logger.Log($"[RandomEventsManager] Another obstacle is within area - skipping");
            return null;
        }

        var currentBiome = GetCurrentBiome();

        var inTunnel = TrainCarHelper.GetIsInTunnel(eventRequest.intendedPos.Value);

        if (inTunnel)
        {
            Logger.Log($"[RandomEventsManager] Target is inside a tunnel - skipping");
            return null;
        }

        Logger.Log($"[RandomEventsManager] Current biome: {currentBiome} inTunnel={inTunnel}");

        Biome? biomeToUse;

        if (eventRequest.biome.HasValue)
            biomeToUse = eventRequest.biome.Value;
        else if (eventRequest.ignoreBiome)
            biomeToUse = null;
        else
            biomeToUse = currentBiome;

        Obstacle? obstacle;

        if (eventRequest.obstacleType != null)
            obstacle = GetRandomObstacleForType(eventRequest.obstacleType.Value, eventRequest.forceEverythingInPool);
        else
            obstacle = biomeToUse != null ? GetRandomObstacleForBiome(biomeToUse.Value) : GetRandomObstacle();

        if (obstacle == null)
        {
            Logger.Log($"[RandomEventsManager] No obstacle found for biome '{biomeToUse}' and event type={eventRequest.obstacleType}");
            return null;
        }

        Logger.Log($"[RandomEventsManager] Using obstacle:\n{obstacle}");

        var prefab = GetObstaclePrefab(obstacle);

        Logger.Log($"[RandomEventsManager] Using prefab: {prefab}");

        return EmitObstacleEvent(eventRequest, obstacle, prefab);
    }

    public Biome GetCurrentBiome()
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

        Logger.Log($"[RandomEventsManager] Found asset objects: {string.Join(",", all.Select(x => x.name))} prefabName={obstacle.PrefabName}");

        if (obstacle.PrefabName != null)
            return all.First(x => x.name == obstacle.PrefabName) ?? throw new Exception($"Prefab '{obstacle.PrefabName}' not found in AssetBundle {bundle}");

        return all[_rng.Next(all.Length)];
    }

    public AssetBundle LoadBundle(string pathInsideAssetBundles)
    {
        var bundlePath = Path.Combine(Main.ModEntry.Path, "Dependencies/AssetBundles", pathInsideAssetBundles);

        Logger.Log($"[RandomEventsManager] Loading bundle from: {bundlePath}");

        if (!File.Exists(bundlePath))
            throw new Exception($"Asset bundle not found at {bundlePath}");

        return AssetBundle.LoadFromFile(bundlePath);
    }

    public AssetBundle LoadObstacleBundle(Obstacle obstacle)
    {
        return LoadBundle(obstacle.AssetBundleName);
    }

    public (RailTrack, Vector3, Quaternion) GetObstaclePositionFromCarLocal(float distance, bool? flipDirection = false)
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

        var isTrainStopped = PlayerManager.Car.GetAbsSpeed() < 1f;
        var isForwardOnTrack = TrackWalking.GetIsForwardsOnTrack(currentTrack, car.transform);

        var (resultTrack, resultLocalPos, resultRotationAlongTrack) =
            isTrainStopped ?
                TrackWalking.GetAheadTrack(currentTrack, startLocalPos, flipDirection == true ? !isForwardOnTrack : isForwardOnTrack, distance) :
                TrackWalking.GetAheadTrack(currentTrack, startLocalPos, flipDirection == true ? -car.rb.velocity : car.rb.velocity, distance);

        Logger.Log($"[RandomEventsManager] Chosen position {resultLocalPos} on track '{resultTrack.name}' isTrainStopped={isTrainStopped} flip={flipDirection} isForwardsOnTrack={isForwardOnTrack}");

        return (resultTrack, resultLocalPos, resultRotationAlongTrack);
    }

    public void Reset()
    {
        Logger.Log($"[RandomEventsManager] Reset");

        // triggers re-creation
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

        foreach (var unsub in TrainCarHelper.HornUnsubscribes)
            unsub();
    }
}
