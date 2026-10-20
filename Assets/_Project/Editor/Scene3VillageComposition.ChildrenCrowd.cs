using System;
using System.Collections.Generic;
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
    private const string CrowdAssets=ActorAssets+"/ChildrenCrowd";
    private const string CrowdOutput=ActorOutput+"/ShoulderGroups";
    private const string CrowdMarker="TreKhoacVai_12Be";
    private struct PoseBone
    {
        public Vector3 source,target;public Quaternion rotation;
        public Vector3 Point(Vector3 p)=>target+rotation*(p-source);
        public Vector3 Direction(Vector3 n)=>rotation*n;
    }
    private sealed class ChildPose
    {
        public PoseBone leftUpper,leftLower,rightUpper,rightLower,leftThigh,leftCalf,rightThigh,rightCalf;
        public Vector3 Vertex(Vector3 p)
        {
            float ax=Mathf.Abs(p.x),arm=Smooth(.15f,.25f,ax)*Smooth(.54f,.59f,p.y)*(1-Smooth(.86f,.93f,p.y));
            if(arm>0){var upper=p.x<0?leftUpper:rightUpper;var lower=p.x<0?leftLower:rightLower;float elbow=Smooth(.30f,.405f,ax);return Vector3.Lerp(p,Vector3.Lerp(upper.Point(p),lower.Point(p),elbow),arm);}
            float leg=1-Smooth(.45f,.56f,p.y);if(leg>0){var upper=p.x<0?leftThigh:rightThigh;var lower=p.x<0?leftCalf:rightCalf;return Vector3.Lerp(p,Vector3.Lerp(upper.Point(p),lower.Point(p),1-Smooth(.23f,.35f,p.y)),leg);}return p;
        }
        public Vector3 Normal(Vector3 p,Vector3 n)
        {
            float ax=Mathf.Abs(p.x),arm=Smooth(.15f,.25f,ax)*Smooth(.54f,.59f,p.y)*(1-Smooth(.86f,.93f,p.y));
            if(arm>0){var upper=p.x<0?leftUpper:rightUpper;var lower=p.x<0?leftLower:rightLower;return Vector3.Lerp(n,Vector3.Lerp(upper.Direction(n),lower.Direction(n),Smooth(.30f,.405f,ax)),arm).normalized;}
            float leg=1-Smooth(.45f,.56f,p.y);if(leg>0){var upper=p.x<0?leftThigh:rightThigh;var lower=p.x<0?leftCalf:rightCalf;return Vector3.Lerp(n,Vector3.Lerp(upper.Direction(n),lower.Direction(n),1-Smooth(.23f,.35f,p.y)),leg).normalized;}return n;
        }
    }
    private static float Smooth(float a,float b,float v)=>Mathf.SmoothStep(0,1,Mathf.InverseLerp(a,b,v));
    private static void ArmBones(int side,bool raised,bool behind,out PoseBone upper,out PoseBone lower)
    {
        var shoulder=new Vector3(side*.17f,.813f,-.025f);var elbow=new Vector3(side*.365f,.745f,-.015f);var wrist=new Vector3(side*.49f,.675f,.005f);
        var upperDirection=raised?new Vector3(side*.19f,.035f,behind?-.085f:.06f):new Vector3(side*.018f,-.20f,.012f);
        var lowerDirection=raised?new Vector3(side*.13f,-.014f,behind?.07f:-.005f):new Vector3(side*.015f,-.14f,.04f);
        var a=Quaternion.FromToRotation(elbow-shoulder,upperDirection);var newElbow=shoulder+a*(elbow-shoulder);
        upper=new PoseBone{source=shoulder,target=shoulder,rotation=a};lower=new PoseBone{source=elbow,target=newElbow,rotation=Quaternion.FromToRotation(wrist-elbow,lowerDirection)};
    }
    private static void LegBones(int side,out PoseBone upper,out PoseBone lower)
    {
        var hip=new Vector3(side*.105f,.505f,-.045f);var knee=new Vector3(side*(side<0?.245f:.255f),.285f,.015f);var ankle=new Vector3(side*(side<0?.315f:.365f),.065f,-.065f);
        var a=Quaternion.FromToRotation(knee-hip,new Vector3(side*.008f,-.26f,.028f));var newKnee=hip+a*(knee-hip);
        upper=new PoseBone{source=hip,target=hip,rotation=a};lower=new PoseBone{source=knee,target=newKnee,rotation=Quaternion.FromToRotation(ankle-knee,new Vector3(side*.008f,-.25f,-.035f))};
    }
    private static Mesh ShoulderPoseMesh(int variant)
    {
        string path=CrowdAssets+"/Tre_KhoacVai_"+variant+".asset";var existing=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(existing)return existing;
        var source=NormalizedChildMesh();var pose=new ChildPose();
        ArmBones(-1,variant!=0,variant==1,out pose.leftUpper,out pose.leftLower);ArmBones(1,variant!=2,variant==1,out pose.rightUpper,out pose.rightLower);
        LegBones(-1,out pose.leftThigh,out pose.leftCalf);LegBones(1,out pose.rightThigh,out pose.rightCalf);
        var mesh=UnityEngine.Object.Instantiate(source);mesh.name="Tre_KhoacVai_"+variant;var vertices=source.vertices;var normals=source.normals;var tangents=source.tangents;
        for(int i=0;i<vertices.Length;i++)
        {
            var p=vertices[i];vertices[i]=pose.Vertex(p);normals[i]=pose.Normal(p,normals[i]);if(tangents.Length==vertices.Length){var d=pose.Normal(p,new Vector3(tangents[i].x,tangents[i].y,tangents[i].z));tangents[i]=new Vector4(d.x,d.y,d.z,tangents[i].w);}
        }
        float bottom=vertices.Min(v=>v.y),top=vertices.Max(v=>v.y),scale=1.2f/(top-bottom);
        for(int i=0;i<vertices.Length;i++)vertices[i]=new Vector3(vertices[i].x,vertices[i].y-bottom,vertices[i].z)*scale;
        mesh.vertices=vertices;mesh.normals=normals;if(tangents.Length==vertices.Length)mesh.tangents=tangents;mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,path);return mesh;
    }

    public static void BatchInstallShoulderCrowd()
    {
        try
        {
            EditorSceneManager.OpenScene(ScenePath);InstallShoulderCrowd();DynamicGI.UpdateEnvironment();double ready=EditorApplication.timeSinceStartup+6;EditorApplication.CallbackFunction capture=null;
            capture=()=>{if(EditorApplication.timeSinceStartup<ready)return;EditorApplication.update-=capture;try{CaptureVillageActors();CaptureShoulderGroup();EditorSceneManager.SaveScene(SceneManager.GetActiveScene());Inspect(true);EditorApplication.Exit(0);}catch(Exception e){Debug.LogException(e);File.WriteAllText(CrowdOutput+"/error.txt",e.ToString());EditorApplication.Exit(1);}};EditorApplication.update+=capture;
        }
        catch(Exception e){Debug.LogException(e);Directory.CreateDirectory(CrowdOutput);File.WriteAllText(CrowdOutput+"/error.txt",e.ToString());EditorApplication.Exit(1);}
    }
    [MenuItem("KuTy/Scene 3/Add twelve children with arms around shoulders")]
    public static void InstallShoulderCrowd()
    {
        var scene=SceneManager.GetActiveScene();if(scene.path!=ScenePath || EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Open the outside village scene in Edit Mode.");
        Directory.CreateDirectory(CrowdOutput);string backup=OutputFolder+"/before-children-shoulder-crowd-20261018.unity";if(!File.Exists(backup) && !EditorSceneManager.SaveScene(scene,backup,true))throw new Exception("Cannot save pre-crowd backup.");
        var before=GameplaySnapshot();Undo.RegisterFullObjectHierarchyUndo(Find("BaiBanBi_VaNhomTre"),"More children, arms around shoulders");
        var marker=Find(CrowdMarker);if(!marker)marker=Group(CrowdMarker,Find(ActorsMarker).transform);
        var report=new StringBuilder();PlaceShoulderCrowd(AssetDatabase.LoadAssetAtPath<Material>(ActorAssets+"/trecon_Mat.mat"),report);Physics.SyncTransforms();VerifyGameplay(before);ValidateVillageActors(report);ValidateShoulderContacts(report);
        AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);File.WriteAllText(CrowdOutput+"/report.txt",report.ToString());
        for(int i=0;i<3;i++)CaptureChildMeshInspect(ShoulderPoseMesh(i),"pose_"+i);
        if(File.Exists(CrowdOutput+"/error.txt"))File.Delete(CrowdOutput+"/error.txt");File.WriteAllText(CrowdOutput+"/result.json","{\"passed\":true,\"children\":12,\"groups\":4,\"poses\":3}");
    }
    private static void PlaceShoulderCrowd(Material material,StringBuilder report)
    {
        var area=Find("BaiBanBi_VaNhomTre").transform;var game=area.GetComponentInChildren<MarbleShootingGame>();var data=new SerializedObject(game);
        var ring=(Collider)data.FindProperty("ringCollider").objectReferenceValue;var floor=(Collider)data.FindProperty("marbleGround").objectReferenceValue;var center=ring.transform.position;center.y=floor.bounds.max.y;
        var kids=area.Find("TreMoi_QuanhSanBi");if(!kids)kids=Group("TreMoi_QuanhSanBi",area).transform;
        float[] angles={45,130,220,305},heights={1.20f,1.18f,1.25f,1.22f};
        for(int g=0;g<4;g++)
        {
            float a=angles[g]*Mathf.Deg2Rad;var middle=center+new Vector3(Mathf.Cos(a)*3.05f,0,Mathf.Sin(a)*3.05f);var forward=(center-middle).normalized;var rotation=Quaternion.LookRotation(forward);var right=rotation*Vector3.right;
            for(int j=0;j<3;j++)
            {
                int index=g*3+j+1;var holder=ActorVisual(kids,"TreChoibi_"+index,2,material);
                foreach(Transform old in holder)if(old.name!="DangKhoacVai")old.gameObject.SetActive(false);
                var visual=holder.Find("DangKhoacVai");if(!visual)visual=MeshProp(holder,"DangKhoacVai",holder.position,ShoulderPoseMesh(j),material).transform;
                visual.localPosition=Vector3.zero;visual.localRotation=Quaternion.identity;visual.localScale=Vector3.one;visual.GetComponent<MeshFilter>().sharedMesh=ShoulderPoseMesh(j);visual.GetComponent<Renderer>().sharedMaterial=material;visual.GetComponent<Renderer>().shadowCastingMode=ShadowCastingMode.On;
                float scale=heights[g]/1.2f;var foot=middle+right*((j-1)*.37f*scale);holder.SetPositionAndRotation(foot,rotation);holder.localScale=Vector3.one*scale;
                var capsule=holder.GetComponent<CapsuleCollider>();if(!capsule)capsule=Undo.AddComponent<CapsuleCollider>(holder.gameObject);capsule.center=new Vector3(0,.55f,0);capsule.height=1.1f;capsule.radius=.16f;
                report.AppendLine(holder.name+": group "+(g+1)+", pose "+j+", height "+heights[g]+", foot "+foot);
            }
        }
        report.AppendLine("12 children, four close shoulder-linked triples; three baked mesh poses with shared original textures/material; no per-frame pose calculation or additional Animator.");
    }
    private static void ValidateShoulderContacts(StringBuilder report)
    {
        var kids=Find("TreMoi_QuanhSanBi").transform;float largest=0;
        // Inspect the actual deformed hand and shoulder vertex clouds, not just actor roots.
        var bind=NormalizedChildMesh().vertices;
        for(int group=0;group<4;group++)for(int link=0;link<2;link++)
        {
            var left=kids.Find("TreChoibi_"+(group*3+link+1));var right=kids.Find("TreChoibi_"+(group*3+link+2));
            var a=left.Find("DangKhoacVai").GetComponent<MeshFilter>().sharedMesh.vertices;var b=right.Find("DangKhoacVai").GetComponent<MeshFilter>().sharedMesh.vertices;
            for(int direction=0;direction<2;direction++)
            {
                var handRoot=direction==0?left:right;var shoulderRoot=direction==0?right:left;var handMesh=direction==0?a:b;var shoulderMesh=direction==0?b:a;int side=direction==0?1:-1;float closest=100;
                var hands=Enumerable.Range(0,bind.Length).Where(i=>bind[i].x*side>.50f && bind[i].y>.59f && bind[i].y<.72f).Select(i=>handRoot.TransformPoint(handMesh[i])).ToArray();
                var shoulders=Enumerable.Range(0,bind.Length).Where(i=>bind[i].y>.79f && bind[i].y<.87f && Mathf.Abs(bind[i].x)>.1f && Mathf.Abs(bind[i].x)<.21f).Select(i=>shoulderRoot.TransformPoint(shoulderMesh[i])).ToArray();
                foreach(var hand in hands)foreach(var shoulder in shoulders)closest=Mathf.Min(closest,Vector3.Distance(hand,shoulder));
                largest=Mathf.Max(largest,closest);if(closest>.13f)throw new Exception("Hand misses neighbour's shoulder: group "+group+", link "+link+", side "+direction+", gap "+closest);
            }
        }
        report.AppendLine("PASS: sixteen hand-to-neighbour-shoulder contacts, worst vertex gap "+largest.ToString("F3")+" m. Height, grounding, approach rays and walking also checked.");
    }
    private static void CaptureShoulderGroup()
    {
        var kids=Find("TreMoi_QuanhSanBi").transform;var first=kids.Find("TreChoibi_2");var center=first.position+Vector3.up*.65f;
        var obj=new GameObject("ShoulderGroupCamera"){hideFlags=HideFlags.HideAndDontSave};var camera=obj.AddComponent<Camera>();camera.enabled=false;camera.fieldOfView=45;camera.nearClipPlane=.03f;camera.farClipPlane=200;
        camera.transform.position=center+first.forward*3.2f+Vector3.up*.2f;camera.transform.LookAt(center);var rt=new RenderTexture(1400,900,24);rt.Create();var old=RenderTexture.active;var image=new Texture2D(1400,900,TextureFormat.RGB24,false);
        try{RenderPipeline.SubmitRenderRequest(camera,new UniversalRenderPipeline.SingleCameraRequest{destination=rt});RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,1400,900),0,0);image.Apply();File.WriteAllBytes(CrowdOutput+"/01_NhomKhoacVai.png",image.EncodeToPNG());}
        finally{RenderTexture.active=old;rt.Release();UnityEngine.Object.DestroyImmediate(image);UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(obj);}
    }
    private static Mesh NormalizedChildMesh()
    {
        EnsureFolder(CrowdAssets);string path=CrowdAssets+"/Tre_BindPose_1m2.asset";var existing=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(existing)return existing;
        var importer=(ModelImporter)AssetImporter.GetAtPath(ActorModels[2]);bool readable=importer.isReadable;
        if(!readable){importer.isReadable=true;importer.SaveAndReimport();}
        GameObject temp=null;
        try
        {
            temp=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(ActorModels[2]));temp.hideFlags=HideFlags.HideAndDontSave;
            var filters=temp.GetComponentsInChildren<MeshFilter>();var mesh=new Mesh{name="Tre_BindPose_1m2",indexFormat=IndexFormat.UInt32};
            mesh.CombineMeshes(filters.Select(f=>new CombineInstance{mesh=f.sharedMesh,subMeshIndex=0,transform=temp.transform.worldToLocalMatrix*f.transform.localToWorldMatrix}).ToArray(),true,true);
            var b=mesh.bounds;float scale=1.2f/b.size.y;var v=mesh.vertices;for(int i=0;i<v.Length;i++)v[i]=new Vector3(v[i].x-b.center.x,v[i].y-b.min.y,v[i].z-b.center.z)*scale;
            mesh.vertices=v;mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,path);return mesh;
        }
        finally{if(temp)UnityEngine.Object.DestroyImmediate(temp);if(!readable){importer.isReadable=false;importer.SaveAndReimport();}}
    }
    public static void BatchInspectChildShoulders()
    {
        try
        {
            EditorSceneManager.OpenScene(ScenePath);Directory.CreateDirectory(CrowdOutput);var mesh=NormalizedChildMesh();var report=new StringBuilder();report.AppendLine(mesh.bounds.ToString("F4"));
            var vertices=mesh.vertices;
            for(int i=0;i<24;i++)
            {
                float y=i*.05f;var band=vertices.Where(v=>v.y>=y && v.y<y+.05f).ToArray();
                if(band.Length>0)report.AppendLine(y.ToString("F2")+" "+band.Length+" X="+band.Min(v=>v.x).ToString("F3")+".."+band.Max(v=>v.x).ToString("F3")+" Z="+band.Min(v=>v.z).ToString("F3")+".."+band.Max(v=>v.z).ToString("F3"));
            }
            File.WriteAllText(CrowdOutput+"/source-profile.txt",report.ToString());CaptureChildMeshInspect(mesh,"source");AssetDatabase.SaveAssets();EditorApplication.Exit(0);
        }
        catch(Exception e){Debug.LogException(e);Directory.CreateDirectory(CrowdOutput);File.WriteAllText(CrowdOutput+"/error.txt",e.ToString());EditorApplication.Exit(1);}
    }
    private static void CaptureChildMeshInspect(Mesh mesh,string name)
    {
        var obj=new GameObject("ChildPoseInspection"){hideFlags=HideFlags.HideAndDontSave,layer=31};obj.transform.position=Vector3.up*10;obj.AddComponent<MeshFilter>().sharedMesh=mesh;
        var material=new Material(Shader.Find("Universal Render Pipeline/Unlit"));material.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Material>(ActorAssets+"/trecon_Mat.mat").GetTexture("_BaseMap"));obj.AddComponent<MeshRenderer>().sharedMaterial=material;
        var camObject=new GameObject("ChildPoseCamera"){hideFlags=HideFlags.HideAndDontSave};var camera=camObject.AddComponent<Camera>();camera.enabled=false;camera.orthographic=true;camera.orthographicSize=.72f;camera.cullingMask=1<<31;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.18f,.18f,.18f);camera.nearClipPlane=.02f;camera.farClipPlane=10;
        var rt=new RenderTexture(900,1000,24);rt.Create();var old=RenderTexture.active;var image=new Texture2D(900,1000,TextureFormat.RGB24,false);
        try
        {
            foreach(int side in new[]{0,1})
            {
                var center=obj.transform.position+Vector3.up*.6f;camera.transform.position=center+(side==0?Vector3.forward:Vector3.right)*3;camera.transform.LookAt(center);
                RenderPipeline.SubmitRenderRequest(camera,new UniversalRenderPipeline.SingleCameraRequest{destination=rt});RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,900,1000),0,0);image.Apply();File.WriteAllBytes(CrowdOutput+"/"+name+(side==0?"_front":"_side")+".png",image.EncodeToPNG());
            }
        }
        finally{RenderTexture.active=old;rt.Release();UnityEngine.Object.DestroyImmediate(image);UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(camObject);UnityEngine.Object.DestroyImmediate(obj);UnityEngine.Object.DestroyImmediate(material);}
    }
}
