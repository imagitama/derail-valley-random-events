using System;
using System.Collections.Generic;
using System.Linq;
using DV.Utils;
using DV.VFX;
using Unity.Mathematics;
using UnityEngine;
using UnityModManagerNet;

namespace DerailValleyRandomEvents;

// TODO: move helpers to utils
public static class TrainCarHelper
{
    static TrainCarHelper()
    {
        CleanupHelper.Add(typeof(TrainCarHelper), () =>
        {
            Logger.Log($"Unsubbing from {hornUnsubscribes.Count} horns");
            foreach (var unsub in hornUnsubscribes.ToList())
                unsub();
        });
    }

    private static UnityModManager.ModEntry.ModLogger Logger => Main.ModEntry.Logger;

    private static List<Action> hornUnsubscribes = [];

    public static Action? SubscribeToHorn(TrainCar trainCar, Action onStart, Action onEnd)
    {
        Logger.Log($"SubscribeToHorn {trainCar.name}");

        var simFlow = trainCar.SimController.simFlow;

        var isActive = false;

        void OnValue(float newVal)
        {
            if (newVal > 0.5)
            {
                if (isActive != true)
                {
                    Logger.Log($"SubscribeToHorn {trainCar.name} isActive => true");
                    isActive = true;
                    onStart();
                }
            }
            else
            {
                if (isActive)
                {
                    Logger.Log($"SubscribeToHorn {trainCar.name} isActive => false");
                    isActive = false;
                    onEnd();
                }
            }
        }
        ;

        if (simFlow.TryGetPort("horn.HORN", out var port))
        {
            Logger.Log($"SubscribeToHorn {trainCar.name} - got port");

            port.ValueUpdatedInternally += OnValue;

            var unsub = () =>
            {
                port.ValueUpdatedInternally -= OnValue;
            };

            hornUnsubscribes.Add(unsub);

            return unsub;
        }
        else
        {
            Logger.Log($"SubscribeToHorn {trainCar.name} - could NOT get port");
        }

        return null;
    }

    /// <summary>
    /// Copied from DV.Rain.isInTunnel
    /// </summary>
    /// <param name="pos"></param>
    /// <returns></returns>
    public static bool GetIsInTunnel(Vector3 pos)
    {
        var ceilingDetection = SingletonBehaviour<CeilingDetection>.Instance;
        int index = ceilingDetection.worldPositionedArray.GetIndex((float3)pos);
        if (index < 0 || (double)ceilingDetection.copiedResults[index].point.y <= (double)pos.y + 3.0)
            return false;
        return true;
    }

    public static TrainCar? GetCarFromCollision(Collision collision)
    {
        var current = collision.collider.transform;

        while (current != null)
        {
            var car = current.GetComponent<TrainCar>();

            if (car != null)
                return car;

            current = current.parent;
        }

        return null;
    }
}