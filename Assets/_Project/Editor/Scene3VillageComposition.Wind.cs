using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public static partial class Scene3VillageComposition
{
    public static void UpdateRiceGusts()
    {
        var fields=Find(VillageName).GetComponentsInChildren<RuralRiceFieldRenderer>();
        if(fields.Length!=16) throw new Exception("Expected sixteen rice fields.");
        var material=fields[0].sharedMaterial;
        if(fields.Any(f=>f.sharedMaterial!=material)) throw new Exception("Rice fields must share their wind material.");
        if(!material.HasProperty("_GustAmplitude")) throw new Exception("New gust shader not imported yet.");
        Undo.RecordObject(material,"Travelling random gusts through rice fields");
        material.SetFloat("_GustAmplitude",.28f); material.SetFloat("_GustInterval",24);
        material.SetFloat("_GustTravelSpeed",14); material.SetFloat("_GustWidth",10);
        material.SetVector("_WindDirection",new Vector4(.8f,.6f,0,0));
        material.SetFloat("_WindPreviewTime",-1);
        EditorUtility.SetDirty(material); AssetDatabase.SaveAssets();
        CaptureRiceWind(material);
        EditorUtility.SetDirty(material); AssetDatabase.SaveAssets();
        var errors=ShaderUtil.GetShaderMessages(material.shader).Where(m=>m.severity==UnityEditor.Rendering.ShaderCompilerMessageSeverity.Error).ToArray();
        if(errors.Length>0) throw new Exception(string.Join("; ",errors.Select(e=>e.message)));
        if(fields.Any(f=>f.nearDistance!=6 || f.middleDistance!=25)) throw new Exception("LOD distances unexpectedly changed.");
        File.WriteAllText(OutputFolder+"/wind_verification.txt","PASS: Shader compiled after rendering 49 wind frames.\nAll sixteen fields share the gust material.\nLOD thresholds remain 6/25 metres.\nPreview time restored to -1 for live animation.\nNo per-plant objects or physics were added.\n");
        SceneView.RepaintAll();
        Debug.Log("[VillageComposition] Random travelling rice gusts configured and verified.");
    }

    private static void CaptureRiceWind(Material material)
    {
        var folder=OutputFolder+"/Wind"; Directory.CreateDirectory(folder);
        var obj=new GameObject("RiceWindPreviewCamera"){hideFlags=HideFlags.HideAndDontSave};
        var camera=obj.AddComponent<Camera>(); camera.enabled=false;
        camera.transform.SetPositionAndRotation(new Vector3(8,2.1f,-1),Quaternion.LookRotation(new Vector3(22,1,4)-new Vector3(8,2.1f,-1)));
        camera.fieldOfView=60; camera.nearClipPlane=.1f; camera.farClipPlane=180; camera.allowHDR=true;
        camera.GetUniversalAdditionalCameraData().renderPostProcessing=false;
        var destination=new RenderTexture(960,600,24,RenderTextureFormat.ARGB32); destination.Create();
        var texture=new Texture2D(960,600,TextureFormat.RGB24,false);
        var previous=RenderTexture.active;
        try
        {
            for(var frame=0;frame<49;frame++)
            {
                material.SetFloat("_WindPreviewTime",6+frame*.25f);
                RenderPipeline.SubmitRenderRequest(camera,new UniversalRenderPipeline.SingleCameraRequest{destination=destination});
                RenderTexture.active=destination; texture.ReadPixels(new Rect(0,0,960,600),0,0); texture.Apply();
                File.WriteAllBytes(folder+"/wind_"+frame.ToString("00")+".png",texture.EncodeToPNG());
            }
        }
        finally
        {
            material.SetFloat("_WindPreviewTime",-1);
            RenderTexture.active=previous;
            UnityEngine.Object.DestroyImmediate(texture); destination.Release(); UnityEngine.Object.DestroyImmediate(destination);
            UnityEngine.Object.DestroyImmediate(obj);
        }
    }
}
