using DV.OriginShift;
using DV.Utils;
using DV.WorldTools;
using UnityEngine;

namespace DerailValleyRandomEvents;

public static class PlayerUtils
{
    public static Vector3? GetPlayerLocalPosition()
    {
        Vector3? worldPos = GetPlayerWorldPosition();

        if (worldPos == null)
            return null;

        return worldPos - OriginShift.currentMove;
    }

    public static Vector3? GetPlayerGlobalPosition()
    {
        Vector3? worldPos = GetPlayerWorldPosition();

        if (worldPos == null)
            return null;

        return worldPos + OriginShift.currentMove;
    }

    public static Vector3? GetPlayerWorldPosition()
    {
        if (PlayerManager.PlayerTransform?.position == null || OriginShift.currentMove == null)
            return null;

        Vector3 worldPos = PlayerManager.PlayerTransform.position;
        return worldPos;
    }

    public static Biome GetCurrentBiome()
    {
        BiomeProvider instance = SingletonBehaviour<BiomeProvider>.Instance;
        var currentBiome = instance.CurrentBiome;
        return currentBiome;
    }
}