using System.Collections.Generic;
using System.Linq;
using DV.UI;
using DV.UIFramework;
using DV.Utils;
using UnityEngine;
using UnityModManagerNet;

namespace DerailValleyRandomEvents;

public static class NotificationHelper
{
    private static UnityModManager.ModEntry.ModLogger Logger => Main.ModEntry.Logger;
    public static NotificationManager NotificationManager => SingletonBehaviour<ACanvasController<CanvasController.ElementType>>.Instance.NotificationManager;
    public static List<GameObject> notificationObjs = []; // note NotificationManager deletes these objects when hidden

    static NotificationHelper()
    {
        CleanupHelper.Add(typeof(NotificationHelper), () =>
        {
            Logger.Log($"Cleanup {notificationObjs.Count} notifications");
            foreach (var obj in notificationObjs.ToList())
                GameObject.Destroy(obj);
        });
    }

    public static void ShowNotification(string message, string[]? locParams = null)
    {
        Logger.Log($"Show notification '{message}' params={(locParams != null ? string.Join(",", locParams) : "none")}");
        var obj = NotificationManager.ShowNotification(message, locParams, duration: 3f, localize: false, clearExisting: false);
        notificationObjs.Add(obj);
    }

    public static void ShowNotificationViaRadio(string message, string[]? localParams = null)
    {
        ShowNotification(message, localParams);

        // TODO: do this randomly so players dont get used to it
        if (Main.settings.WarningSoundEffectVolume != 0)
            SoundHelper.PlaySound("static.mp3", spatial: true, volume: Main.settings.WarningSoundEffectVolume);
    }
}