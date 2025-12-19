using UnityEngine;
using UnityModManagerNet;
using System;
using System.Linq;
using System.Collections.Generic;
using CommsRadioAPI;

namespace DerailValleyRandomEvents;

public enum AnimalType
{
    Cow,
    Sheep,
    Pig,
    Chicken,
    Goat,
    Cat
}

public static class AnimalHelper
{
    static UnityModManager.ModEntry.ModLogger Logger => Main.ModEntry.Logger;
    private readonly static System.Random _rng = new System.Random();
    public static Dictionary<AnimalType, List<GameObject>> animalPrefabs = [];

    static AnimalHelper()
    {
        CleanupHelper.Add(typeof(AnimalHelper), () =>
        {
            animalPrefabs.Clear();
        });
    }

    public static GameObject GetRandomPrefab(AnimalType type)
    {
        if (animalPrefabs.TryGetValue(type, out var cachedPrefabs))
            return cachedPrefabs[_rng.Next(cachedPrefabs.Count)];

        // NOTE: cattle zones can also have sheep/pigs/etc.
        var zones = UnityEngine.Object.FindObjectsOfType<DV.VFX.CattleZone>();
        if (zones == null || zones.Length == 0)
            throw new Exception("No cattle zones found");

        // Logger.Log($"Found zones: {string.Join(",", zones.Select(x => x.gameObject.name))}");

        var zoneForAnimal = zones.ToList().Find(x => x.gameObject.name.ToLower().Contains(GetNameFromType(type).ToLower())) ?? throw new Exception($"No zone found for type {type}");

        var prefabs = zoneForAnimal.agentPrefabs;
        if (prefabs.Length == 0)
            throw new Exception("Zone has no prefabs");

        Logger.Log($"Got prefabs:");
        foreach (var prefab1 in prefabs)
            TransformUtils.DumpObject(prefab1);

        List<GameObject> prefabGameObjects = prefabs.Select(x => x.prefab).ToList();

        var prefabGO = prefabGameObjects[_rng.Next(prefabGameObjects.Count)];

        // default colliders are basic capsules

        var existingCollider = prefabGO.GetComponent<Collider>();
        if (existingCollider is not BoxCollider)
            GameObject.Destroy(existingCollider);

        var existingColliders = prefabGO.GetComponentsInChildren<Collider>();
        foreach (var col in existingColliders)
            GameObject.Destroy(col);

        animalPrefabs[type] = prefabGameObjects;

        return prefabGO;
    }

    public static string GetNameFromType(AnimalType type)
    {
        return type switch
        {
            AnimalType.Cow => "cow",
            AnimalType.Sheep => "sheep",
            AnimalType.Pig => "pig",
            AnimalType.Chicken => "chicken",
            AnimalType.Goat => "goat",
            AnimalType.Cat => "cat",
            _ => throw new Exception($"Unknown type: {type}"),
        };
    }
}