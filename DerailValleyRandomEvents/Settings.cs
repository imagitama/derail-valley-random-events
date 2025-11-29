using UnityModManagerNet;

namespace DerailValleyRandomEvents;

public class Settings : UnityModManager.ModSettings
{
    public float CheckIntervalSeconds = 1.0f;      // how often to evaluate
    public float MinIntervalSeconds = 120.0f;       // min time between events
    public float MaxIntervalSeconds = 360.0f;       // max time between events
    public float InitialMinDelay = 30.0f;          // delay after game start
    public float RandomChance = 0.25f;             // 25% chance per check
    public float ObstacleSpawnDistance = 500f;     // 500m from your train

    public override void Save(UnityModManager.ModEntry modEntry)
    {
        Save(this, modEntry);
    }
}
