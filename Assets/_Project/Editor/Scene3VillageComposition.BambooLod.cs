using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

public static partial class Scene3VillageComposition
{
    private const string BambooLodOutput="Tools/Scene3/BambooLod";
    [MenuItem("KuTy/Scene 3/Bamboo imported/7 Apply and validate smooth LOD")]
    public static void ApplySmoothBambooLod()
    {
        RequireArtScene();Directory.CreateDirectory(BambooLodOutput);
        var cover=BambooCover();string plants=EditorJsonUtility.ToJson(cover);var gameplay=GameplaySnapshot();string checkpoint=ReadArtCheckpoint();
        if(!File.Exists(BambooLodOutput+"/before.unity"))
        {
            if(!EditorSceneManager.SaveScene(SceneManager.GetActiveScene(),BambooLodOutput+"/before.unity",true))throw new IOException("Cannot save LOD baseline");
            foreach(int source in new[]{1,2,4})for(int lod=0;lod<3;lod++)
            {
                string name="Bambusa_"+source.ToString("00")+"_LOD"+lod+".asset";
                File.Copy(BambusaModels+"/"+name,BambooLodOutput+"/"+name);
            }
        }
        var report=new StringBuilder();
        foreach(int source in new[]{1,2,4})for(int lod=0;lod<3;lod++)
        {
            var mesh=ReadBambusaMesh(source,lod);
            report.AppendLine(mesh.name+": "+mesh.triangles.Length/3+" triangles");
        }
        AssetDatabase.SaveAssets();cover.Rebuild();
        if(plants!=EditorJsonUtility.ToJson(cover))throw new Exception("Vegetation layout/settings changed");
        VerifyGameplay(gameplay);if(checkpoint!=ReadArtCheckpoint())throw new Exception("Checkpoint changed");
        ValidateBambooLodLogic(report);
        var shader=Shader.Find("KuTy/Village/Imported Bambusa");
        if(!shader || ShaderUtil.GetShaderMessages(shader).Any(m=>m.severity==UnityEditor.Rendering.ShaderCompilerMessageSeverity.Error))throw new Exception("Bamboo shader compile failed");
        CaptureBambooLodMotion(cover,report);
        report.AppendLine("PASS: plant/prototype serialization, gameplay and checkpoint unchanged. No build CPU/GPU timing measured.");
        File.WriteAllText(BambooLodOutput+"/validation.txt",report.ToString());
    }
    private static void ValidateBambooLodLogic(StringBuilder report)
    {
        var flags=BindingFlags.NonPublic|BindingFlags.Static;var type=typeof(VillageGroundCoverRenderer);
        var select=type.GetMethod("SelectBambooLod",flags);var advance=type.GetMethod("AdvanceBambooWeights",flags);
        Func<float,int,int> choose=(distance,previous)=>(int)select.Invoke(null,new object[]{distance,previous,10f,24f});
        if(choose(10.5f,0)!=0 || choose(10.8f,0)!=1 || choose(9.5f,1)!=1 || choose(9.2f,1)!=0 ||
           choose(24.5f,1)!=1 || choose(24.8f,1)!=2 || choose(23.5f,2)!=2 || choose(23.2f,2)!=1)
            throw new Exception("Hysteresis regression");
        Vector3 weights=Vector3.right;
        for(int frame=0;frame<180;frame++)
        {
            int target=frame<6?1:frame<12?0:frame<18?2:1;
            var next=(Vector3)advance.Invoke(null,new object[]{weights,target,1f/60});
            if(Mathf.Abs(next.x+next.y+next.z-1)>.00001f || next.x<0 || next.y<0 || next.z<0 || Vector3.Distance(weights,next)>.060f)
                throw new Exception("Fade continuity/coverage regression");
            weights=next;
        }
        if(Vector3.Distance(weights,Vector3.up)>.00001f)throw new Exception("Fade did not settle");
        report.AppendLine("PASS: both hysteresis bands, reversal during fade, three-way coverage, continuity and exact settling.");
    }
    private static void CaptureBambooLodMotion(VillageGroundCoverRenderer cover,StringBuilder report)
    {
        int index=Array.FindIndex(cover.plants,p=>cover.prototypes[p.prototype].name=="Bambusa_Source02");
        Vector3 origin=cover.plants[index].position;
        var obj=new GameObject("BambooLodMotionCamera"){hideFlags=HideFlags.HideAndDontSave};var cam=obj.AddComponent<Camera>();cam.enabled=false;
        cam.fieldOfView=50;cam.nearClipPlane=.03f;cam.farClipPlane=220;cam.GetUniversalAdditionalCameraData().renderPostProcessing=true;
        var rt=new RenderTexture(1280,720,24);var tex=new Texture2D(1280,720,TextureFormat.RGB24,false);var previous=RenderTexture.active;
        var caches=typeof(VillageGroundCoverRenderer).GetField("cameraLods",BindingFlags.Instance|BindingFlags.NonPublic);
        try
        {
            rt.Create();int saved=0;
            // Simulate 60 Hz in Edit Mode; repeated renders are not a frame-time benchmark.
            for(int frame=0;frame<360;frame++)
            {
                float distance=frame<120?Mathf.Lerp(8,12,frame/119f):frame<240?Mathf.Lerp(12,26,(frame-120)/119f):Mathf.Lerp(26,8,(frame-240)/119f);
                cam.transform.position=origin+new Vector3(0,1.7f,-distance);cam.transform.LookAt(origin+Vector3.up*2.2f);
                var dictionary=(System.Collections.IDictionary)caches.GetValue(cover);
                if(dictionary.Contains(cam))
                {
                    object state=dictionary[cam];state.GetType().GetField("time").SetValue(state,Time.realtimeSinceStartupAsDouble-1.0/60);
                }
                RenderPipeline.SubmitRenderRequest(cam,new UniversalRenderPipeline.SingleCameraRequest{destination=rt});
                var actual=(System.Collections.IDictionary)caches.GetValue(cover);
                if(!actual.Contains(cam))throw new Exception("Camera did not enter the instanced render path");
                var cameraState=actual[cam];var stateType=cameraState.GetType();
                var current=((Vector3[])stateType.GetField("weights").GetValue(cameraState))[index];
                if(Mathf.Abs(current.x+current.y+current.z-1)>.00001f)throw new Exception("Actual rendered LOD coverage invalid");
                if(frame==0 || frame==86 || frame==102 || frame==119 || frame==219 || frame==232 || frame==239 || frame==260 || frame==350)
                {
                    report.AppendLine("Frame "+frame+", distance="+distance.ToString("F2")+", actual LOD weights="+current.ToString("F3")+", cover draws="+cover.LastDrawCalls);
                    RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,1280,720),0,0);tex.Apply();File.WriteAllBytes(BambooLodOutput+"/motion_"+(saved++).ToString("00")+".png",tex.EncodeToPNG());
                }
            }
            report.AppendLine("PASS: rendered 360 camera steps 8→12→26→8 metres, crossing both thresholds in both directions; nine review captures.");
        }
        finally{RenderTexture.active=previous;rt.Release();UnityEngine.Object.DestroyImmediate(tex);UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(obj);cover.Rebuild();}
    }
}
