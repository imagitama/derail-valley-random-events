using System;
using System.Linq;
using LocoSim.Implementations;
using UnityEngine;
using UnityModManagerNet;

namespace DerailValleyRandomEvents;

public class ObstacleComponent : MonoBehaviour
{
    private static UnityModManager.ModEntry.ModLogger Logger => Main.ModEntry.Logger;
    private bool _isHighlighted = false;
    private Material? _highlightMaterial;
    private GameObject? _highlightObject;
    public Obstacle obstacle;
    public Rigidbody rb;
    public Action<TrainCar> OnStrongImpact;
    private const float _distanceToLookAtPlayer = 25f;
    // exploding
    private bool _isExplodingEnabled = false;
    private Transform baseStuff;
    private Transform explodingStuff;
    // smooth movement
    Quaternion? _targetRot;
    Vector3? _targetPos;
    const float _degreesThresholdWhenFinishedTurning = 5f;
    const float _thresholdWhenFinishedMoving = 0.1f;
    // scaredness
    private bool _isCurrentlyScared = false;
    private const float _distanceToBeScared = 75f;
    public float _degreesPerSec = 300f;
    public float _moveSpeed = 5f;

    float? _uprightDelayStart;
    const float _uprightDelaySeconds = 1f;
    const float _maxUprightAngle = 30f;
    private DebugBox? debugBox;

    void Start()
    {
        Logger.Log($"Obstacle.Start type={obstacle.Type}");

        rb = GetComponent<Rigidbody>();

        if (rb == null)
            throw new Exception("No rb");

        if (obstacle.ExplodeThreshold != null && obstacle.ExplodeThreshold > 0)
            SetupExploding();
    }

    void SetupExploding()
    {
        Logger.Log($"Obstacle.SetupExploding");

        baseStuff = transform.Find("[Base]");

        if (baseStuff == null)
            throw new Exception("Need [Base]");

        explodingStuff = transform.Find("[Explode]");

        if (explodingStuff == null)
            throw new Exception("Need [Explode]");

        explodingStuff.gameObject.SetActive(false);

        _isExplodingEnabled = true;
    }

    void OnExplode()
    {
        Logger.Log($"Obstacle.OnExplode force={obstacle.ExplodeForce!.Value} radius={obstacle.ExplodeRadius!.Value} upwards={obstacle.ExplodeUpwards!.Value}");

        baseStuff.gameObject.SetActive(false);

        Destroy(rb);
        Destroy(GetComponent<Collider>());

        explodingStuff.gameObject.SetActive(true);

        // TODO: add collision stuff to each gibblet

        var bodies = explodingStuff.GetComponentsInChildren<Rigidbody>();
        foreach (var rb in bodies)
        {
            rb.gameObject.layer = (int)DVLayer.Train_Big_Collider;
            rb.AddExplosionForce(obstacle.ExplodeForce.Value, transform.position, obstacle.ExplodeRadius.Value, obstacle.ExplodeUpwards.Value, ForceMode.Impulse);
        }

        var particles = explodingStuff.GetComponentsInChildren<ParticleSystem>();
        foreach (var ps in particles)
        {
            ps.transform.localScale = Vector3.one * 2;
            ps.Play();
        }
    }

    bool GetIsTippedOver()
    {
        var up = rb.transform.up;
        return Vector3.Angle(up, Vector3.up) > _maxUprightAngle;
    }

    void LookAtPlayer()
    {
        var aimPos = PlayerManager.PlayerTransform.position;

        if (PlayerManager.Car != null)
            aimPos = PlayerManager.Car.transform.position;

        aimPos.y = transform.position.y;

        _targetRot = Quaternion.LookRotation((aimPos - transform.position).normalized, Vector3.up);
    }

    void Update()
    {
        if (Main.settings.ShowDebugStuff)
        {
            if (debugBox == null)
                debugBox = new DebugBox(this.transform);

            debugBox.Update();
        }
        else
        {
            if (debugBox != null)
            {
                debugBox.Cleanup();
                debugBox = null;
            }
        }

        if (obstacle.ScaredOfHorn)
            CheckIfShouldBeScared();
    }

    void CheckIfShouldBeScared()
    {
        if (PlayerManager.Car == null)
            return;

        // can only be scared if nearby
        if (Vector3.Distance(transform.position, PlayerManager.Car.transform.position) > _distanceToBeScared)
            return;

        var simFlow = PlayerManager.Car.SimController.simFlow;

        // TODO: move to TrainCarHelper
        if (simFlow.TryGetPort("horn.HORN", out Port port))
        {
            var hornValue = port.Value;

            if (hornValue > 0.5)
            {
                _isCurrentlyScared = true;
                OnScaredByHorn();
            }
        }
        else
        {
            Logger.Log($"Obstacle: FOUND NO HORN");
        }
    }

    void OnScaredByHorn()
    {
        var toPlayer = PlayerManager.Car.transform.position - transform.position;
        toPlayer.y = 0f;

        var runDir = -toPlayer.normalized;

        _targetRot = Quaternion.LookRotation(runDir);
        _targetPos = transform.position + runDir * 10f;

        Logger.Log($"Obstacle: Scared of horn! rot={_targetRot} pos={_targetPos}");
    }

    void FixedUpdate()
    {
        if (rb == null)
            return;

        HandleUprightRecovery();

        SmoothlyMoveTowardsTarget();

        if (obstacle.Gravity != 1)
            rb.AddForce(Physics.gravity * (obstacle.Gravity - 1f) * rb.mass);

        if (obstacle.LookAtPlayer && _targetRot == null && !_isCurrentlyScared)
        {
            if (GetNeedsToLookAtPlayer())
                LookAtPlayer();
        }
    }

    private bool _wasTippedOver = false;

    void HandleUprightRecovery()
    {
        var isTippedOver = GetIsTippedOver();

        if (isTippedOver != _wasTippedOver)
        {
            // Logger.Log("Got tipped over!");
            _wasTippedOver = isTippedOver;
        }

        if (isTippedOver)
        {
            if (_uprightDelayStart == null)
            {
                _uprightDelayStart = Time.time;
                return;
            }

            if (Time.time - _uprightDelayStart.Value >= _uprightDelaySeconds)
            {
                var currentUp = rb.transform.up;
                var axis = Vector3.Cross(currentUp, Vector3.up);

                if (axis.sqrMagnitude > 0.0001f)
                {
                    axis.Normalize();
                    rb.angularVelocity = axis * ((_degreesPerSec * 2) * Mathf.Deg2Rad);
                }

                _uprightDelayStart = null;

                // Logger.Log("We are upright");
            }
        }
        else
        {
            _uprightDelayStart = null;
        }
    }

    void SmoothlyMoveTowardsTarget()
    {
        if (_targetRot.HasValue)
        {
            // var q = Quaternion.RotateTowards(rb.rotation, _targetRot.Value, _degreesPerSec * Time.fixedDeltaTime);
            // rb.MoveRotation(q);

            Vector3 forward = rb.transform.forward;
            Vector3 targetForward = _targetRot.Value * Vector3.forward;

            forward.y = 0f;
            targetForward.y = 0f;

            if (forward.sqrMagnitude < 0.0001f || targetForward.sqrMagnitude < 0.0001f)
                return;

            forward.Normalize();
            targetForward.Normalize();

            float angleDeg = Vector3.SignedAngle(forward, targetForward, Vector3.up);

            float maxRadPerSec = _degreesPerSec * Mathf.Deg2Rad;
            float desiredRadPerSec = Mathf.Clamp(
                angleDeg * Mathf.Deg2Rad / Time.fixedDeltaTime,
                -maxRadPerSec,
                maxRadPerSec
            );

            Vector3 av = rb.angularVelocity;
            av.y = desiredRadPerSec;
            rb.angularVelocity = av;

            if (Quaternion.Angle(rb.rotation, _targetRot.Value) < _degreesThresholdWhenFinishedTurning)
                _targetRot = null;
        }
        else if (_targetPos.HasValue)
        {
            var p = Vector3.MoveTowards(rb.position, _targetPos.Value, _moveSpeed * Time.fixedDeltaTime);
            rb.MovePosition(p);

            if (Vector3.Distance(rb.position, _targetPos.Value) < _thresholdWhenFinishedMoving)
            {
                _targetPos = null;
                _isCurrentlyScared = false;

                Logger.Log($"Obstacle no longer scared");
            }
        }
    }

    bool GetNeedsToLookAtPlayer()
    {
        return Vector3.Distance(transform.position, PlayerManager.PlayerTransform.transform.position) < _distanceToLookAtPlayer;
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

        var car = GetCarFromCollision(collision);

        if (car != null)
        {
            Logger.Log($"Obstacle: Impact car={car} impulse={impulse} threshold={obstacle.DerailThreshold} type={obstacle.Type}");

            if (obstacle.DerailThreshold > 0 && impulse >= obstacle.DerailThreshold)
            {
                // Logger.Log($"Obstacle: Strong impact car={car} impulse={impulse} threshold={obstacle.DerailThreshold} type={obstacle.Type}");
                Logger.Log($"Obstacle: Strong impact!!!");
                OnStrongImpact.Invoke(car);
            }

            if (_isExplodingEnabled && GetMustExplode(impulse))
            {
                OnExplode();
            }
        }
    }

    TrainCar? GetCarFromCollision(Collision collision)
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

    public string GetObstacleInfo()
    {
        // TODO: better
        // TODO: add a unique ID to help
        return $"{obstacle.Type}";
    }

    public void SetIsHighlighted(bool isHighlighted, Material? highlightMaterial = null)
    {
        _isHighlighted = isHighlighted;
        _highlightMaterial = highlightMaterial;

        UpdateState();
    }

    private void UpdateState()
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

    private void HideHighlightObject()
    {
        if (_highlightObject == null)
            return;

        _highlightObject.SetActive(false);
    }

    private void ShowHighlightObject()
    {
        if (_highlightObject != null)
        {
            _highlightObject.SetActive(true);
            return;
        }

        var meshFilter = GetComponentInChildren<MeshFilter>();
        if (meshFilter == null)
        {
            Logger.Log("[ObstacleComponent] No MeshFilter found - cannot create highlight cube");
            return;
        }

        var mesh = meshFilter.sharedMesh;
        if (mesh == null)
        {
            Logger.Log("[ObstacleComponent] No mesh found - cannot create highlight cube");
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