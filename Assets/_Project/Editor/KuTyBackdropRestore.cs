using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class KuTyBackdropRestore
{
    private const string ScenePath = "Assets/_Project/Scenes/Act1_Office_Blockout.unity";
    private const string BackupPath = "Assets/_Project/Scenes/Backups/Office_BeforeBackdropRestore.unity";
    private const string CityMaterialPath = "Assets/_Project/Art/Environment/City/Materials/MAT_City_material.mat";
    private const string HorizonMaterialPath = "Assets/_Project/Art/Environment/City/Materials/MAT_SunsetHorizon.mat";
    private const string OriginalHorizonTexturePath = "Assets/_Project/Art/Environment/City/Textures/T_SunsetHorizon_Original.png";

    [MenuItem("KuTy/Office Intro/Restore Reference Backdrop")]
    public static void Restore()
    {
        if (EditorApplication.isPlaying)
            throw new InvalidOperationException("Restore the backdrop in Edit Mode.");

        var scene = SceneManager.GetActiveScene();
        if (scene.path != ScenePath)
            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        EditorSceneManager.SaveScene(scene);
        EnsureFolder("Assets/_Project/Scenes/Backups");
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(BackupPath))
            AssetDatabase.DeleteAsset(BackupPath);
        if (!AssetDatabase.CopyAsset(ScenePath, BackupPath))
            throw new InvalidOperationException("Could not create the backdrop restore safety copy.");

        var exterior = GameObject.Find("OFFICE_BLOCKOUT/05_Exterior_Backdrop");
        if (!exterior)
            throw new InvalidOperationException("05_Exterior_Backdrop was not found.");
        Undo.RecordObject(exterior, "Restore reference backdrop");
        exterior.SetActive(true);

        var citySlot = exterior.transform.Find("CitySilhouette_SLOT");
        var horizon = exterior.transform.Find("SunsetHorizon_Backdrop");
        if (!citySlot || !horizon)
            throw new InvalidOperationException("The city slot or sunset horizon is missing.");

        Undo.RecordObject(citySlot.gameObject, "Restore reference backdrop");
        Undo.RecordObject(horizon.gameObject, "Restore reference backdrop");
        citySlot.gameObject.SetActive(true);
        horizon.gameObject.SetActive(true);

        foreach (Transform child in citySlot)
        {
            if (child.name.StartsWith("ImportedCity_Exterior", StringComparison.Ordinal))
            {
                Undo.RecordObject(child.gameObject, "Restore reference backdrop");
                child.gameObject.SetActive(true);
            }
            else if (child.name == "Visual_REPLACE")
            {
                Undo.RecordObject(child.gameObject, "Restore reference backdrop");
                child.gameObject.SetActive(false);
            }
        }

        var cityMaterial = AssetDatabase.LoadAssetAtPath<Material>(CityMaterialPath);
        if (!cityMaterial)
            throw new InvalidOperationException("MAT_City_material was not found.");
        Undo.RecordObject(cityMaterial, "Restore city backdrop material");
        cityMaterial.DisableKeyword("_EMISSION");
        cityMaterial.SetTexture("_EmissionMap", null);
        cityMaterial.SetColor("_EmissionColor", Color.black);
        cityMaterial.SetColor("_BaseColor", Color.white);
        cityMaterial.SetColor("_Color", Color.white);
        cityMaterial.globalIlluminationFlags = MaterialGlobalIlluminationFlags.EmissiveIsBlack;
        EditorUtility.SetDirty(cityMaterial);

        var horizonMaterial = AssetDatabase.LoadAssetAtPath<Material>(HorizonMaterialPath);
        var originalHorizon = AssetDatabase.LoadAssetAtPath<Texture2D>(OriginalHorizonTexturePath);
        if (!horizonMaterial || !originalHorizon)
            throw new InvalidOperationException("The original sunset horizon material or texture is missing.");
        Undo.RecordObject(horizonMaterial, "Restore original sunset backdrop");
        horizonMaterial.SetTexture("_BaseMap", originalHorizon);
        horizonMaterial.SetTexture("_MainTex", originalHorizon);
        horizonMaterial.SetColor("_BaseColor", Color.white);
        horizonMaterial.SetColor("_Color", Color.white);
        EditorUtility.SetDirty(horizonMaterial);

        // Rebuild the three atmospheric layers. The restored reference uses these
        // layers to hide the lower city gaps and soften the distant skyline.
        KuTyCityBackdropImporter.AddCityCloudBank();

        scene = SceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(scene);
        AssetDatabase.SaveAssets();
        EditorSceneManager.SaveScene(scene);
        KuTyCityBackdropImporter.CaptureCityPreview();
        Debug.Log("OFFICE_REFERENCE_BACKDROP_RESTORED: sunset horizon, city, and 3 cloud layers; city window emission disabled.");
    }

    private static void EnsureFolder(string path)
    {
        var parts = path.Split('/');
        var current = parts[0];
        for (var i = 1; i < parts.Length; i++)
        {
            var next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }
}
