using UnityEngine;

namespace DerailValleyRandomEvents;

public static class TrainCarHelper
{
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

        trainCar.brakeSystem.SetHandbrakePosition(0);
    }
}