using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class Scene2TexturedDecorSetup
{
    const string ScenePath = "Assets/_Project/Scenes/Act2_MemoryWorld_Home.unity";
    const string ArtRoot = "Assets/_Project/Art/Scene2/BedroomDecor";

    // Batch entry point. Reuses the identical FBX assets already imported in the project.
    [MenuItem("KuTy/Scene 2/Add textured bedroom decor")]
    public static void Apply()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Exit Play Mode first.");
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (scene.path != ScenePath)
        {
            if (scene.isDirty) throw new InvalidOperationException("Save the current scene before switching to Scene 2.");
            scene = EditorSceneManager.OpenScene(ScenePath);
        }
        var bedroom = GameObject.Find("BedroomArea");
        if (!bedroom) throw new InvalidOperationException("BedroomArea is missing.");
        var existing = bedroom.transform.Find("Decor_Textured");
        if (existing && existing.childCount != 0)
            throw new InvalidOperationException("Decor_Textured already exists; inspect it before adding duplicates.");
        var bed = GameObject.Find("BedroomArea/RusticBedroom_Visual/BedBlock");
        var anchor = bed ? bed.transform.position : bedroom.transform.position;
        var holder = existing ? existing.gameObject : new GameObject("Decor_Textured");
        holder.transform.SetParent(bedroom.transform, false);
        string[] models = { "tranh", "tivi", "tudocanhan" };
        string[] names = { "tranhtuong", "ke_tivi", "ruongdo" };
        for (int i = 0; i < models.Length; i++)
        {
            string modelPath = "Assets/_Project/Art/Scene2/Models/" + models[i] + ".fbx";
            string folder = ArtRoot + "/" + names[i];
            Directory.CreateDirectory(folder + "/Textures");
            Directory.CreateDirectory(folder + "/Materials");
            AssetDatabase.Refresh();
            var importer = (ModelImporter)AssetImporter.GetAtPath(modelPath);
            importer.ExtractTextures(folder + "/Textures");
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            var textures = AssetDatabase.FindAssets("t:Texture2D", new[] { folder + "/Textures" })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<Texture2D>).ToArray();
            var colors = textures.Where(t => {
                string n = t.name.ToLowerInvariant();
                return n.Contains("basecolor") || n.Contains("base_color") || n.Contains("albedo") || n.Contains("diffuse");
            }).ToArray();
            if (colors.Length == 0)
                colors = textures.Where(t => {
                    string n = t.name.ToLowerInvariant();
                    return !n.Contains("normal") && !n.Contains("metallic") && !n.Contains("roughness") && !n.Contains("occlusion") && !n.Contains("height");
                }).ToArray();
            if (colors.Length == 0) throw new InvalidOperationException("No color texture extracted for " + names[i]);
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(model, holder.transform);
            instance.name = names[i];
            foreach (var renderer in instance.GetComponentsInChildren<Renderer>(true))
            {
                var materials = renderer.sharedMaterials;
                for (int m = 0; m < materials.Length; m++)
                {
                    var source = materials[m];
                    var color = source && source.mainTexture
                        ? colors.FirstOrDefault(t => t.name == source.mainTexture.name) : null;
                    if (!color && colors.Length == 1) color = colors[0];
                    if (!color) throw new InvalidOperationException("Cannot match texture for " + renderer.name);
                    string materialPath = folder + "/Materials/" + color.name + ".mat";
                    var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
                    if (!material)
                    {
                        var shader = Shader.Find("Universal Render Pipeline/Lit");
                        if (!shader) throw new InvalidOperationException("URP Lit shader missing.");
                        material = new Material(shader);
                        material.SetTexture("_BaseMap", color);
                        material.SetColor("_BaseColor", Color.white);
                        material.SetFloat("_Smoothness", .2f);
                        AssetDatabase.CreateAsset(material, materialPath);
                    }
                    materials[m] = material;
                }
                renderer.sharedMaterials = materials;
            }
            // Keep the new props together beside the bedroom for easy manual placement.
            instance.transform.rotation = Quaternion.Euler(0, 90, 0);
            var bounds = BoundsOf(instance);
            float targetSize = i == 0 ? 1.0f : 1.5f;
            instance.transform.localScale *= targetSize / Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
            bounds = BoundsOf(instance);
            var target = new Vector3(anchor.x - 5f, 0f, anchor.z + (i - 1) * 2f);
            instance.transform.position += new Vector3(target.x - bounds.center.x,
                target.y - bounds.min.y, target.z - bounds.center.z);
            Debug.Log("SCENE2_DECOR " + names[i] + " textures=" + textures.Length + " color=" + colors[0].name + " bounds=" + BoundsOf(instance));
        }
        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Scene save failed.");
        // Reload the saved scene to validate material/texture references on disk.
        EditorSceneManager.OpenScene(ScenePath);
        var saved = GameObject.Find("BedroomArea/Decor_Textured");
        if (!saved || saved.transform.childCount != 3) throw new InvalidOperationException("Saved decor group incomplete.");
        foreach (var r in saved.GetComponentsInChildren<Renderer>(true))
            foreach (var m in r.sharedMaterials)
                if (!m || !m.GetTexture("_BaseMap")) throw new InvalidOperationException("Missing saved color map: " + r.name);
        Debug.Log("SCENE2_DECOR_SUCCESS: saved and reloaded 3 textured props.");
    }

    static Bounds BoundsOf(GameObject root)
    {
        var rs = root.GetComponentsInChildren<Renderer>(true);
        if (rs.Length == 0) throw new InvalidOperationException("No renderers on " + root.name);
        var b = rs[0].bounds;
        foreach (var r in rs) b.Encapsulate(r.bounds);
        return b;
    }
}
