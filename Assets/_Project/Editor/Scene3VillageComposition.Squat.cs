using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

public static partial class Scene3VillageComposition
{
    private const string SquatOutput="Tools/Scene3/ChildSquat";
    [MenuItem("KuTy/Scene 3/Art direction/4 Correct child squat")]
    public static void CorrectChildSquat()
    {
        RequireArtScene();Directory.CreateDirectory(SquatOutput);
        var before=GameplaySnapshot();string checkpoint=ReadArtCheckpoint();
        if(!File.Exists(SquatOutput+"/before.unity"))
        {
            if(!EditorSceneManager.SaveScene(SceneManager.GetActiveScene(),SquatOutput+"/before.unity",true))throw new IOException("Backup failed");
            for(int i=0;i<3;i++)if(!AssetDatabase.CopyAsset(ArtAssets+"/TreNgoiXom_"+i+".asset",ArtAssets+"/TreNgoiXom_"+i+"_BeforeSquat.asset"))throw new IOException("Mesh backup failed");
            CaptureSquat("Before");
        }
        for(int i=0;i<3;i++)ArtCrouchMesh(i,true);
        AssetDatabase.SaveAssets();ValidateArtSample();VerifyGameplay(before);
        if(checkpoint!=ReadArtCheckpoint())throw new Exception("Checkpoint changed");
        VerifyArtWalking(ArtRingCenter());
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        CaptureSquat("After");
        File.WriteAllText(SquatOutput+"/validation.txt","PASS: three squat mesh assets updated in place; original meshes backed up.\nGrounding/scale, gameplay serialization and checkpoint unchanged; marble yard walking passed.\n");
    }
    private static Mesh ArtSpectatorMesh()
    {
        const string path=ArtAssets+"/TreDungXem_TayThaLong.asset";
        var existing=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(existing)return existing;
        var source=NormalizedChildMesh();var pose=new ChildPose();
        ArmBones(-1,false,false,out pose.leftUpper,out pose.leftLower);
        ArmBones(1,false,false,out pose.rightUpper,out pose.rightLower);
        LegBones(-1,out pose.leftThigh,out pose.leftCalf);LegBones(1,out pose.rightThigh,out pose.rightCalf);
        var mesh=UnityEngine.Object.Instantiate(source);mesh.name="TreDungXem_TayThaLong";
        var vertices=source.vertices;var normals=source.normals;
        for(int i=0;i<vertices.Length;i++){var p=vertices[i];vertices[i]=pose.Vertex(p);normals[i]=pose.Normal(p,normals[i]);}
        float bottom=vertices.Min(v=>v.y),height=vertices.Max(v=>v.y)-bottom;
        for(int i=0;i<vertices.Length;i++)vertices[i]=new Vector3(vertices[i].x,vertices[i].y-bottom,vertices[i].z)*(1.2f/height);
        mesh.vertices=vertices;mesh.normals=normals;mesh.tangents=new Vector4[0];mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,path);return mesh;
    }

    [MenuItem("KuTy/Scene 3/Art direction/5 Relax spectator arms")]
    public static void RelaxSpectatorArms()
    {
        RequireArtScene();const string output="Tools/Scene3/ChildSpectator";Directory.CreateDirectory(output);
        var before=GameplaySnapshot();string checkpoint=ReadArtCheckpoint();
        var child=Find("BanChoiBi_4").GetComponent<MeshFilter>();
        if(!File.Exists(output+"/before.unity"))
        {
            if(!EditorSceneManager.SaveScene(SceneManager.GetActiveScene(),output+"/before.unity",true))throw new IOException("Backup failed");
            File.WriteAllText(output+"/original-mesh.txt",AssetDatabase.GetAssetPath(child.sharedMesh));
            CaptureSquat("Before","BanChoiBi_4",output,.60f,2.7f);
        }
        Undo.RecordObject(child,"Relax standing spectator arms");child.sharedMesh=ArtSpectatorMesh();EditorUtility.SetDirty(child);
        ValidateArtSample();VerifyGameplay(before);VerifyArtWalking(ArtRingCenter());
        if(checkpoint!=ReadArtCheckpoint())throw new Exception("Checkpoint changed");
        AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        CaptureSquat("After","BanChoiBi_4",output,.60f,2.7f);
        File.WriteAllText(output+"/validation.txt","PASS: spectator uses a separate mesh with both arms relaxed. Shared shoulder-linked crowd meshes untouched.\nActor scale/grounding, gameplay serialization, checkpoint and yard walking passed.\n");
    }

    private static void CaptureSquat(string phase,string childName="BanChoiBi_1",string output=SquatOutput,float centerHeight=.43f,float distance=2.05f)
    {
        string folder=output+"/"+phase;Directory.CreateDirectory(folder);
        var child=Find(childName).transform;
        Vector3[] directions={child.right,child.forward,(child.forward+child.right).normalized};
        for(int i=0;i<directions.Length;i++)
        {
            var obj=new GameObject("SquatReview"){hideFlags=HideFlags.HideAndDontSave};var cam=obj.AddComponent<Camera>();
            cam.enabled=false;cam.fieldOfView=38;cam.nearClipPlane=.03f;cam.farClipPlane=200;
            Vector3 target=child.position+Vector3.up*centerHeight;
            cam.transform.position=target+directions[i]*distance+Vector3.up*.1f;cam.transform.LookAt(target);
            cam.GetUniversalAdditionalCameraData().renderPostProcessing=true;
            var rt=new RenderTexture(900,900,24);var tex=new Texture2D(900,900,TextureFormat.RGB24,false);var previous=RenderTexture.active;
            try{rt.Create();RenderPipeline.SubmitRenderRequest(cam,new UniversalRenderPipeline.SingleCameraRequest{destination=rt});RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,900,900),0,0);tex.Apply();File.WriteAllBytes(folder+"/"+i+".png",tex.EncodeToPNG());}
            finally{RenderTexture.active=previous;rt.Release();UnityEngine.Object.DestroyImmediate(tex);UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(obj);}
        }
    }
}
