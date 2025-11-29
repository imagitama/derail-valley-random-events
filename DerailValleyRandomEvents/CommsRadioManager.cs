using CommsRadioAPI;
using UnityEngine;

namespace DerailValleyRandomEvents;

public class CommsRadioManager
{
    CommsRadioMode? _mode;

    public void Start()
    {
        Main.ModEntry.Logger.Log("[CommsRadioManager] Start");

        ControllerAPI.Ready += () =>
        {
            Main.ModEntry.Logger.Log("[CommsRadioManager] Controller is ready");
            _mode = CommsRadioMode.Create(new ObstacleClearerBehavior(), new Color(0.53f, 0f, 1f));
        };
    }

    public void Stop()
    {
        Main.ModEntry.Logger.Log("[CommsRadioManager] Stop");

        if (_mode != null)
            GameObject.Destroy(_mode);
    }
}