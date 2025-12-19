using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using DV;
using DV.Utils;
using DV.VFX;
using DV.WeatherSystem;
using LocoSim.Implementations;
using UnityEngine;
using UnityModManagerNet;

namespace DerailValleyRandomEvents;

public class ObstacleComponent : MonoBehaviour
{
    static UnityModManager.ModEntry.ModLogger Logger => Main.ModEntry.Logger;

    public Obstacle obstacle;
    public Rigidbody rb;
    public Animator animator;
    public Action<TrainCar> OnStrongImpact;
    public float distanceToLookAtPlayer = 25f; // TODO: move to obstacle
    public float? uprightDelay;

    // exploding
    public const string baseTransformName = "[Base]";
    public const string explodeTransformName = "[Explode]";
    public bool hasExploded = false;
    Transform unexplodedStuff;
    Transform explodingStuff;
    List<ParticleSystem> childParticleSystems = [];
    List<Rigidbody> childRigidbodies = [];

    float? _hideBaseStuffDelay;

    // smooth movement
    Quaternion? _targetRot;
    Vector3? _targetPos;
    const float _degreesThresholdWhenFinishedTurning = 5f;
    const float _thresholdWhenFinishedMoving = 0.5f; // at least 0.5m as it gets "stuck" when being so close

    // scaredness
    public bool isScared = false;
    public float distanceToBeScared = 75f; // TODO: move to obstacle
    public float uprightDelaySeconds = 1f; // TODO: move to obstacle
    public float maxUprightAngle = 30f; // TODO: move to obstacle

    // debugging
    DebugBox? debugBox;

    // walking around
    // TODO: move to obstacle
    public float minRadius = 2f;
    public float maxRadius = 5f;
    public float nextPickNewWalkTargetTime;
    public float minPickTime = 10f;
    public float maxPickTime = 20f;
    public float minAngleInfront = -90f;
    public float maxAngleInfront = 90f;
    float? timeWhenTargetRotSet;
    float? timeWhenTargetPosSet;
    float maxSmoothMovementTime = 10f; // when stuck just give up

    // highlighting
    bool _isHighlighted = false;
    Material? _highlightMaterial;
    GameObject? _highlightObject;

    void Start()
    {
        Logger.Log($"Start '{obstacle.Label}'");

        // TransformUtils.LogHierarchy(this.transform);

        if (OnStrongImpact == null)
            throw new Exception("Need a strong impact callback");

        rb = GetComponent<Rigidbody>();

        if (rb == null)
            throw new Exception("No rigidbody");

        if (obstacle.ExplodeThreshold != null && obstacle.ExplodeThreshold > 0)
            SetupExploding();

        animator = GetComponentInChildren<Animator>();

        if (animator != null)
            StartAnimator();

        // Debug.Log($"Animator: {animator}");

        if (obstacle.AnimalType != null)
            ScheduleNextPickNewWalkTarget();
    }

    void Update()
    {
        UpdateExplosion();

        // if (Main.settings.ShowDebugStuff)
        // {
        //     if (debugBox == null)
        //         debugBox = new DebugBox(this.transform);

        //     debugBox.Update();
        // }
        // else
        // {
        //     if (debugBox != null)
        //     {
        //         debugBox.Cleanup();
        //         debugBox = null;
        //     }
        // }

        if (obstacle.ScaredOfHorn)
            UpdateShouldBeScared();

        // always call at end
        UpdateAnimator();
    }

    void FixedUpdate()
    {
        if (rb == null)
            return;

        // if (obstacle.Id == 0)
        //     Debug.Log($"tipped={GetIsTippedOver()} look={GetNeedsToLookAtPlayer()} smooth={GetNeedsToMoveSmoothlyTowardsTarget()}");

        if (GetIsTippedOver())
            ForceUprightIfNeeded();
        else if (GetNeedsToLookAtPlayer())
            KeepLookingAtPlayer();
        else if (GetNeedsToMoveSmoothlyTowardsTarget())
            MoveSmoothlyTowardsTarget();
        else
            KeepUpright();
    }

    void SetupExploding()
    {
        // Debug.Log($"Setup exploding obstacle={obstacle.Label}");

        // TODO: rename transform to "unexploded"
        unexplodedStuff = transform.Find(baseTransformName);
        if (unexplodedStuff == null)
            Logger.Log($"Failed to find {baseTransformName}");

        explodingStuff = transform.Find(explodeTransformName);
        if (explodingStuff == null)
            Logger.Log($"Failed to find {explodeTransformName}");

        // Debug.Log($"Exploding={explodingStuff}");

        if (explodingStuff != null)
        {
            explodingStuff.gameObject.SetActive(false);

            childRigidbodies = explodingStuff.GetComponentsInChildren<Rigidbody>().ToList();

            var particles = explodingStuff.GetComponentsInChildren<ParticleSystem>();
            foreach (var ps in particles)
            {
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

                // skip child particle systems and hope they are linked together
                if (ps.transform.parent.GetComponent<ParticleSystem>() != null)
                    continue;

                childParticleSystems.Add(ps);

                var collider = GetComponent<Collider>();
                // TODO: make setting
                ps.transform.position = collider.bounds.center;
                // TODO: make setting
                // ps.transform.localScale = new Vector3(20f, 20f, 20f);
            }
        }

        // Debug.Log($"Explode setup done ps={childParticleSystems.Count} rb={childRigidbodies.Count}");
    }

    public void Explode()
    {
        if (hasExploded)
            return;

        hasExploded = true;

        Logger.Log($"Explode!!! obstacle={obstacle.Label} force={obstacle.ExplodeForce!.Value} radius={obstacle.ExplodeRadius!.Value} upwards={obstacle.ExplodeUpwards!.Value}");

        if (unexplodedStuff != null)
        {
            // TODO: make delayed hiding a setting
            if (obstacle.AnimalType != null)
            {
                PreventTrainCollision();

                _hideBaseStuffDelay = Time.time + 0.1f;
            }
            else
            {
                HideBaseStuff();
            }
        }

        if (explodingStuff != null)
        {
            // Debug.Log($"Turn on exploding stuff");

            explodingStuff.gameObject.SetActive(true);

            // TODO: on gibblet collision remove it

            foreach (var childRb in childRigidbodies)
            {
                childRb.gameObject.layer = (int)DVLayer.Train_Big_Collider;
                childRb.AddExplosionForce(obstacle.ExplodeForce.Value, transform.position, obstacle.ExplodeRadius.Value, obstacle.ExplodeUpwards.Value, ForceMode.Impulse);
            }

            foreach (var ps in childParticleSystems)
            {
                ps.Clear();
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                ps.Play();
            }

        }
    }

    void PreventTrainCollision()
    {
        gameObject.layer = (int)DVLayer.Default;
    }

    // animal stuff

    bool GetIsTippedOver()
    {
        var up = rb.transform.up;
        return Vector3.Angle(up, Vector3.up) > maxUprightAngle;
    }

    void KeepLookingAtPlayer()
    {
        var aimPos = PlayerManager.PlayerTransform.position;

        if (PlayerManager.Car != null)
            aimPos = PlayerManager.Car.transform.position;

        aimPos.y = transform.position.y;

        SetTargetRot(Quaternion.LookRotation((aimPos - transform.position).normalized, Vector3.up));
    }

    void PickNewWalkTarget()
    {
        if (_targetPos != null)
        {
            Debug.Log("ALREADY GOING");
            ScheduleNextPickNewWalkTarget();
            return;
        }

        float angle = UnityEngine.Random.Range(minAngleInfront, maxAngleInfront);
        float distance = UnityEngine.Random.Range(minRadius, maxRadius);

        Quaternion rot = Quaternion.AngleAxis(angle, Vector3.up);
        Vector3 dir = rot * transform.forward;

        SetTargetRot(Quaternion.LookRotation(dir));
        SetTargetPos(transform.position + dir.normalized * distance);

        // if (obstacle.Id == 0)
        // Debug.Log($"Picked: {_targetPos}");
    }

    void ScheduleNextPickNewWalkTarget()
    {
        // Debug.Log($"Schedule next");
        nextPickNewWalkTargetTime = Time.time + UnityEngine.Random.Range(minPickTime, maxPickTime);
    }

    void UpdateExplosion()
    {
        if (_hideBaseStuffDelay == null)
            return;

        if (Time.time < _hideBaseStuffDelay)
            return;

        HideBaseStuff();
    }

    void HideBaseStuff()
    {
        if (unexplodedStuff != null)
            unexplodedStuff.gameObject.SetActive(false);

        _hideBaseStuffDelay = null;

        GameObject.Destroy(rb);

        var col = GetComponent<Collider>();

        // Debug.Log($"col={col}");

        GameObject.Destroy(col);
    }

    // copied from CattleZone
    private static readonly int ap_Action = Animator.StringToHash("Action");
    private static readonly int ap_Move = Animator.StringToHash("Move");
    private static readonly int ap_Speed = Animator.StringToHash("Speed");
    private static readonly int as_Idle = Animator.StringToHash("Base Layer.Idle");

    void StartAnimator()
    {
        // copied from InitializeAgents
        animator.Update(animator.GetCurrentAnimatorStateInfo(0).length * UnityEngine.Random.value);
        animator.Play(as_Idle);
    }

    void UpdateAnimator()
    {
        if (animator == null || rb == null)
            return;

        // Debug.Log("UpdateAnimator");

        float deltaTime =
            Time.deltaTime *
            (Globals.G.GameParams.DayLengthInMinutes * 60f);

        float speed = rb.velocity.magnitude;

        // if (obstacle.Id == 0)
        //     Debug.Log(speed);

        if (_targetPos != null || _targetRot != null || speed > 0.001f)
        {
            var dist = _targetPos != null ? Vector3.Distance(transform.position, _targetPos.Value) : 0;
            float num = dist / deltaTime * obstacle.AnimationSpeedScale;

            num *= 4; // TODO: configure?

            if (isScared)
                num *= 2; // TODO: configure?

            animator.SetBool(ap_Move, true);
            var animSpeed = Mathf.Max(0.1f, num);
            animator.SetFloat(ap_Speed, animSpeed);
        }
        else
        {
            animator.SetBool(ap_Move, false);
            var animSpeed = 0.1f;
            animator.SetFloat(ap_Speed, animSpeed);
        }

        if (Time.time >= nextPickNewWalkTargetTime)
        {
            // if (obstacle.Id == 0)
            //     Debug.Log("NEED TO WALK...");
            PickNewWalkTarget();
            ScheduleNextPickNewWalkTarget();
        }

        // Debug.Log("UpdateAnimator.Done");
    }

    float? scaredCooldown;

    void UpdateShouldBeScared()
    {
        // Debug.Log("UpdateShouldBeScared");

        if (PlayerManager.Car == null)
            return;

        // can only be scared if nearby
        if (Vector3.Distance(transform.position, PlayerManager.Car.transform.position) > distanceToBeScared)
            return;

        var simFlow = PlayerManager.Car.SimController.simFlow;

        // TODO: move to TrainCarHelper
        if (simFlow.TryGetPort("horn.HORN", out Port port))
        {
            var hornValue = port.Value;

            if (hornValue > 0.5)
            {
                if (scaredCooldown != null && Time.time < scaredCooldown)
                    return;

                ScaredByHorn();
            }
        }
        else
        {
            // Debug.Log($"Obstacle: FOUND NO HORN");
        }

        // Debug.Log("UpdateShouldBeScared.Done");
    }

    public void ScaredByHorn()
    {
        if (PlayerManager.Car == null)
            return;

        isScared = true;
        scaredCooldown = Time.time + UnityEngine.Random.Range(4f, 6f);

        var toPlayer = PlayerManager.Car.transform.position - transform.position;
        toPlayer.y = 0f;

        var baseDir = -toPlayer.normalized;

        var randomYaw = UnityEngine.Random.Range(-15f, 15f); // TODO: configure?
        var runDir = Quaternion.AngleAxis(randomYaw, Vector3.up) * baseDir;
        runDir.y = 0f;

        SetTargetRot(Quaternion.LookRotation(runDir));
        var runDistance = 10f;// TODO: configure?
        SetTargetPos(transform.position + runDir * runDistance);

        Logger.Log($"Scared of horn!!! obstacle={obstacle.Label} rot={_targetRot} pos={_targetPos}");
    }

    void KeepUpright()
    {
        var forward = rb.transform.forward;
        forward.y = 0f;

        if (forward.sqrMagnitude < 0.0001f)
            return;

        var targetRot = Quaternion.LookRotation(forward.normalized, Vector3.up);
        rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRot, 10f * Time.fixedDeltaTime));
    }

    void ForceUprightIfNeeded()
    {
        if (uprightDelay == null)
        {
            if (GetIsTippedOver())
            {
                if (obstacle.Id == 0)
                    Debug.Log("Tipped over!");
                uprightDelay = Time.time + uprightDelaySeconds;
            }
            return;
        }

        if (Time.time < uprightDelay)
            return;

        uprightDelay = null;

        if (!GetIsTippedOver())
            return;

        ForceUpright();
    }

    void ForceUpright()
    {
        rb.angularVelocity = Vector3.zero;

        var forward = Vector3.ProjectOnPlane(rb.transform.forward, Vector3.up);
        if (forward.sqrMagnitude < 0.0001f)
            forward = Vector3.forward;

        rb.MoveRotation(Quaternion.LookRotation(forward, Vector3.up));
    }

    bool GetNeedsToMoveSmoothlyTowardsTarget()
    {
        return _targetPos != null || _targetRot != null;
    }

    public void SetTargetPos(Vector3 pos)
    {
        _targetPos = pos;
        timeWhenTargetPosSet = Time.time;
    }

    public void SetTargetRot(Quaternion rot)
    {
        _targetRot = rot;
        timeWhenTargetRotSet = Time.time;
    }

    void MoveSmoothlyTowardsTarget()
    {
        // Debug.Log("MoveSmoothlyTowardsTarget");

        var turnSpeed = isScared ? obstacle.ScaredTurnSpeed : obstacle.TurnSpeed;
        var moveSpeed = isScared ? obstacle.ScaredMoveSpeed : obstacle.MoveSpeed;

        if (_targetRot.HasValue)
        {
            if (timeWhenTargetRotSet != null && Time.time > (timeWhenTargetRotSet + maxSmoothMovementTime))
            {
                // if (obstacle.Id == 0)
                //     Debug.Log("Giving up rotation :(");

                _targetRot = null;
                timeWhenTargetRotSet = null;
                return;
            }


            Vector3 forward = rb.transform.forward;
            Vector3 targetForward = _targetRot.Value * Vector3.forward;

            forward.y = 0f;
            targetForward.y = 0f;

            if (forward.sqrMagnitude < 0.0001f || targetForward.sqrMagnitude < 0.0001f)
                return;

            forward.Normalize();
            targetForward.Normalize();

            float angleDeg = Vector3.SignedAngle(forward, targetForward, Vector3.up);

            // float maxRadPerSec = turnSpeed * Mathf.Deg2Rad;
            // float desiredRadPerSec = Mathf.Clamp(
            //     angleDeg * Mathf.Deg2Rad / Time.fixedDeltaTime,
            //     -maxRadPerSec,
            //     maxRadPerSec
            // );

            // Vector3 av = rb.angularVelocity;
            // av.y = desiredRadPerSec;
            // rb.angularVelocity = av;

            float maxRadPerSec = turnSpeed * Mathf.Deg2Rad;

            float desiredRadPerSec = Mathf.Clamp(
                angleDeg * Mathf.Deg2Rad / Time.fixedDeltaTime,
                -maxRadPerSec,
                maxRadPerSec
            );

            float delta = desiredRadPerSec - rb.angularVelocity.y;

            rb.AddTorque(Vector3.up * delta, ForceMode.VelocityChange);

            // if (obstacle.Id == 0)
            //     Debug.Log($"TURNING scared={isScared} speed={turnSpeed} deg={angleDeg} delta={delta}");

            if (Quaternion.Angle(rb.rotation, _targetRot.Value) < _degreesThresholdWhenFinishedTurning)
                _targetRot = null;
        }
        else if (_targetPos.HasValue)
        {
            if (timeWhenTargetPosSet != null && Time.time > (timeWhenTargetPosSet + maxSmoothMovementTime))
            {
                // if (obstacle.Id == 0)
                //     Debug.Log("Giving up position :(");

                _targetPos = null;
                timeWhenTargetPosSet = null;
                return;
            }

            var toTarget = _targetPos.Value - rb.position;
            toTarget.y = 0f;

            if (toTarget.sqrMagnitude > 0.0001f)
            {
                var targetRot = Quaternion.LookRotation(toTarget.normalized, Vector3.up);
                var newRot = Quaternion.RotateTowards(
                    rb.rotation,
                    targetRot,
                    turnSpeed * Time.fixedDeltaTime
                );

                rb.MoveRotation(newRot);
            }

            var p = Vector3.MoveTowards(rb.position, _targetPos.Value, moveSpeed * Time.fixedDeltaTime);
            rb.MovePosition(p);

            if (Vector3.Distance(rb.position, _targetPos.Value) < _thresholdWhenFinishedMoving)
            {
                if (obstacle.Id == 0)
                    Debug.Log($"Finished moving to targetPos={_targetPos}");
                _targetPos = null;
                isScared = false;
                scaredCooldown = null;
            }
        }

        // Debug.Log("MoveSmoothlyTowardsTarget.Done");
    }

    bool GetNeedsToLookAtPlayer()
    {
        if (obstacle.LookAtPlayer == false || _targetRot != null || isScared)
            return false;

        return Vector3.Distance(transform.position, PlayerManager.PlayerTransform.transform.position) < distanceToLookAtPlayer;
    }

    bool GetMustExplode(float impulse)
    {
        if (obstacle.ExplodeThreshold == 0 || obstacle.ExplodeThreshold == null || obstacle.ExplodeForce == null || obstacle.ExplodeRadius == null || obstacle.ExplodeUpwards == null)
            return false;

        return impulse >= obstacle.ExplodeThreshold;
    }

    void OnCollisionEnter(Collision collision)
    {
        var impulse = collision.impulse.magnitude;

        var car = TrainCarHelper.GetCarFromCollision(collision);

        if (car != null)
        {
            // Debug.Log($"'{obstacle.Label}' was impacted car={car} impulse={impulse} threshold={obstacle.DerailThreshold}");

            if (obstacle.DerailThreshold > 0 && impulse >= obstacle.DerailThreshold)
            {
                // Debug.Log($"Strong impact!!!");
                OnStrongImpact.Invoke(car);
            }

            if (GetMustExplode(impulse))
                Explode();
        }
    }

    public void SetIsHighlighted(bool isHighlighted, Material? highlightMaterial = null)
    {
        _isHighlighted = isHighlighted;
        _highlightMaterial = highlightMaterial;

        UpdateState();
    }

    void UpdateState()
    {
        if (_isHighlighted)
        {
            ShowHighlightObject();
        }
        else
        {
            HideHighlightObject();
        }
    }

    void HideHighlightObject()
    {
        if (_highlightObject == null)
            return;

        _highlightObject.SetActive(false);
    }

    void ShowHighlightObject()
    {
        if (_highlightObject != null)
        {
            _highlightObject.SetActive(true);
            return;
        }

        var meshFilter = GetComponentInChildren<MeshFilter>();
        if (meshFilter == null)
        {
            // Debug.Log("[ObstacleComponent] No MeshFilter found - cannot create highlight cube");
            return;
        }

        var mesh = meshFilter.sharedMesh;
        if (mesh == null)
        {
            // Debug.Log("[ObstacleComponent] No mesh found - cannot create highlight cube");
            return;
        }

        Bounds b = mesh.bounds;

        _highlightObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
        _highlightObject.name = "HighlightCube";
        _highlightObject.transform.SetParent(meshFilter.transform, false);

        _highlightObject.transform.localPosition = b.center;
        _highlightObject.transform.localRotation = Quaternion.identity;
        _highlightObject.transform.localScale = b.size;

        var collider = _highlightObject.GetComponent<Collider>();
        if (collider != null)
            Destroy(collider);

        var renderer = _highlightObject.GetComponent<MeshRenderer>();
        renderer.material = _highlightMaterial;
    }
}