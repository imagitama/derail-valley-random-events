using UnityEngine;
using System.Collections.Generic;
using UnityModManagerNet;
using System;
using System.Threading.Tasks;
using System.Linq;
using DV.OriginShift;

namespace DerailValleyRandomEvents;

// TODO: merge this with ObstacleSpawner
public static class SpawnerHelper
{
    private static UnityModManager.ModEntry.ModLogger Logger => Main.ModEntry.Logger;

    // public GameObject prefab;
    // public int count = 10;
    public static bool doItSlowly = false;

    // private float areaDiameter = 20f;
    // private Vector2 scaleRange = new Vector2(0.5f, 1.5f);
    private static Color gizmoColor = new Color(0f, 1f, 0f, 0.6f);
    private static int gizmoSegments = 64;
    private static float spiralStep = 0.5f;
    private static float spiralAngleStep = 30f;
    private static float attemptDelay = 0.01f;

    private static List<GameObject> spawns = new List<GameObject>();
    public static Func<GameObject, Obstacle, GameObject> Create;

    private static bool hasActiveProbe;
    private static Vector3 probePosition;
    private static Vector3 probeHalfExtents;
    private static Quaternion probeRotation;

    struct FreeSpot
    {
        public Vector3 position;
        public Quaternion rotation;
        public float rotationError;
    }

    // void OnDrawGizmosSelected()
    // {
    //     float radius = areaDiameter * 0.5f;

    //     Gizmos.color = gizmoColor;

    //     Vector3 center = transform.position;
    //     Vector3 prev = center + new Vector3(radius, 0f, 0f);

    //     for (int i = 1; i <= gizmoSegments; i++)
    //     {
    //         float a = (i / (float)gizmoSegments) * Mathf.PI * 2f;
    //         Vector3 next = center + new Vector3(
    //             Mathf.Cos(a) * radius,
    //             0f,
    //             Mathf.Sin(a) * radius
    //         );

    //         Gizmos.DrawLine(prev, next);
    //         prev = next;
    //     }

    //     if (hasActiveProbe)
    //     {
    //         Gizmos.color = Color.yellow;
    //         Gizmos.matrix = Matrix4x4.TRS(probePosition, probeRotation, Vector3.one);
    //         Gizmos.DrawWireCube(Vector3.zero, probeHalfExtents * 2f);
    //         Gizmos.matrix = Matrix4x4.identity;
    //     }
    // }

    private static Vector3? intendedPos_tmp;

    // public static void SpawnAround(Vector3 intendedPos, GameObject prefab, int count, float maxRadius, float minScale, float maxScale, Quaternion? rotation = null, Obstacle? obstacle = null)
    // {
    //     SpawnAroundAsync(intendedPos, prefab, count, maxRadius, minScale, maxScale, rotation, obstacle);
    // }

    public static void OnFrame()
    {
        if (!intendedPos_tmp.HasValue)
            return;

        var areaDiameter = 20f;
        float radius = areaDiameter * 0.5f;

        Vector3 center = intendedPos_tmp.Value;
        Vector3 prev = center + new Vector3(radius, 0f, 0f);

        for (int i = 1; i <= gizmoSegments; i++)
        {
            float a = (i / (float)gizmoSegments) * Mathf.PI * 2f;
            Vector3 next = center + new Vector3(
                Mathf.Cos(a) * radius,
                0f,
                Mathf.Sin(a) * radius
            );

            Debug.DrawLine(prev, next, gizmoColor);
            prev = next;
        }

        if (hasActiveProbe)
        {
            Vector3[] corners = new Vector3[8];
            Vector3 he = probeHalfExtents;

            corners[0] = new Vector3(-he.x, -he.y, -he.z);
            corners[1] = new Vector3(he.x, -he.y, -he.z);
            corners[2] = new Vector3(he.x, -he.y, he.z);
            corners[3] = new Vector3(-he.x, -he.y, he.z);
            corners[4] = new Vector3(-he.x, he.y, -he.z);
            corners[5] = new Vector3(he.x, he.y, -he.z);
            corners[6] = new Vector3(he.x, he.y, he.z);
            corners[7] = new Vector3(-he.x, he.y, he.z);

            Matrix4x4 m = Matrix4x4.TRS(probePosition, probeRotation, Vector3.one);

            for (int i = 0; i < 4; i++)
            {
                Debug.DrawLine(m.MultiplyPoint3x4(corners[i]), m.MultiplyPoint3x4(corners[(i + 1) % 4]), Color.yellow);
                Debug.DrawLine(m.MultiplyPoint3x4(corners[i + 4]), m.MultiplyPoint3x4(corners[((i + 1) % 4) + 4]), Color.yellow);
                Debug.DrawLine(m.MultiplyPoint3x4(corners[i]), m.MultiplyPoint3x4(corners[i + 4]), Color.yellow);
            }
        }
    }

    public static void SpawnAround(Vector3 intendedPos, GameObject prefab, int count, float maxRadius, float minScale, float maxScale, Quaternion? rotation = null, Obstacle? obstacle = null)
    {
        intendedPos_tmp = intendedPos;

        Logger.Log($"SpawnAround intendedPos={intendedPos} prefab={prefab} count={count} maxRadius={maxRadius} minScale={minScale} maxScale={maxScale} rotation={rotation} obstacle={(obstacle == null ? "null" : obstacle.Type)}");

        // TODO: support multiple rigidbodies
        // var rb = prefab.GetComponentInChildren<Rigidbody>();
        // if (rb != null)
        // {
        //     rb.velocity = Vector3.zero;
        //     rb.angularVelocity = Vector3.zero;
        // }

        // // TODO: handle multiple colliders instead of first
        // var col = prefab.GetComponentInChildren<Collider>();

        // if (col == null)
        //     throw new Exception("Need a collider");

        // float baseRadius = maxRadius * 0.5f;
        float r = 0f;
        // float r = Random.Range(0f, spiralStep);

        int layer = (int)DVLayer.Train_Big_Collider;
        int mask = 1 << layer;

        for (int i = 0; i < count; i++)
        {
            Vector3 scale = obstacle != null ? ObstacleSpawner.GetObstacleScale(obstacle) : Vector3.one * UnityEngine.Random.Range(minScale, maxScale);

            // NOTE: adds to internal registry that gets cleaned up
            var obj = Create?.Invoke(prefab, obstacle);
            obj!.transform.localScale = scale;

            var col = obj!.GetComponent<Collider>();
            var rb = obj!.GetComponent<Rigidbody>();
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;

            Vector3 halfExtents = col.bounds.extents;

            // float scale = Random.Range(scaleRange.x, scaleRange.y);
            Quaternion instanceRotation = rotation.HasValue ? rotation.Value : Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f);

            halfExtents += Vector3.one * 0.5f;

            List<FreeSpot> freeSpots = new List<FreeSpot>();

            Logger.Log($"  Spawn #{i} halfExtents={halfExtents} layer={obj.layer}");

            while (r <= maxRadius)
            {
                for (int pass = 0; pass < 2; pass++)
                {
                    float angle = UnityEngine.Random.Range(0f, 360f);

                    while (angle < 360f)
                    {
                        float rad = angle * Mathf.Deg2Rad;

                        Vector3 offset = new Vector3(
                            Mathf.Cos(rad) * r,
                            0f,
                            Mathf.Sin(rad) * r
                        );

                        Vector3 position = intendedPos + offset;

                        if (pass == 0)
                        {
                            probePosition = position;
                            probeHalfExtents = halfExtents;
                            probeRotation = instanceRotation;
                            hasActiveProbe = true;

                            // if (doItSlowly)
                            //     await Task.Delay(TimeSpan.FromSeconds(attemptDelay));

                            // yield return new WaitForSeconds(attemptDelay);

                            var hits = Physics.OverlapBox(
                                position,
                                halfExtents,
                                instanceRotation,
                                mask,
                                QueryTriggerInteraction.Ignore
                            );

                            // Debug.DrawLine(position, position + Vector3.up * 2f, Color.red, 2f);

                            if (hits.Length == 0)
                            {
                                // Logger.Log($"    Free spot pos={position} rot={instanceRotation}");

                                freeSpots.Add(new FreeSpot
                                {
                                    position = position,
                                    rotation = instanceRotation,
                                    rotationError = 0f
                                });
                            }
                            else
                            {

                                // Logger.Log($"    Hits: {string.Join(",", hits.Select(x => x.name))}");
                            }
                        }
                        else
                        {
                            for (float rotY = 0f; rotY < 360f; rotY += 10f)
                            {
                                Quaternion testRot = Quaternion.Euler(0f, rotY, 0f);

                                probePosition = position;
                                probeHalfExtents = halfExtents;
                                probeRotation = testRot;
                                hasActiveProbe = true;

                                // if (doItSlowly)
                                //     await Task.Delay(TimeSpan.FromSeconds(attemptDelay));

                                var hits = Physics.OverlapBox(
                                    position,
                                    halfExtents,
                                    testRot,
                                    mask,
                                    QueryTriggerInteraction.Ignore
                                );

                                // Debug.DrawLine(position, position + Vector3.up * 2f, Color.blue, 2f);

                                if (hits.Length == 0)
                                {
                                    float error = Quaternion.Angle(instanceRotation, testRot);

                                    freeSpots.Add(new FreeSpot
                                    {
                                        position = position,
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

                // var worldPosition = best.position - OriginShift.currentMove;
                // var height = Terrain.activeTerrain.SampleHeight(worldPosition);
                // float groundY = height + OriginShift.currentMove.y;

                var posY = best.position.y + halfExtents.y;
                var position = new Vector3(best.position.x, posY, best.position.z);

                // position.y += 5f;

                Logger.Log($"Found {freeSpots.Count} free spots eventPos={intendedPos} pos={position} best={best.position} terrainY={Terrain.activeTerrain.transform.position.y}");

                // var obj = GameObject.Instantiate(prefab, position, best.rotation);

                // spawns.Add(obj);

                // public static GameObject Create(GameObject prefab, Obstacle obstacle)

                obj!.transform.position = position;

                spawns.Add(obj);
            }
            else
            {
                Logger.Log($"Failed to find a spot for ${i}");
            }

            hasActiveProbe = false;
        }
    }

    public static void Cleanup()
    {
        foreach (var spawn in spawns.ToList())
            GameObject.Destroy(spawn);

        spawns.Clear();
    }
}
