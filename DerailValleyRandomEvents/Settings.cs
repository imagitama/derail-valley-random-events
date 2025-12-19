using UnityModManagerNet;

namespace DerailValleyRandomEvents;

public class Settings : UnityModManager.ModSettings, IDrawable
{
    public const float DefaultObstacleSpawnDistance = 500f;

    [Draw(Label = "Enable random events")]
    public bool RandomSpawningEnabled = false; // FALSE only while still WIP
    [Draw(Label = "How often to check if we need to emit (seconds, default 1)")]
    public float CheckIntervalSeconds = 1.0f;

    [Draw(Label = "Percent chance of an event occuring every check (percent, default 0.25)")]
    public float RandomChance = 0.25f;
    [Draw(Label = "Min time between events (seconds, default 1800 or 30 min)")]
    public float MinIntervalSeconds = 1800.0f;
    [Draw(Label = "Max time between events (guaranteed after this) (seconds, default 3600 or 1 hour)")]
    public float MaxIntervalSeconds = 3600.0f;
    [Draw(Label = "Minimum wait after loading into the game (seconds, default 100)")]
    public float InitialDelay = 100.0f;
    [Draw(Label = "How far infront of your train to spawn an obstacle (meters, default 500)")]
    public float ObstacleSpawnDistance = DefaultObstacleSpawnDistance;
    [Draw(Label = "How far should an obstacle be until it is cleaned up (removed) from the game (meters, default 550)")]
    public float ObstacleCleanupDistance = DefaultObstacleSpawnDistance + 50f;
    [Draw(Label = "If to render debugging stuff")]
    public bool ShowDebugStuff = false;
    [Draw(Label = "Chance of getting a warning notification before an event (percent, 0 to disable, default 0.75)")]
    public float WarningChance = 0.75f;
    [Draw(Label = "Volume of warning message sound effect (0 to disable, default 1)")]
    public float WarningSoundEffectVolume = 1f;

    public override void Save(UnityModManager.ModEntry modEntry)
    {
        Save(this, modEntry);
    }

    public void OnChange()
    {

    }
}
