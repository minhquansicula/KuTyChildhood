using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Keeps the office gameplay collider and interaction anchors while replacing only the desk visuals.
public static class KuTyOfficeDeskAsset
{
    private const string ScenePath = "Assets/_Project/Scenes/Act1_RealWorld.unity";
    private const string ModelPath = "Assets/_Project/Art/Models/office-desk.fbx";
    private const string MaterialPath = "Assets/_Project/Art/Materials/OfficeDesk_Imported.mat";
    private const string InstanceName = "OfficeDeskModel_Imported";

    [MenuItem("KuTy/Assets/Place imported office desk in Act 1")]
    public static void PlaceFromMenu()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Exit Play Mode before editing the office scene.");
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        Place();
    }

    public static void Place()
    {
        var modelAsset = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
        var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        if (modelAsset == null || material == null)
            throw new InvalidOperationException("Import the office desk FBX and material first.");

        var scene = EditorSceneManager.OpenScene(ScenePath);
        var deskRoot = GameObject.Find("OfficeDesk");
        if (deskRoot == null || deskRoot.GetComponent<BoxCollider>() == null)
            throw new InvalidOperationException("The Act 1 OfficeDesk gameplay anchor is missing.");

        var existing = deskRoot.transform.Find(InstanceName);
        if (existing != null)
        {
            Debug.Log("Imported office desk is already placed; leaving its scene adjustments intact.");
            return;
        }

        var instance = (GameObject)PrefabUtility.InstantiatePrefab(modelAsset, scene);
        instance.name = InstanceName;
        instance.transform.SetParent(deskRoot.transform, false);
        instance.transform.localPosition = Vector3.zero;
        instance.transform.localRotation = Quaternion.identity;
        instance.transform.localScale = Vector3.one;

        var renderers = instance.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
            throw new InvalidOperationException("The imported office desk has no renderers.");
        foreach (var renderer in renderers)
            renderer.sharedMaterial = material;

        // The provided model's source units are arbitrary. Fit it to the existing desk footprint.
        var bounds = GetBounds(renderers);
        var desiredSize = new Vector3(2.65f, .88f, 1.25f);
        instance.transform.localScale = new Vector3(
            desiredSize.x / Mathf.Max(bounds.size.x, .001f),
            desiredSize.y / Mathf.Max(bounds.size.y, .001f),
            desiredSize.z / Mathf.Max(bounds.size.z, .001f));
        bounds = GetBounds(renderers);
        instance.transform.position += new Vector3(
            deskRoot.transform.position.x - bounds.center.x,
            -bounds.min.y,
            deskRoot.transform.position.z - bounds.center.z);

        // The existing BoxCollider remains the sole desk collider so interaction/raycast behavior is unchanged.
        foreach (var collider in instance.GetComponentsInChildren<Collider>(true))
            collider.enabled = false;
        foreach (Transform child in deskRoot.transform)
            if (child.name.StartsWith("DeskTopPlaceholder", StringComparison.Ordinal) ||
                child.name.StartsWith("DeskLegPlaceholder", StringComparison.Ordinal))
                child.gameObject.SetActive(false);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("KUTY_OFFICE_DESK_IMPORT_PASSED: model=" + ModelPath + ", fittedSize=" + GetBounds(renderers).size);
    }

    private static Bounds GetBounds(Renderer[] renderers)
    {
        var bounds = renderers[0].bounds;
        for (var i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);
        return bounds;
    }
}
