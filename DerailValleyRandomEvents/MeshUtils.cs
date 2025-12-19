using System;
using System.Reflection;
using UnityEngine;
using UnityModManagerNet;

namespace DerailValleyRandomEvents;

public static class MeshUtils
{
    private static UnityModManager.ModEntry.ModLogger Logger => Main.ModEntry.Logger;

    public static GameObject CreateArrow(Vector3? scale = null, Color? color = null)
    {
        var root = new GameObject("Arrow");

        var shaft = GameObject.CreatePrimitive(PrimitiveType.Cube);
        shaft.transform.SetParent(root.transform, false);
        shaft.transform.localScale = new Vector3(1f, 0.1f, 0.1f);
        shaft.transform.localPosition = new Vector3(0.5f, 0f, 0f);
        GameObject.Destroy(shaft.GetComponent<Collider>());
        var shaftRenderer = shaft.GetComponent<MeshRenderer>();

        var head = new GameObject("ArrowHead");
        head.transform.SetParent(root.transform, false);
        head.transform.localScale = new Vector3(0.25f, 0.25f, 0.25f);
        head.transform.localRotation = Quaternion.Euler(0, 0, 90f);
        var headRenderer = head.AddComponent<MeshRenderer>();
        var meshFilter = head.AddComponent<MeshFilter>();
        meshFilter.mesh = CreatePyramidMesh(); ;

        shaftRenderer.material.color = color ?? Color.white;
        headRenderer.material.color = color ?? Color.white;

        root.transform.localScale = scale ?? new Vector3(1, 1, 1);
        root.transform.rotation = Quaternion.Euler(0f, 0f, 0f);

        return root;
    }

    public static Mesh CreatePyramidMesh()
    {
        var mesh = new Mesh();

        Vector3 p0 = new Vector3(-0.5f, 0f, -0.5f);
        Vector3 p1 = new Vector3(0.5f, 0f, -0.5f);
        Vector3 p2 = new Vector3(0.5f, 0f, 0.5f);
        Vector3 p3 = new Vector3(-0.5f, 0f, 0.5f);
        Vector3 top = new Vector3(0f, 1f, 0f);

        mesh.vertices =
        [
            p0, p1, p2, p3,     // base 0–3
            p0, p1, top,        // side 1 (4–6)
            p1, p2, top,        // side 2 (7–9)
            p2, p3, top,        // side 3 (10–12)
            p3, p0, top         // side 4 (13–15)
        ];

        mesh.triangles =
        [
            0, 1, 2,
            0, 2, 3,
            4, 6, 5,
            7, 9, 8,
            10, 12, 11,
            13, 15, 14
        ];

        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        return mesh;
    }

    public static GameObject CreateDebugSphere(float scale = 2f)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.transform.localScale = new Vector3(scale, scale, scale);

        var collider = go.GetComponent<Collider>();
        if (collider != null)
            GameObject.Destroy(collider);

        var renderer = go.GetComponent<MeshRenderer>();
        var mat = new Material(Shader.Find("Standard"));
        mat.color = new Color(1f, 0f, 0f, 0.35f);
        mat.SetFloat("_Mode", 3);                       // Transparent
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);
        mat.DisableKeyword("_ALPHATEST_ON");
        mat.EnableKeyword("_ALPHABLEND_ON");
        mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        mat.renderQueue = 3000;

        renderer.material = mat;

        return go;
    }

    public static BoxCollider AddOrUpdateCombinedBoxCollider(GameObject root)
    {
        var renderers = root.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
            throw new Exception("No renderers found in children");

        Bounds bounds = renderers[0].bounds;

        for (int i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);

        // NOTE: destroying an existing collider wont actually happen until next frame

        var collider = root.GetComponent<BoxCollider>() ?? root.AddComponent<BoxCollider>();
        collider.center = root.transform.InverseTransformPoint(bounds.center);
        collider.size = root.transform.InverseTransformVector(bounds.size);

        return collider;
    }
}