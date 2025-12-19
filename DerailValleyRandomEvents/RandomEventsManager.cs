using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DV.WorldTools;
using DV.Utils;
using UnityEngine;
using UnityModManagerNet;
using System.Threading.Tasks;
using DerailValleyUsefulUtils;

namespace DerailValleyRandomEvents;

public class RandomEventsManager
{
    private UnityModManager.ModEntry.ModLogger Logger => Main.ModEntry.Logger;
    private readonly System.Random _rng = new System.Random();
    private float _nextCheckTime;
    private float _nextEligibleEventTime;
    private GameObject? _updateDriver;
    private GameObject? _debugSphere;
    private float? _nextCleanupTime;
    public bool PreventAllObstacleDerailment = false;
    public static bool IsCleanupEnabled = true;

    public enum EventCategory
    {
        Obstacle
    }

    public RandomEventsManager()
    {
        Logger.Log("[RandomEventsManager] Start");

        CleanupHelper.Add(typeof(RandomEventsManager), () =>
        {
            GameObject.Destroy(_updateDriver);
            GameObject.Destroy(_debugSphere);
        });

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
        var totalCount = obstacles.Count;

        var cleanupCount = 0;

        foreach (var obstacleObj in obstacles)
        {
            var distance = Vector3.Distance(playerPos, obstacleObj.transform.position);
            var maxDistance = Main.settings.ObstacleCleanupDistance;
            if (distance > maxDistance)
            {
                Logger.Log($"[RandomEventsManager] Cleaning up '{obstacleObj.name}' (distance {distance} > {maxDistance})");
                ObstacleSpawner.CleanupObstacle(obstacleObj);
                cleanupCount++;
            }
        }

        Logger.Log($"[RandomEventsManager] Cleaned up {cleanupCount} / {totalCount} obstacles");
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

            _debugSphere = MeshUtils.CreateDebugSphere();

            var parent = WorldMover.OriginShiftParent;
            _debugSphere.transform.SetParent(parent);
        }

        var newSpherePos = new Vector3(obstacleLocalPos.x, obstacleLocalPos.y + 5f, obstacleLocalPos.z);
        _debugSphere.transform.position = newSpherePos;
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

    public Obstacle GetRandomObstacleForType(string type, bool forceEverythingInPool = false)
    {
        var poolable = ObstacleRegistry.Obstacles
            .Where(x => x.Type == type && (forceEverythingInPool || x.InPool != false))
            .ToList();

        return poolable[_rng.Next(poolable.Count)];
    }

    public SpawnedEvent EmitObstacleEvent(EventRequest eventRequest, Obstacle incomingObstacle)
    {
        bool showWarning = Main.settings.WarningChance == 0 ? false : UnityEngine.Random.value < Main.settings.WarningChance;
        if (showWarning)
            NotificationHelper.ShowNotificationViaRadio($"An obstacle has been reported ahead! Be careful!");

        var spawnCount =
            incomingObstacle.MaxSpawnCount != null ?
                UnityEngine.Random.Range(incomingObstacle.MinSpawnCount, incomingObstacle.MaxSpawnCount.Value) :
                incomingObstacle.MaxSpawnCount != null ?
                    UnityEngine.Random.Range(1, incomingObstacle.MaxSpawnCount.Value) :
                        incomingObstacle.MinSpawnCount;

        var localPos = eventRequest.intendedPos;

        if (localPos == null)
            throw new Exception("Cannot emit obstacle event without a position");

        var rotation = eventRequest.intendedRot != null ? eventRequest.intendedRot.Value : Quaternion.identity;

        Logger.Log($"[RandomEventsManager] Emit obstacle event at position={localPos} rotation={rotation} type={incomingObstacle.Type} count={spawnCount} ({incomingObstacle.MinSpawnCount} -> {incomingObstacle.MaxSpawnCount}) warn={showWarning}");

        if (PreventAllObstacleDerailment)
            incomingObstacle.DerailThreshold = 0;

        if (incomingObstacle.MaxRadius != null)
        {
            // TODO: do this better
            SpawnerHelper.Create = (prefab, obstacle) =>
            {
                var obj = ObstacleSpawner.Create(prefab, obstacle);
                obj.GetComponent<ObstacleComponent>().OnStrongImpact = (trainCar) => OnStrongImpact(trainCar, incomingObstacle);
                return obj;
            };

            AsyncHelper.StartCoroutine(
                SpawnerHelper.SpawnAroundCoroutine(
                    obstacle: incomingObstacle,
                    localPos.Value,
                    spawnCount,
                    maxRadius: 1000
                )
            );
        }
        else
        {
            for (var i = 0; i < spawnCount; i++)
            {
                Logger.Log($"[RandomEventsManager] Spawn obstacle #{i}");

                var prefab = ObstacleSpawner.GetRandomObstaclePrefab(incomingObstacle);

                Logger.Log($"[RandomEventsManager] Using prefab: {prefab}");

                // NOTE: stored internally with obstacle spawner
                var obj = ObstacleSpawner.Create(prefab, incomingObstacle);
                obj.GetComponent<ObstacleComponent>().OnStrongImpact = (trainCar) => OnStrongImpact(trainCar, incomingObstacle);

                obj.transform.localScale = ObstacleSpawner.GetObstacleScale(incomingObstacle);
                obj.transform.rotation = rotation;

                if (ObstacleSpawner.RotationMultiplier != null)
                    obj.transform.rotation *= Quaternion.Euler(ObstacleSpawner.RotationMultiplier.Value);

                var jitterDistance = 0.25f;

                var obstaclePosInSky = new Vector3(localPos.Value.x, localPos.Value.y + incomingObstacle.SpawnHeightFromGround, localPos.Value.z);

                Vector3 spawnPos = obstaclePosInSky;
                spawnPos.x += jitterDistance;

                // cannot spawn multiple objects inside each other otherwise physics freaks out
                if (incomingObstacle.VerticalSpawnGap != null)
                {
                    var gap = i * incomingObstacle.VerticalSpawnGap.Value;
                    spawnPos.y += gap;
                }
                // TODO: combine with vertical?
                else if (incomingObstacle.HorizontalSpawnGap != null)
                {
                    var track = eventRequest.intendedTrack;

                    if (track == null)
                        throw new Exception("Need a track");

                    float offset = i - (spawnCount - 1) / 2f;
                    var gap = offset * incomingObstacle.HorizontalSpawnGap.Value;

                    spawnPos.z += gap;
                }

                if (incomingObstacle.JitterAmount > 0)
                    spawnPos = GetJitteredPos(spawnPos, incomingObstacle.JitterAmount);

                obj.transform.position = spawnPos;

                if (incomingObstacle.RotationOffset != null)
                    obj.transform.rotation = obj.transform.rotation * incomingObstacle.RotationOffset.Value;

                if (incomingObstacle.TranslateOffset != null)
                    ObstacleSpawner.ApplyTranslateOffset(offsetPercent: incomingObstacle.TranslateOffset.Value, obj);
            }
        }

        return new SpawnedEvent()
        {
            position = localPos.Value,
            obstacle = incomingObstacle,
            distance = eventRequest.distance!.Value, // trusting it
            count = spawnCount
        };

    }

    private void OnStrongImpact(TrainCar car, Obstacle obstacle)
    {
        Logger.Log($"Strong impact between car '{car}' and obstacle '{obstacle.Label}': causing derail");

        car.Derail();
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

        if (eventRequest.ignoreNearbyCheck != true && GetIsObstacleNearby(obstacleLocalPos))
        {
            Logger.Log($"[RandomEventsManager] Another obstacle is within area - skipping");
            return null;
        }

        if (TrainCarHelper.GetIsInTunnel(eventRequest.intendedPos.Value))
        {
            Logger.Log($"[RandomEventsManager] Target is inside a tunnel - skipping");
            return null;
        }

        if (eventRequest.ignoreBuiltUpAreaCheck != true && BuiltUpAreaHelper.GetIsNearBuiltUpArea(eventRequest.intendedPos.Value))
        {
            Logger.Log($"[RandomEventsManager] Target is near a built up area - skipping");
            return null;
        }

        var currentBiome = PlayerUtils.GetCurrentBiome();

        Logger.Log($"[RandomEventsManager] Current biome: {currentBiome}");

        Biome? biomeToUse;

        if (eventRequest.biome.HasValue)
            biomeToUse = eventRequest.biome.Value;
        else if (eventRequest.ignoreBiome)
            biomeToUse = null;
        else
            biomeToUse = currentBiome;

        Obstacle? obstacle = null;

        if (eventRequest.obstacle != null)
            obstacle = eventRequest.obstacle;

        if (obstacle == null)
        {
            if (eventRequest.obstacleType != null)
                obstacle = GetRandomObstacleForType(eventRequest.obstacleType, eventRequest.forceEverythingInPool);
            else
                obstacle = biomeToUse != null ? GetRandomObstacleForBiome(biomeToUse.Value) : GetRandomObstacle();
        }

        if (obstacle == null)
        {
            Logger.Log($"[RandomEventsManager] No obstacle found for biome '{biomeToUse}' and event type={eventRequest.obstacleType}");
            return null;
        }

        Logger.Log($"[RandomEventsManager] Using obstacle:\n{obstacle}");

        return EmitObstacleEvent(eventRequest, obstacle);
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
}
