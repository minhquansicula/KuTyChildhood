using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Persist explicit texture references instead of FBX filename-based texture discovery.</summary>
public static class Scene2TextureRecovery
{
    const string Root = "Assets/_Project/Art/Scene2";
    const string House = Root + "/Models/Architecture/SM_TraditionalHouseInterior.fbx";
    const string Bedroom = Root + "/Models/Bedroom/SM_RusticBedroom.fbx";

    [MenuItem("KuTy/Scene 2/Restore original house textures")]
    public static void Restore()
    {
        var scene = SceneManager.GetActiveScene();
        if (EditorApplication.isPlayingOrWillChangePlaymode || scene.name != "Act2_MemoryWorld_Home")
            throw new InvalidOperationException("Open Scene 2 in Edit Mode first.");
        var backup = Path.Combine(".unity-validation", "scene2-texture-recovery-" + DateTime.Now.ToString("yyyyMMdd-HHmmss"));
        Directory.CreateDirectory(backup);
        File.Copy(scene.path, Path.Combine(backup, "Before.unity"));
        File.Copy(House + ".meta", Path.Combine(backup, "House.fbx.meta"));
        File.Copy(Bedroom + ".meta", Path.Combine(backup, "Bedroom.fbx.meta"));

        var report = new StringBuilder();
        var replacements = new Dictionary<Material, Material>();
        var slotChanges = new Dictionary<Renderer, Material[]>();
        RestoreModel(House, "Architecture", false, replacements, report);
        RestoreModel(Bedroom, "Bedroom", true, replacements, report);

        // Capture explicit scene overrides before reimport replaces embedded subassets.
        foreach (var renderer in scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Renderer>(true)))
        {
            var materials = renderer.sharedMaterials;
            bool changed = false;
            for (int i = 0; i < materials.Length; i++)
                if (materials[i] && replacements.TryGetValue(materials[i], out var replacement))
                { materials[i] = replacement; changed = true; }
            if (changed) slotChanges.Add(renderer, materials);
        }
        AssetDatabase.SaveAssets();
        foreach (var model in new[] { House, Bedroom })
            ((ModelImporter)AssetImporter.GetAtPath(model)).SaveAndReimport();
        foreach (var pair in slotChanges)
        {
            Undo.RecordObject(pair.Key, "Restore original Scene 2 textures");
            pair.Key.sharedMaterials = pair.Value;
            PrefabUtility.RecordPrefabInstancePropertyModifications(pair.Key);
            EditorUtility.SetDirty(pair.Key);
        }
        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Scene save failed.");
        File.WriteAllText(Path.Combine(backup, "restored-materials.txt"), report.ToString());
        Verify();
        Debug.Log("SCENE2_TEXTURE_RECOVERY_SUCCESS materials=" + replacements.Count + " renderers=" + slotChanges.Count + " backup=" + backup);
    }

    static void RestoreModel(string modelPath, string category, bool bedroom,
        Dictionary<Material, Material> replacements, StringBuilder report)
    {
        var model = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
        var importer = (ModelImporter)AssetImporter.GetAtPath(modelPath);
        if (!model || importer == null) throw new InvalidOperationException("Missing model: " + modelPath);
        var bindings = new Dictionary<Material, string>();
        foreach (var renderer in model.GetComponentsInChildren<Renderer>(true))
        foreach (var material in renderer.sharedMaterials)
        {
            if (!material) throw new InvalidOperationException("Missing material on " + renderer.name);
            string suffix = bedroom ? "RusticBedroom" : "TraditionalHouse_" + TexturePart(renderer.name);
            string texturePath = Root + "/Textures/" + category + "/T_" + suffix + "_BaseColor.jpeg";
            if (!AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath))
                throw new InvalidOperationException("Cannot match original texture: " + renderer.name + " -> " + texturePath);
            if (bindings.TryGetValue(material, out var prior) && prior != suffix)
                throw new InvalidOperationException("Ambiguous source material: " + material.name);
            bindings[material] = suffix;
        }
        foreach (var pair in bindings)
        {
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(Root + "/Textures/" + category + "/T_" + pair.Value + "_BaseColor.jpeg");
            string materialPath = Root + "/Materials/" + category + "/MAT_" + pair.Value + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (!material)
            {
                material = new Material(pair.Key) { name = "MAT_" + pair.Value };
                AssetDatabase.CreateAsset(material, materialPath);
            }
            Undo.RecordObject(material, "Restore original texture");
            material.SetTexture("_BaseMap", texture);
            if (material.HasProperty("_MainTex")) material.SetTexture("_MainTex", texture);
            EditorUtility.SetDirty(material);
            // Existing remaps already have a material asset path; retain their source identifier.
            var existing = importer.GetExternalObjectMap().Where(x => x.Value == pair.Key).Select(x => x.Key).ToArray();
            if (existing.Length > 0)
                foreach (var id in existing) importer.AddRemap(id, material);
            else importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), pair.Key.name), material);
            replacements[pair.Key] = material;
            report.AppendLine(pair.Key.name + " -> " + materialPath + " -> " + AssetDatabase.GetAssetPath(texture));
        }
    }

    static string TexturePart(string sourceNode)
    {
        switch (sourceNode)
        {
            case "Bàn thờ": return "Altar";
            case "Tủ đồ": return "Cabinet";
            case "Máy chơi nhạc": return "MusicPlayer";
            case "Chair": return "Chair";
            case "Floor": return "Floor";
        }
        var part = Regex.Match(sourceNode, @"^tripo_part_(\d+)$");
        if (!part.Success) throw new InvalidOperationException("Unrecognized house source node: " + sourceNode);
        return "Part" + part.Groups[1].Value;
    }

    [MenuItem("KuTy/Scene 2/Verify restored house textures")]
    public static void Verify()
    {
        int count = 0;
        foreach (var modelPath in new[] { House, Bedroom })
        foreach (var renderer in AssetDatabase.LoadAssetAtPath<GameObject>(modelPath).GetComponentsInChildren<Renderer>(true))
        foreach (var material in renderer.sharedMaterials)
        {
            if (!material || !material.GetTexture("_BaseMap") || !AssetDatabase.GetAssetPath(material).EndsWith(".mat"))
                throw new InvalidOperationException("Model texture is not persistent: " + renderer.name);
            count++;
        }
        foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
        foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
        foreach (var material in renderer.sharedMaterials)
            if (material && (material.name.StartsWith("MAT_TraditionalHouse_") || material.name == "MAT_RusticBedroom")
                && !material.GetTexture("_BaseMap"))
                throw new InvalidOperationException("Scene texture is missing: " + renderer.name);
        Debug.Log("SCENE2_TEXTURE_VERIFY_SUCCESS modelMaterialSlots=" + count);
        SceneView.RepaintAll();
    }

    [MenuItem("KuTy/Scene 2/Capture restored texture preview")]
    public static void Capture()
    {
        var camera = SceneView.lastActiveSceneView ? SceneView.lastActiveSceneView.camera : Camera.main;
        if (!camera) throw new InvalidOperationException("No preview camera.");
        var oldTarget = camera.targetTexture;
        var oldActive = RenderTexture.active;
        var target = RenderTexture.GetTemporary(1280, 720, 24);
        var image = new Texture2D(1280, 720, TextureFormat.RGB24, false);
        try
        {
            camera.targetTexture = target;
            camera.Render();
            RenderTexture.active = target;
            image.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
            image.Apply();
            File.WriteAllBytes("Temp/scene2-restored-textures.png", image.EncodeToPNG());
        }
        finally
        {
            camera.targetTexture = oldTarget;
            RenderTexture.active = oldActive;
            RenderTexture.ReleaseTemporary(target);
            UnityEngine.Object.DestroyImmediate(image);
        }
    }
}
