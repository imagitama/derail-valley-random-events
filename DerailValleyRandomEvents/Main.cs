using System;
using System.Reflection;
using HarmonyLib;
using UnityModManagerNet;
using UnityEngine;
using DerailValleyModToolbar;

namespace DerailValleyRandomEvents;

#if DEBUG
[EnableReloading]
#endif
public static class Main
{
    public static UnityModManager.ModEntry ModEntry;
    public static Settings settings;
    public static RandomEventsManager randomEventsManager;
    public static CommsRadioManager commsRadioManager;

    private static bool Load(UnityModManager.ModEntry modEntry)
    {
        ModEntry = modEntry;

        Harmony? harmony = null;
        try
        {
#if DEBUG
            ModEntry.Logger.Log("In debug mode");
#endif

            settings = Settings.Load<Settings>(modEntry);

            modEntry.OnGUI = OnGUI;
            modEntry.OnSaveGUI = OnSaveGUI;
            modEntry.OnUnload = OnUnload;

            harmony = new Harmony(modEntry.Info.Id);
            harmony.PatchAll(Assembly.GetExecutingAssembly());

            randomEventsManager = new RandomEventsManager();
            randomEventsManager.Start();

            commsRadioManager = new CommsRadioManager();
            commsRadioManager.Start();

            ModToolbarAPI.Register(modEntry)
                .AddPanelControl(
                    label: "Random Events",
                    icon: "icon.png",
                    tooltip: "Configure Random Events",
                    type: typeof(InGameWindow),
                    title: "Random Events",
                    width: 400
                )
                .Finish();

            ModEntry.Logger.Log("DerailValleyRandomEvents started");
        }
        catch (Exception ex)
        {
            ModEntry.Logger.LogException($"Failed to load {modEntry.Info.DisplayName}:", ex);
            harmony?.UnpatchAll(modEntry.Info.Id);
            return false;
        }

        return true;
    }

    private static void OnGUI(UnityModManager.ModEntry modEntry)
    {
        settings.Draw(modEntry);
    }

    private static void OnSaveGUI(UnityModManager.ModEntry modEntry)
    {
        settings.Save(modEntry);

        if (randomEventsManager != null)
            randomEventsManager.Reset();
    }

    private static bool OnUnload(UnityModManager.ModEntry modEntry)
    {
        ModEntry.Logger.Log("DerailValleyRandomEvents unloading...");

        randomEventsManager.Stop();
        randomEventsManager = null;

        commsRadioManager.Stop();
        commsRadioManager = null;

        ObstacleSpawner.ClearAllObstacles();

        ModToolbarAPI.Unregister(modEntry);

        ModEntry.Logger.Log("DerailValleyRandomEvents unloaded");
        return true;
    }
}
