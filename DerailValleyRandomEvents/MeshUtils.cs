using System;
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

    private static Mesh CreatePyramidMesh()
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

}