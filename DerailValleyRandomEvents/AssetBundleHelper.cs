using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityModManagerNet;

namespace DerailValleyRandomEvents;

public static class AssetBundleHelper
{
    private static UnityModManager.ModEntry.ModLogger Logger => Main.ModEntry.Logger;
    private static Dictionary<string, AssetBundle> _obstacleBundles = [];
    private readonly static System.Random _rng = new System.Random();

    static AssetBundleHelper()
    {
        CleanupHelper.Add(typeof(AssetBundleHelper), () =>
        {
            foreach (var kv in _obstacleBundles.ToList())
            {
                var assetBundle = kv.Value;
                Logger.Log($"[AssetBundleHelper] Unload '{assetBundle.name}'");
                assetBundle.Unload(true);
            }
        });
    }

    public static AssetBundle LoadBundle(string pathInsideAssetBundles)
    {
        var bundlePath = Path.Combine(Main.ModEntry.Path, "Dependencies/AssetBundles", pathInsideAssetBundles);

        Logger.Log($"[AssetBundleHelper] Loading bundle from: {bundlePath}");

        if (!File.Exists(bundlePath))
            throw new Exception($"Asset bundle not found at {bundlePath}");

        return AssetBundle.LoadFromFile(bundlePath);
    }

    public static AssetBundle LoadObstacleBundle(Obstacle obstacle)
    {
        // if (obstacle.AssetBundleName == null)
        //     throw new Exception("Need an asset bundle name");
        return LoadBundle(obstacle.AssetBundleName);
    }

    public static GameObject GetRandomObstaclePrefab(Obstacle obstacle)
    {
        AssetBundle bundle;

        if (_obstacleBundles.ContainsKey(obstacle.AssetBundleName))
        {
            bundle = _obstacleBundles[obstacle.AssetBundleName];
        }
        else
        {
            bundle = LoadObstacleBundle(obstacle);

            _obstacleBundles[obstacle.AssetBundleName] = bundle;
        }

        var all = bundle.LoadAllAssets<GameObject>();

        Logger.Log($"[RandomEventsManager] Found asset objects: {string.Join(",", all.Select(x => x.name))} prefabName={obstacle.PrefabName}");

        if (obstacle.PrefabName != null)
            return all.First(x => x.name == obstacle.PrefabName) ?? throw new Exception($"Prefab '{obstacle.PrefabName}' not found in AssetBundle {bundle}");

        return all[_rng.Next(all.Length)];
    }
}