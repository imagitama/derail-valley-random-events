using System.Collections.Generic;
using System.Linq;
using DV.WorldTools;
using UnityEngine;

namespace DerailValleyRandomEvents;

public class Obstacle
{
    // basics
    public string Type;
    public List<Biome> Biomes = [];
    public string AssetBundleName;
    public string? PrefabName;
    public bool? InPool;
    public float DerailThreshold = 150000; // newton-seconds (0 to disable, light bump in DE2 is 130,000~)

    // spawning
    public int MinSpawnCount = 1;
    public int? MaxSpawnCount = 1;
    public float SpawnHeightFromGround = 1f;
    public float? VerticalSpawnGap; // distance between spawns to avoid physics exploding
    public float? HorizontalSpawnGap; // distance between spawns to avoid physics exploding
    public float? MaxRadius; // if to spawn on the ground inside a circle

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

    // offsets
    public Vector3? ScaleOffset;
    public Quaternion? RotationOffset;
    public Vector3? TranslateOffset; // percentages eg. x=0.5 will move x=50% from center

    // exploding!
    public float? ExplodeThreshold; // newton-seconds (0 to disable, light bump in DE2 is 130,000~)
    public float? ExplodeForce = 10;
    public float? ExplodeRadius = 5;
    public float? ExplodeUpwards = 5;

    // other
    public float JitterAmount = 1f;
    public Vector3? CenterOfMass; // default center of collider

    // animals
    public AnimalType? AnimalType;
    public bool LookAtPlayer = false;
    public bool ScaredOfHorn = false;
    public float MoveSpeed = 0.5f; // metres per sec
    public float ScaredMoveSpeed = 2f; // metres per sec
    public float TurnSpeed = 45f; // degrees per sec
    public float ScaredTurnSpeed = 360f; // degrees per sec
    public float AnimationSpeedScale = 1f;
    public bool UprightOnTipOver = false;

    // internal (do not copy)
    public int Id = -1;
    public string Label => $"{Type} #{Id}";

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
            DerailThreshold = DerailThreshold,
            // spawning
            MinSpawnCount = MinSpawnCount,
            MaxSpawnCount = MaxSpawnCount,
            SpawnHeightFromGround = SpawnHeightFromGround,
            VerticalSpawnGap = VerticalSpawnGap,
            HorizontalSpawnGap = HorizontalSpawnGap,
            MaxRadius = MaxRadius,
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
            // offsets
            ScaleOffset = ScaleOffset,
            RotationOffset = RotationOffset,
            TranslateOffset = TranslateOffset,
            // more physics
            Gravity = Gravity,
            // exploding
            ExplodeThreshold = ExplodeThreshold,
            ExplodeForce = ExplodeForce,
            ExplodeRadius = ExplodeRadius,
            ExplodeUpwards = ExplodeUpwards,
            // other
            JitterAmount = JitterAmount,
            CenterOfMass = CenterOfMass,
            // animals
            AnimalType = AnimalType,
            LookAtPlayer = LookAtPlayer,
            ScaredOfHorn = ScaredOfHorn,
            MoveSpeed = MoveSpeed,
            ScaredMoveSpeed = ScaredMoveSpeed,
            TurnSpeed = TurnSpeed,
            ScaredTurnSpeed = ScaredTurnSpeed,
            AnimationSpeedScale = AnimationSpeedScale,
            UprightOnTipOver = UprightOnTipOver
        };
    }

    public override string ToString()
    {
        return "Obstacle(" +
$"Type={Type}," +
$"Biomes={string.Join(",", Biomes)}," +
$"DerailThreshold={DerailThreshold}," +
// spawning
$"MinSpawnCount={MinSpawnCount}," +
$"MaxSpawnCount={MaxSpawnCount}," +
$"SpawnHeightFromGround={SpawnHeightFromGround}," +
$"VerticalSpawnGap={VerticalSpawnGap}," +
$"HorizontalSpawnGap={HorizontalSpawnGap}," +
$"MaxRadius={MaxRadius}," +
// rigidbody
$"MinScale={MinScale}," +
$"MaxScale={MaxScale}," +
$"MinMass={MinMass}," +
$"MaxMass={MaxMass}," +
$"Drag={Drag}," +
$"AngularDrag={AngularDrag}," +
// physics
$"DynamicFriction={DynamicFriction}," +
$"StaticFriction={StaticFriction}," +
$"Bounciness={Bounciness}," +
// offsets
$"ScaleOffset={ScaleOffset}," +
$"RotationOffset={RotationOffset}," +
$"TranslateOffset={TranslateOffset}," +
$"Gravity={Gravity}," +
// exploding
$"ExplodeThreshold={ExplodeThreshold}," +
$"ExplodeForce={ExplodeForce}," +
$"ExplodeRadius={ExplodeRadius}," +
$"ExplodeUpwards={ExplodeUpwards}," +
// other
$"JitterAmount={JitterAmount}," +
$"CenterOfMass={CenterOfMass}," +
// animals
$"AnimalType={AnimalType}," +
$"LookAtPlayer={LookAtPlayer}," +
$"ScaredOfHorn={ScaredOfHorn}," +
$"MoveSpeed={MoveSpeed}," +
$"ScaredMoveSpeed={ScaredMoveSpeed}," +
$"TurnSpeed={TurnSpeed}," +
$"AnimationSpeedScale={AnimationSpeedScale}," +
$"UprightOnTipOver={UprightOnTipOver}" +
")";
    }
}

public class EventRequest
{
    public Biome? biome; // if null use whatever player is in (or anything if ignoring)
    public string? obstacleType; // if null decided based on biome/random
    public float? distance; // if null use settings
    // optional
    public Obstacle? obstacle;
    public bool ignoreNearbyCheck = false;
    public bool ignoreBuiltUpAreaCheck = false;
    public bool flipDirection = false;
    public bool ignoreBiome = false;
    public bool forceEverythingInPool = false;
    // populate later
    public Vector3? intendedPos;
    public Quaternion? intendedRot;
    public bool? isForward;
    public RailTrack? intendedTrack;
}

public class SpawnedEvent
{
    public Obstacle obstacle;
    public float distance;
    public int count;
    public Vector3 position;
    public Quaternion rotation;
}