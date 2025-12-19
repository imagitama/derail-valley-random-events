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
            Type = "Rockslide",
            AssetBundleName = "rocks",
            Biomes = [Biome.Rock],
            MinSpawnCount = 5,
            MaxSpawnCount = 10,
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
            Type = "FallenTrees",
            AssetBundleName = "trees",
            Biomes = [Biome.Forest],
            MinSpawnCount = 1,
            MaxSpawnCount = 1,
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
            RotationOffset = Quaternion.Euler(90f, 90f, 0), // lay across track
            TranslateOffset = new Vector3(10f, 0, 0),
            Gravity = 2
        },
        new Obstacle()
        {
            Type = "FunRamp",
            InPool = false,
            PrefabName = "ObstacleRamp",
            MinSpawnCount = 1,
            MaxSpawnCount = 1,
            AssetBundleName = "fun",
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
            JitterAmount = 0,
            ScaleOffset = new Vector3(1.5f, 1f, 1.5f),
            RotationOffset = Quaternion.Euler(0, -90f, 0),
            DerailThreshold = 5000 // very low for launch!
        },
        new Obstacle()
        {
            Type = "OldCows",
            AssetBundleName = "cow",
            InPool = false,
            Biomes = [Biome.Meadow, Biome.Field],
            MinSpawnCount = 2,
            MaxSpawnCount = 5,
            MaxRadius = 5f,
            MinScale = 2f,
            MaxScale = 2.5f,
            MinMass = 1000f,
            MaxMass = 2000f,
            Drag = 0,
            AngularDrag = 10f, // prevent poor balancing // tip over better
            DynamicFriction = 0.9f,
            StaticFriction = 0.9f,
            Bounciness = 0,
            Gravity = 2,
            // explode!
            ExplodeThreshold = 15000,
            ExplodeForce = 25,
            ExplodeRadius = 1,
            ExplodeUpwards = 1,
            // other
            CenterOfMass = new Vector3(0f, 1f, 0f), // tip over better
            // animals
            LookAtPlayer = true,
            ScaredOfHorn = true,
        },
        new Obstacle()
        {
            Type = "Cows",
            AssetBundleName = "bloodsplat",
            Biomes = [Biome.Meadow, Biome.Field],
            MinSpawnCount = 1,
            MaxSpawnCount = 10,
            MaxRadius = 5f,
            MinScale = 0.8f,
            MaxScale = 1.2f,
            MinMass = 1000f,
            MaxMass = 2000f,
            Drag = 0,
            AngularDrag = 10f, // prevent poor balancing // tip over better
            DynamicFriction = 0.9f,
            StaticFriction = 0.9f,
            Bounciness = 0,
            Gravity = 2,
            // explode!
            ExplodeThreshold = 15000,
            ExplodeForce = 25,
            ExplodeRadius = 1,
            ExplodeUpwards = 1,
            // other
            CenterOfMass = new Vector3(0f, -0.5f, 0f), // fix tipping over
            LookAtPlayer = true,
            // animals
            AnimalType = AnimalType.Cow,
            ScaredOfHorn = true,
            TurnSpeed = 90f,
            ScaredTurnSpeed = 360f,
            MoveSpeed = 0.25f,
            ScaredMoveSpeed = 1f
        },
        // other animalz
        new Obstacle()
        {
            Type = "Cats",
            InPool = false,
            AssetBundleName = "bloodsplat",
            Biomes = [],
            MinSpawnCount = 1,
            MaxSpawnCount = 10,
            MaxRadius = 5f,
            MinScale = 0.8f,
            MaxScale = 1.2f,
            MinMass = 1000f,
            MaxMass = 2000f,
            Drag = 0,
            AngularDrag = 10f, // prevent poor balancing
            DynamicFriction = 0.9f,
            StaticFriction = 0.9f,
            Bounciness = 0,
            Gravity = 2,
            // explode!
            ExplodeThreshold = 15000,
            ExplodeForce = 25,
            ExplodeRadius = 1,
            ExplodeUpwards = 1,
            // other
            LookAtPlayer = true,
            // animals
            AnimalType = AnimalType.Cat,
            ScaredOfHorn = true,
            TurnSpeed = 90f,
            ScaredTurnSpeed = 360f,
            MoveSpeed = 0.5f,
            ScaredMoveSpeed = 1f
        },
        new Obstacle()
        {
            Type = "Chicken",
            InPool = false,
            AssetBundleName = "bloodsplat",
            Biomes = [],
            MinSpawnCount = 5,
            MaxSpawnCount = 10,
            MaxRadius = 5f,
            MinScale = 0.8f,
            MaxScale = 1.2f,
            MinMass = 1000f,
            MaxMass = 2000f,
            Drag = 0,
            AngularDrag = 10f, // prevent poor balancing
            DynamicFriction = 0.9f,
            StaticFriction = 0.9f,
            Bounciness = 0,
            Gravity = 2,
            // explode!
            ExplodeThreshold = 15000,
            ExplodeForce = 25,
            ExplodeRadius = 1,
            ExplodeUpwards = 1,
            // other
            LookAtPlayer = true,
            // animals
            AnimalType = AnimalType.Chicken,
            ScaredOfHorn = true,
            TurnSpeed = 180f,
        },
        new Obstacle()
        {
            Type = "Goats",
            InPool = false,
            AssetBundleName = "bloodsplat",
            Biomes = [],
            MinSpawnCount = 1,
            MaxSpawnCount = 10,
            MaxRadius = 5f,
            MinScale = 0.8f,
            MaxScale = 1.2f,
            MinMass = 1000f,
            MaxMass = 2000f,
            Drag = 0,
            AngularDrag = 10f, // prevent poor balancing
            DynamicFriction = 0.9f,
            StaticFriction = 0.9f,
            Bounciness = 0,
            Gravity = 2,
            // explode!
            ExplodeThreshold = 15000,
            ExplodeForce = 25,
            ExplodeRadius = 1,
            ExplodeUpwards = 1,
            // other
            LookAtPlayer = true,
            // animals
            AnimalType = AnimalType.Goat,
            ScaredOfHorn = true,
            TurnSpeed = 180f,
        },
        new Obstacle()
        {
            Type = "Pigs",
            InPool = false,
            AssetBundleName = "bloodsplat",
            Biomes = [],
            MinSpawnCount = 1,
            MaxSpawnCount = 10,
            MaxRadius = 5f,
            MinScale = 0.8f,
            MaxScale = 1.2f,
            MinMass = 1000f,
            MaxMass = 2000f,
            Drag = 0,
            AngularDrag = 10f, // prevent poor balancing
            DynamicFriction = 0.9f,
            StaticFriction = 0.9f,
            Bounciness = 0,
            Gravity = 2,
            // explode!
            ExplodeThreshold = 15000,
            ExplodeForce = 25,
            ExplodeRadius = 1,
            ExplodeUpwards = 1,
            // other
            LookAtPlayer = true,
            // animals
            AnimalType = AnimalType.Pig,
            ScaredOfHorn = true,
            TurnSpeed = 180f,
        },
        new Obstacle()
        {
            Type = "Sheep",
            InPool = false,
            AssetBundleName = "bloodsplat",
            Biomes = [],
            MinSpawnCount = 1,
            MaxSpawnCount = 10,
            MaxRadius = 5f,
            MinScale = 0.8f,
            MaxScale = 1.2f,
            MinMass = 1000f,
            MaxMass = 2000f,
            Drag = 0,
            AngularDrag = 10f, // prevent poor balancing
            DynamicFriction = 0.9f,
            StaticFriction = 0.9f,
            Bounciness = 0,
            Gravity = 2,
            // explode!
            ExplodeThreshold = 15000,
            ExplodeForce = 25,
            ExplodeRadius = 1,
            ExplodeUpwards = 1,
            // other
            LookAtPlayer = true,
            // animals
            AnimalType = AnimalType.Sheep,
            ScaredOfHorn = true,
            TurnSpeed = 180f,
        },
    };

    public static Obstacle GetObstacleByType(string type)
    {
        return Obstacles.Find(o => o.Type == type);
    }
}