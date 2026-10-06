using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Plants the user's 5,986-triangle rice model across the eight existing fields.</summary>
public static class Scene3LightRicePlanting
{
    private const string ScenePath = "Assets/_Project/Scenes/Act3_MemoryWorld_OutSide.unity";
    private const string ModelPath = "Assets/_Project/Art/Models/Scene3/CayLuaNhe_Cum3x3.fbx";
    private const string PrefabPath = "Assets/_Project/Prefabs/Scene3/CayLuaNhe_Cum3x3.prefab";
    private const string MaterialsPath = "Assets/_Project/Art/Materials/Scene3/CayLuaNhe";

    [MenuItem("KuTy/Scene 3/Plant new light rice across fields")]
    public static void Plant()
    {
        var scene = SceneManager.GetActiveScene();
        if (scene.path != ScenePath) throw new Exception("Open scene 3 before planting rice.");
        var fields = Find(scene, "RuongLua_Moi");
        if (!fields) throw new Exception("New village rice fields were not found.");
        var prefab = CreatePrefab(scene);
        var total = 0;
        foreach (Transform field in fields.transform)
        {
            foreach (Transform child in field)
                if (child.name.StartsWith("CayLuaMoi_") || child.name.StartsWith("CayLuaGoc_"))
                    child.gameObject.SetActive(false);

            var parts = field.name.Split('_');
            if (parts.Length < 3 || !int.TryParse(parts[parts.Length - 2], out var side) ||
                !int.TryParse(parts[parts.Length - 1], out var fieldZ))
                throw new Exception("Unexpected rice field name: " + field.name);

            var waterAreas = new List<Bounds>();
            foreach (Transform child in field)
                if (child.name == "MatNuoc" && child.TryGetComponent<MeshRenderer>(out var renderer))
                    waterAreas.Add(renderer.bounds);
            if (waterAreas.Count == 0) throw new Exception("No water area in " + field.name);

            var centerX = side * 23f;
            var random = new System.Random(3109 + (side + 1) * 71 + fieldZ * 13);
            var planted = 0;
            for (var row = 0; row < 5; row++)
            for (var col = 0; col < 10; col++)
            {
                var x = centerX - 15f + col * (30f / 9f);
                var z = fieldZ - 7f + row * 3.5f;
                var insideWater = false;
                foreach (var bounds in waterAreas)
                    if (x >= bounds.min.x + 1f && x <= bounds.max.x - 1f &&
                        z >= bounds.min.z + 1f && z <= bounds.max.z - 1f)
                        insideWater = true;
                if (!insideWater) continue;

                var name = $"CayLuaNhe_{row:00}_{col:00}";
                if (field.Find(name)) { planted++; continue; }

                var clump = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
                clump.name = name;
                clump.transform.SetParent(field, true);
                clump.transform.position = new Vector3(x + (float)(random.NextDouble() - .5) * .17f,
                                                        0f, z + (float)(random.NextDouble() - .5) * .17f);
                clump.transform.rotation = Quaternion.Euler(0, random.Next(-15, 16), 0);
                var scale = .94f + (float)random.NextDouble() * .12f;
                clump.transform.localScale = Vector3.one * scale;
                planted++;
            }
            total += planted;
            Debug.Log($"[LightRice] {field.name}: {planted} nine-plant clumps");
        }

        EditorSceneManager.MarkSceneDirty(scene);
        AssetDatabase.SaveAssets();
        if (!EditorSceneManager.SaveScene(scene)) throw new Exception("Could not save scene 3.");
        Debug.Log($"[LightRice] Planted {total} clumps ({total * 9} individual rice plants) from CayLuaNhe_Cum3x3.fbx.");
    }

    private static GameObject CreatePrefab(Scene scene)
    {
        AssetDatabase.ImportAsset(ModelPath, ImportAssetOptions.ForceSynchronousImport);
        var model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
        if (!model) throw new Exception("Light rice FBX has not imported: " + ModelPath);
        if (!AssetDatabase.IsValidFolder(MaterialsPath))
            AssetDatabase.CreateFolder("Assets/_Project/Art/Materials/Scene3", "CayLuaNhe");

        var stage = new GameObject("CayLuaNhe_Cum3x3");
        SceneManager.MoveGameObjectToScene(stage, scene);
        try
        {
            var visual = (GameObject)PrefabUtility.InstantiatePrefab(model, scene);
            visual.name = "Visual_9CayLua";
            visual.transform.SetParent(stage.transform, false);
            visual.transform.localPosition = Vector3.zero;
            // Blender FBX keeps the plant's upright axis on local Z in this import.
            // Rotate the visual once so that the root and its ground pivot stay on Unity Y.
            visual.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
            var renderers = visual.GetComponentsInChildren<MeshRenderer>(true);
            if (renderers.Length == 0) throw new Exception("Light rice model has no renderer.");
            foreach (var renderer in renderers)
            {
                var slots = renderer.sharedMaterials;
                for (var i = 0; i < slots.Length; i++)
                    slots[i] = MaterialFor(slots[i] ? slots[i].name : "", i);
                renderer.sharedMaterials = slots;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }

            var bounds = renderers[0].bounds;
            foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
            if (bounds.size.y < .01f) throw new Exception("Light rice model has invalid bounds.");
            var sizeCorrection = 2f / bounds.size.y;
            visual.transform.localScale *= sizeCorrection;
            bounds = renderers[0].bounds;
            foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
            visual.transform.position -= new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
            Debug.Log($"[LightRice] Imported cluster bounds after scale: {bounds.size.ToString("F2")}");
            var prefab = PrefabUtility.SaveAsPrefabAsset(stage, PrefabPath);
            if (!prefab) throw new Exception("Could not save light rice prefab.");
            return prefab;
        }
        finally { UnityEngine.Object.DestroyImmediate(stage); }
    }

    private static Material MaterialFor(string sourceName, int index)
    {
        var label = sourceName.ToLowerInvariant();
        string name;
        Color color;
        if (label.Contains("than")) { name = "ThanLua"; color = new Color(.28f,.34f,.055f); }
        else if (label.Contains("sam")) { name = "LaLuaSam"; color = new Color(.36f,.34f,.075f); }
        else if (label.Contains("la_lua")) { name = "LaLua"; color = new Color(.45f,.40f,.085f); }
        else if (label.Contains("sang")) { name = "HatLuaSang"; color = new Color(.78f,.50f,.07f); }
        else if (label.Contains("bong") || label.Contains("goldenrod"))
            { name = "BongLua"; color = new Color(.7083f,.3838f,.0104f); }
        else
        {
            var fallback = new[] { "ThanLua", "LaLua", "LaLuaSam", "BongLua", "HatLuaSang" };
            var colors = new[] { new Color(.28f,.34f,.055f), new Color(.45f,.40f,.085f),
                                 new Color(.36f,.34f,.075f), new Color(.7083f,.3838f,.0104f),
                                 new Color(.78f,.50f,.07f) };
            name = fallback[Mathf.Clamp(index, 0, 4)];
            color = colors[Mathf.Clamp(index, 0, 4)];
        }
        var path = $"{MaterialsPath}/{name}.mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (!material)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (!shader) throw new Exception("URP Lit shader is missing.");
            material = new Material(shader) { name = name };
            AssetDatabase.CreateAsset(material, path);
        }
        material.SetColor("_BaseColor", color);
        material.SetFloat("_Smoothness", .14f);
        material.enableInstancing = true;
        EditorUtility.SetDirty(material);
        return material;
    }

    private static GameObject Find(Scene scene, string name)
    {
        foreach (var root in scene.GetRootGameObjects())
        foreach (var transform in root.GetComponentsInChildren<Transform>(true))
            if (transform.name == name) return transform.gameObject;
        return null;
    }
}
