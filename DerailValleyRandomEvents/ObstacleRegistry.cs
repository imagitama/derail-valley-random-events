using System.Collections.Generic;
using DV.WorldTools;
using UnityEngine;

namespace DerailValleyRandomEvents;

public static class ObstacleRegistry
{
    public static List<Obstacle> Obstacles = new()
    {
        new Obstacle()
        {
            Type = ObstacleType.Rockslide,
            Biome = Biome.Rock,
            MinSpawnCount = 5,
            MaxSpawnCount = 10,
            AssetBundleName = "rocks",
            SpawnHeightFromGround = 5f,
            VerticalSpawnGap = 2f,
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
        new Obstacle()
        {
            Type = ObstacleType.FallenTrees,
            Biome = Biome.Forest,
            MinSpawnCount = 1,
            MaxSpawnCount = 1,
            AssetBundleName = "trees",
            SpawnHeightFromGround = 2f,
            VerticalSpawnGap = 3f,
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
        new Obstacle()
        {
            InPool = false,
            Type = ObstacleType.FunRamp,
            MinSpawnCount = 1,
            MaxSpawnCount = 1,
            AssetBundleName = "fun",
            PrefabName = "ObstacleRamp",
            SpawnHeightFromGround = 2f,
            VerticalSpawnGap = 3f,
            MinScale = 1f,
            MaxScale = 1f,
            MinMass = 999999f,
            MaxMass = 999999f,
            Drag = 0,
            AngularDrag = 0,
            DynamicFriction = 0,
            StaticFriction = 0,
            Bounciness = 0,
            DerailThreshold = 50000 // very low for launch!
        },
        new Obstacle()
        {
            Type = ObstacleType.Cows,
            Biome = Biome.Meadow,
            MinSpawnCount = 1,
            MaxSpawnCount = 5,
            AssetBundleName = "cow",
            SpawnHeightFromGround = 5f,
            HorizontalSpawnGap = 5f,
            MinScale = 2f,
            MaxScale = 2.5f,
            MinMass = 1000f,
            MaxMass = 2000f,
            Drag = 0,
            AngularDrag = 1f,
            DynamicFriction = 0.9f,
            StaticFriction = 0.9f,
            Bounciness = 0,
            Gravity = 2,
            // explode!
            ExplodeThreshold = 15000,
            ExplodeForce = 25,
            ExplodeRadius = 1,
            ExplodeUpwards = 1,
            LookAtPlayer = true
        }
    };

    public static Obstacle GetObstacleByType(ObstacleType type)
    {
        return Obstacles.Find(o => o.Type == type);
    }
}