using System;
using UnityModManagerNet;

namespace DerailValleyRandomEvents;

public static class CleanupHelper
{
    private static UnityModManager.ModEntry.ModLogger Logger => Main.ModEntry.Logger;
    public static Action cleanupFuncs;

    public static void Add(Type source, Action cleanupFunc)
    {
        Logger.Log($"[Cleanup] Add func from {source}");
        cleanupFuncs += cleanupFunc;
    }

    public static void Cleanup()
    {
        Logger.Log($"[Cleanup] Perform cleanup");
        cleanupFuncs.Invoke();
    }
}