using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class KuTyOfficeChairSetup
{
    private const string ScenePath = "Assets/_Project/Scenes/Act1_RealWorld.unity";
    private const string ModelPath = "Assets/_Project/Art/Models/OfficeSceneNew/office-chair.fbx";
    private const string MaterialPath = "Assets/_Project/Art/Materials/OfficeChair_New.mat";
    private const string BaseColorPath = "Assets/_Project/Art/Textures/OfficeSceneNew/Chair/office_chair_3d_model_basecolor.JPEG";
    private const string NormalPath = "Assets/_Project/Art/Textures/OfficeSceneNew/Chair/office_chair_3d_model_normal.JPEG";
    private const string MetallicPath = "Assets/_Project/Art/Textures/OfficeSceneNew/Chair/office_chair_3d_model_metallic.JPEG";
    private const string RequestFile = "KuTyOfficeChairSetup.request";

    static KuTyOfficeChairSetup()
    {
        if (File.Exists(Path.Combine(Directory.GetCurrentDirectory(), RequestFile)))
            EditorApplication.delayCall += RunRequestedSetup;
    }

    [MenuItem("KuTy/Setup/Install office chair from imported asset")]
    public static void InstallOfficeChair()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            Debug.LogWarning("[KuTy] Chair setup is waiting for Edit Mode and a completed import.");
            EditorApplication.delayCall += InstallOfficeChair;
            return;
        }

        var scene = EditorSceneManager.GetActiveScene();
        if (scene.path != ScenePath)
        {
            if (scene.isDirty)
            {
                Debug.LogWarning("[KuTy] Chair setup stopped because another scene has unsaved changes.");
                return;
            }

            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        AssetDatabase.ImportAsset(ModelPath, ImportAssetOptions.ForceUpdate);
        ConfigureNormalMap();

        GameObject chairAnchor = GameObject.Find("OfficeChair");
        GameObject modelAsset = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
        if (chairAnchor == null || modelAsset == null)
        {
            Debug.LogError("[KuTy] OfficeChair or imported chair model could not be found.");
            return;
        }

        Material material = BuildMaterial();
        Transform existing = chairAnchor.transform.Find("OfficeChairModel_New");
        GameObject model = existing != null
            ? existing.gameObject
            : (GameObject)PrefabUtility.InstantiatePrefab(modelAsset, chairAnchor.transform);

        model.name = "OfficeChairModel_New";
        model.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.Euler(0f, 180f, 0f));
        model.transform.localScale = Vector3.one;

        Renderer[] renderers = model.GetComponentsInChildren<Renderer>(true);
        foreach (Renderer renderer in renderers)
        {
            var materials = new Material[renderer.sharedMaterials.Length];
            for (int i = 0; i < materials.Length; i++) materials[i] = material;
            renderer.sharedMaterials = materials;
            renderer.gameObject.layer = chairAnchor.layer;

            if (renderer is MeshRenderer && renderer.TryGetComponent<MeshFilter>(out var filter) && filter.sharedMesh != null)
            {
                var collider = renderer.GetComponent<MeshCollider>();
                if (collider == null) collider = renderer.gameObject.AddComponent<MeshCollider>();
                collider.sharedMesh = filter.sharedMesh;
                collider.convex = false;
                collider.isTrigger = false;
            }
        }

        SetLayerRecursively(model.transform, chairAnchor.layer);
        FitToAnchor(model.transform, renderers, chairAnchor.transform.position, 1.18f);

        // This anchor now represents the actual chair at the player's workstation.
        chairAnchor.transform.position = new Vector3(1.4f, 0f, 1.55f);
        chairAnchor.transform.rotation = Quaternion.identity;
        FitToAnchor(model.transform, renderers, chairAnchor.transform.position, 1.18f);

        EditorUtility.SetDirty(chairAnchor);
        EditorUtility.SetDirty(model);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Selection.activeGameObject = model;
        SceneView.lastActiveSceneView?.FrameSelected();
        Debug.Log("[KuTy] Office chair installed, material assigned, collider added and interaction anchor preserved.");
    }

    private static void RunRequestedSetup()
    {
        string requestPath = Path.Combine(Directory.GetCurrentDirectory(), RequestFile);
        if (!File.Exists(requestPath)) return;

        InstallOfficeChair();
        if (GameObject.Find("OfficeChair")?.transform.Find("OfficeChairModel_New") != null)
            File.Delete(requestPath);
    }

    private static void ConfigureNormalMap()
    {
        if (AssetImporter.GetAtPath(NormalPath) is not TextureImporter importer) return;
        if (importer.textureType == TextureImporterType.NormalMap) return;
        importer.textureType = TextureImporterType.NormalMap;
        importer.SaveAndReimport();
    }

    private static Material BuildMaterial()
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        if (material == null)
        {
            material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            AssetDatabase.CreateAsset(material, MaterialPath);
        }

        material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(BaseColorPath));
        material.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>(NormalPath));
        material.SetTexture("_MetallicGlossMap", AssetDatabase.LoadAssetAtPath<Texture2D>(MetallicPath));
        material.SetFloat("_BumpScale", 1f);
        material.SetFloat("_Metallic", 0.7f);
        material.SetFloat("_Smoothness", 0.32f);
        material.EnableKeyword("_NORMALMAP");
        material.EnableKeyword("_METALLICSPECGLOSSMAP");
        EditorUtility.SetDirty(material);
        AssetDatabase.SaveAssets();
        return material;
    }

    private static void FitToAnchor(Transform model, Renderer[] renderers, Vector3 floorPosition, float targetHeight)
    {
        if (!TryGetBounds(renderers, out Bounds bounds) || bounds.size.y <= 0.0001f) return;

        float scale = targetHeight / bounds.size.y;
        model.localScale *= scale;
        if (!TryGetBounds(renderers, out bounds)) return;
        model.position += Vector3.up * (floorPosition.y - bounds.min.y);
    }

    private static bool TryGetBounds(Renderer[] renderers, out Bounds bounds)
    {
        bounds = default;
        bool found = false;
        foreach (Renderer renderer in renderers)
        {
            if (!renderer.enabled) continue;
            if (!found)
            {
                bounds = renderer.bounds;
                found = true;
            }
            else bounds.Encapsulate(renderer.bounds);
        }
        return found;
    }

    private static void SetLayerRecursively(Transform root, int layer)
    {
        root.gameObject.layer = layer;
        foreach (Transform child in root) SetLayerRecursively(child, layer);
    }
}
