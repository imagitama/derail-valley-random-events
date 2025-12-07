using System;
using System.Linq;
using dnlib.DotNet.MD;
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

    void Start()
    {
        Logger.Log($"Obstacle.Start obstacle={obstacle}");

        rigidbody = GetComponent<Rigidbody>();

        if (rigidbody == null)
            throw new Exception("No rigidbody");
    }

    void FixedUpdate()
    {
        if (rigidbody == null || obstacle.Gravity == 1f)
            return;

        rigidbody.AddForce(Physics.gravity * (obstacle.Gravity - 1f) * rigidbody.mass);
    }

    void OnCollisionEnter(Collision collision)
    {
        var impulse = collision.impulse.magnitude;

        if (GetIsLocoColliding(collision) && impulse >= obstacle.ImpulseThreshold)
        {
            Logger.Log($"Obstacle.OnStrongImpact obstacle={obstacle}");
            OnStrongImpact.Invoke();
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