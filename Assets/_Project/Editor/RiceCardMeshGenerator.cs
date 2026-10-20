using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public static class RiceCardMeshGenerator
{
    private const string OutputPath = "Assets/_Project/Art/Models/Scene3/RiceOptimized";

    [MenuItem("KuTy/Rice/Apply Rice Card Meshes to open village")]
    public static void ApplyToOpenVillage()
    {
        var scene = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
        if (EditorApplication.isPlayingOrWillChangePlaymode ||
            scene.path != "Assets/_Project/Scenes/Act3_MemoryWorld_OutSide.unity")
        {
            Debug.LogWarning("Open the outdoor village in Edit Mode before applying rice card meshes.");
            return;
        }
        GenerateAll();
        var lod0 = AssetDatabase.LoadAssetAtPath<Mesh>(OutputPath + "/CayLua_Card_LOD0.asset");
        var lod1 = AssetDatabase.LoadAssetAtPath<Mesh>(OutputPath + "/CayLua_Card_LOD1.asset");
        var lod2 = AssetDatabase.LoadAssetAtPath<Mesh>(OutputPath + "/CayLua_Card_LOD2.asset");
        var material = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Art/Materials/Scene3/RiceOptimized/Lua_Ruong_Chung.mat");
        int changed = 0;
        foreach (var renderer in UnityEngine.Object.FindObjectsOfType<RuralRiceFieldRenderer>(true))
        {
            if (renderer.gameObject.scene != scene) continue;
            Undo.RecordObject(renderer, "Apply rice card meshes");
            renderer.nearMesh = lod0;
            renderer.middleMesh = lod1;
            renderer.farMesh = lod2;
            renderer.sharedMaterial = material;
            renderer.Regenerate();
            EditorUtility.SetDirty(renderer);
            changed++;
        }
        foreach (var regions in UnityEngine.Object.FindObjectsOfType<VillageRiceRegions>(true))
        {
            if (regions.gameObject.scene != scene) continue;
            regions.Rebuild();
            EditorUtility.SetDirty(regions);
        }
        // Applying meshes is explicit and reviewable. Reloading code must never save the open scene.
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
        Debug.Log($"[RiceCardMeshGenerator] Applied card meshes to {changed} fields; save the scene after reviewing.");
    }

    [MenuItem("KuTy/Rice/Generate Rice Card Meshes")]
    public static void GenerateAll()
    {
        GenerateLOD0();
        GenerateLOD1();
        GenerateLOD2();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[RiceCardMeshGenerator] Successfully generated Rice Card Meshes (LOD0, LOD1, LOD2)!");
    }

    // LOD0: Star-Quad (3 intersecting quads at 0, 60, 120 deg), 3 vertical height segments
    public static Mesh GenerateLOD0()
    {
        return BuildCardMesh("CayLua_Card_LOD0", quadCount: 3, heightSegments: 3, height: 0.92f, baseWidth: 0.16f, midWidth: 0.48f, topWidth: 0.44f);
    }

    // LOD1: Cross-Quad (2 intersecting quads at 0, 90 deg), 2 vertical height segments
    public static Mesh GenerateLOD1()
    {
        return BuildCardMesh("CayLua_Card_LOD1", quadCount: 2, heightSegments: 2, height: 0.92f, baseWidth: 0.16f, midWidth: 0.48f, topWidth: 0.44f);
    }

    // LOD2: Cross-Quad (2 intersecting flat quads), 1 vertical segment
    public static Mesh GenerateLOD2()
    {
        return BuildCardMesh("CayLua_Card_LOD2", quadCount: 2, heightSegments: 1, height: 0.92f, baseWidth: 0.22f, midWidth: 0.48f, topWidth: 0.44f);
    }

    private static Mesh BuildCardMesh(string assetName, int quadCount, int heightSegments, float height, float baseWidth, float midWidth, float topWidth)
    {
        var vertices = new List<Vector3>();
        var normals = new List<Vector3>();
        var tangents = new List<Vector4>();
        var uvs = new List<Vector2>();
        var colors = new List<Color>();
        var triangles = new List<int>();

        float angleStep = 180f / quadCount;

        for (int q = 0; q < quadCount; q++)
        {
            float angleRad = (q * angleStep) * Mathf.Deg2Rad;
            Vector3 dir = new Vector3(Mathf.Cos(angleRad), 0, Mathf.Sin(angleRad));
            Vector3 perp = new Vector3(-dir.z, 0, dir.x);

            int baseVertexIndex = vertices.Count;

            for (int seg = 0; seg <= heightSegments; seg++)
            {
                float t = (float)seg / heightSegments;
                float y = t * height;

                // Width curve: slender root, wide drooping leaf canopy, tapered tips
                float w;
                if (t < 0.4f)
                {
                    w = Mathf.Lerp(baseWidth, midWidth, t / 0.4f);
                }
                else
                {
                    w = Mathf.Lerp(midWidth, topWidth, (t - 0.4f) / 0.6f);
                }

                // Slight natural outward bulge for 3D fullness
                float outwardBulge = Mathf.Sin(t * Mathf.PI) * 0.04f;

                Vector3 leftPos = -dir * (w * 0.5f) + perp * outwardBulge + Vector3.up * y;
                Vector3 rightPos = dir * (w * 0.5f) + perp * outwardBulge + Vector3.up * y;

                // Hemispherical / spherical normals: point radially outward from center
                // so lighting is volumetric and soft across the clump!
                Vector3 leftNorm = (leftPos + Vector3.up * 0.35f).normalized;
                Vector3 rightNorm = (rightPos + Vector3.up * 0.35f).normalized;

                // Tangent along the quad direction
                Vector4 leftTan = new Vector4(dir.x, 0, dir.z, 1.0f);
                Vector4 rightTan = new Vector4(dir.x, 0, dir.z, 1.0f);

                // Wind bend weights in vertex color:
                // R = bend weight (cubic with height)
                // G = flutter weight (higher on top leaves/heads)
                // B = translucency / thinness weight
                float bend = t * t * (3.0f - 2.0f * t); // smoothstep
                float flutter = t > 0.5f ? (t - 0.5f) * 2.0f : 0f;
                float translucency = Mathf.Lerp(0.2f, 1.0f, t);
                Color vertColor = new Color(bend, flutter, translucency, 1.0f);

                vertices.Add(leftPos);
                vertices.Add(rightPos);

                normals.Add(leftNorm);
                normals.Add(rightNorm);

                tangents.Add(leftTan);
                tangents.Add(rightTan);

                uvs.Add(new Vector2(0f, t));
                uvs.Add(new Vector2(1f, t));

                colors.Add(vertColor);
                colors.Add(vertColor);
            }

            // Generate two-sided triangle faces for each height segment
            for (int seg = 0; seg < heightSegments; seg++)
            {
                int row0 = baseVertexIndex + seg * 2;
                int row1 = row0 + 2;

                // Front face
                triangles.Add(row0);
                triangles.Add(row1);
                triangles.Add(row0 + 1);

                triangles.Add(row0 + 1);
                triangles.Add(row1);
                triangles.Add(row1 + 1);

                // Back face (so two-sided rendering is flawless even without Cull Off)
                triangles.Add(row0);
                triangles.Add(row0 + 1);
                triangles.Add(row1);

                triangles.Add(row0 + 1);
                triangles.Add(row1 + 1);
                triangles.Add(row1);
            }
        }

        var mesh = new Mesh
        {
            name = assetName,
            indexFormat = IndexFormat.UInt16
        };

        mesh.SetVertices(vertices);
        mesh.SetNormals(normals);
        mesh.SetTangents(tangents);
        mesh.SetUVs(0, uvs);
        mesh.SetColors(colors);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateBounds();

        string assetPath = $"{OutputPath}/{assetName}.asset";
        var existing = AssetDatabase.LoadAssetAtPath<Mesh>(assetPath);
        if (existing != null)
        {
            EditorUtility.CopySerialized(mesh, existing);
            UnityEngine.Object.DestroyImmediate(mesh);
            EditorUtility.SetDirty(existing);
            return existing;
        }
        else
        {
            AssetDatabase.CreateAsset(mesh, assetPath);
            return mesh;
        }
    }
}
