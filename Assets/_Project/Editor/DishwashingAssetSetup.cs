using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Imports the delivered exports only. Never opens or modifies the Blender source.</summary>
public static class DishwashingAssetSetup
{
    public const string Root = "Assets/_Project/Art/Scene2/Dishwashing";
    public const string DataPath = Root + "/DishwashingAssets.asset";
    private const string Source = "Tools/SimulatorAssets/Dishwashing_Set/Export";
    private static readonly string[] Names = { "DishBowl", "DishPlate", "WashBasin", "ScrubSponge", "SoapBar", "DryingRack" };
    private static readonly string[] Materials = { "CeramicBody", "BlueRim", "BasinAluminum", "SpongeFoam", "SpongeScrub", "SoapIvory", "BambooWood" };

    [MenuItem("KuTy/Scene 2/Import Blender dishwashing set")]
    public static void Install()
    {
        var scene = SceneManager.GetActiveScene();
        if (EditorApplication.isPlayingOrWillChangePlaymode || scene.path != "Assets/_Project/Scenes/Act2_MemoryWorld_Home.unity" || scene.isDirty)
            throw new InvalidOperationException("Open the saved Home scene in Edit Mode first. Unsaved work will not be discarded.");
        var simulator = UnityEngine.Object.FindObjectOfType<Scene2HouseholdSimulator>();
        if (!simulator) throw new InvalidOperationException("Household simulator missing.");
        foreach (string folder in new[] { "Models", "Textures", "Materials", "Prefabs" }) Directory.CreateDirectory(Root + "/" + folder);
        // Configure texture types before model/material import can inspect normal slots.
        foreach (string file in Directory.GetFiles(Source + "/Textures", "*.png")) CopyExport(file, Root + "/Textures/" + Path.GetFileName(file));
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        foreach (string file in Directory.GetFiles(Root + "/Textures", "*.png"))
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(file.Replace('\\', '/'));
            importer.textureType = file.Contains("_Normal") ? TextureImporterType.NormalMap : TextureImporterType.Default;
            importer.sRGBTexture = file.Contains("_BaseColor"); importer.maxTextureSize = 1024;
            importer.mipmapEnabled = true; importer.wrapMode = TextureWrapMode.Repeat;
            importer.SaveAndReimport();
        }
        foreach (string name in Names) CopyExport(Source + "/FBX/" + name + ".fbx", Root + "/Models/" + name + ".fbx");
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        foreach (string name in Names)
        {
            var importer = (ModelImporter)AssetImporter.GetAtPath(Root + "/Models/" + name + ".fbx");
            importer.globalScale = 1; importer.useFileScale = true; importer.bakeAxisConversion = true;
            importer.importAnimation = false; importer.importCameras = false; importer.importLights = false;
            importer.addCollider = false; importer.materialImportMode = ModelImporterMaterialImportMode.None;
            importer.SaveAndReimport();
        }
        var shader = Shader.Find("Universal Render Pipeline/Lit");
        if (!shader) throw new InvalidOperationException("URP/Lit shader missing.");
        foreach (string name in Materials)
        {
            string path = Root + "/Materials/" + name + ".mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (!mat) { mat = new Material(shader); AssetDatabase.CreateAsset(mat, path); }
            mat.shader = shader; mat.color = Color.white;
            mat.SetTexture("_BaseMap", Texture(name + "_BaseColor"));
            mat.SetTexture("_BumpMap", Texture(name == "BasinAluminum" ? name + "_Normal_v02" : name + "_Normal"));
            mat.SetFloat("_BumpScale", .7f); mat.EnableKeyword("_NORMALMAP");
            // URP Lit reads metallic from R and smoothness from A (not a roughness RGB map).
            PackMetallicSmoothness(name, name == "BasinAluminum" ? 1f : 0f);
            mat.SetTexture("_MetallicGlossMap", Texture(name + "_MetallicSmoothness"));
            mat.SetFloat("_Smoothness", 1f); mat.SetFloat("_Metallic", name == "BasinAluminum" ? 1 : 0);
            mat.SetFloat("_SmoothnessTextureChannel", 0); mat.EnableKeyword("_METALLICSPECGLOSSMAP");
            mat.DisableKeyword("_SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A");
            EditorUtility.SetDirty(mat);
        }
        var prefabs = Names.Select(BuildPrefab).ToArray();
        var data = AssetDatabase.LoadAssetAtPath<DishwashingAssets>(DataPath);
        if (!data) { data = ScriptableObject.CreateInstance<DishwashingAssets>(); AssetDatabase.CreateAsset(data, DataPath); }
        data.bowl = prefabs[0]; data.plate = prefabs[1]; data.basin = prefabs[2];
        data.sponge = prefabs[3]; data.soap = prefabs[4]; data.rack = prefabs[5];
        MeasureBowl(data);
        EditorUtility.SetDirty(data);
        Undo.RecordObject(simulator, "Bind Blender dishwashing assets");
        simulator.ConfigureDishwashing(data); EditorUtility.SetDirty(simulator);
        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        Directory.CreateDirectory("Tools/ProjectAudit/Simulator");
        var report = new StringBuilder("Dishwashing Unity import validation\n");
        report.AppendLine("Bowl bounds (metres): " + data.bowlSize.ToString("F4"));
        foreach (var prefab in prefabs)
        {
            var renderers = prefab.GetComponentsInChildren<MeshRenderer>(true);
            report.AppendLine(prefab.name + ": " + renderers.Length + " visible renderer(s), " + prefab.GetComponentsInChildren<Collider>(true).Length + " physics colliders; " +
                prefab.GetComponentsInChildren<Transform>(true).Count(t => t.name.StartsWith("COL_")) + " proxies");
        }
        for (int i = 0; i < 6; i++) report.AppendLine("Contact " + i + ": " + data.stainPoints[i].ToString("F4") + " normal " + data.stainNormals[i].ToString("F3"));
        File.WriteAllText("Tools/ProjectAudit/Simulator/dishwashing-import.txt", report.ToString());
        Debug.Log("[Dishwashing] Imported six isolated prefabs, seven URP materials, measured surface contacts; bound to Home.\n" + report);
    }

    private static void CopyExport(string source, string target)
    {
        if (!File.Exists(source)) throw new FileNotFoundException("Missing delivered export", source);
        if (!File.Exists(target)) File.Copy(source, target);
        else if (!File.ReadAllBytes(source).SequenceEqual(File.ReadAllBytes(target)))
            throw new InvalidOperationException("Refusing to overwrite an independently modified import: " + target);
    }
    private static Texture2D Texture(string name) => AssetDatabase.LoadAssetAtPath<Texture2D>(Root + "/Textures/" + name + ".png");
    private static void PackMetallicSmoothness(string name, float metallic)
    {
        // Decode source bytes into a linear texture: no sRGB conversion and no compressed readback.
        var roughness = new Texture2D(2, 2, TextureFormat.RGBA32, false, true);
        var packed = new Texture2D(2, 2, TextureFormat.RGBA32, false, true);
        try
        {
            if (!roughness.LoadImage(File.ReadAllBytes(Source + "/Textures/" + name + "_Roughness.png"))) throw new InvalidOperationException("Invalid roughness PNG");
            packed.Reinitialize(roughness.width, roughness.height);
            var pixels = roughness.GetPixels32();
            for (int i = 0; i < pixels.Length; i++) pixels[i] = new Color32((byte)(metallic * 255), 0, 0, (byte)(255 - pixels[i].r));
            packed.SetPixels32(pixels); packed.Apply();
            string path = Root + "/Textures/" + name + "_MetallicSmoothness.png";
            File.WriteAllBytes(path, packed.EncodeToPNG());
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.sRGBTexture = false; importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency = false; importer.maxTextureSize = 1024; importer.mipmapEnabled = true;
            importer.SaveAndReimport();
        }
        finally { UnityEngine.Object.DestroyImmediate(roughness); UnityEngine.Object.DestroyImmediate(packed); }
    }

    private static GameObject BuildPrefab(string name)
    {
        var root = new GameObject(name);
        try
        {
            var model = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Root + "/Models/" + name + ".fbx"), root.transform);
            model.name = "Visual";
            foreach (var t in model.GetComponentsInChildren<Transform>(true).Where(t => t.name.StartsWith("COL_")).ToArray())
                if (t) UnityEngine.Object.DestroyImmediate(t.gameObject);
            foreach (var c in model.GetComponentsInChildren<Collider>(true)) UnityEngine.Object.DestroyImmediate(c);
            // FBX keeps a corrective rotation on the mesh root. Empty axes are still in that
            // source basis: convert them to engine-native axes so prefab.up means physical up.
            Quaternion correction = Quaternion.Inverse(model.transform.rotation);
            foreach (var attachment in model.GetComponentsInChildren<Transform>(true).Where(t => t != model.transform && !t.GetComponent<MeshFilter>()))
                attachment.rotation *= correction;
            foreach (var renderer in model.GetComponentsInChildren<MeshRenderer>(true))
            {
                var mesh = renderer.GetComponent<MeshFilter>().sharedMesh;
                // materialImportMode=None can erase source slot names; FBX renderer slot order is documented in the handoff.
                string[] slots = name == "DishBowl" || name == "DishPlate" ? new[] { "CeramicBody", "BlueRim" } :
                    name == "ScrubSponge" ? new[] { "SpongeFoam", "SpongeScrub" } :
                    new[] { name == "WashBasin" ? "BasinAluminum" : name == "SoapBar" ? "SoapIvory" : "BambooWood" };
                if (mesh.subMeshCount != slots.Length) throw new InvalidOperationException("Unexpected material slots: " + name);
                renderer.sharedMaterials = slots.Select(s => AssetDatabase.LoadAssetAtPath<Material>(Root + "/Materials/" + s + ".mat")).ToArray();
            }
            return PrefabUtility.SaveAsPrefabAsset(root, Root + "/Prefabs/" + name + ".prefab");
        }
        finally { UnityEngine.Object.DestroyImmediate(root); }
    }

    private static void MeasureBowl(DishwashingAssets data)
    {
        var bowl = UnityEngine.Object.Instantiate(data.bowl);
        try
        {
            var renderers = bowl.GetComponentsInChildren<Renderer>();
            Bounds bounds = renderers[0].bounds;
            foreach (var r in renderers.Skip(1)) bounds.Encapsulate(r.bounds);
            data.bowlSize = bounds.size;
            if (Vector3.Distance(bounds.size, new Vector3(.14f, .065f, .14f)) > .003f || Mathf.Abs(bounds.min.y) > .002f)
                throw new InvalidOperationException("FBX dimensions, pivot or orientation mismatch: " + bounds);
            var colliders = bowl.GetComponentsInChildren<MeshFilter>().Select(filter =>
            {
                var collider = filter.gameObject.AddComponent<MeshCollider>(); collider.sharedMesh = filter.sharedMesh; return collider;
            }).ToArray();
            Physics.SyncTransforms();
            for (int side = 0; side < 2; side++) for (int patch = 0; patch < 3; patch++)
            {
                float angle = (patch + .45f) * Mathf.PI * 2 / 3;
                Vector3 radial = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * (side == 0 ? .025f : .018f);
                var ray = new Ray(radial + Vector3.up * (side == 0 ? .2f : -.2f), side == 0 ? Vector3.down : Vector3.up);
                RaycastHit nearest = default; bool found = false;
                foreach (var collider in colliders) if (collider.Raycast(ray, out var hit, 1) && (!found || hit.distance < nearest.distance)) { nearest = hit; found = true; }
                if (!found) throw new InvalidOperationException("No imported bowl surface at patch " + patch);
                int i = side * 3 + patch;
                data.stainPoints[i] = bowl.transform.InverseTransformPoint(nearest.point + nearest.normal * .0008f);
                data.stainNormals[i] = bowl.transform.InverseTransformDirection(nearest.normal);
            }
        }
        finally { UnityEngine.Object.DestroyImmediate(bowl); }
    }
}
