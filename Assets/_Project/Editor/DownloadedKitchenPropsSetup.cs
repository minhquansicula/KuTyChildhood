using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Imports isolated copies of the user's downloads; never changes the Home layout.</summary>
public static class DownloadedKitchenPropsSetup
{
    public const string Root = "Assets/_Project/Art/Scene2/DownloadedKitchenProps";
    private static readonly string[] Names = { "WashStation", "Bowl", "Plate", "Faucet" };
    private static readonly string[] Patterns = { "*rua*chen*.fbx", "cai_chen.fbx", "cai_dia.fbx", "voi-nuoc.fbx" };

    [MenuItem("KuTy/Scene 2/Finish downloaded kitchen PBR and previews")]
    public static void FinishMaterials()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Use Edit Mode.");
        Directory.CreateDirectory("Tools/DownloadedKitchenProps/Preview");
        var report = new StringBuilder("PBR material read-back\n");
        foreach (string name in Names)
        {
            string folder = Root + "/" + name;
            string textures = folder + "/Textures";
            string metallicPath = Directory.GetFiles(textures, "*_metallic.png").Single();
            string roughnessPath = Directory.GetFiles(textures, "*_roughness.png").Single();
            foreach (string path in new[] { metallicPath, roughnessPath })
            {
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                importer.sRGBTexture = false;
                importer.SaveAndReimport();
            }
            var metallic = new Texture2D(2, 2, TextureFormat.RGBA32, false, true);
            var roughness = new Texture2D(2, 2, TextureFormat.RGBA32, false, true);
            Texture2D packed = null;
            try
            {
                if (!metallic.LoadImage(File.ReadAllBytes(metallicPath)) || !roughness.LoadImage(File.ReadAllBytes(roughnessPath)))
                    throw new InvalidOperationException("Cannot decode PBR maps for " + name);
                if (metallic.width != roughness.width || metallic.height != roughness.height)
                    throw new InvalidOperationException("PBR texture sizes differ for " + name);
                var pixels = metallic.GetPixels32();
                var roughPixels = roughness.GetPixels32();
                for (int i = 0; i < pixels.Length; i++)
                    pixels[i] = new Color32(pixels[i].r, 0, 0, (byte)(255 - roughPixels[i].r));
                packed = new Texture2D(metallic.width, metallic.height, TextureFormat.RGBA32, false, true);
                packed.SetPixels32(pixels);
                packed.Apply();
                string packedPath = textures + "/" + name + "_MetallicSmoothness.png";
                File.WriteAllBytes(packedPath, packed.EncodeToPNG());
                AssetDatabase.ImportAsset(packedPath, ImportAssetOptions.ForceSynchronousImport);
                var importer = (TextureImporter)AssetImporter.GetAtPath(packedPath);
                importer.sRGBTexture = false;
                importer.alphaSource = TextureImporterAlphaSource.FromInput;
                importer.alphaIsTransparency = false;
                importer.SaveAndReimport();
                var material = AssetDatabase.LoadAssetAtPath<Material>(folder + "/Materials/" + name + "_0.mat");
                material.SetTexture("_MetallicGlossMap", AssetDatabase.LoadAssetAtPath<Texture2D>(packedPath));
                material.SetFloat("_Metallic", 1f);
                material.SetFloat("_Smoothness", 1f);
                material.SetFloat("_SmoothnessTextureChannel", 0f);
                material.EnableKeyword("_METALLICSPECGLOSSMAP");
                material.DisableKeyword("_SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A");
                EditorUtility.SetDirty(material);
                report.AppendLine(name + ": shader=" + material.shader.name + ", base=" + AssetDatabase.GetAssetPath(material.GetTexture("_BaseMap")) + ", normal=" + AssetDatabase.GetAssetPath(material.GetTexture("_BumpMap")) + ", metallic(R)/1-roughness(A)=" + AssetDatabase.GetAssetPath(material.GetTexture("_MetallicGlossMap")));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(metallic);
                UnityEngine.Object.DestroyImmediate(roughness);
                if (packed) UnityEngine.Object.DestroyImmediate(packed);
            }
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(folder + "/" + name + ".prefab");
            var preview = new PreviewRenderUtility();
            Texture2D image = null;
            try
            {
                var instance = UnityEngine.Object.Instantiate(prefab);
                preview.AddSingleGO(instance);
                var renderers = instance.GetComponentsInChildren<Renderer>(true);
                var bounds = renderers[0].bounds;
                foreach (var renderer in renderers.Skip(1)) bounds.Encapsulate(renderer.bounds);
                float radius = bounds.extents.magnitude;
                preview.camera.fieldOfView = 35;
                preview.camera.nearClipPlane = .001f;
                preview.camera.farClipPlane = 100f;
                preview.camera.transform.position = bounds.center + new Vector3(1.1f, .9f, -1.4f).normalized * radius * 3.8f;
                preview.camera.transform.LookAt(bounds.center);
                preview.camera.clearFlags = CameraClearFlags.SolidColor;
                preview.camera.backgroundColor = new Color(.15f, .17f, .19f);
                preview.lights[0].intensity = 1.4f;
                preview.lights[0].transform.rotation = Quaternion.Euler(45, 35, 0);
                preview.lights[1].intensity = .8f;
                preview.ambientColor = new Color(.4f, .4f, .4f);
                preview.BeginStaticPreview(new Rect(0, 0, 768, 768));
                preview.Render(true);
                image = preview.EndStaticPreview();
                File.WriteAllBytes("Tools/DownloadedKitchenProps/Preview/" + name + ".png", image.EncodeToPNG());
            }
            finally
            {
                if (image) UnityEngine.Object.DestroyImmediate(image);
                preview.Cleanup();
            }
        }
        AssetDatabase.SaveAssets();
        File.WriteAllText("Tools/DownloadedKitchenProps/pbr-report.txt", report.ToString());
        Debug.Log(report.ToString());
    }

    [MenuItem("KuTy/Scene 2/Import downloaded kitchen props (assets only)")]
    public static void Import()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Import in Edit Mode; no running game will be stopped automatically.");
        if (Directory.Exists(Root))
            throw new InvalidOperationException("Import folder already exists. Refusing to overwrite local assets: " + Root);
        string downloads = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
        // Resolve all sources before creating anything.
        var sources = Patterns.Select(p => Directory.GetFiles(downloads, p).Single()).ToArray();
        var shader = Shader.Find("Universal Render Pipeline/Lit");
        if (!shader) throw new InvalidOperationException("URP Lit shader is unavailable.");
        var report = new StringBuilder("Downloaded kitchen props — isolated import\n");
        report.AppendLine("Home scene, gameplay, existing assets and original downloads are not modified.");
        for (int i = 0; i < Names.Length; i++)
        {
            string folder = Root + "/" + Names[i];
            Directory.CreateDirectory(folder + "/Textures");
            Directory.CreateDirectory(folder + "/Materials");
            string modelPath = folder + "/" + Names[i] + ".fbx";
            File.Copy(sources[i], modelPath, false);
            AssetDatabase.ImportAsset(modelPath, ImportAssetOptions.ForceSynchronousImport);
            var importer = (ModelImporter)AssetImporter.GetAtPath(modelPath);
            importer.importAnimation = false;
            importer.importCameras = false;
            importer.importLights = false;
            importer.addCollider = false;
            importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
            importer.materialLocation = ModelImporterMaterialLocation.InPrefab;
            importer.SaveAndReimport();
            bool extracted = importer.ExtractTextures(folder + "/Textures");
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            importer.SaveAndReimport();
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
            var materials = model.GetComponentsInChildren<Renderer>(true)
                .SelectMany(r => r.sharedMaterials).Where(m => m).Distinct().ToArray();
            var remap = new System.Collections.Generic.Dictionary<Material, Material>();
            report.AppendLine("\n" + Names[i] + " source: " + sources[i]);
            report.AppendLine("Embedded textures extracted: " + extracted);
            for (int j = 0; j < materials.Length; j++)
            {
                var original = materials[j];
                var material = new Material(shader) { name = Names[i] + "_" + j };
                var albedo = original.HasProperty("_BaseMap") ? original.GetTexture("_BaseMap") : original.mainTexture;
                material.SetTexture("_BaseMap", albedo);
                material.SetColor("_BaseColor", original.HasProperty("_Color") ? original.GetColor("_Color") : Color.white);
                if (albedo)
                {
                    material.SetTextureScale("_BaseMap", original.mainTextureScale);
                    material.SetTextureOffset("_BaseMap", original.mainTextureOffset);
                }
                var normal = original.HasProperty("_BumpMap") ? original.GetTexture("_BumpMap") : null;
                if (normal)
                {
                    string path = AssetDatabase.GetAssetPath(normal);
                    var textureImporter = AssetImporter.GetAtPath(path) as TextureImporter;
                    if (textureImporter && path.StartsWith(Root, StringComparison.Ordinal))
                    {
                        textureImporter.textureType = TextureImporterType.NormalMap;
                        textureImporter.SaveAndReimport();
                    }
                    material.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture>(path));
                    material.EnableKeyword("_NORMALMAP");
                }
                material.SetFloat("_Metallic", original.HasProperty("_Metallic") ? original.GetFloat("_Metallic") : 0f);
                material.SetFloat("_Smoothness", original.HasProperty("_Glossiness") ? original.GetFloat("_Glossiness") : .3f);
                AssetDatabase.CreateAsset(material, folder + "/Materials/" + material.name + ".mat");
                remap.Add(original, material);
                report.AppendLine("Material " + original.name + " -> " + material.name + "; base=" + AssetDatabase.GetAssetPath(albedo) + "; normal=" + AssetDatabase.GetAssetPath(normal));
                foreach (string property in original.GetTexturePropertyNames())
                    if (original.GetTexture(property)) report.AppendLine("  Source texture " + property + ": " + AssetDatabase.GetAssetPath(original.GetTexture(property)));
            }
            // A preview scene prevents temporary objects dirtying the user's open scene.
            var preview = EditorSceneManager.NewPreviewScene();
            try
            {
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(model, preview);
                instance.name = Names[i];
                foreach (var renderer in instance.GetComponentsInChildren<Renderer>(true))
                    renderer.sharedMaterials = renderer.sharedMaterials.Select(m => m && remap.ContainsKey(m) ? remap[m] : m).ToArray();
                var renderers = instance.GetComponentsInChildren<Renderer>(true);
                if (renderers.Length == 0) throw new InvalidOperationException("No renderers in " + Names[i]);
                var bounds = renderers[0].bounds;
                foreach (var renderer in renderers.Skip(1)) bounds.Encapsulate(renderer.bounds);
                report.AppendLine("Source-import bounds center=" + bounds.center.ToString("F5") + " size=" + bounds.size.ToString("F5"));
                report.AppendLine("Vertices=" + instance.GetComponentsInChildren<MeshFilter>(true).Sum(f => f.sharedMesh ? f.sharedMesh.vertexCount : 0));
                PrefabUtility.SaveAsPrefabAsset(instance, folder + "/" + Names[i] + ".prefab");
                report.AppendLine("Prefab saved. Source scale/pivot unchanged; placement and gameplay binding pending.");
            }
            finally { EditorSceneManager.ClosePreviewScene(preview); }
        }
        AssetDatabase.SaveAssets();
        Directory.CreateDirectory("Tools/DownloadedKitchenProps");
        File.WriteAllText("Tools/DownloadedKitchenProps/import-report.txt", report.ToString());
        Debug.Log(report.ToString());
    }
}
