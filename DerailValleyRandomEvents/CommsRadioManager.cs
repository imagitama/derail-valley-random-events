using CommsRadioAPI;
using UnityEngine;
using UnityModManagerNet;

namespace DerailValleyRandomEvents;

public class CommsRadioManager
{
    UnityModManager.ModEntry.ModLogger Logger => Main.ModEntry.Logger;
    CommsRadioMode? _mode;

    public CommsRadioManager()
    {
        Logger.Log("[CommsRadioManager] Start");

        CleanupHelper.Add(typeof(RandomEventsManager), () =>
        {
            if (_mode != null)
                GameObject.Destroy(_mode);
        });

        ControllerAPI.Ready += () =>
        {
            Logger.Log("[CommsRadioManager] Controller is ready");
            _mode = CommsRadioMode.Create(new ObstacleClearerBehavior(), new Color(0.53f, 0f, 1f));
        };
    }
}