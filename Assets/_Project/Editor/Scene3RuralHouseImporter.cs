using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Turns the five Blender-generated FBX files into coloured URP prefabs with
/// simple physical colliders. No gameplay script or existing scene is changed.
/// </summary>
public static class Scene3RuralHouseImporter
{
    private const string ModelDir = "Assets/_Project/Art/Models/Scene3/RuralHouses";
    private const string MaterialDir = "Assets/_Project/Art/Materials/Scene3/RuralHouses";
    private const string TextureDir = "Assets/_Project/Art/Textures/Scene3/RuralHouses";
    private const string PrefabDir = "Assets/_Project/Prefabs/Scene3/RuralHouses";

    private static readonly string[] Names =
    {
        "Nha01_NgoiDo_HienTon",
        "Nha02_NgoiNau_HienTon",
        "Nha03_NgoiReu_HienTon",
        "Nha04_NgoiDoSam_HienTon",
        "Nha05_NgoiCu_HienTon",
    };

    private static readonly (string name, Color color, float metallic, float smoothness)[] Palette =
    {
        ("Limewash_Cream", new Color(.59f, .53f, .35f), 0, .18f),
        ("Whitewash_Warm", new Color(.69f, .66f, .53f), 0, .18f),
        ("Ochre_Earth", new Color(.51f, .36f, .22f), 0, .18f),
        ("Clay_Wall", new Color(.44f, .31f, .21f), 0, .16f),
        ("Moss_Lime", new Color(.49f, .53f, .38f), 0, .18f),
        ("Roof_Clay_Tile", new Color(.39f, .14f, .09f), 0, .20f),
        ("Roof_Old_Tile", new Color(.24f, .20f, .17f), 0, .18f),
        ("Roof_Mossy_Tile", new Color(.34f, .29f, .18f), 0, .16f),
        ("Roof_Galvanized_Tin", new Color(.31f, .35f, .35f), .59f, .39f),
        ("Roof_Faded_Red_Tin", new Color(.36f, .20f, .17f), .65f, .35f),
        ("Dark_Timber", new Color(.22f, .15f, .10f), 0, .18f),
        ("Bamboo", new Color(.44f, .37f, .20f), 0, .18f),
        ("Old_Brick", new Color(.40f, .22f, .15f), 0, .14f),
        ("Stone_Plaster", new Color(.44f, .43f, .36f), 0, .14f),
        ("Window_Dark", new Color(.10f, .16f, .16f), 0, .72f),
    };

    [MenuItem("KuTy/Scene 3/Build five rural house prefabs")]
    public static void Build()
    {
        EnsureFolder(MaterialDir);
        EnsureFolder(PrefabDir);
        var materials = BuildMaterials();
        var built = 0;

        foreach (var name in Names)
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>($"{ModelDir}/{name}.fbx");
            if (model == null)
            {
                Debug.LogError($"Missing rural house FBX: {name}");
                continue;
            }

            var root = new GameObject(name);
            try
            {
                var visual = (GameObject)PrefabUtility.InstantiatePrefab(model);
                visual.name = "Visual";
                visual.transform.SetParent(root.transform, false);
                var renderers = visual.GetComponentsInChildren<Renderer>(true);
                foreach (var renderer in renderers)
                {
                    var slots = renderer.sharedMaterials;
                    for (var i = 0; i < slots.Length; i++)
                    {
                        if (slots[i] == null) continue;
                        foreach (var pair in materials)
                        {
                            if (!slots[i].name.Contains(pair.Key)) continue;
                            slots[i] = pair.Value;
                            break;
                        }
                    }
                    renderer.sharedMaterials = slots;
                    GameObjectUtility.SetStaticEditorFlags(renderer.gameObject, StaticEditorFlags.BatchingStatic);
                }

                var bounds = CalculateLocalBounds(root.transform, renderers);
                var collider = root.AddComponent<BoxCollider>();
                collider.center = new Vector3(bounds.center.x, bounds.center.y, bounds.center.z + .35f);
                collider.size = new Vector3(bounds.size.x * .83f, bounds.size.y * .78f, bounds.size.z * .66f);
                var path = $"{PrefabDir}/{name}.prefab";
                PrefabUtility.SaveAsPrefabAsset(root, path);
                built++;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[Scene3RuralHouseImporter] Built {built}/5 rural house prefabs in {PrefabDir}");
        if (built != Names.Length) throw new Exception("Rural house prefab build incomplete.");
    }

    private static Dictionary<string, Material> BuildMaterials()
    {
        var result = new Dictionary<string, Material>();
        var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        if (shader == null) throw new Exception("Could not find a compatible lit shader.");

        foreach (var entry in Palette)
        {
            var path = $"{MaterialDir}/{entry.name}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(shader) { name = entry.name };
                AssetDatabase.CreateAsset(material, path);
            }
            material.shader = shader;
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>($"{TextureDir}/{entry.name}_Weathered.png");
            if (texture == null) throw new Exception($"Missing weathered texture for {entry.name}.");
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", Color.white);
            if (material.HasProperty("_Color")) material.SetColor("_Color", Color.white);
            if (material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap", texture);
            if (material.HasProperty("_MainTex")) material.SetTexture("_MainTex", texture);
            material.SetFloat("_Metallic", entry.metallic);
            material.SetFloat("_Smoothness", entry.smoothness);
            EditorUtility.SetDirty(material);
            result.Add(entry.name, material);
        }
        return result;
    }

    private static Bounds CalculateLocalBounds(Transform root, Renderer[] renderers)
    {
        var hasBounds = false;
        var bounds = new Bounds(Vector3.zero, Vector3.zero);
        foreach (var renderer in renderers)
        {
            var world = renderer.bounds;
            for (var x = -1; x <= 1; x += 2)
            for (var y = -1; y <= 1; y += 2)
            for (var z = -1; z <= 1; z += 2)
            {
                var point = root.InverseTransformPoint(world.center + Vector3.Scale(world.extents, new Vector3(x, y, z)));
                if (!hasBounds) { bounds = new Bounds(point, Vector3.zero); hasBounds = true; }
                else bounds.Encapsulate(point);
            }
        }
        return bounds;
    }

    private static void EnsureFolder(string path)
    {
        var parts = path.Split('/');
        var current = parts[0];
        for (var i = 1; i < parts.Length; i++)
        {
            var next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }
}
