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
    private const string BambooAssets="Assets/_Project/Art/Models/Scene3/BambooSample";
    private const string BambooOutput=OutputFolder+"/BambooSample";
    private const string BambooMarker="BambooArtSample_15m";
    [Serializable] private sealed class BambooBaseline
    {
        public VillageGroundCoverRenderer.Plant[] plants;
        public float nearDistance,middleDistance,farDistance;
    }
    [Serializable] private sealed class BambooBaselineEnvelope { public BambooBaseline MonoBehaviour; }
    private static bool InBambooSample(VillageGroundCoverRenderer.Plant plant)
    {
        return plant.prototype==4 && plant.position.x>4 && plant.position.x<8.5f && plant.position.z>=28 && plant.position.z<=43;
    }
    private static VillageGroundCoverRenderer BambooCover()=>Find(MaxtreeRoot).GetComponent<VillageGroundCoverRenderer>();

    [MenuItem("KuTy/Scene 3/Bamboo/1 Apply 15m bamboo sample")]
    public static void ApplyBambooSample()
    {
        RequireArtScene();
        var cover=BambooCover();
        if(Find(BambooMarker))throw new InvalidOperationException("Sample already applied. Use its Undo or baseline before reapplying.");
        if(cover.prototypes.Length<=4 || !cover.prototypes[4].near || !cover.prototypes[4].near.name.StartsWith("TreTanXumXe"))
            throw new InvalidOperationException("Expected the existing reference bamboo at prototype 4.");
        var removed=cover.plants.Where(InBambooSample).ToArray();
        if(removed.Length==0)throw new InvalidOperationException("No reference bamboo found in the sample strip x=4..8.5, z=28..43.");
        Directory.CreateDirectory(BambooOutput);
        string backup=BambooOutput+"/before.unity";
        if(File.Exists(backup))
        {
            if(File.ReadAllText(BambooOutput+"/cover-before.json")!=EditorJsonUtility.ToJson(cover,true))
                throw new InvalidOperationException("Vegetation differs from the existing baseline. Inspect before reapplying.");
        }
        else
        {
            if(!EditorSceneManager.SaveScene(SceneManager.GetActiveScene(),backup,true))throw new IOException("Cannot back up scene.");
            File.WriteAllText(BambooOutput+"/cover-before.json",EditorJsonUtility.ToJson(cover,true));
            File.WriteAllText(BambooOutput+"/checkpoint-before.txt",ReadArtCheckpoint());
            CaptureBambooViews("Before");
        }
        VerifyBambooSampleWalk();
        var gameplay=GameplaySnapshot();gameplay.Remove(cover);
        string checkpoint=ReadArtCheckpoint();
        var preserved=cover.plants.Where(p=>!InBambooSample(p)).ToArray();
        var originalPrototypes=cover.prototypes.ToArray();
        var sky=RenderSettings.skybox;var sun=Find("Sun").transform.rotation;
        EnsureFolder(BambooAssets);
        var shader=Shader.Find("KuTy/Village/Soft Bamboo");
        if(!shader || ShaderUtil.GetShaderMessages(shader).Any(m=>m.severity==UnityEditor.Rendering.ShaderCompilerMessageSeverity.Error))
            throw new Exception("Bamboo shader missing or contains compile errors.");
        string materialPath=BambooAssets+"/TreLaMem.mat";
        var material=AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if(!material){material=new Material(shader){name="TreLaMem",enableInstancing=true};AssetDatabase.CreateAsset(material,materialPath);}
        var prototypes=originalPrototypes.ToList();
        for(int variant=0;variant<3;variant++)prototypes.Add(new VillageGroundCoverRenderer.Prototype {
            name="TreLaMem_"+variant,near=SoftBambooMesh(variant,0),middle=SoftBambooMesh(variant,1),far=SoftBambooMesh(variant,2),
            materials=new[]{material},maxDistance=155});
        var plants=preserved.ToList();
        // Three unequal groups, with clear windows between their canopies.
        Vector2[] positions={new Vector2(5.9f,28.7f),new Vector2(7.2f,29.8f),new Vector2(6.4f,33.7f),
            new Vector2(7.3f,34.9f),new Vector2(6.1f,39.4f),new Vector2(7.2f,40.4f),new Vector2(6.4f,42.5f)};
        float[] heights={4.5f,5.4f,4.7f,5.7f,4.6f,5.3f,4.8f};
        for(int i=0;i<positions.Length;i++)
        {
            int id=originalPrototypes.Length+i%3;
            var mesh=prototypes[id].near;float scale=heights[i]/mesh.bounds.size.y;
            var p=positions[i];
            plants.Add(new VillageGroundCoverRenderer.Plant {prototype=id,position=new Vector3(p.x,ReferenceHeight(p.x,p.y)-.025f-mesh.bounds.min.y*scale,p.y),
                scale=scale,yaw=(i*137.508f+29)%360,paddy=false});
        }
        Undo.IncrementCurrentGroup();int undo=Undo.GetCurrentGroup();Undo.SetCurrentGroupName("15m soft bamboo sample");
        Undo.RecordObject(cover,"Replace only sample bamboo instances");
        try
        {
            cover.prototypes=prototypes.ToArray();cover.plants=plants.ToArray();cover.Rebuild();EditorUtility.SetDirty(cover);
            Group(BambooMarker,null);
            VerifyGameplay(gameplay);
            if(ReadArtCheckpoint()!=checkpoint || RenderSettings.skybox!=sky || Find("Sun").transform.rotation!=sun)
                throw new Exception("Checkpoint/sky/light changed during bamboo sample.");
            if(!preserved.SequenceEqual(cover.plants.Take(preserved.Length)))throw new Exception("Grass or outside bamboo changed.");
            for(int i=0;i<originalPrototypes.Length;i++)
                if(!originalPrototypes[i].Equals(cover.prototypes[i]))throw new Exception("Original prototype changed.");
            VerifyReferenceWalking();VerifyBambooSampleWalk();
            AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            if(!EditorSceneManager.SaveScene(SceneManager.GetActiveScene()))throw new IOException("Cannot save bamboo sample.");
            Undo.CollapseUndoOperations(undo);
        }
        catch {Undo.RevertAllDownToGroup(undo);cover.Rebuild();throw;}
        var report=new StringBuilder("PASS: scoped 15m sample installed.\n");
        report.AppendLine("Removed sample instances: "+removed.Length+"; new sample instances: "+positions.Length+".");
        report.AppendLine("Retained unchanged plants outside sample (including all grass): "+preserved.Length+".");
        report.AppendLine("Original prototypes unchanged: "+originalPrototypes.Length+". Appended variants: 3, each with 3 LOD meshes and one shared instanced material.");
        for(int i=originalPrototypes.Length;i<prototypes.Count;i++)report.AppendLine(prototypes[i].name+" triangles: "+
            prototypes[i].near.triangles.Length/3+" / "+prototypes[i].middle.triangles.Length/3+" / "+prototypes[i].far.triangles.Length/3);
        report.AppendLine("Existing gameplay script payloads unchanged: "+gameplay.Count+". Checkpoint, backdrop and directional light unchanged.");
        report.AppendLine("Existing renderer, tile culling, LOD distances, no-shadow policy retained. No new runtime scripts, colliders, or leaf GameObjects.");
        report.AppendLine("Walking checks passed: reference routes plus sample path in both directions. Build frame-time measurement pending.");
        File.WriteAllText(BambooOutput+"/report.txt",report.ToString());
        SceneView.RepaintAll();
        Debug.Log("[BambooSample] Seven new clumps saved in the 15m sample strip; rest of village untouched.");
    }

    private sealed class BambooGeometry
    {
        public readonly List<Vector3> vertices=new List<Vector3>();
        private readonly List<int> triangles=new List<int>();
        private readonly List<Color> colors=new List<Color>();
        private readonly List<Vector2> uv=new List<Vector2>();
        private int Vertex(Vector3 p,Color color,Vector2 tex)
        {int i=vertices.Count;vertices.Add(p);colors.Add(color);uv.Add(tex);return i;}
        public void Tube(Vector3 a,Vector3 b,float radiusA,float radiusB,float variation,int sides)
        {
            var axis=(b-a).normalized;var right=Vector3.Cross(axis,Mathf.Abs(axis.y)>.9f?Vector3.right:Vector3.up).normalized;
            var forward=Vector3.Cross(axis,right);int start=vertices.Count;
            for(int row=0;row<2;row++)for(int i=0;i<=sides;i++)
            {
                float angle=i*2*Mathf.PI/sides;var p=(row==0?a:b)+(right*Mathf.Cos(angle)+forward*Mathf.Sin(angle))*(row==0?radiusA:radiusB);
                Vertex(p,new Color(Mathf.Pow(Mathf.Clamp01(p.y/5.5f),2)*.18f,0,variation,1),new Vector2(i/(float)sides,row));
            }
            for(int i=0;i<sides;i++){int j=start+i,k=j+sides+1;triangles.AddRange(new[]{j,k,j+1,j+1,k,k+1});}
        }
        public void Leaf(Vector3 start,Vector3 direction,float length,float width,float roll,float variation,int segments)
        {
            var forward=direction.normalized;var side=Vector3.Cross(forward,Vector3.up).normalized;
            side=Quaternion.AngleAxis(roll,forward)*side;
            int first=vertices.Count;
            for(int i=0;i<=segments;i++)
            {
                float t=i/(float)segments;
                Vector3 spine=start+forward*length*t+Vector3.up*(Mathf.Sin(t*Mathf.PI)*length*.14f-t*t*length*.28f);
                float w=Mathf.Pow(Mathf.Max(0,Mathf.Sin(t*Mathf.PI)),.85f)*width*.5f;
                var color=new Color(Mathf.Pow(Mathf.Clamp01(spine.y/5.5f),1.4f),1,variation,1);
                Vertex(spine-side*w,color,new Vector2(0,t));Vertex(spine+side*w,color,new Vector2(1,t));
            }
            for(int i=0;i<segments;i++){int a=first+i*2;triangles.AddRange(new[]{a,a+2,a+1,a+1,a+2,a+3});}
        }
        public Mesh Save(string name)
        {
            var mesh=new Mesh {name=name,indexFormat=IndexFormat.UInt32};
            mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.SetColors(colors);mesh.SetUVs(0,uv);
            mesh.RecalculateNormals();mesh.RecalculateBounds();
            string path=BambooAssets+"/"+name+".asset";
            var existing=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(existing){EditorUtility.CopySerialized(mesh,existing);UnityEngine.Object.DestroyImmediate(mesh);EditorUtility.SetDirty(existing);return existing;}
            AssetDatabase.CreateAsset(mesh,path);return mesh;
        }
    }

    private static Mesh SoftBambooMesh(int variant,int lod)
    {
        string name="TreLaMem_"+variant+"_LOD"+lod;
        var existing=AssetDatabase.LoadAssetAtPath<Mesh>(BambooAssets+"/"+name+".asset");
        if(existing && existing.vertices.All(p=>!float.IsNaN(p.x) && !float.IsNaN(p.y) && !float.IsNaN(p.z) &&
            !float.IsInfinity(p.x) && !float.IsInfinity(p.y) && !float.IsInfinity(p.z)))return existing;
        var geometry=new BambooGeometry();int stems=5+variant;
        for(int stem=0;stem<stems;stem++)
        {
            var rng=new System.Random(731+variant*7919+stem*347);
            float a=stem*2.39996f+variant*.53f;
            float h=4.2f+(float)rng.NextDouble()*1.3f;
            var root=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a))*(.13f+.1f*(stem%3));
            var lean=new Vector3(Mathf.Cos(a),0,Mathf.Sin(a))*(variant==0?.5f:variant==1?.95f:1.25f);
            Func<float,Vector3> point=t=>root+Vector3.up*(h*t)+lean*(t*t);
            int segments=lod==0?14:lod==1?10:7,sides=lod==0?7:5;
            for(int k=0;k<segments;k++)
            {
                float t=k/(float)segments,u=(k+1)/(float)segments;
                float radius=.047f*(1-t*.8f);
                geometry.Tube(point(t),point(u),radius,.047f*(1-u*.8f),.36f,sides);
            }
            // Raised culm nodes are geometry; no texture or transparency needed for the sample.
            for(int node=1;node<11;node++)
            {
                if(lod==2 && node%2==1)continue;
                float t=node/12f,r=.047f*(1-t*.8f)*1.2f;
                geometry.Tube(point(t-.0025f),point(t+.0025f),r,r,.86f,sides);
            }
            for(int b=0;b<7;b++)
            {
                if(lod==1 && b%3==1 || lod==2 && b%2==1)continue;
                var branchRng=new System.Random(3389+variant*1019+stem*307+b*73);
                float t=.49f+b*.069f,ba=a+b*2.39996f;
                Vector3 start=point(t),outward=new Vector3(Mathf.Cos(ba),0,Mathf.Sin(ba));
                float reach=.65f+(float)branchRng.NextDouble()*.42f;
                Vector3 end=start+outward*reach+Vector3.up*(.12f-b*.017f);
                Func<float,Vector3> branch=s=>Vector3.Lerp(start,end,s)+Vector3.up*(Mathf.Sin(s*Mathf.PI)*.22f-s*s*.16f);
                for(int k=0;k<3;k++)geometry.Tube(branch(k/3f),branch((k+1)/3f),.009f*(1-k*.25f),.006f*(1-k*.25f),.44f,4);
                for(int twig=0;twig<3;twig++)
                {
                    if(lod==2 && twig==1)continue;
                    float s=.4f+twig*.27f;Vector3 origin=branch(s);
                    Vector3 twigDirection=Quaternion.Euler(0,(twig%2==0?-1:1)*38,0)*outward;
                    Vector3 tip=origin+twigDirection*.42f+Vector3.down*.10f;
                    if(lod<2)geometry.Tube(origin,tip,.0045f,.0015f,.48f,3);
                    int leafCount=lod==0?9:lod==1?6:4;
                    for(int l=0;l<leafCount;l++)
                    {
                        float f=(l+.2f)/leafCount;
                        var leafRng=new System.Random(4373+variant*1093+stem*293+b*61+twig*19+l*7);
                        float yaw=(l%2==0?-1:1)*(32+(float)leafRng.NextDouble()*30);
                        Vector3 direction=Quaternion.Euler(0,yaw,0)*twigDirection+Vector3.down*(.1f+(float)leafRng.NextDouble()*.4f);
                        float length=.22f+(float)leafRng.NextDouble()*.20f;
                        geometry.Leaf(Vector3.Lerp(origin,tip,f),direction,length,.052f+(float)leafRng.NextDouble()*.027f,
                            (float)leafRng.NextDouble()*65-32,(float)leafRng.NextDouble(),lod==0?3:2);
                    }
                }
            }
        }
        return geometry.Save(name);
    }

    [MenuItem("KuTy/Scene 3/Bamboo/2 Validate and capture bamboo sample")]
    public static void CaptureBambooComparison()
    {
        if(Find(BambusaVillageMarker)){CaptureWholeVillageBambooComparison();return;}
        RequireArtScene();if(!Find(BambooMarker))throw new Exception("Apply the bamboo sample first.");
        ValidateBambooSampleState();
        VerifyReferenceWalking();VerifyBambooSampleWalk();
        if(ShaderUtil.GetShaderMessages(Shader.Find("KuTy/Village/Soft Bamboo")).Any(m=>m.severity==UnityEditor.Rendering.ShaderCompilerMessageSeverity.Error))
            throw new Exception("Bamboo shader compile error.");
        CaptureBambooViews("After");
    }

    private static void ValidateBambooSampleState()
    {
        var cover=BambooCover();
        var baseline=JsonUtility.FromJson<BambooBaselineEnvelope>(File.ReadAllText(BambooOutput+"/cover-before.json")).MonoBehaviour;
        var kept=baseline.plants.Where(p=>!InBambooSample(p)).ToArray();
        if(!kept.SequenceEqual(cover.plants.Take(kept.Length)) || cover.plants.Length!=kept.Length+7)
            throw new Exception("Grass or out-of-sample vegetation differs from the baseline.");
        if(cover.nearDistance!=baseline.nearDistance || cover.middleDistance!=baseline.middleDistance || cover.farDistance!=baseline.farDistance)
            throw new Exception("Shared grass/bamboo LOD distances changed.");
        foreach(var prototype in cover.prototypes.Where(p=>p.name!=null && p.name.StartsWith("TreLaMem_")))
        {
            if(prototype.middle.triangles.Length>=prototype.near.triangles.Length || prototype.far.triangles.Length>=prototype.middle.triangles.Length)
                throw new Exception("Bamboo LOD triangle reduction failed.");
            foreach(var mesh in new[]{prototype.near,prototype.middle,prototype.far})
                if(mesh.vertices.Any(p=>float.IsNaN(p.x)||float.IsNaN(p.y)||float.IsNaN(p.z)||float.IsInfinity(p.x)||float.IsInfinity(p.y)||float.IsInfinity(p.z)))
                    throw new Exception("Invalid bamboo mesh vertex: "+mesh.name);
        }
        foreach(var plant in cover.plants.Skip(kept.Length))
        {
            var mesh=cover.prototypes[plant.prototype].near;
            float bottom=plant.position.y+mesh.bounds.min.y*plant.scale;
            if(Mathf.Abs(bottom-(ReferenceHeight(plant.position.x,plant.position.z)-.025f))>.001f)
                throw new Exception("Bamboo root grounding failed.");
        }
        File.WriteAllText(BambooOutput+"/validation.txt","PASS: all retained vegetation matches disk baseline; seven sample clumps; shared LOD distances unchanged.\nAll nine meshes finite, all LOD triangle counts decrease, all roots grounded 2.5cm into terrain.\nCheckpoint still matches baseline: "+(ReadArtCheckpoint()==File.ReadAllText(BambooOutput+"/checkpoint-before.txt"))+"\n");
    }

    private static void VerifyBambooSampleWalk()
    {
        var obj=new GameObject("BambooWalkingProbe"){hideFlags=HideFlags.HideAndDontSave};var cc=obj.AddComponent<CharacterController>();
        cc.height=1.6f;cc.center=Vector3.up*.8f;cc.radius=.24f;cc.stepOffset=.3f;
        try
        {
            for(int direction=0;direction<2;direction++)
            {
                Vector3[] route={new Vector3(-4.25f,.3f,27),new Vector3(-5,.3f,36),new Vector3(-3.67f,.3f,44)};
                if(direction==1)Array.Reverse(route);
                cc.enabled=false;obj.transform.position=route[0];cc.enabled=true;Physics.SyncTransforms();
                for(int target=1;target<route.Length;target++)
                {
                    var to=route[target];
                    for(int step=0;step<240;step++)
                    {var d=to-obj.transform.position;d.y=0;if(d.magnitude<.12f)break;cc.Move(Vector3.ClampMagnitude(d,.06f)+Vector3.down*.025f);}
                    var remain=to-obj.transform.position;remain.y=0;if(remain.magnitude>.25f || obj.transform.position.y<-.1f)throw new Exception("Sample path blocked at "+obj.transform.position);
                }
            }
        }
        finally{UnityEngine.Object.DestroyImmediate(obj);}
    }

    private static void CaptureBambooViews(string phase)
    {
        string folder=BambooOutput+"/"+phase;Directory.CreateDirectory(folder);
        Vector3[] positions={new Vector3(1.5f,1.8f,34.5f),new Vector3(3,1.8f,27),new Vector3(-5,2.1f,37),new Vector3(6,10,24)};
        Vector3[] targets={new Vector3(6.6f,2.9f,34.5f),new Vector3(6,2.3f,38),new Vector3(6.2f,2.8f,36),new Vector3(6,1.4f,36)};
        var report=new StringBuilder();var cover=BambooCover();
        for(int i=0;i<positions.Length;i++)
        {
            var obj=new GameObject("BambooCapture"){hideFlags=HideFlags.HideAndDontSave};var camera=obj.AddComponent<Camera>();
            camera.enabled=false;camera.fieldOfView=65;camera.nearClipPlane=.03f;camera.farClipPlane=300;camera.allowHDR=true;
            camera.transform.SetPositionAndRotation(positions[i],Quaternion.LookRotation(targets[i]-positions[i]));
            camera.GetUniversalAdditionalCameraData().renderPostProcessing=true;
            var rt=new RenderTexture(1600,900,24);var image=new Texture2D(1600,900,TextureFormat.RGB24,false);var previous=RenderTexture.active;
            try
            {
                rt.Create();RenderPipeline.SubmitRenderRequest(camera,new UniversalRenderPipeline.SingleCameraRequest{destination=rt});
                RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,1600,900),0,0);image.Apply();File.WriteAllBytes(folder+"/"+(i+1).ToString("00")+".png",image.EncodeToPNG());
                report.AppendLine("View "+i+": all ground-cover visible instances="+cover.LastVisiblePlants+", cover draws="+cover.LastDrawCalls+" (not whole-scene or build timing).");
            }
            finally{RenderTexture.active=previous;rt.Release();UnityEngine.Object.DestroyImmediate(image);UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(obj);}
        }
        File.WriteAllText(folder+"/render-stats.txt",report.ToString());
    }
}
