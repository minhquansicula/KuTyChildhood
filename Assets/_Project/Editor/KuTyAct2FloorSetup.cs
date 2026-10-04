using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Rebuilds only the home scene's Ground surface from its existing Floor atlas.</summary>
public static class KuTyAct2FloorSetup
{
    private const string ScenePath = "Assets/_Project/Scenes/Act2_MemoryWorld_Home.unity";
    private const string MeshPath = "Assets/_Project/Art/Models/Act2Home/GroundTerracotta.asset";
    private const string MaterialPath = "Assets/_Project/Art/Materials/Act2GroundTerracotta.mat";

    // Clean 12-by-8 tile section in the lower-left of Floor's texture atlas.
    // Keep UVs inside this island: the full texture also contains walls and black gaps.
    private static readonly Rect TileUV = Rect.MinMaxRect(0.015f, 0.014f, 0.425f, 0.295f);
    private const float PatchWidth = 5.4f;
    private const float PatchDepth = 3.6f;

    [MenuItem("KuTy/Setup/Refresh Scene 2 Ground and Camera")]
    public static void Apply()
    {
        var scene = SceneManager.GetActiveScene();
        if (EditorApplication.isPlayingOrWillChangePlaymode || scene.path != ScenePath)
            throw new InvalidOperationException("Open Act2_MemoryWorld_Home in Edit Mode first.");

        var ground = GameObject.Find("Ground");
        var floor = GameObject.Find("HouseLayout/TraditionalHouseRoom_Visual/Floor");
        var camera = GameObject.Find("Player/PlayerCamera");
        if (ground == null || floor == null || camera == null)
            throw new InvalidOperationException("Scene 2 is missing Ground, Floor, or PlayerCamera.");

        var sourceMaterial = floor.GetComponent<MeshRenderer>().sharedMaterial;
        var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        if (material == null)
        {
            material = new Material(sourceMaterial) { name = "Act2GroundTerracotta" };
            AssetDatabase.CreateAsset(material, MaterialPath);
        }
        else
        {
            Undo.RecordObject(material, "Refresh Scene 2 ground material");
            material.CopyPropertiesFromMaterial(sourceMaterial);
        }

        var generated = BuildMesh(ground.transform.lossyScale);
        var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(MeshPath);
        if (mesh == null)
        {
            mesh = generated;
            AssetDatabase.CreateAsset(mesh, MeshPath);
        }
        else
        {
            Undo.RecordObject(mesh, "Refresh Scene 2 ground mesh");
            EditorUtility.CopySerialized(generated, mesh);
            UnityEngine.Object.DestroyImmediate(generated);
        }

        var filter = ground.GetComponent<MeshFilter>();
        var renderer = ground.GetComponent<MeshRenderer>();
        Undo.RecordObjects(new UnityEngine.Object[] { filter, renderer, camera.transform },
            "Scene 2 floor and camera");
        filter.sharedMesh = mesh;
        renderer.sharedMaterial = material;
        var position = camera.transform.localPosition;
        position.y = 1.8f;
        camera.transform.localPosition = position;
        PrefabUtility.RecordPrefabInstancePropertyModifications(camera.transform);

        EditorUtility.SetDirty(mesh);
        EditorUtility.SetDirty(material);
        EditorSceneManager.MarkSceneDirty(scene);
        AssetDatabase.SaveAssets();
        EditorSceneManager.SaveScene(scene);
        Debug.Log("ACT2_FLOOR_UPDATED: Ground uses Floor terracotta atlas; camera height 1.8.");
    }

    private static Mesh BuildMesh(Vector3 scale)
    {
        var vertices = new List<Vector3>();
        var uvs = new List<Vector2>();
        var triangles = new List<int>();
        float width = Mathf.Abs(scale.x);
        float depth = Mathf.Abs(scale.z);
        if (width < 0.001f || depth < 0.001f)
            throw new InvalidOperationException("Ground must have a nonzero width and depth.");

        for (float x = 0; x < width; x += PatchWidth)
        {
            for (float z = 0; z < depth; z += PatchDepth)
            {
                float w = Mathf.Min(PatchWidth, width - x);
                float d = Mathf.Min(PatchDepth, depth - z);
                var uv = new Rect(TileUV.x, TileUV.y,
                    TileUV.width * w / PatchWidth, TileUV.height * d / PatchDepth);
                AddQuad(vertices, uvs, triangles,
                    new Vector3(x / width - 0.5f, 0.5f, z / depth - 0.5f),
                    new Vector3(x / width - 0.5f, 0.5f, (z + d) / depth - 0.5f),
                    new Vector3((x + w) / width - 0.5f, 0.5f, (z + d) / depth - 0.5f),
                    new Vector3((x + w) / width - 0.5f, 0.5f, z / depth - 0.5f), uv);
            }
        }

        // Preserve the original cube's thickness and closed underside. The BoxCollider is unchanged.
        var a = new Vector3(-0.5f, -0.5f, -0.5f);
        var b = new Vector3(-0.5f, -0.5f, 0.5f);
        var c = new Vector3(0.5f, -0.5f, 0.5f);
        var d0 = new Vector3(0.5f, -0.5f, -0.5f);
        AddQuad(vertices, uvs, triangles, a, d0, c, b, TileUV);
        AddQuad(vertices, uvs, triangles, a, b, b + Vector3.up, a + Vector3.up, TileUV);
        AddQuad(vertices, uvs, triangles, b, c, c + Vector3.up, b + Vector3.up, TileUV);
        AddQuad(vertices, uvs, triangles, c, d0, d0 + Vector3.up, c + Vector3.up, TileUV);
        AddQuad(vertices, uvs, triangles, d0, a, a + Vector3.up, d0 + Vector3.up, TileUV);

        var mesh = new Mesh { name = "GroundTerracotta" };
        mesh.SetVertices(vertices);
        mesh.SetUVs(0, uvs);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateTangents();
        mesh.RecalculateBounds();
        return mesh;
    }

    private static void AddQuad(List<Vector3> vertices, List<Vector2> uvs, List<int> triangles,
        Vector3 a, Vector3 b, Vector3 c, Vector3 d, Rect uv)
    {
        int start = vertices.Count;
        vertices.AddRange(new[] { a, b, c, d });
        uvs.AddRange(new[] { new Vector2(uv.xMin, uv.yMin), new Vector2(uv.xMin, uv.yMax),
            new Vector2(uv.xMax, uv.yMax), new Vector2(uv.xMax, uv.yMin) });
        triangles.AddRange(new[] { start, start + 1, start + 2, start, start + 2, start + 3 });
    }
}
