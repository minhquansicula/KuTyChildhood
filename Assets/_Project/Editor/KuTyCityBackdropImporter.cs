using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class KuTyCityBackdropImporter
{
    private const string ScenePath = "Assets/_Project/Scenes/Act1_Office_Blockout.unity";
    private const string ModelPath = "Assets/_Project/Art/Environment/City/Models/City_Exterior.fbx";
    private const string SlotPath = "OFFICE_BLOCKOUT/05_Exterior_Backdrop/CitySilhouette_SLOT";
    private const string ImportedName = "ImportedCity_Exterior";
    private const string MaterialFolder = "Assets/_Project/Art/Environment/City/Materials";
    private const string TextureFolder = "Assets/_Project/Art/Environment/City/Textures";
    private const string ReportPath = "Assets/Screenshots/CityExterior_Import.txt";
    private const string PreviewPath = "Assets/Screenshots/CityExterior_Preview.png";
    private const string CloudTexturePath = "Assets/_Project/Art/Environment/City/Textures/T_CityCloudBank.png";
    private const string CloudMaterialPath = "Assets/_Project/Art/Environment/City/Materials/MAT_CityCloudBank.mat";

    [MenuItem("KuTy/Office Intro/Replace City Backdrop")]
    public static void ReplaceCityBackdrop()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (scene.path != ScenePath)
            throw new InvalidOperationException($"Open {ScenePath} before replacing the city backdrop.");

        GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
        if (!model)
            throw new FileNotFoundException("City FBX was not imported.", ModelPath);

        Transform slot = FindByPath(SlotPath);
        if (!slot)
            throw new InvalidOperationException($"City slot was not found: {SlotPath}");

        Transform oldBlockout = slot.Find("Visual_REPLACE");
        if (oldBlockout)
        {
            Undo.RecordObject(oldBlockout.gameObject, "Hide old city blockout");
            oldBlockout.gameObject.SetActive(false);
        }

        Transform previous = slot.Find(ImportedName);
        if (previous)
            Undo.DestroyObjectImmediate(previous.gameObject);

        GameObject instance = PrefabUtility.InstantiatePrefab(model, scene) as GameObject;
        if (!instance)
            throw new InvalidOperationException("Unity could not instantiate the imported city FBX.");

        Undo.RegisterCreatedObjectUndo(instance, "Place imported city backdrop");
        instance.name = ImportedName;
        instance.transform.SetParent(slot, false);
        instance.transform.localPosition = Vector3.zero;
        instance.transform.localRotation = Quaternion.identity;
        instance.transform.localScale = Vector3.one;

        Bounds sourceBounds = CalculateBounds(instance);
        if (sourceBounds.size.sqrMagnitude < 0.000001f)
            throw new InvalidOperationException("The imported city has no usable renderer bounds.");

        // Keep the longer horizontal side parallel to the office window wall.
        // This FBX arrives with its vertical axis inverted. Flip it upright, then
        // rotate its longer footprint side so it runs along the window wall.
        float yaw = sourceBounds.size.x > sourceBounds.size.z ? 90f : 0f;
        instance.transform.localRotation = Quaternion.Euler(180f, yaw, 0f);

        Bounds rotatedBounds = CalculateBounds(instance);
        float scale = Mathf.Min(
            18f / Mathf.Max(rotatedBounds.size.x, 0.001f),
            52f / Mathf.Max(rotatedBounds.size.z, 0.001f));
        if (rotatedBounds.size.y * scale > 18f)
            scale = 18f / rotatedBounds.size.y;

        instance.transform.localScale = Vector3.one * scale;
        Bounds scaledBounds = CalculateBounds(instance);

        // The office east wall is at x=7. Put the city beyond it and align it to the full window strip.
        Vector3 offset = new Vector3(
            9f - scaledBounds.min.x,
            -3f - scaledBounds.min.y,
            -scaledBounds.center.z);
        instance.transform.position += offset;

        Bounds finalBounds = CalculateBounds(instance);
        EditorUtility.SetDirty(instance);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        Directory.CreateDirectory("Assets/Screenshots");
        File.WriteAllText(ReportPath,
            $"Source: {ModelPath}\n" +
            $"Scene: {scene.path}\n" +
            $"Slot: {SlotPath}\n" +
            $"Old blockout active: {(oldBlockout ? oldBlockout.gameObject.activeSelf.ToString() : "missing")}\n" +
            $"Renderers: {instance.GetComponentsInChildren<Renderer>(true).Length}\n" +
            $"Source bounds: center={sourceBounds.center}, size={sourceBounds.size}\n" +
            $"Local rotation: {instance.transform.localEulerAngles}\n" +
            $"Uniform scale: {scale:F5}\n" +
            $"Final bounds: center={finalBounds.center}, size={finalBounds.size}\n" +
            $"Final position: {instance.transform.position}\n");
        AssetDatabase.ImportAsset(ReportPath, ImportAssetOptions.ForceUpdate);

        CaptureCityPreview();
        Selection.activeGameObject = instance;
        Debug.Log($"CITY_BACKDROP_REPLACED: {instance.name}, bounds={finalBounds.size}, scene preserved and saved.");
    }

    [MenuItem("KuTy/Office Intro/Capture City Preview")]
    public static void CaptureCityPreview()
    {
        if (SceneManager.GetActiveScene().path != ScenePath)
            throw new InvalidOperationException($"Open {ScenePath} before capturing the city preview.");

        Directory.CreateDirectory("Assets/Screenshots");
        GameObject cameraObject = new GameObject("TemporaryCityPreviewCamera");
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.fieldOfView = 62f;
        camera.nearClipPlane = 0.05f;
        camera.farClipPlane = 150f;
        camera.clearFlags = CameraClearFlags.Skybox;

        try
        {
            camera.transform.position = new Vector3(4.7f, 1.65f, -0.4f);
            camera.transform.LookAt(new Vector3(17f, 3.2f, 0f));
            RenderTexture renderTexture = new RenderTexture(1366, 768, 24, RenderTextureFormat.ARGB32);
            camera.targetTexture = renderTexture;
            camera.Render();

            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = renderTexture;
            Texture2D image = new Texture2D(1366, 768, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, 1366, 768), 0, 0);
            image.Apply();
            File.WriteAllBytes(PreviewPath, image.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(image);
            RenderTexture.active = previous;
            camera.targetTexture = null;
            renderTexture.Release();
            UnityEngine.Object.DestroyImmediate(renderTexture);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(cameraObject);
        }

        AssetDatabase.ImportAsset(PreviewPath, ImportAssetOptions.ForceUpdate);
        Debug.Log($"CITY_BACKDROP_PREVIEW: {PreviewPath}");
    }

    [MenuItem("KuTy/Office Intro/Extract And Apply City Textures")]
    public static void ExtractAndApplyCityTextures()
    {
        EnsureFolder(MaterialFolder);
        EnsureFolder(TextureFolder);

        ModelImporter importer = AssetImporter.GetAtPath(ModelPath) as ModelImporter;
        if (!importer)
            throw new InvalidOperationException($"ModelImporter was not found for {ModelPath}");

        bool extracted = importer.ExtractTextures(TextureFolder);
        AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);

        Material[] embeddedMaterials = AssetDatabase.LoadAllAssetsAtPath(ModelPath)
            .OfType<Material>()
            .ToArray();
        if (embeddedMaterials.Length == 0)
            throw new InvalidOperationException("The city FBX contains no material sub-assets.");

        importer = AssetImporter.GetAtPath(ModelPath) as ModelImporter;
        foreach (Material embedded in embeddedMaterials)
        {
            string safeName = SanitizeFileName(embedded.name);
            string materialPath = $"{MaterialFolder}/MAT_City_{safeName}.mat";
            Material external = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (!external)
            {
                external = new Material(embedded);
                AssetDatabase.CreateAsset(external, materialPath);
            }
            else
            {
                EditorUtility.CopySerialized(embedded, external);
                EditorUtility.SetDirty(external);
            }

            importer.AddRemap(
                new AssetImporter.SourceAssetIdentifier(typeof(Material), embedded.name),
                external);
        }

        importer.SaveAndReimport();
        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        CaptureCityPreview();

        int textureCount = AssetDatabase.FindAssets("t:Texture2D", new[] { TextureFolder }).Length;
        Debug.Log($"CITY_TEXTURES_APPLIED: extracted={extracted}, textures={textureCount}, materials={embeddedMaterials.Length}");
    }

    [MenuItem("KuTy/Office Intro/Add City Cloud Bank")]
    public static void AddCityCloudBank()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (scene.path != ScenePath)
            throw new InvalidOperationException($"Open {ScenePath} before adding the city cloud bank.");

        Transform slot = FindByPath(SlotPath);
        if (!slot)
            throw new InvalidOperationException($"City slot was not found: {SlotPath}");

        EnsureFolder(MaterialFolder);
        EnsureFolder(TextureFolder);
        CreateCloudTexture();
        Material material = CreateOrUpdateCloudMaterial();

        Transform previous = slot.Find("CityCloudBank");
        if (previous)
            Undo.DestroyObjectImmediate(previous.gameObject);

        GameObject root = new GameObject("CityCloudBank");
        Undo.RegisterCreatedObjectUndo(root, "Add city cloud bank");
        root.transform.SetParent(slot, false);

        CreateCloudLayer(root.transform, material, "CloudLayer_Near",
            new Vector3(8.35f, 0.25f, 0f), new Vector2(18f, 2.8f), 0.08f, new Color(0.76f, 0.84f, 0.91f, 0.88f));
        CreateCloudLayer(root.transform, material, "CloudLayer_Middle",
            new Vector3(9.1f, 0.65f, 0.25f), new Vector2(19.5f, 2.45f), 0.43f, new Color(0.83f, 0.89f, 0.95f, 0.70f));
        CreateCloudLayer(root.transform, material, "CloudLayer_Far",
            new Vector3(10.2f, 1.05f, -0.35f), new Vector2(21f, 2.15f), 0.71f, new Color(0.68f, 0.78f, 0.87f, 0.48f));

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        CaptureCityPreview();
        Selection.activeGameObject = root;
        Debug.Log("CITY_CLOUD_BANK_ADDED: 3 transparent layers created in CitySilhouette_SLOT.");
    }

    private static Transform FindByPath(string path)
    {
        string[] parts = path.Split('/');
        GameObject root = SceneManager.GetActiveScene().GetRootGameObjects()
            .FirstOrDefault(item => item.name == parts[0]);
        if (!root)
            return null;

        Transform current = root.transform;
        for (int i = 1; i < parts.Length && current; i++)
            current = current.Find(parts[i]);
        return current;
    }

    private static Bounds CalculateBounds(GameObject root)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
            return new Bounds(root.transform.position, Vector3.zero);

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);
        return bounds;
    }

    private static void EnsureFolder(string fullPath)
    {
        string[] parts = fullPath.Split('/');
        string current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }

    private static string SanitizeFileName(string value)
    {
        foreach (char invalid in Path.GetInvalidFileNameChars())
            value = value.Replace(invalid, '_');
        return string.IsNullOrWhiteSpace(value) ? "Material" : value;
    }

    private static void CreateCloudTexture()
    {
        const int width = 1024;
        const int height = 256;
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false, true);
        Color[] pixels = new Color[width * height];

        for (int y = 0; y < height; y++)
        {
            float v = y / (float)(height - 1);
            float verticalFade = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(v / 0.22f))
                * Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((1f - v) / 0.34f));
            for (int x = 0; x < width; x++)
            {
                float u = x / (float)(width - 1);
                float large = Mathf.PerlinNoise(u * 5.4f + 11.3f, v * 2.2f + 3.7f);
                float detail = Mathf.PerlinNoise(u * 14.5f + 2.1f, v * 5.8f + 9.4f);
                float wisps = Mathf.PerlinNoise(u * 31f + 8.5f, v * 8f + 1.6f);
                float density = large * 0.62f + detail * 0.28f + wisps * 0.10f;
                float alpha = Mathf.SmoothStep(0.38f, 0.68f, density) * verticalFade;
                float brightness = Mathf.Lerp(0.78f, 1f, density);
                pixels[y * width + x] = new Color(brightness, brightness, brightness, alpha);
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();
        File.WriteAllBytes(CloudTexturePath, texture.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(texture);
        AssetDatabase.ImportAsset(CloudTexturePath, ImportAssetOptions.ForceUpdate);

        TextureImporter importer = AssetImporter.GetAtPath(CloudTexturePath) as TextureImporter;
        if (importer)
        {
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency = true;
            importer.wrapMode = TextureWrapMode.Repeat;
            importer.filterMode = FilterMode.Bilinear;
            importer.mipmapEnabled = true;
            importer.SaveAndReimport();
        }
    }

    private static Material CreateOrUpdateCloudMaterial()
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (!shader)
            throw new InvalidOperationException("URP Unlit shader was not found.");

        Material material = AssetDatabase.LoadAssetAtPath<Material>(CloudMaterialPath);
        if (!material)
        {
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, CloudMaterialPath);
        }
        else
        {
            material.shader = shader;
        }

        material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(CloudTexturePath));
        material.SetColor("_BaseColor", Color.white);
        material.SetFloat("_Surface", 1f);
        material.SetFloat("_Blend", 0f);
        material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        material.SetFloat("_ZWrite", 0f);
        material.SetFloat("_Cull", (float)UnityEngine.Rendering.CullMode.Off);
        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        EditorUtility.SetDirty(material);
        AssetDatabase.SaveAssets();
        return material;
    }

    private static void CreateCloudLayer(Transform parent, Material material, string name,
        Vector3 position, Vector2 size, float textureOffset, Color tint)
    {
        GameObject layer = GameObject.CreatePrimitive(PrimitiveType.Quad);
        layer.name = name;
        layer.transform.SetParent(parent, false);
        layer.transform.localPosition = position;
        layer.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
        layer.transform.localScale = new Vector3(size.x, size.y, 1f);

        Collider collider = layer.GetComponent<Collider>();
        if (collider)
            UnityEngine.Object.DestroyImmediate(collider);

        MeshRenderer renderer = layer.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;

        MaterialPropertyBlock properties = new MaterialPropertyBlock();
        properties.SetColor("_BaseColor", tint);
        properties.SetVector("_BaseMap_ST", new Vector4(1.15f, 1f, textureOffset, 0f));
        renderer.SetPropertyBlock(properties);
    }
}
