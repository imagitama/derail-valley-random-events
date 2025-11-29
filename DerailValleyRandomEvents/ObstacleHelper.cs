using System;
using DV.OriginShift;
using UnityEngine;
using UnityModManagerNet;

namespace DerailValleyRandomEvents;

public static class ObstacleHelper
{
    private static UnityModManager.ModEntry.ModLogger Logger => Main.ModEntry.Logger;

    public static Vector3? WalkAlongTrackLocal(
        RailTrack startTrack,
        Vector3 startLocalPos,
        float distanceToWalkMeters,
        bool isForward,
        Vector3 playerForwardGlobal
    )
    {
        Vector3 currentMove = OriginShift.currentMove;

        if (currentMove == null || playerForwardGlobal == null)
            return null;

        Vector3 startGlobalPos = startLocalPos + currentMove;
        playerForwardGlobal.Normalize();

        // Logger.Log(
        //     $"WalkAlongTrackLocal ===== BEGIN WALK =====\n" +
        //     $"WalkAlongTrackLocal Start local={startLocalPos} global={startGlobalPos}\n" +
        //     $"WalkAlongTrackLocal Distance={distanceToWalkMeters:0.00} m\n" +
        //     $"WalkAlongTrackLocal Track={startTrack.name}\n" +
        //     $"WalkAlongTrackLocal Logical direction={(isForward ? "FORWARD" : "BACKWARD")}"
        // );

        var (closestPoint, _) = RailTrack.GetClosestPoint(startTrack, startGlobalPos);
        if (!closestPoint.HasValue)
        {
            Logger.Log("[ObstacleHelper.WalkAlongTrackLocal] ERROR: GetClosestPoint returned null");
            return null;
        }

        RailTrack currentTrack = startTrack;
        int currentIndex = closestPoint.Value.index;

        Func<int, int> nextIndexFunc;
        Func<int, bool> endOfTrackFunc;
        bool walkIncreasingIndex = false;

        {
            var pts = currentTrack.GetKinkedPointSet().points;
            if (pts != null && pts.Length >= 2)
            {
                int idx = Mathf.Clamp(currentIndex, 1, pts.Length - 2);

                Vector3 p = (Vector3)pts[idx].position + currentMove;
                Vector3 p1 = (Vector3)pts[idx + 1].position + currentMove;
                Vector3 p0 = (Vector3)pts[idx - 1].position + currentMove;

                Vector3 dirIncreasing = (p1 - p).normalized;
                Vector3 dirDecreasing = (p0 - p).normalized;

                float dotInc = Vector3.Dot(dirIncreasing, playerForwardGlobal);
                float dotDec = Vector3.Dot(dirDecreasing, playerForwardGlobal);

                bool realForwardIsIncreasing = dotInc > dotDec;

                Logger.Log(
                    $"WalkAlongTrackLocal Tangent alignment: dotInc={dotInc:0.000} dotDec={dotDec:0.000} -> " +
                    $"realForward={(realForwardIsIncreasing ? "+index" : "-index")}"
                );

                walkIncreasingIndex =
                    isForward
                    ? realForwardIsIncreasing
                    : !realForwardIsIncreasing;

                Logger.Log(
                    $"[ObstacleHelper.WalkAlongTrackLocal] Mapped walking direction = {(walkIncreasingIndex ? "+index" : "-index")}"
                );

                if (walkIncreasingIndex)
                {
                    nextIndexFunc = i => i + 1;
                    endOfTrackFunc = i => i >= pts.Length - 1;
                }
                else
                {
                    nextIndexFunc = i => i - 1;
                    endOfTrackFunc = i => i <= 0;
                }
            }
            else
            {
                Logger.Log("[ObstacleHelper.WalkAlongTrackLocal] ERROR: no tangent available");
                return null;
            }
        }

        float walked = 0f;
        const int MaxHops = 32;
        int hops = 0;

        while (walked < distanceToWalkMeters && hops < MaxHops)
        {
            var pts = currentTrack.GetKinkedPointSet().points;
            if (pts == null || pts.Length < 2)
            {
                Logger.Log("[ObstacleHelper.WalkAlongTrackLocal] ERROR: no points on track");
                return null;
            }

            currentIndex = Mathf.Clamp(currentIndex, 0, pts.Length - 1);

            // Logger.Log(
            //     $"[ObstacleHelper.WalkAlongTrackLocal] Track={currentTrack.name} idx={currentIndex} walked={walked:0.00}/{distanceToWalkMeters:0.00}"
            // );

            // check end of track
            if (endOfTrackFunc(currentIndex))
            {
                Vector3 exitGlobal =
                    (Vector3)pts[currentIndex].position + currentMove;

                Logger.Log(
                    $"[ObstacleHelper.WalkAlongTrackLocal] Reached end of track {currentTrack.name}"
                );

                // decide continuation based on real forward direction
                RailTrack? inT = currentTrack.GetInBranch()?.track;
                RailTrack? outT = currentTrack.GetOutBranch()?.track;

                bool hasIn = inT != null;
                bool hasOut = outT != null;

                // if both exists, it's a split -> stop early
                // TODO: actually handle this
                if (hasIn && hasOut)
                {
                    Logger.Log("[ObstacleHelper.WalkAlongTrackLocal] SPLIT detected -> stopping early");
                    return exitGlobal - currentMove;
                }

                RailTrack? nextTrack =
                    walkIncreasingIndex ? outT : inT;

                if (nextTrack == null)
                {
                    Logger.Log("[ObstacleHelper.WalkAlongTrackLocal] No continuation -> stopping");
                    return exitGlobal - currentMove;
                }

                // otherwise continue
                currentTrack = nextTrack;
                hops++;

                var newPts = currentTrack.GetKinkedPointSet().points;
                if (newPts == null || newPts.Length < 2)
                {
                    Logger.Log("[ObstacleHelper.WalkAlongTrackLocal] Next track invalid -> stop");
                    return exitGlobal - currentMove;
                }

                // pick the end closest to exit
                int newLast = newPts.Length - 1;
                Vector3 p0 = (Vector3)newPts[0].position + currentMove;
                Vector3 p1 = (Vector3)newPts[newLast].position + currentMove;
                currentIndex =
                    (Vector3.Distance(exitGlobal, p0) <
                     Vector3.Distance(exitGlobal, p1))
                    ? 0
                    : newLast;

                Logger.Log(
                    $"[ObstacleHelper.WalkAlongTrackLocal] Hopped to track {currentTrack.name} starting at index {currentIndex}"
                );

                continue;
            }

            // normal segment walking
            int nextIndex = nextIndexFunc(currentIndex);

            Vector3 a = (Vector3)pts[currentIndex].position + currentMove;
            Vector3 b = (Vector3)pts[nextIndex].position + currentMove;

            float segLen = Vector3.Distance(a, b);

            // Logger.Log(
            //     $"[ObstacleHelper.WalkAlongTrackLocal] Segment {currentIndex}->{nextIndex} len={segLen:0.00}"
            // );

            if (walked + segLen >= distanceToWalkMeters)
            {
                float t = (distanceToWalkMeters - walked) / segLen;
                Vector3 result = Vector3.Lerp(a, b, t) - currentMove;

                Logger.Log($"[ObstacleHelper.WalkAlongTrackLocal] RETURN inside segment -> local={result}");
                return result;
            }

            walked += segLen;
            currentIndex = nextIndex;
        }

        Logger.Log("[ObstacleHelper.WalkAlongTrackLocal] Max hops reached");
        return null;
    }
}