using UnityModManagerNet;

namespace DerailValleyRandomEvents;

public class Settings : UnityModManager.ModSettings, IDrawable
{
    [Draw(Label = "How often to check if we need to emit (in seconds)")]
    public float CheckIntervalSeconds = 1.0f;      // how often to evaluate
    [Draw(Label = "Minimum delay between events")]
    public float MinIntervalSeconds = 120.0f;       // min time between events
    [Draw(Label = "Maximum delay between events")]
    public float MaxIntervalSeconds = 360.0f;       // max time between events
    [Draw(Label = "Initial delay after loading a save game")]
    public float InitialMinDelay = 30.0f;          // delay after game start
    [Draw(Label = "Percentage chance of an event occuring (eg. 0.5 for 50%)")]
    public float RandomChance = 0.25f;             // 25% chance per check
    [Draw(Label = "How far infront of your train to spawn an obstacle (in meters)")]
    public float ObstacleSpawnDistance = 500f;     // 500m from your train
    [Draw(Label = "If to render debugging stuff")]
    public bool ShowDebugStuff = false;

    public override void Save(UnityModManager.ModEntry modEntry)
    {
        Save(this, modEntry);
    }

    public void OnChange()
    {

    }
}
