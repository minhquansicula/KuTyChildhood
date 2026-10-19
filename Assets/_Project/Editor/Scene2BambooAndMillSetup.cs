using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class Scene2BambooAndMillSetup
{
    const string Root = "Assets/_Project/Art/Scene2";
    static readonly string[] Names = { "BambooTable", "GrainMill" };

    [MenuItem("KuTy/Scene 2/Add bamboo table and grain mill")]
    public static void Add()
    {
        var scene = SceneManager.GetActiveScene();
        if (EditorApplication.isPlayingOrWillChangePlaymode || scene.path != "Assets/_Project/Scenes/Act2_MemoryWorld_Home.unity")
            throw new InvalidOperationException("Open Scene 2 in Edit Mode first.");
        var yard = GameObject.Find("HouseLayout/YardProps");
        var grain = GameObject.Find("HouseLayout/YardProps/GrainPile");
        if (!yard || !grain) throw new InvalidOperationException("YardProps or GrainPile is missing.");
        foreach (var name in Names)
            if (yard.transform.Find(name)) throw new InvalidOperationException(name + " already exists.");
        // Match the current grain storage position, including the user's placement edits.
        var grainBounds = BoundsOf(grain);
        var positions = new[] {
            new Vector3(grainBounds.center.x + 2.8f, grainBounds.min.y, grainBounds.center.z - 2.2f),
            new Vector3(grainBounds.center.x, grainBounds.min.y, grainBounds.center.z - 1.4f)
        };
        for (int i = 0; i < Names.Length; i++)
        {
            string name = Names[i];
            string modelPath = Root + "/Models/Yard/SM_" + name + ".fbx";
            string textureFolder = Root + "/Textures/Yard/" + name;
            string matPath = Root + "/Materials/Yard/MAT_" + name + ".mat";
            Directory.CreateDirectory(textureFolder);
            AssetDatabase.Refresh();
            var importer = (ModelImporter)AssetImporter.GetAtPath(modelPath);
            importer.ExtractTextures(textureFolder);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            var textures = AssetDatabase.FindAssets("t:Texture2D", new[] { textureFolder }).Select(AssetDatabase.GUIDToAssetPath).ToArray();
            var colors = textures.Where(p => {
                string n = Path.GetFileNameWithoutExtension(p).ToLowerInvariant();
                return !n.Contains("normal") && !n.Contains("metallic") && !n.Contains("roughness") && !n.Contains("occlusion") && !n.Contains("height");
            }).ToArray();
            if (colors.Length != 1) throw new InvalidOperationException("Could not identify the original color atlas for " + name);
            var material = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (!material)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(material, matPath);
            }
            material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(colors[0]));
            material.SetColor("_BaseColor", Color.white);
            material.SetFloat("_Smoothness", .15f);
            string normal = textures.FirstOrDefault(p => p.ToLowerInvariant().Contains("normal"));
            if (normal != null)
            {
                var ti = (TextureImporter)AssetImporter.GetAtPath(normal);
                if (ti.textureType != TextureImporterType.NormalMap) { ti.textureType = TextureImporterType.NormalMap; ti.SaveAndReimport(); }
                material.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>(normal));
                material.EnableKeyword("_NORMALMAP");
            }
            EditorUtility.SetDirty(material);
            foreach (var embedded in AssetDatabase.LoadAllAssetsAtPath(modelPath).OfType<Material>())
                importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), embedded.name), material);
            importer.SaveAndReimport();
            var go = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(modelPath), yard.transform);
            Undo.RegisterCreatedObjectUndo(go, "Add " + name);
            go.name = name;
            go.transform.rotation = Quaternion.identity;
            var bounds = BoundsOf(go);
            go.transform.localScale *= (i == 0 ? 2.1f : 1.15f) / Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
            bounds = BoundsOf(go);
            go.transform.position += positions[i] - new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
            foreach (var renderer in go.GetComponentsInChildren<Renderer>())
                renderer.sharedMaterials = renderer.sharedMaterials.Select(_ => material).ToArray();
            bounds = BoundsOf(go);
            var box = Undo.AddComponent<BoxCollider>(go);
            box.center = go.transform.InverseTransformPoint(bounds.center);
            box.size = new Vector3(bounds.size.x / go.transform.lossyScale.x, bounds.size.y / go.transform.lossyScale.y, bounds.size.z / go.transform.lossyScale.z);
            PrefabUtility.RecordPrefabInstancePropertyModifications(go.transform);
            Debug.Log("BAMBOO_MILL_ADDED " + name + " bounds=" + bounds + " color=" + colors[0]);
        }
        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Scene save failed.");
        View();
        Debug.Log("BAMBOO_MILL_SAVED: two textured props added to HouseLayout/YardProps.");
    }

    [MenuItem("KuTy/Scene 2/View bamboo table and grain mill")]
    public static void View()
    {
        var table = GameObject.Find("HouseLayout/YardProps/BambooTable");
        var mill = GameObject.Find("HouseLayout/YardProps/GrainMill");
        if (!table || !mill || !SceneView.lastActiveSceneView) return;
        var b = BoundsOf(table);
        b.Encapsulate(BoundsOf(mill));
        var from = b.center + new Vector3(1.5f, 2.8f, -5.5f);
        SceneView.lastActiveSceneView.LookAt(b.center, Quaternion.LookRotation(b.center - from), 3.3f, false, true);
    }
    [MenuItem("KuTy/Scene 2/Align bamboo and mill inside courtyard")]
    public static void AlignInside()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || SceneManager.GetActiveScene().path != "Assets/_Project/Scenes/Act2_MemoryWorld_Home.unity")
            throw new InvalidOperationException("Open Scene 2 in Edit Mode first.");
        var grain = BoundsOf(GameObject.Find("HouseLayout/YardProps/GrainPile"));
        for (int i = 0; i < Names.Length; i++)
        {
            var go = GameObject.Find("HouseLayout/YardProps/" + Names[i]);
            var b = BoundsOf(go);
            var target = new Vector3(grain.center.x + (i == 0 ? 2.8f : 0f), grain.min.y, grain.center.z - (i == 0 ? 2.2f : 1.4f));
            Undo.RecordObject(go.transform, "Align " + Names[i]);
            go.transform.position += target - new Vector3(b.center.x, b.min.y, b.center.z);
            PrefabUtility.RecordPrefabInstancePropertyModifications(go.transform);
        }
        var scene = SceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        View();
    }
    static Bounds BoundsOf(GameObject root)
    {
        var rs = root.GetComponentsInChildren<Renderer>(true);
        if (rs.Length == 0) throw new InvalidOperationException("No renderers: " + root.name);
        var b = rs[0].bounds;
        foreach (var r in rs) b.Encapsulate(r.bounds);
        return b;
    }
}
