using UnityEngine;
using UnityModManagerNet;

namespace DerailValleyRandomEvents;

public static class BuiltUpAreaHelper
{
    private static UnityModManager.ModEntry.ModLogger Logger => Main.ModEntry.Logger;

    public static float GetSqrMagnitude(Vector3 pos, Vector3 otherPos)
    {
        return (pos - otherPos).sqrMagnitude;
    }

    public static bool GetIsNearBuiltUpArea(Vector3 pos)
    {
        var stationControllers = StationController.allStations;

        Logger.Log($"GetIsNearBuiltUpArea pos={pos} stations={stationControllers.Count}");

        foreach (var stationController in stationControllers)
        {
            var range = GetStationJobGenerationRange(stationController);

            var sqrDistanceFromStation = GetSqrMagnitude(pos, range.stationCenterAnchor.position);

            if (range.IsPlayerInJobGenerationZone(sqrDistanceFromStation))
            {
                Logger.Log($"Is within job generation zone station={stationController} dist={sqrDistanceFromStation}");
                return true;
            }
        }

        return false;
    }

    public static StationJobGenerationRange GetStationJobGenerationRange(StationController stationController)
    {
        if (stationController.TryGetComponent(out StationJobGenerationRange comp))
            return comp;

        throw new System.Exception($"Could not get StationJobGenerationRange for station {stationController.name}");
    }
}