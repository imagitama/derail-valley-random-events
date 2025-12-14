using System.Linq;
using UnityEngine;
using UnityModManagerNet;

namespace DerailValleyRandomEvents;

public static class TrackWalking
{
    public static UnityModManager.ModEntry.ModLogger Logger => Main.ModEntry.Logger;

    public static bool IsTrackOutBranch(this RailTrack current, RailTrack next)
    {
        if (!current.outIsConnected)
            return false;

        return current.GetAllOutBranches().Any(b => b.track == next);
    }

    public static RailTrack GetNextTrack(this RailTrack current, bool direction)
    {
        return direction ? GetOutTrack(current) : GetInTrack(current);
    }

    public static bool GetDirectionFromPrev(this RailTrack current, RailTrack prev)
    {
        if (IsTrackOutBranch(current, prev))
        {
            return false;
        }

        return true;
    }

    public static RailTrack GetOutTrack(this RailTrack current)
    {
        if (current.outJunction != null)
        {
            if (current.outJunction.inBranch.track == current)
            {
                return current.outJunction.outBranches[current.outJunction.selectedBranch].track;
            }
            else
            {
                return current.outJunction.inBranch.track;
            }
        }
        else
        {
            return current.outBranch.track;
        }
    }
    public static RailTrack GetInTrack(this RailTrack current)
    {
        if (current.inJunction != null)
        {
            if (current.inJunction.inBranch.track == current)
            {
                return current.inJunction.outBranches[current.inJunction.selectedBranch].track;
            }
            else
            {
                return current.inJunction.inBranch.track;
            }
        }
        else
        {
            return current.inBranch.track;
        }
    }

    public static bool GetIsForwardsOnTrack(RailTrack track, Transform trainTransform)
    {
        var curve = track.curve;

        float t = curve.GetClosestT(trainTransform.position);

        float dt = 0.001f;
        Vector3 p1 = curve.GetPointAt(t);
        Vector3 p2 = curve.GetPointAt(Mathf.Clamp01(t + dt));

        Vector3 trackDir = (p2 - p1).normalized;

        Vector3 forward = trainTransform.forward;
        return Vector3.Dot(forward, trackDir) > 0f;
    }

    public static float GetClosestT(this BezierCurve curve, Vector3 worldPos)
    {
        float length = curve.length;
        int samples = Mathf.Clamp(Mathf.CeilToInt(length * 10f), 50, 2000);

        float bestT = 0;
        float bestDist = float.MaxValue;

        for (int i = 0; i <= samples; i++)
        {
            float t = i / (float)samples;
            float dist = Vector3.SqrMagnitude(curve.GetPointAt(t) - worldPos);
            if (dist < bestDist)
            {
                bestDist = dist;
                bestT = t;
            }
        }
        return bestT;
    }

    public static (RailTrack, Vector3, Quaternion) GetAheadTrack(this RailTrack current, Vector3 startWorldPos, Vector3 velocity, double aheadDistance)
    {
        var closestT = GetClosestT(current.curve, startWorldPos);
        var currentCarDistanceOnTrack = current.curve.length * closestT;
        var isForwardOnTrack = IsTravellingForward(velocity, current.curve.GetPointAt(0), current.curve.GetPointAt(1));
        return current.GetAheadTrackWithDirection(currentCarDistanceOnTrack, isForwardOnTrack, aheadDistance);
    }

    public static (RailTrack, Vector3, Quaternion) GetAheadTrack(this RailTrack current, Vector3 startWorldPos, bool isForwardOnTrack, double aheadDistance)
    {
        var closestT = GetClosestT(current.curve, startWorldPos);
        var currentCarDistanceOnTrack = current.curve.length * closestT;
        return current.GetAheadTrackWithDirection(currentCarDistanceOnTrack, isForwardOnTrack, aheadDistance);
    }

    public static (RailTrack, Vector3, Quaternion) GetAheadTrackWithDirection(this RailTrack current, double currentDistance, bool direction, double aheadDistance)
    {
        aheadDistance -= direction ? current.curve.length - currentDistance : currentDistance;

        Logger.Log($"GetAheadTrack current={current.name} length={current.curve.length} currentDistance={currentDistance} direction={direction} aheadDistance={aheadDistance}");

        while (aheadDistance >= 0.0f)
        {
            RailTrack nextTrack = current.GetNextTrack(direction);

            Logger.Log($"Loop distanceLeft={aheadDistance} next={nextTrack.name} length={current.curve.length}");

            if (nextTrack == null)
                break;

            direction = nextTrack.GetDirectionFromPrev(current);
            current = nextTrack;

            aheadDistance -= current.curve.length;
        }

        double span = direction ? current.curve.length + aheadDistance : -aheadDistance;

        if (span < 0) span = 0;
        if (span > current.curve.length) span = current.curve.length;

        double t = span / current.curve.length;
        Vector3 pos = current.curve.GetPointAt((float)t);

        Vector3 forward = current.curve.GetTangentAt((float)t).normalized;
        Quaternion rot = Quaternion.LookRotation(forward, Vector3.up);

        return (current, pos, rot);
    }

    public static bool IsTravellingForward(
        Vector3 velocity,
        Vector3 lineStart,
        Vector3 lineEnd)
    {
        var lineDir = (lineEnd - lineStart).normalized;
        return Vector3.Dot(velocity, lineDir) > 0f;
    }
}