using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class WaterRiceAssetSetup
{
    public const string Root = "Assets/_Project/Art/Scene2/WaterRice";
    public const string DataPath = Root + "/WaterRiceAssets.asset";
    private const string Source = "Tools/SimulatorAssets/WaterRice_Set/Export";
    private static readonly string[] Names = { "CoconutLadle", "RiceMeasuringCup", "WaterBucket", "RiceBasket", "LadleWater", "CupRice", "BucketWater", "BasketRice" };
    private static readonly string[][] Slots = {
        new[] { "WR_CoconutShell", "WR_CoconutInterior", "WR_HandleWood" }, new[] { "WR_Aluminum" }, new[] { "WR_BluePlastic" },
        new[] { "WR_BambooWeave", "WR_HandleWood" }, new[] { "WR_Water" }, new[] { "WR_Rice" }, new[] { "WR_Water" }, new[] { "WR_Rice" }
    };
    [MenuItem("KuTy/Scene 2/Import Blender water and rice set")]
    public static void Install()
    {
        var scene = SceneManager.GetActiveScene();
        if (EditorApplication.isPlayingOrWillChangePlaymode || scene.path != "Assets/_Project/Scenes/Act2_MemoryWorld_Home.unity" || scene.isDirty)
            throw new InvalidOperationException("Open the saved Home scene in Edit Mode first. Unsaved work will not be discarded.");
        var simulator = UnityEngine.Object.FindObjectOfType<Scene2HouseholdSimulator>();
        if (!simulator) throw new InvalidOperationException("Simulator missing.");
        foreach (var folder in new[] { "Models", "Textures", "Materials", "Prefabs" }) Directory.CreateDirectory(Root + "/" + folder);
        foreach (var file in Directory.GetFiles(Source + "/Textures", "*.png")) Copy(file, Root + "/Textures/" + Path.GetFileName(file));
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        foreach (var file in Directory.GetFiles(Root + "/Textures", "*.png"))
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(file.Replace('\\', '/'));
            importer.textureType = file.Contains("_Normal") ? TextureImporterType.NormalMap : TextureImporterType.Default;
            importer.sRGBTexture = file.Contains("_BaseColor"); importer.convertToNormalmap = false;
            importer.maxTextureSize = 1024; importer.mipmapEnabled = true; importer.SaveAndReimport();
        }
        foreach (var name in Names) Copy(Source + "/FBX/" + name + ".fbx", Root + "/Models/" + name + ".fbx");
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        foreach (var name in Names)
        {
            var importer = (ModelImporter)AssetImporter.GetAtPath(Root + "/Models/" + name + ".fbx");
            importer.globalScale = 1; importer.useFileScale = true; importer.bakeAxisConversion = true;
            importer.importAnimation = false; importer.importCameras = false; importer.importLights = false;
            importer.addCollider = false; importer.materialImportMode = ModelImporterMaterialImportMode.None;
            importer.SaveAndReimport();
        }
        foreach (var name in Slots.SelectMany(s => s).Distinct()) BuildMaterial(name);
        var prefabs = Names.Select((name, i) => BuildPrefab(name, Slots[i])).ToArray();
        var data = AssetDatabase.LoadAssetAtPath<WaterRiceAssets>(DataPath);
        if (!data) { data = ScriptableObject.CreateInstance<WaterRiceAssets>(); AssetDatabase.CreateAsset(data, DataPath); }
        data.ladle = prefabs[0]; data.cup = prefabs[1]; data.bucket = prefabs[2]; data.basket = prefabs[3];
        data.ladleWater = prefabs[4]; data.cupRice = prefabs[5]; data.bucketWater = prefabs[6]; data.basketRice = prefabs[7];
        data.waterMaterial = Mat("WR_Water"); data.riceMaterial = Mat("WR_Rice"); BuildReceiver(data);
        var report = new StringBuilder("WaterRice Unity import validation\n");
        var expected = new[] { new Vector3(.32f, .055f, .11f), new Vector3(.13f, .1f, .09f), new Vector3(.32f, .36f, .32f), new Vector3(.38f, .2f, .38f) };
        for (int i = 0; i < prefabs.Length; i++)
        {
            var instance = UnityEngine.Object.Instantiate(prefabs[i]);
            try
            {
                Bounds bounds = BoundsOf(instance);
                report.AppendLine(Names[i] + " bounds=" + bounds.size.ToString("F4") + " pivot-to-center=" + bounds.center.ToString("F4"));
                if (i < 4)
                {
                    if (Vector3.Distance(bounds.size, expected[i]) > .004f) throw new InvalidOperationException("Unexpected size/orientation: " + Names[i] + " " + bounds);
                    foreach (string suffix in new[] { "Grip", "PourSpout", "FillCenter", "ContentsRoot" })
                    {
                        var marker = instance.GetComponentsInChildren<Transform>(true).Single(t => t.name == Names[i] + "_" + suffix);
                        report.AppendLine("  " + suffix + " p=" + marker.position.ToString("F4") + " forward=" + marker.forward.ToString("F3"));
                        if (suffix == "ContentsRoot" && Vector3.Dot(marker.forward, Vector3.up) < .99f)
                            throw new InvalidOperationException("ContentsRoot +Z must be physical up: " + Names[i]);
                    }
                }
                if (instance.GetComponentsInChildren<Transform>(true).Any(t => t.name.StartsWith("COL_")) || instance.GetComponentsInChildren<Collider>(true).Length != 0)
                    throw new InvalidOperationException("Physics proxy leaked into prefab.");
            }
            finally { UnityEngine.Object.DestroyImmediate(instance); }
        }
        EditorUtility.SetDirty(data); Undo.RecordObject(simulator, "Bind imported water/rice art"); simulator.ConfigureWaterRice(data);
        EditorUtility.SetDirty(simulator); AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        Directory.CreateDirectory("Tools/ProjectAudit/Simulator"); File.WriteAllText("Tools/ProjectAudit/Simulator/waterrice-import.txt", report.ToString());
        Debug.Log("[WaterRice] Eight isolated prefabs, eight URP materials and existing-pot receiver imported. No prop layout changed.\n" + report);
    }
    private static void Copy(string source, string target)
    {
        if (!File.Exists(target)) File.Copy(source, target);
        else if (!File.ReadAllBytes(source).SequenceEqual(File.ReadAllBytes(target))) throw new InvalidOperationException("Refusing to overwrite modified import: " + target);
    }
    private static string MapPath(string material, string kind)
    {
        var paths = Directory.GetFiles(Root + "/Textures", material + "_" + kind + "*.png");
        if (paths.Length != 1) throw new InvalidOperationException("Ambiguous/missing map: " + material + " " + kind);
        return paths[0].Replace('\\', '/');
    }
    private static Material Mat(string name) => AssetDatabase.LoadAssetAtPath<Material>(Root + "/Materials/" + name + ".mat");
    private static void BuildMaterial(string name)
    {
        var material = Mat(name);
        if (!material) { material = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(material, Root + "/Materials/" + name + ".mat"); }
        material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(MapPath(name, "BaseColor")));
        material.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>(MapPath(name, "Normal"))); material.SetFloat("_BumpScale", .6f); material.EnableKeyword("_NORMALMAP");
        var rough = new Texture2D(2, 2, TextureFormat.RGBA32, false, true);
        try
        {
            rough.LoadImage(File.ReadAllBytes(MapPath(name, "Roughness")));
            var pixels = rough.GetPixels32();
            for (int i = 0; i < pixels.Length; i++) pixels[i] = new Color32(name == "WR_Aluminum" ? (byte)255 : (byte)0, 0, 0, (byte)(255 - pixels[i].r));
            rough.SetPixels32(pixels); rough.Apply(); string path = Root + "/Textures/" + name + "_MetallicSmoothness.png";
            File.WriteAllBytes(path, rough.EncodeToPNG()); AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path); importer.sRGBTexture = false; importer.alphaSource = TextureImporterAlphaSource.FromInput; importer.SaveAndReimport();
            material.SetTexture("_MetallicGlossMap", AssetDatabase.LoadAssetAtPath<Texture2D>(path));
        }
        finally { UnityEngine.Object.DestroyImmediate(rough); }
        material.SetFloat("_Smoothness", 1); material.SetFloat("_SmoothnessTextureChannel", 0); material.EnableKeyword("_METALLICSPECGLOSSMAP");
        material.DisableKeyword("_SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A");
        material.SetColor("_BaseColor", name == "WR_Water" ? new Color(1, 1, 1, .63f) : Color.white);
        if (name == "WR_Water")
        {
            material.SetFloat("_Surface", 1); material.SetFloat("_Blend", 0); material.SetFloat("_ZWrite", 0);
            material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha); material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); material.SetOverrideTag("RenderType", "Transparent"); material.renderQueue = 3000;
        }
        EditorUtility.SetDirty(material);
    }
    private static GameObject BuildPrefab(string name, string[] slots)
    {
        var root = new GameObject(name);
        try
        {
            var model = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Root + "/Models/" + name + ".fbx"), root.transform);
            foreach (var t in model.GetComponentsInChildren<Transform>(true).Where(t => t.name.StartsWith("COL_")).ToArray()) if (t) UnityEngine.Object.DestroyImmediate(t.gameObject);
            foreach (var c in model.GetComponentsInChildren<Collider>(true)) UnityEngine.Object.DestroyImmediate(c);
            // The FBX mesh-root corrective rotation does not preserve Empty frame axes.
            // Normalize semantic +Z directions from the delivered axis contract; positions
            // remain the measured imported positions. Native body axes: X=handle, Y=up.
            foreach (var marker in model.GetComponentsInChildren<Transform>(true))
            {
                if (marker.name.EndsWith("_ContentsRoot") || marker.name.EndsWith("_FillCenter"))
                    marker.rotation = Quaternion.LookRotation(Vector3.up, Vector3.forward);
                else if (marker.name.EndsWith("_Grip")) marker.rotation = Quaternion.LookRotation(Vector3.back, Vector3.up);
                else if (marker.name.EndsWith("_PourSpout")) marker.rotation = Quaternion.LookRotation(new Vector3(-.988936f, .14834f, 0), Vector3.up);
            }
            foreach (var renderer in model.GetComponentsInChildren<MeshRenderer>(true))
            {
                if (renderer.GetComponent<MeshFilter>().sharedMesh.subMeshCount != slots.Length) throw new InvalidOperationException("Material slot mismatch: " + name);
                renderer.sharedMaterials = slots.Select(Mat).ToArray();
            }
            return PrefabUtility.SaveAsPrefabAsset(root, Root + "/Prefabs/" + name + ".prefab");
        }
        finally { UnityEngine.Object.DestroyImmediate(root); }
    }
    private static Bounds BoundsOf(GameObject root)
    {
        var renderers = root.GetComponentsInChildren<MeshRenderer>(true); Bounds b = renderers[0].bounds;
        foreach (var r in renderers.Skip(1)) b.Encapsulate(r.bounds); return b;
    }
    private static void BuildReceiver(WaterRiceAssets data)
    {
        var root = new GameObject("ExistingKitchenReceivingPot");
        var source = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Scene2BlenderKitchenIntegration.ArtRoot + "/Models/SM_RiceWashingPot.fbx"), root.transform);
        try
        {
            foreach (var renderer in source.GetComponentsInChildren<MeshRenderer>(true).ToArray())
                if (renderer.name != "Pot_Body" && renderer.name != "Handle_Left" && renderer.name != "Handle_Right") UnityEngine.Object.DestroyImmediate(renderer.gameObject);
            foreach (var c in root.GetComponentsInChildren<Collider>(true)) UnityEngine.Object.DestroyImmediate(c);
            Bounds bounds = BoundsOf(root); source.transform.position -= new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
            bounds = BoundsOf(root);
            data.receiverFloor = Vector3.up * .015f; data.receiverRadius = Mathf.Min(bounds.extents.x, bounds.extents.z) * .86f;
            data.receiverHeight = bounds.size.y * .65f;
            data.receiver = PrefabUtility.SaveAsPrefabAsset(root, Root + "/Prefabs/ExistingKitchenReceivingPot.prefab");
        }
        finally { UnityEngine.Object.DestroyImmediate(root); }
    }
}
