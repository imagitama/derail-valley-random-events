using System;
using System.Reflection;
using HarmonyLib;
using UnityModManagerNet;
using UnityEngine;

namespace DerailValleyRandomEvents;

public static class Main
{
    public static UnityModManager.ModEntry ModEntry;
    public static Settings Settings;
    public static RandomEventsManager RandomEventsManager;
    public static CommsRadioManager CommsRadioManager;
    private static GameObject _inGameWindow;

    private static bool Load(UnityModManager.ModEntry modEntry)
    {
        ModEntry = modEntry;

        Harmony? harmony = null;
        try
        {
            Settings = Settings.Load<Settings>(modEntry);

            modEntry.OnGUI = OnGUI;
            modEntry.OnSaveGUI = OnSaveGUI;

            harmony = new Harmony(modEntry.Info.Id);
            harmony.PatchAll(Assembly.GetExecutingAssembly());

            RandomEventsManager = new RandomEventsManager();
            RandomEventsManager.Start();

            CommsRadioManager = new CommsRadioManager();
            CommsRadioManager.Start();

            _inGameWindow = new GameObject("DerailValleyRandomEvents_CustomWindow");
            _inGameWindow.AddComponent<InGameWindow>();
            UnityEngine.Object.DontDestroyOnLoad(_inGameWindow);

            ModEntry.Logger.Log("DerailValleyRandomEvents started");
        }
        catch (Exception ex)
        {
            ModEntry.Logger.LogException($"Failed to load {modEntry.Info.DisplayName}:", ex);
            harmony?.UnpatchAll(modEntry.Info.Id);
            return false;
        }

        modEntry.OnUnload = Unload;
        return true;
    }

    private static void OnGUI(UnityModManager.ModEntry modEntry)
    {
        GUILayout.Label("Mod Settings", GUI.skin.label);

        GUILayout.Label("How often to check if we need to emit (in seconds):");
        Settings.CheckIntervalSeconds = float.Parse(GUILayout.TextField(Settings.CheckIntervalSeconds.ToString()));

        GUILayout.Label("Minimum delay between events:");
        Settings.MinIntervalSeconds = float.Parse(GUILayout.TextField(Settings.MinIntervalSeconds.ToString()));

        GUILayout.Label("Maximum delay between events:");
        Settings.MaxIntervalSeconds = float.Parse(GUILayout.TextField(Settings.MaxIntervalSeconds.ToString()));

        GUILayout.Label("Initial delay after loading a save game:");
        Settings.InitialMinDelay = float.Parse(GUILayout.TextField(Settings.InitialMinDelay.ToString()));

        GUILayout.Label("Percentage chance of an event occuring (eg. 0.5 for 50%):");
        Settings.RandomChance = float.Parse(GUILayout.TextField(Settings.RandomChance.ToString()));

        GUILayout.Label("How far infront of your train to spawn an obstacle (in meters):");
        Settings.ObstacleSpawnDistance = float.Parse(GUILayout.TextField(Settings.ObstacleSpawnDistance.ToString()));
    }

    private static void OnSaveGUI(UnityModManager.ModEntry modEntry)
    {
        Settings.Save(modEntry);
    }

    private static bool Unload(UnityModManager.ModEntry entry)
    {
        if (_inGameWindow != null)
            GameObject.Destroy(_inGameWindow);

        RandomEventsManager.Stop();
        CommsRadioManager.Stop();

        ModEntry.Logger.Log("DerailValleyRandomEvents stopped");
        return true;
    }
}
