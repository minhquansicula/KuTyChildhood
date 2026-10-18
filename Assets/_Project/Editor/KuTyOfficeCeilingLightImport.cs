using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

public static class KuTyOfficeCeilingLightImport
{
    private const string QueueFlag = "Library/KuTyRunCeilingLightImport.flag";
    private const string ScenePath = "Assets/_Project/Scenes/Act1_Office_Blockout.unity";
    private const string ModelPath = "Assets/_Project/Art/Office/Lighting/Models/OfficeCeilingLight.fbx";
    private const string HousingTexturePath = "Assets/_Project/Art/Office/Lighting/Textures/T_CeilingLight_Housing_BaseColor.png";
    private const string DiffuserTexturePath = "Assets/_Project/Art/Office/Lighting/Textures/T_CeilingLight_Diffuser_BaseColor.png";
    private const string MaterialFolder = "Assets/_Project/Art/Office/Lighting/Materials";
    private const string HousingMaterialPath = MaterialFolder + "/MAT_CeilingLight_Housing.mat";
    private const string DiffuserMaterialPath = MaterialFolder + "/MAT_CeilingLight_Diffuser.mat";

    [InitializeOnLoadMethod]
    private static void RunQueuedImport()
    {
        if (!File.Exists(QueueFlag))
            return;
        EditorApplication.delayCall += () =>
        {
            if (!File.Exists(QueueFlag) || EditorApplication.isPlayingOrWillChangePlaymode)
                return;
            try
            {
                Apply();
                File.Delete(QueueFlag);
                Debug.Log("OFFICE_CEILING_LIGHT_QUEUE_COMPLETED");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        };
    }

    [MenuItem("KuTy/Office Intro/Replace All Ceiling Lights")]
    public static void Apply()
    {
        if (EditorApplication.isPlaying)
            throw new InvalidOperationException("Run ceiling-light replacement in Edit Mode.");

        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        ConfigureModel();
        ConfigureTexture(HousingTexturePath);
        ConfigureTexture(DiffuserTexturePath);

        Directory.CreateDirectory(MaterialFolder);
        var housing = GetOrCreateMaterial(HousingMaterialPath, "MAT_CeilingLight_Housing");
        housing.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(HousingTexturePath));
        housing.SetColor("_BaseColor", new Color(.42f, .42f, .39f, 1f));
        housing.SetFloat("_Metallic", 0.08f);
        housing.SetFloat("_Smoothness", 0.12f);
        EditorUtility.SetDirty(housing);

        var diffuser = GetOrCreateMaterial(DiffuserMaterialPath, "MAT_CeilingLight_Diffuser");
        diffuser.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(DiffuserTexturePath));
        diffuser.SetColor("_BaseColor", new Color(.42f, .34f, .21f, 1f));
        diffuser.SetFloat("_Metallic", 0f);
        diffuser.SetFloat("_Smoothness", .18f);
        diffuser.EnableKeyword("_EMISSION");
        diffuser.SetColor("_EmissionColor", new Color(.18f, .11f, .045f));
        diffuser.globalIlluminationFlags = MaterialGlobalIlluminationFlags.BakedEmissive;
        EditorUtility.SetDirty(diffuser);

        var model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
        if (!model)
            throw new InvalidOperationException("Ceiling light FBX could not be loaded.");

        var slots = Resources.FindObjectsOfTypeAll<GameObject>()
            .Where(go => go.scene == scene && go.name == "CeilingLight_SLOT")
            .OrderBy(go => go.transform.position.x)
            .ThenBy(go => go.transform.position.z)
            .ToArray();
        if (slots.Length == 0)
            throw new InvalidOperationException("No CeilingLight_SLOT objects were found.");

        var report = new StringBuilder();
        report.AppendLine("OFFICE_CEILING_LIGHT_IMPORT");
        report.AppendLine("Slots=" + slots.Length);
        report.AppendLine("SourceRenderers=" + string.Join(", ", model.GetComponentsInChildren<Renderer>(true).Select(r => r.name)));
        report.AppendLine("SourceMaterials=" + string.Join(", ", AssetDatabase.LoadAllAssetsAtPath(ModelPath).OfType<Material>().Select(m => m.name)));

        foreach (var slot in slots)
        {
            var oldImported = slot.transform.Find("ImportedOfficeCeilingLight");
            if (oldImported)
                UnityEngine.Object.DestroyImmediate(oldImported.gameObject);

            var oldRenderers = slot.GetComponentsInChildren<Renderer>(true)
                .Where(renderer => !IsUnderImported(renderer.transform))
                .ToArray();
            if (oldRenderers.Length == 0)
                throw new InvalidOperationException("Ceiling slot has no original visual bounds: " + HierarchyPath(slot.transform));

            var targetBounds = CombinedBounds(oldRenderers);
            foreach (var renderer in oldRenderers)
            {
                Undo.RecordObject(renderer, "Hide blockout ceiling light");
                renderer.enabled = false;
            }

            var instance = PrefabUtility.InstantiatePrefab(model, scene) as GameObject;
            if (!instance)
                throw new InvalidOperationException("Could not instantiate ceiling light FBX.");
            Undo.RegisterCreatedObjectUndo(instance, "Import office ceiling light");
            instance.name = "ImportedOfficeCeilingLight";
            instance.transform.SetParent(slot.transform, false);

            var renderers = instance.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
                throw new InvalidOperationException("Imported ceiling light has no renderers.");
            AssignHousingMaterial(renderers, housing);
            FitToBounds(instance.transform, renderers, targetBounds);
            AddDiffuserPanel(slot.transform, targetBounds, diffuser);

            report.AppendLine(HierarchyPath(slot.transform) + " target=" + targetBounds + " imported=" + CombinedBounds(renderers));
        }

        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        CapturePreview();
        File.WriteAllText("Library/KuTyCeilingLightImportReport.txt", report.ToString());
        Debug.Log("OFFICE_CEILING_LIGHTS_REPLACED: count=" + slots.Length + ", textures=2, existing Light components preserved.");
    }

    [MenuItem("KuTy/Office Intro/Restore Original Ceiling Lights")]
    public static void RestoreOriginalLights()
    {
        if (EditorApplication.isPlaying)
            throw new InvalidOperationException("Restore ceiling lights in Edit Mode.");

        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        var slots = Resources.FindObjectsOfTypeAll<GameObject>()
            .Where(go => go.scene == scene && go.name == "CeilingLight_SLOT")
            .ToArray();

        var restored = 0;
        foreach (var slot in slots)
        {
            for (var i = slot.transform.childCount - 1; i >= 0; i--)
            {
                var child = slot.transform.GetChild(i);
                if (child.name == "ImportedOfficeCeilingLight" || child.name == "ImportedLightDiffuser")
                    Undo.DestroyObjectImmediate(child.gameObject);
            }

            foreach (var renderer in slot.GetComponentsInChildren<Renderer>(true))
            {
                if (!IsUnderImported(renderer.transform))
                {
                    Undo.RecordObject(renderer, "Restore original ceiling light");
                    renderer.enabled = true;
                }
            }
            restored++;
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("OFFICE_ORIGINAL_CEILING_LIGHTS_RESTORED: count=" + restored);
    }

    private static void ConfigureModel()
    {
        var importer = AssetImporter.GetAtPath(ModelPath) as ModelImporter;
        if (!importer)
            throw new InvalidOperationException("Ceiling light model importer is missing.");
        importer.importAnimation = false;
        importer.importCameras = false;
        importer.importLights = false;
        importer.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
        importer.SaveAndReimport();
    }

    private static void ConfigureTexture(string path)
    {
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (!importer)
            throw new InvalidOperationException("Texture importer is missing: " + path);
        importer.textureType = TextureImporterType.Default;
        importer.sRGBTexture = true;
        importer.wrapMode = TextureWrapMode.Repeat;
        importer.filterMode = FilterMode.Bilinear;
        importer.maxTextureSize = 2048;
        importer.SaveAndReimport();
    }

    private static Material GetOrCreateMaterial(string path, string name)
    {
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material)
            return material;
        material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = name };
        AssetDatabase.CreateAsset(material, path);
        return material;
    }

    private static void AssignHousingMaterial(Renderer[] renderers, Material housing)
    {
        foreach (var renderer in renderers)
        {
            var source = renderer.sharedMaterials;
            var assigned = new Material[Math.Max(1, source.Length)];
            for (var i = 0; i < assigned.Length; i++)
                assigned[i] = housing;
            renderer.sharedMaterials = assigned;
        }
    }

    private static void FitToBounds(Transform root, Renderer[] renderers, Bounds target)
    {
        var importedRotation = root.localRotation;
        var candidates = new[]
        {
            Vector3.zero, new Vector3(90, 0, 0), new Vector3(0, 0, 90),
            new Vector3(90, 0, 90), new Vector3(0, 90, 0), new Vector3(0, 90, 90)
        };
        var bestRotation = importedRotation;
        var bestScore = float.PositiveInfinity;
        foreach (var candidate in candidates)
        {
            root.localRotation = importedRotation * Quaternion.Euler(candidate);
            var size = CombinedBounds(renderers).size;
            var factors = new Vector3(target.size.x / Mathf.Max(size.x, .0001f), target.size.y / Mathf.Max(size.y, .0001f), target.size.z / Mathf.Max(size.z, .0001f));
            var min = Mathf.Max(Mathf.Min(factors.x, Mathf.Min(factors.y, factors.z)), .0001f);
            var max = Mathf.Max(factors.x, Mathf.Max(factors.y, factors.z));
            var score = max / min;
            if (score < bestScore)
            {
                bestScore = score;
                bestRotation = root.localRotation;
            }
        }
        root.localRotation = bestRotation;
        var source = CombinedBounds(renderers);
        root.localScale = Vector3.Scale(root.localScale, new Vector3(
            target.size.x / Mathf.Max(source.size.x, .0001f),
            target.size.y / Mathf.Max(source.size.y, .0001f),
            target.size.z / Mathf.Max(source.size.z, .0001f)));
        source = CombinedBounds(renderers);
        root.position += target.center - source.center;
    }

    private static void AddDiffuserPanel(Transform slot, Bounds target, Material material)
    {
        var panel = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Undo.RegisterCreatedObjectUndo(panel, "Create ceiling light diffuser");
        panel.name = "ImportedLightDiffuser";
        panel.transform.SetParent(slot, true);
        panel.transform.position = new Vector3(target.center.x, target.min.y - .006f, target.center.z);
        panel.transform.rotation = Quaternion.identity;
        panel.transform.localScale = new Vector3(Mathf.Max(target.size.x * .36f, .05f), .008f, Mathf.Max(target.size.z * .36f, .05f));
        UnityEngine.Object.DestroyImmediate(panel.GetComponent<Collider>());
        panel.GetComponent<Renderer>().sharedMaterial = material;
    }

    private static Bounds CombinedBounds(Renderer[] renderers)
    {
        var bounds = renderers[0].bounds;
        for (var i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);
        return bounds;
    }

    private static bool IsUnderImported(Transform transform)
    {
        while (transform)
        {
            if (transform.name == "ImportedOfficeCeilingLight" || transform.name == "ImportedLightDiffuser")
                return true;
            transform = transform.parent;
        }
        return false;
    }

    private static string HierarchyPath(Transform transform)
    {
        var path = transform.name;
        while (transform.parent)
        {
            transform = transform.parent;
            path = transform.name + "/" + path;
        }
        return path;
    }

    private static void CapturePreview()
    {
        Directory.CreateDirectory("Assets/Screenshots");
        var go = new GameObject("TemporaryCeilingLightPreview");
        var camera = go.AddComponent<Camera>();
        camera.GetUniversalAdditionalCameraData().renderPostProcessing = true;
        camera.fieldOfView = 64;
        camera.nearClipPlane = .05f;
        camera.farClipPlane = 100;
        camera.transform.position = new Vector3(-1.2f, 1.35f, 1.6f);
        camera.transform.LookAt(new Vector3(1.2f, 3.05f, -.8f));
        var rt = RenderTexture.GetTemporary(1366, 768, 24);
        var previous = RenderTexture.active;
        var texture = new Texture2D(1366, 768, TextureFormat.RGB24, false);
        try
        {
            camera.targetTexture = rt;
            camera.Render();
            RenderTexture.active = rt;
            texture.ReadPixels(new Rect(0, 0, 1366, 768), 0, 0);
            texture.Apply();
            File.WriteAllBytes("Assets/Screenshots/OfficeCeilingLights_Imported.png", texture.EncodeToPNG());
        }
        finally
        {
            camera.targetTexture = null;
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(rt);
            UnityEngine.Object.DestroyImmediate(texture);
            UnityEngine.Object.DestroyImmediate(go);
        }
        AssetDatabase.Refresh();
    }
}
