using System;
using System.Linq;
using dnlib.DotNet.MD;
using DV.Localization.Debug;
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
    public Rigidbody rigidbody;
    public Action OnStrongImpact;
    // exploding
    private Transform baseStuff;
    private Transform explodingStuff;

    void Start()
    {
        Logger.Log($"Obstacle.Start type={obstacle.Type}");

        rigidbody = GetComponent<Rigidbody>();

        if (rigidbody == null)
            throw new Exception("No rigidbody");

        if (obstacle.ExplodeThreshold != null)
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
    }

    void OnExplode()
    {
        Logger.Log($"Explode! force={obstacle.ExplodeForce.Value} radius={obstacle.ExplodeRadius.Value} upwards={obstacle.ExplodeUpwards.Value}");

        baseStuff.gameObject.SetActive(false);

        Destroy(rigidbody);

        Destroy(GetComponent<Collider>());

        explodingStuff.gameObject.SetActive(true);

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

    void LookAtPlayer()
    {
        var aimPos = PlayerManager.PlayerTransform.position;

        if (PlayerManager.Car != null)
            aimPos = PlayerManager.Car.transform.position;

        aimPos.y = transform.position.y;
        transform.LookAt(aimPos);
    }

    private float stillThreshold = 0.1f;

    void FixedUpdate()
    {
        if (rigidbody == null)
            return;

        if (obstacle.Gravity != 1)
        {
            rigidbody.AddForce(Physics.gravity * (obstacle.Gravity - 1f) * rigidbody.mass);
        }

        if (obstacle.LookAtPlayer)
        {
            if (rigidbody.velocity.sqrMagnitude < stillThreshold * stillThreshold)
                LookAtPlayer();
        }
    }

    bool GetMustExplode(float impulse)
    {
        if (obstacle.ExplodeThreshold == null || obstacle.ExplodeForce == null || obstacle.ExplodeRadius == null || obstacle.ExplodeUpwards == null)
            return false;

        return impulse >= obstacle.ExplodeThreshold;
    }

    void OnCollisionEnter(Collision collision)
    {
        var impulse = collision.impulse.magnitude;

        if (GetIsLocoColliding(collision))
        {
            if (impulse >= obstacle.DerailThreshold)
            {
                Logger.Log($"Obstacle.OnStrongImpact type={obstacle.Type}");
                OnStrongImpact.Invoke();
            }

            if (GetMustExplode(impulse))
            {
                OnExplode();
            }
        }
    }

    bool GetIsLocoColliding(Collision collision)
    {
        // TODO: cache/do this way better
        return PlayerManager.Car != null && collision.collider == PlayerManager.Car.carColliders.transform.GetComponentsInChildren<Collider>().ToList().Contains(collision.collider);
    }

    public string GetObstacleInfo()
    {
        if (obstacle == null)
            throw new Exception("Need an obstacle");

        // TODO: better
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