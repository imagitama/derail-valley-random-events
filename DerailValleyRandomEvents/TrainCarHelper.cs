using System;
using System.Collections.Generic;
using DV.Utils;
using DV.VFX;
using Unity.Mathematics;
using UnityEngine;
using UnityModManagerNet;

namespace DerailValleyRandomEvents;

public static class TrainCarHelper
{
    private static UnityModManager.ModEntry.ModLogger Logger => Main.ModEntry.Logger;

    public static void RerailTrain(TrainCar trainCar, bool isReverse = false)
    {
        var (closestTrack, point) = RailTrack.GetClosest(trainCar.transform.position);

        if (point == null)
            return;

        var rerailPos = (Vector3)point.Value.position + WorldMover.currentMove;

        var forward = point.Value.forward;

        if (isReverse)
            forward = -forward;

        void OnRerailed()
        {
            trainCar.brakeSystem.SetHandbrakePosition(0); //, forced: true
            trainCar.OnRerailed -= OnRerailed;
        }

        trainCar.OnRerailed += OnRerailed;

        if (trainCar.derailed)
            trainCar.Rerail(closestTrack, rerailPos, forward);
        else
            trainCar.SetTrack(closestTrack, rerailPos, forward);
    }

    public static void ReverseTrain(TrainCar trainCar)
    {
        var (closestTrack, point) = RailTrack.GetClosest(trainCar.transform.position);

        if (point == null)
            return;

        var rerailPos = (Vector3)point.Value.position + WorldMover.currentMove;

        var forward = point.Value.forward;

        trainCar.SetTrack(closestTrack, rerailPos, forward);

        // TODO: do it in callback
        trainCar.brakeSystem.SetHandbrakePosition(0);
    }

    public static List<Action> HornUnsubscribes = [];

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

            HornUnsubscribes.Add(unsub);

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
}