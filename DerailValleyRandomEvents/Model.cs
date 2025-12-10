using System.Collections.Generic;
using System.Linq;
using DV.WorldTools;
using UnityEngine;

namespace DerailValleyRandomEvents;

public enum ObstacleType
{
    Rockslide,
    Cows,
    FallenTrees,
    FunRamp
}

public class Obstacle
{
    // basics
    public ObstacleType Type;
    public List<Biome> Biomes = [];
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
    public Vector3? TranslateOffset;
    public float DerailThreshold = 150000; // newton-seconds (light bump in DE2 is 130,000~)
    // exploding!
    public float? ExplodeThreshold; // newton-seconds (light bump in DE2 is 130,000~)
    public float? ExplodeForce = 10;
    public float? ExplodeRadius = 5;
    public float? ExplodeUpwards = 5;
    // other
    public bool LookAtPlayer = false;
    public bool ScaredOfHorn = false;

    public Obstacle Clone()
    {
        return new Obstacle()
        {
            // basics
            Type = Type,
            Biomes = Biomes.ToList(),
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
            TranslateOffset = TranslateOffset,
            // more physics
            Gravity = Gravity,
            DerailThreshold = DerailThreshold,
            // exploding
            ExplodeThreshold = ExplodeThreshold,
            ExplodeForce = ExplodeForce,
            ExplodeRadius = ExplodeRadius,
            ExplodeUpwards = ExplodeUpwards,
            // other
            LookAtPlayer = LookAtPlayer,
            ScaredOfHorn = ScaredOfHorn
        };
    }

    public override string ToString()
    {
        return "Obstacle(" +
$"Type={Type}," +
$"Biomes={string.Join(",", Biomes)}," +
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
$"TranslateOffset={TranslateOffset}," +
$"Gravity={Gravity}," +
$"DerailThreshold={DerailThreshold}," +
$"ExplodeThreshold={ExplodeThreshold}," +
$"ExplodeForce={ExplodeForce}," +
$"ExplodeRadius={ExplodeRadius}," +
$"ExplodeUpwards={ExplodeUpwards}," +
$"LookAtPlayer={LookAtPlayer}," +
$"ScaredOfHorn={ScaredOfHorn}" +
")";
    }
}

public class EventRequest
{
    public Biome? biome; // if null use whatever player is in (or anything if ignoring)
    public ObstacleType? obstacleType; // if null decided based on biome/random
    public float? distance; // if null use settings
    // optional
    public bool ignoreNearbyCheck = false;
    public bool flipDirection = false;
    public bool ignoreBiome = false;
    // populate later
    public Vector3? intendedPos;
    public bool? isForward;
    public RailTrack? intendedTrack;
}

public class SpawnedEvent
{
    public Obstacle obstacle;
    public float distance;
    public int count;
}