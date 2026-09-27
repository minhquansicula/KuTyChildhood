using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

/// <summary>
/// Installs the final Act 2 bed and house-front visuals without replacing the
/// existing BedBlock collider, interaction trigger, or quest objects.
/// </summary>
public static class KuTyAct2HomeAssetSetup
{
    private const string ScenePath = "Assets/_Project/Scenes/Act2_MemoryWorld_Home.unity";
    private const string BedAssetPath = "Assets/_Project/Art/Models/Act2Home/bed.fbx";
    private const string HouseAssetPath = "Assets/_Project/Art/Models/Act2Home/house-front.fbx";

    private const string EnvironmentRootName = "Act2 Imported Home";
    private const string BedVisualName = "BedVisual_Final";
    private const string HouseVisualName = "HouseFrontVisual_Final";

    [MenuItem("KuTy/Setup/Install Act 2 home assets")]
    public static void InstallMenu()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Exit Play Mode before installing Act 2 assets.");

        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        Install();
    }

    public static void Install()
    {
        if (SceneManager.GetActiveScene().path != ScenePath)
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        GameObject bedBlock = GameObject.Find("BedBlock");
        if (bedBlock == null)
            throw new InvalidOperationException("Act 2 is missing the required BedBlock object.");

        GameObject bedAsset = AssetDatabase.LoadAssetAtPath<GameObject>(BedAssetPath);
        GameObject houseAsset = AssetDatabase.LoadAssetAtPath<GameObject>(HouseAssetPath);
        if (bedAsset == null || houseAsset == null)
            throw new InvalidOperationException("The Act 2 bed or house-front model has not been imported.");

        DestroyIfPresent("__HousePreview");
        DestroyIfPresent(BedVisualName);
        DestroyIfPresent(HouseVisualName);

        GameObject environmentRoot = GameObject.Find(EnvironmentRootName);
        if (environmentRoot == null)
            environmentRoot = new GameObject(EnvironmentRootName);
        environmentRoot.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        environmentRoot.transform.localScale = Vector3.one;

        InstallBed(bedAsset, bedBlock);
        InstallHouse(houseAsset, environmentRoot.transform);
        HideBlockoutWallVisuals();

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();

        Debug.Log("KUTY_ACT2_HOME_ASSETS_INSTALLED");
    }

    private static void InstallBed(GameObject source, GameObject bedBlock)
    {
        var visual = (GameObject)PrefabUtility.InstantiatePrefab(source);
        if (visual == null)
            throw new InvalidOperationException("Could not instantiate the Act 2 bed model.");

        visual.name = BedVisualName;
        visual.transform.SetPositionAndRotation(Vector3.zero, Quaternion.Euler(-90f, 0f, 0f));
        visual.transform.localScale = Vector3.one;

        RemoveImportedColliders(visual);

        var bedCollider = bedBlock.GetComponent<BoxCollider>();
        if (bedCollider == null)
            bedCollider = bedBlock.AddComponent<BoxCollider>();

        Bounds target = bedCollider.bounds;
        Bounds sourceBounds = GetRendererBounds(visual);
        float scale = Mathf.Min(
            target.size.x * 0.94f / Mathf.Max(sourceBounds.size.x, 0.001f),
            target.size.z * 0.96f / Mathf.Max(sourceBounds.size.z, 0.001f));

        visual.transform.localScale = Vector3.one * scale;
        AlignToFloorAndCenter(visual, target.center, target.min.y);
        visual.transform.SetParent(bedBlock.transform, true);

        var placeholderRenderer = bedBlock.GetComponent<MeshRenderer>();
        if (placeholderRenderer != null)
            placeholderRenderer.enabled = false;

        ConfigureRenderers(visual, true);
        SetStaticRecursively(visual);
    }

    private static void InstallHouse(GameObject source, Transform parent)
    {
        var visual = (GameObject)PrefabUtility.InstantiatePrefab(source, parent);
        if (visual == null)
            throw new InvalidOperationException("Could not instantiate the Act 2 house-front model.");

        visual.name = HouseVisualName;
        visual.transform.SetPositionAndRotation(Vector3.zero, Quaternion.Euler(-90f, 0f, 0f));
        visual.transform.localScale = Vector3.one;

        RemoveImportedColliders(visual);

        // The generated Act 2 room is 26 x 22 metres. A 20.5-metre facade
        // sits just inside its collision walls and encloses every current task.
        Bounds sourceBounds = GetRendererBounds(visual);
        float scale = 20.5f / Mathf.Max(sourceBounds.size.x, 0.001f);
        visual.transform.localScale = Vector3.one * scale;
        AlignToFloorAndCenter(visual, new Vector3(0f, 0f, 1.25f), 0f);

        // This asset is more than 1.6M triangles. Receiving light but not
        // casting a second full shadow pass keeps it usable in the prototype.
        ConfigureRenderers(visual, false);
        SetStaticRecursively(visual);
    }

    private static void AlignToFloorAndCenter(GameObject visual, Vector3 targetCenter, float targetFloor)
    {
        Bounds bounds = GetRendererBounds(visual);
        Vector3 offset = new Vector3(
            targetCenter.x - bounds.center.x,
            targetFloor - bounds.min.y,
            targetCenter.z - bounds.center.z);
        visual.transform.position += offset;
    }

    private static Bounds GetRendererBounds(GameObject root)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
            throw new InvalidOperationException(root.name + " has no renderer to fit.");

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);
        return bounds;
    }

    private static void RemoveImportedColliders(GameObject root)
    {
        foreach (Collider collider in root.GetComponentsInChildren<Collider>(true))
            UnityEngine.Object.DestroyImmediate(collider);
    }

    private static void ConfigureRenderers(GameObject root, bool castsShadows)
    {
        foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
        {
            renderer.shadowCastingMode = castsShadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
            renderer.receiveShadows = true;
            renderer.lightProbeUsage = LightProbeUsage.BlendProbes;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.BlendProbes;
        }
    }

    private static void SetStaticRecursively(GameObject root)
    {
        StaticEditorFlags flags = StaticEditorFlags.OccluderStatic |
                                  StaticEditorFlags.OccludeeStatic |
                                  StaticEditorFlags.ReflectionProbeStatic;

        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            GameObjectUtility.SetStaticEditorFlags(child.gameObject, flags);
    }

    private static void HideBlockoutWallVisuals()
    {
        // Keep the old BoxColliders as the proven gameplay boundary, but let
        // the imported house supply the visible walls instead of the cubes.
        string[] wallNames = { "BackWall", "LeftWall", "RightWall", "FrontWall" };
        foreach (string wallName in wallNames)
        {
            GameObject wall = GameObject.Find(wallName);
            if (wall != null && wall.TryGetComponent(out MeshRenderer renderer))
                renderer.enabled = false;
        }
    }

    private static void DestroyIfPresent(string objectName)
    {
        GameObject existing = GameObject.Find(objectName);
        if (existing != null)
            UnityEngine.Object.DestroyImmediate(existing);
    }
}
