using System;
using CommsRadioAPI;
using UnityEngine;
using UnityModManagerNet;

namespace DerailValleyRandomEvents;

class ObstacleClearerBehavior : AStateBehaviour
{
    private UnityModManager.ModEntry.ModLogger Logger => Main.ModEntry.Logger;
    private LayerMask _mask;
    private ObstacleComponent? _obstacleToClear;

    public ObstacleClearerBehavior(
        ObstacleComponent? obstacleToClear = null, bool shouldAttemptClear = false) : base(
            new CommsRadioState(titleText: "Clear Obstacle", contentText: $"Use this on an obstacle to clear it\nObstacle: {(obstacleToClear != null ? $"{obstacleToClear.obstacle.Label}" : "none")}"))
    {
        _obstacleToClear = obstacleToClear;

        _mask = LayerMask.GetMask("Train_Big_Collider");

        Logger.Log($"[ObstacleClearerBehavior] Constructor obstacleToClear={obstacleToClear} shouldAttemptClear={shouldAttemptClear}");

        if (obstacleToClear != null && shouldAttemptClear)
        {
            ClearObstacle();
        }
    }

    private void ClearObstacle()
    {
        if (_obstacleToClear == null)
            return;

        Logger.Log($"[ObstacleClearerBehavior] Clear type={_obstacleToClear.obstacle.Type}");

        ObstacleSpawner.ClearObstacle(_obstacleToClear);
    }

    public void PointToObstacle(ObstacleComponent? obstacle, CommsRadioUtility? utility = null)
    {
        if (obstacle == null)
        {
            _obstacleToClear = null;
            return;
        }

        _obstacleToClear = obstacle;

        var validMaterial = utility?.GetMaterial(VanillaMaterial.Valid);

        if (validMaterial == null)
            throw new Exception("No valid material");

        _obstacleToClear.SetIsHighlighted(true, validMaterial);
    }

    public ObstacleComponent? GetObstacleInHierarchy(Transform potentialObstacle)
    {
        ObstacleComponent? result = potentialObstacle.GetComponent<ObstacleComponent>() ?? potentialObstacle.GetComponentInParent<ObstacleComponent>();
        return result;
    }

    public override AStateBehaviour OnUpdate(CommsRadioUtility utility)
    {
        ObstacleSpawner.UnhighlightAllSpawnedObstacles();

        if (Physics.Raycast(utility.SignalOrigin.position, utility.SignalOrigin.forward, out var hit, 100f, _mask))
        {
            var obstacle = GetObstacleInHierarchy(hit.transform);

            if (obstacle != null)
            {
                PointToObstacle(obstacle, utility);
                return this;
            }
        }

        PointToObstacle(null!);

        return this;
    }

    public override void OnLeave(CommsRadioUtility utility, AStateBehaviour? next)
    {
        Logger.Log($"[ObstacleClearerBehavior] OnLeave next={next}");

        ObstacleSpawner.UnhighlightAllSpawnedObstacles();
    }

    public override AStateBehaviour OnAction(CommsRadioUtility utility, InputAction action)
    {
        Logger.Log($"[ObstacleClearerBehavior] OnAction action={action}");

        return action switch
        {
            InputAction.Activate => new ObstacleClearerBehavior(_obstacleToClear, true),
            _ => throw new ArgumentException(),
        };
    }
}