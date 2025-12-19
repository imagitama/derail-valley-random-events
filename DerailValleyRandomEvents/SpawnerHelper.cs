using UnityEngine;
using System.Collections.Generic;
using UnityModManagerNet;
using System;
using System.Linq;
using System.Collections;

namespace DerailValleyRandomEvents;

// TODO: merge this with ObstacleSpawner
public static class SpawnerHelper
{
    private static UnityModManager.ModEntry.ModLogger Logger => Main.ModEntry.Logger;
    private static float spiralStep = 0.5f;
    private static float spiralAngleStep = 30f;

    private static List<GameObject> spawns = new List<GameObject>();
    public static Func<GameObject, Obstacle?, GameObject> Create;

    struct FreeSpot
    {
        public Vector3 position;
        public Quaternion rotation;
        public float rotationError;
    }

    static SpawnerHelper()
    {
        CleanupHelper.Add(typeof(SpawnerHelper), () =>
        {
            Logger.Log($"Cleanup {spawns.Count} spawns");
            foreach (var spawn in spawns.ToList())
                GameObject.Destroy(spawn);

            spawns.Clear();

            if (debugBox != null)
            {
                debugBox.Cleanup();
                debugBox = null;
            }
        });
    }

    public struct SpawningObject
    {
        public Vector3 Center;
        public Vector3 HalfExtents;
        public Quaternion Rotation;
    }

    private static SpawningObject? spawningObject;
    private static DebugBox? debugBox;

    public static void OnFrame()
    {
        if (!Main.settings.ShowDebugStuff)
            return;

        if (!spawningObject.HasValue)
        {
            if (debugBox != null)
                debugBox.Cleanup();
            return;
        }

        if (debugBox == null)
            debugBox = new DebugBox(WorldMover.OriginShiftParent);

        debugBox.UpdateDebugBox(spawningObject.Value.Center, spawningObject.Value.HalfExtents, rotation: spawningObject.Value.Rotation, color: Color.yellow);
    }

    /// <summary>
    /// Attempts to spawn X number of instances of a prefab object around a specific point.
    /// It must be run as a coroutine as running it synchronously causes weird glitches.
    /// </summary>
    public static IEnumerator SpawnAroundCoroutine(Obstacle obstacle, Vector3 intendedPos, int count, float maxRadius, Quaternion? rotation = null)
    {
        Logger.Log($"SpawnAroundCoroutine intendedPos={intendedPos} count={count} maxRadius={maxRadius}  rotation={rotation} obstacle={(obstacle == null ? "null" : obstacle.Type)}");

        float r = 0f;

        // while deciding a location our object is spawned *very* far away
        // TODO: uncouple these classes
        RandomEventsManager.IsCleanupEnabled = false;

        int layer = (int)DVLayer.Train_Big_Collider;
        int mask = 1 << layer;

        for (int i = 0; i < count; i++)
        {
            Vector3 scale = ObstacleSpawner.GetObstacleScale(obstacle!);

            var prefab = ObstacleSpawner.GetRandomObstaclePrefab(obstacle!);

            // NOTE: should add to internal registry that gets cleaned up
            var obj = Create?.Invoke(prefab, obstacle);
            obj!.transform.localScale = scale;

            var col = obj!.GetComponent<Collider>();
            var rb = obj!.GetComponent<Rigidbody>();
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;

            Vector3 halfExtents = col.bounds.extents;

            Quaternion instanceRotation = rotation.HasValue ? rotation.Value : Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f);

            halfExtents += Vector3.one * 0.5f;

            List<FreeSpot> freeSpots = new List<FreeSpot>();

            // Logger.Log($"  Spawn #{i} halfExtents={halfExtents}");

            while (r <= maxRadius)
            {
                // TODO: sometime work out how to make the 2nd pass faster (right now it is *way* too slow to be usable)
                for (int pass = 0; pass < 1; pass++)
                {
                    float angle = 0;

                    while (angle < 360f)
                    {
                        float rad = angle * Mathf.Deg2Rad;
                        var betterHeight = halfExtents.y + 0.1f;

                        Vector3 offset = new Vector3(
                            Mathf.Cos(rad) * r,
                            betterHeight,
                            Mathf.Sin(rad) * r
                        );

                        Vector3 instancePosition = intendedPos + offset;

                        spawningObject = new SpawningObject
                        {
                            Center = instancePosition,
                            HalfExtents = halfExtents,
                            Rotation = instanceRotation
                        };

                        if (pass == 0)
                        {
                            yield return null;

                            var hits = Physics.OverlapBox(
                                instancePosition,
                                halfExtents,
                                instanceRotation,
                                mask,
                                QueryTriggerInteraction.Ignore
                            );

                            if (hits.Length == 0)
                            {
                                freeSpots.Add(new FreeSpot
                                {
                                    position = instancePosition,
                                    rotation = instanceRotation,
                                    rotationError = 0f
                                });
                            }
                        }
                        else
                        {
                            for (float rotY = 0f; rotY < 360f; rotY += 10f)
                            {
                                Quaternion testRot = Quaternion.Euler(0f, rotY, 0f);

                                spawningObject = new SpawningObject
                                {
                                    Center = instancePosition,
                                    HalfExtents = halfExtents,
                                    Rotation = testRot
                                };

                                yield return null;

                                var hits = Physics.OverlapBox(
                                    instancePosition,
                                    halfExtents,
                                    testRot,
                                    mask,
                                    QueryTriggerInteraction.Ignore
                                );

                                if (hits.Length == 0)
                                {
                                    float error = Quaternion.Angle(instanceRotation, testRot);

                                    freeSpots.Add(new FreeSpot
                                    {
                                        position = instancePosition,
                                        rotation = testRot,
                                        rotationError = error
                                    });
                                }
                            }
                        }

                        angle += spiralAngleStep;
                    }
                }

                if (freeSpots.Count > 0)
                    break;

                r += spiralStep;
            }

            if (freeSpots.Count > 0)
            {
                FreeSpot best = freeSpots[0];

                for (int k = 1; k < freeSpots.Count; k++)
                {
                    if (freeSpots[k].rotationError < best.rotationError)
                        best = freeSpots[k];
                }

                var finalSpawnPos = best.position;

                // Logger.Log($"Found {freeSpots.Count} free spots eventPos={intendedPos} finalPos={finalSpawnPos} best={best.position} terrainY={Terrain.activeTerrain.transform.position.y}");

                // can be cleaned up by this time
                if (obj != null)
                {
                    obj.transform.position = finalSpawnPos;

                    spawns.Add(obj);
                }
            }
            else
            {
                Logger.Log($"Failed to find a spot for ${i}");
            }
        }

        spawningObject = null;

        RandomEventsManager.IsCleanupEnabled = true;
    }
}
