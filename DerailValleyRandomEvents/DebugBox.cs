using System.Collections.Generic;
using DV.OriginShift;
using UnityEngine;
using UnityModManagerNet;

namespace DerailValleyRandomEvents;

public class DebugBox
{
    private static UnityModManager.ModEntry.ModLogger Logger => Main.ModEntry.Logger;
    GameObject debugBoxObj;
    Mesh debugBoxMesh;
    BoxCollider boxCollider;

    Transform _parent;

    // public DebugBox()
    // {
    //     CleanupHelper.Add(typeof(DebugBox), () =>
    //     {
    //         Cleanup();
    //     });

    //     EnsureDebugBox();
    // }

    public void Cleanup()
    {
        GameObject.Destroy(debugBoxObj);
    }

    public DebugBox(Transform parent)
    {
        // destroyed with parent (hopefully)
        _parent = parent;

        boxCollider = parent.GetComponent<BoxCollider>();

        EnsureDebugBox();
    }

    void EnsureDebugBox()
    {
        if (debugBoxObj != null)
            return;

        debugBoxObj = new GameObject("DerailValleyRandomEvents_DebugBox");

        debugBoxObj.transform.SetParent(_parent ?? WorldMover.OriginShiftParent);

        var mf = debugBoxObj.AddComponent<MeshFilter>();
        var mr = debugBoxObj.AddComponent<MeshRenderer>();

        debugBoxMesh = new Mesh();
        debugBoxMesh.name = "DebugWireBox";
        mf.sharedMesh = debugBoxMesh;

        var mat = new Material(Shader.Find("Hidden/Internal-Colored"));
        mat.hideFlags = HideFlags.HideAndDontSave;
        mat.SetInt("_ZWrite", 0);
        mat.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
        mat.SetInt("_ZTest", (int)UnityEngine.Rendering.CompareFunction.Always);

        mr.sharedMaterial = mat;
    }

    public void Update()
    {
        if (boxCollider == null)
            return;

        var center = boxCollider.transform.position;

        // Logger.Log($"CENTER {center}");

        // var center = boxCollider.bounds.center + OriginShift.currentMove;
        // var center = boxCollider.transform.TransformPoint(boxCollider.center) - OriginShift.currentMove;
        var halfExtents = Vector3.Scale(boxCollider.size * 0.5f, boxCollider.transform.lossyScale);
        var rotation = boxCollider.transform.rotation;

        UpdateDebugBox(center, halfExtents, rotation: null, color: Color.cyan);
    }

    public void UpdateDebugBox(
        Vector3 center,
        Vector3 halfExtents,
        Color color,
        Quaternion? rotation = null,
        float thickness = 0.05f
    )
    {
        EnsureDebugBox();

        var mr = debugBoxObj.GetComponent<MeshRenderer>();
        mr.sharedMaterial.color = color;

        Vector3[] c =
        {
        new(-halfExtents.x, -halfExtents.y, -halfExtents.z),
        new( halfExtents.x, -halfExtents.y, -halfExtents.z),
        new( halfExtents.x, -halfExtents.y,  halfExtents.z),
        new(-halfExtents.x, -halfExtents.y,  halfExtents.z),

        new(-halfExtents.x,  halfExtents.y, -halfExtents.z),
        new( halfExtents.x,  halfExtents.y, -halfExtents.z),
        new( halfExtents.x,  halfExtents.y,  halfExtents.z),
        new(-halfExtents.x,  halfExtents.y,  halfExtents.z),
    };

        for (int i = 0; i < c.Length; i++)
        {
            if (rotation != null)
                c[i] = rotation.Value * c[i] + center;
            else
                c[i] = c[i] + center;
        }

        int[,] edges =
        {
        {0,1},{1,2},{2,3},{3,0},
        {4,5},{5,6},{6,7},{7,4},
        {0,4},{1,5},{2,6},{3,7}
    };

        var verts = new List<Vector3>();
        var tris = new List<int>();

        for (int i = 0; i < edges.GetLength(0); i++)
        {
            Vector3 a = c[edges[i, 0]];
            Vector3 b = c[edges[i, 1]];

            AddThickLine(verts, tris, a, b, thickness);
        }

        debugBoxMesh.Clear();
        debugBoxMesh.SetVertices(verts);
        debugBoxMesh.SetTriangles(tris, 0);
        debugBoxMesh.RecalculateBounds();
    }

    void AddThickLine(
        List<Vector3> verts,
        List<int> tris,
        Vector3 a,
        Vector3 b,
        float thickness
    )
    {
        Vector3 dir = (b - a).normalized;
        Vector3 up = Vector3.up;

        if (Vector3.Dot(dir, up) > 0.99f)
            up = Vector3.right;

        Vector3 right = Vector3.Cross(dir, up).normalized * thickness;

        int start = verts.Count;

        verts.Add(a - right);
        verts.Add(a + right);
        verts.Add(b + right);
        verts.Add(b - right);

        tris.Add(start + 0);
        tris.Add(start + 1);
        tris.Add(start + 2);

        tris.Add(start + 0);
        tris.Add(start + 2);
        tris.Add(start + 3);
    }

}