using System;
using UnityEditor;
using UnityEngine;

public static class HouseholdWaterLookSetup
{
    [MenuItem("KuTy/Scene 2/Polish household water materials")]
    public static void Apply()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode first.");
        var shader = Shader.Find("KuTy/Household Water");
        if (!shader || !shader.isSupported || ShaderUtil.ShaderHasError(shader)) throw new InvalidOperationException("Household water shader failed to compile.");
        var data = AssetDatabase.LoadAssetAtPath<WaterRiceAssets>(WaterRiceAssetSetup.DataPath);
        if (!data || !data.waterMaterial) throw new InvalidOperationException("Import WaterRice first.");
        Undo.RecordObject(data.waterMaterial, "Polish household water");
        data.waterMaterial.shader = shader; data.waterMaterial.shaderKeywords = new string[0];
        data.waterMaterial.SetColor("_BaseColor", new Color(.30f,.40f,.36f,.32f));
        data.waterMaterial.SetFloat("_Smoothness", .94f); data.waterMaterial.SetFloat("_WaveStrength", .016f);
        data.waterMaterial.SetVector("_Ripple", new Vector4(0,0,-100,0)); data.waterMaterial.renderQueue = 3000;
        EditorUtility.SetDirty(data.waterMaterial); AssetDatabase.SaveAssets();
        Debug.Log("[Household water] Clearer water, Fresnel/environment reflection, subtle animated normals and local interaction ripples. No opaque/depth texture or global pipeline changes required.");
    }
}
