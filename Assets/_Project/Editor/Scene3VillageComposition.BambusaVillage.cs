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

public static partial class Scene3VillageComposition
{
    private const string BambusaVillageOutput=BambusaWork+"/WholeVillage";
    private const string BambusaVillageMarker="BambusaImported_WholeVillage";
    private static bool LegacyBambooMesh(Mesh mesh)=>mesh &&
        (mesh.name.StartsWith("TreTanXumXe") || mesh.name.StartsWith("BuiTre_VietNam") || mesh.name.StartsWith("TreLaMem_"));
    private static MeshRenderer[] ActiveLegacyBambooRenderers()=>AllTransforms().Select(t=>t.GetComponent<MeshRenderer>())
        .Where(r=>r && r.enabled && r.gameObject.activeInHierarchy && r.GetComponent<MeshFilter>() && LegacyBambooMesh(r.GetComponent<MeshFilter>().sharedMesh)).ToArray();

    [MenuItem("KuTy/Scene 3/Bamboo imported/5 Replace remaining village bamboo")]
    public static void ReplaceWholeVillageBamboo()
    {
        RequireArtScene();if(Find(BambusaVillageMarker))throw new Exception("Whole-village bamboo replacement is already applied.");
        var cover=BambooCover();var prototypes=cover.prototypes;
        var legacy=prototypes.Select((p,i)=>new{p,i}).Where(x=>LegacyBambooMesh(x.p.near)).Select(x=>x.i).ToArray();
        int[] sources={1,2,4};
        var imported=sources.Select(source=>Array.FindIndex(prototypes,p=>p.name=="Bambusa_Source"+source.ToString("00"))).ToArray();
        if(imported.Any(i=>i<0))throw new Exception("Prepare the three imported variants first.");
        int count=cover.plants.Count(p=>legacy.Contains(p.prototype));
        if(count==0)throw new Exception("No remaining procedural bamboo instances found.");
        // Older non-instanced bamboo must already be inactive. Refuse ambiguous leftovers
        // instead of silently deleting scene hierarchies or their collision boundaries.
        if(ActiveLegacyBambooRenderers().Length>0)throw new Exception("Active non-instanced procedural bamboo requires inspection before replacement.");
        Directory.CreateDirectory(BambusaVillageOutput);
        string baseline=EditorJsonUtility.ToJson(cover,true);
        if(!File.Exists(BambusaVillageOutput+"/before.unity"))
        {
            if(!EditorSceneManager.SaveScene(SceneManager.GetActiveScene(),BambusaVillageOutput+"/before.unity",true))throw new IOException("Cannot save whole-village baseline.");
            File.WriteAllText(BambusaVillageOutput+"/cover-before.json",baseline);
            File.WriteAllText(BambusaVillageOutput+"/checkpoint-before.txt",ReadArtCheckpoint());
            CaptureWholeVillageBamboo("Before");
        }
        else if(File.ReadAllText(BambusaVillageOutput+"/cover-before.json")!=baseline)throw new Exception("Current vegetation differs from the existing baseline.");
        var before=GameplaySnapshot();before.Remove(cover);string checkpoint=ReadArtCheckpoint();
        var sky=RenderSettings.skybox;var sun=Find("Sun").transform.rotation;
        var plants=cover.plants.ToArray();
        var bottoms=imported.Select(id=>prototypes[id].near.vertices.Min(v=>v.y)).ToArray();
        var heights=imported.Select(id=>prototypes[id].near.vertices.Max(v=>v.y)).Zip(bottoms,(top,bottom)=>top-bottom).ToArray();
        for(int i=0;i<plants.Length;i++)
        {
            var plant=plants[i];if(!legacy.Contains(plant.prototype))continue;
            float oldHeight=prototypes[plant.prototype].near.bounds.size.y*plant.scale;
            uint hash=unchecked((uint)(Mathf.RoundToInt(plant.position.x*100)*73856093 ^ Mathf.RoundToInt(plant.position.z*100)*19349663));
            int variant=hash%8==0?0:hash%2==0?1:2;
            float height=variant==0?Mathf.Clamp(oldHeight*.5f,2.9f,3.8f):variant==1?Mathf.Clamp(oldHeight*.73f,4.2f,5.5f):Mathf.Clamp(oldHeight*.82f,4.8f,6.3f);
            plant.prototype=imported[variant];plant.scale=height/heights[variant];
            var p=plant.position;p.y=ReferenceHeight(p.x,p.z)-.025f-bottoms[variant]*plant.scale;plant.position=p;plants[i]=plant;
        }
        Undo.IncrementCurrentGroup();int undo=Undo.GetCurrentGroup();Undo.SetCurrentGroupName("Replace remaining village bamboo with imported models");
        Undo.RecordObject(cover,"Replace all remaining bamboo instances");
        try
        {
            cover.plants=plants;cover.Rebuild();EditorUtility.SetDirty(cover);Group(BambusaVillageMarker,null);
            VerifyGameplay(before);
            if(ReadArtCheckpoint()!=checkpoint || RenderSettings.skybox!=sky || Find("Sun").transform.rotation!=sun)throw new Exception("Checkpoint, sky or sun changed.");
            ValidateWholeVillageBamboo();VerifyReferenceWalking();VerifyBambooSampleWalk();
            AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            if(!EditorSceneManager.SaveScene(SceneManager.GetActiveScene()))throw new IOException("Cannot save village bamboo replacement.");
            Undo.CollapseUndoOperations(undo);
        }
        catch{Undo.RevertAllDownToGroup(undo);cover.Rebuild();throw;}
        File.WriteAllText(BambusaVillageOutput+"/installation.txt","PASS: replaced "+count+" remaining procedural bamboo instances.\nExisting seven imported sample plants retained. All bamboo now uses downloaded source groups 1, 2, 4.\nGameplay script payloads unchanged: "+before.Count+". Sky, sun, checkpoint unchanged.\nReference crossings and sample lane walking checks passed.\n");
        SceneView.RepaintAll();
    }

    [MenuItem("KuTy/Scene 3/Bamboo imported/6 Validate and capture whole village")]
    public static void CaptureWholeVillageBambooComparison()
    {
        RequireArtScene();if(!Find(BambusaVillageMarker))throw new Exception("Whole-village replacement not installed.");
        ValidateWholeVillageBamboo();VerifyReferenceWalking();VerifyBambooSampleWalk();CaptureWholeVillageBamboo("After");
    }

    private static void ValidateWholeVillageBamboo()
    {
        var cover=BambooCover();
        var baseline=JsonUtility.FromJson<BambooBaselineEnvelope>(File.ReadAllText(BambusaVillageOutput+"/cover-before.json")).MonoBehaviour;
        if(cover.plants.Length!=baseline.plants.Length || cover.nearDistance!=baseline.nearDistance || cover.middleDistance!=baseline.middleDistance || cover.farDistance!=baseline.farDistance)
            throw new Exception("Plant count or shared LOD distances changed.");
        int replaced=0,retained=0;var report=new StringBuilder();
        for(int i=0;i<cover.plants.Length;i++)
        {
            var plant=cover.plants[i];var old=baseline.plants[i];var prototype=cover.prototypes[plant.prototype];
            if(LegacyBambooMesh(prototype.near))throw new Exception("Procedural bamboo instance still active.");
            if(plant.Equals(old)){retained++;continue;}
            if(prototype.name==null || !prototype.name.StartsWith("Bambusa_Source") || plant.position.x!=old.position.x || plant.position.z!=old.position.z || plant.yaw!=old.yaw || plant.paddy!=old.paddy)
                throw new Exception("Unexpected vegetation change at "+i);
            float bottom=prototype.near.vertices.Min(v=>v.y)*plant.scale+plant.position.y;
            if(Mathf.Abs(bottom-(ReferenceHeight(plant.position.x,plant.position.z)-.025f))>.001f)throw new Exception("Imported bamboo not grounded.");
            replaced++;
        }
        if(ActiveLegacyBambooRenderers().Length>0)throw new Exception("Procedural bamboo renderer remains visible.");
        foreach(var group in cover.plants.GroupBy(p=>p.prototype).Where(g=>cover.prototypes[g.Key].name.StartsWith("Bambusa_Source")))
            report.AppendLine(cover.prototypes[group.Key].name+": "+group.Count()+" instances.");
        if(ReadArtCheckpoint()!=File.ReadAllText(BambusaVillageOutput+"/checkpoint-before.txt"))throw new Exception("Checkpoint changed since baseline.");
        report.AppendLine("PASS: replaced="+replaced+", unchanged="+retained+" (grass plus previous imported sample). Zero visible procedural bamboo instances/renderers.\nPositions XZ/yaw and shared LOD distances retained; replaced plants grounded. Checkpoint unchanged.");
        File.WriteAllText(BambusaVillageOutput+"/validation.txt",report.ToString());
    }

    private static void CaptureWholeVillageBamboo(string phase)
    {
        string folder=BambusaVillageOutput+"/"+phase;Directory.CreateDirectory(folder);
        Vector3[] positions={new Vector3(1.5f,1.8f,34.5f),new Vector3(-7,1.8f,12),new Vector3(-29,1.8f,36),new Vector3(-6,1.8f,63),new Vector3(47,41,-27)};
        Vector3[] targets={new Vector3(6.6f,2.9f,34.5f),new Vector3(5,2.5f,16),new Vector3(-37,3,43),new Vector3(-18,3,72),new Vector3(-5,0,32)};
        var cover=BambooCover();var stats=new StringBuilder();
        for(int i=0;i<positions.Length;i++)
        {
            var obj=new GameObject("WholeVillageBambooCapture"){hideFlags=HideFlags.HideAndDontSave};var camera=obj.AddComponent<Camera>();
            camera.enabled=false;camera.fieldOfView=65;camera.nearClipPlane=.03f;camera.farClipPlane=300;camera.allowHDR=true;
            camera.transform.SetPositionAndRotation(positions[i],Quaternion.LookRotation(targets[i]-positions[i]));
            camera.GetUniversalAdditionalCameraData().renderPostProcessing=true;
            var rt=new RenderTexture(1600,900,24);var image=new Texture2D(1600,900,TextureFormat.RGB24,false);var previous=RenderTexture.active;
            try
            {
                rt.Create();RenderPipeline.SubmitRenderRequest(camera,new UniversalRenderPipeline.SingleCameraRequest{destination=rt});
                RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,1600,900),0,0);image.Apply();File.WriteAllBytes(folder+"/"+(i+1).ToString("00")+".png",image.EncodeToPNG());
                stats.AppendLine("View "+i+": visible ground-cover instances="+cover.LastVisiblePlants+", cover draws="+cover.LastDrawCalls+"; not build frame timing.");
            }
            finally{RenderTexture.active=previous;rt.Release();UnityEngine.Object.DestroyImmediate(image);UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(obj);}
        }
        File.WriteAllText(folder+"/render-stats.txt",stats.ToString());
    }
}
