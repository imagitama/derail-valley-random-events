using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using UnityModManagerNet;

namespace DerailValleyRandomEvents;

public static class ObstacleRegistry
{
    private static UnityModManager.ModEntry.ModLogger Logger => Main.ModEntry.Logger;
    public static string jsonDirPath = Path.Combine(Main.ModEntry.Path, "Dependencies/Obstacles");
    public static List<Obstacle> Obstacles;
    public const string backupSuffix = ".backup";

    public static Obstacle GetObstacleByType(string type)
    {
        return Obstacles.Find(o => o.Type == type);
    }

    public static void PopulateObstacles()
    {
        Obstacles = LoadObstaclesFromDirectory(jsonDirPath);
    }

    public static List<Obstacle> LoadObstaclesFromDirectory(string directoryPath)
    {
        Logger.Log($"Loading obstacles from {directoryPath}");

        var obstacles = new List<Obstacle>();

        if (!Directory.Exists(directoryPath))
            return obstacles;

        var settings = new JsonSerializerSettings
        {
            Converters = { new StringEnumConverter() },
            MissingMemberHandling = MissingMemberHandling.Ignore,
            NullValueHandling = NullValueHandling.Ignore
        };

        var files = Directory.GetFiles(directoryPath, "*.json", SearchOption.TopDirectoryOnly);

        foreach (var file in files)
        {
            if (file.Contains(backupSuffix))
                continue;

            var json = File.ReadAllText(file);
            var obstacle = JsonConvert.DeserializeObject<Obstacle>(json, settings);

            if (obstacle != null)
                obstacles.Add(obstacle);
        }

        Logger.Log($"Loaded {obstacles.Count} obstacles:\n{string.Join("\n", obstacles)}");

        return obstacles;
    }

    public static void SaveObstacle(string type, Obstacle newObstacle, bool createInitialBackup = false)
    {
        Logger.Log($"Save obstacle type={type}\n{newObstacle}");

        // if (!Directory.Exists(directoryPath))
        //     Directory.CreateDirectory(directoryPath);

        var settings = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented,
            Converters = { new StringEnumConverter() },
            NullValueHandling = NullValueHandling.Ignore
        };

        var json = JsonConvert.SerializeObject(newObstacle, settings);

        var filePath = Path.Combine(jsonDirPath, $"{type}.json");
        var backupFilePath = $"{filePath}{backupSuffix}";

        if (File.Exists(filePath) && !File.Exists(backupFilePath) && createInitialBackup)
        {
            Logger.Log($"Backup old JSON {filePath} => {backupFilePath}");
            File.Copy(filePath, backupFilePath);
        }

        File.WriteAllText(filePath, json);
    }
}