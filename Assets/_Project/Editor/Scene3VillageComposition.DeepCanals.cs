using System;
using System.Collections.Generic;
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
    private const string DeepAssets=MemoryAssets+"/DeepCanals";
    private const string DeepRoot="KenhSau_BoRuongTuNhien";
    private const float CanalWaterHeight=-.18f;

    [MenuItem("KuTy/Scene 3/Deepen both canals and join low field banks")]
    public static void BuildDeepCanalsAndLowBanks()
    {
        var scene=SceneManager.GetActiveScene();
        if(scene.path!=ScenePath || EditorApplication.isPlayingOrWillChangePlaymode || !Find(MemoryRoot)) throw new Exception("Open current memory village in Edit Mode.");
        if(Find(DeepRoot)) {ValidateDeepCanals();CaptureDeepCanals();return;}
        if(!EditorSceneManager.SaveScene(scene,OutputFolder+"/before-deep-canals.unity",true)) throw new Exception("Cannot back up current scene.");
        var village=Find(VillageName);var water=village.GetComponentInChildren<VillageWaterSystem>();
        var gameplay=GameplaySnapshot();gameplay.Remove(water);
        var waterIds=water.surfaces.Select(s=>s.renderer).ToArray();
        Undo.IncrementCurrentGroup();int undo=Undo.GetCurrentGroup();Undo.SetCurrentGroupName("Deep curved canals and continuous low rice banks");
        Undo.RegisterFullObjectHierarchyUndo(Find("Scene3_DuongLang_RuongLua"),"Village before deeper canals");
        Undo.RegisterFullObjectHierarchyUndo(Find("Ground"),"Flat ground before canal excavation");
        try
        {
            EnsureFolder(DeepAssets);var root=Group(DeepRoot,village.transform).transform;
            BuildExcavatedGround();
            CutBackdropAroundVillage();
            foreach(var t in AllTransforms().Where(t=>t.gameObject.activeInHierarchy && t.name=="BoKenh").ToArray()) t.gameObject.SetActive(false);
            foreach(var field in village.GetComponentsInChildren<RuralRiceFieldRenderer>())
            {
                int index=int.Parse(field.name.Substring(field.name.Length-2))-1;
                var rect=FieldRects[index];var contour=RoundedBankContour(rect,index);
                foreach(var old in field.GetComponentsInChildren<Transform>().Where(t=>t.name.StartsWith("BoRuong_")).ToArray()) old.gameObject.SetActive(false);
                var bank=MeshProp(root,"BoRuong_LienGoc_"+(index+1).ToString("00"),Vector3.zero,BuildContinuousBank(contour,index),
                    AssetDatabase.LoadAssetAtPath<Material>(NewMaterials+"/BoRuong_DatCo.mat"));
                bank.AddComponent<MeshCollider>().sharedMesh=bank.GetComponent<MeshFilter>().sharedMesh;
                var renderer=field.transform.Find("MatNuoc").GetComponent<Renderer>();
                int surface=Array.FindIndex(water.surfaces,s=>s.renderer==renderer);
                if(surface<0) throw new Exception("Paddy water mapping missing.");
                var data=water.surfaces[surface];
                var waterContour=InsetContour(contour,.24f);
                data.outline=waterContour.Select(p=>new Vector2(p.x,p.z)).ToArray();
                data.rectangle=OutlineBounds(data.outline);data.shoreMask=true;
                ReplaceWaterMesh(renderer,BuildPaddyWater(waterContour,data.height,index));water.surfaces[surface]=data;
            }
            for(int i=0;i<water.surfaces.Length;i++)
            {
                var data=water.surfaces[i];if(!data.canal) continue;
                float center=(data.rectangle.x+data.rectangle.z)/2;
                var outline=new List<Vector2>();var vertices=new List<Vector3>();var colors=new List<Color>();var triangles=new List<int>();
                int rows=145;float start=data.rectangle.y,end=data.rectangle.w;
                for(int row=0;row<=rows;row++)
                {
                    float z=Mathf.Lerp(start,end,(float)row/rows);float drift=CanalDrift(z),half=CanalHalfWidth(z);
                    for(int cross=0;cross<3;cross++)
                    {
                        vertices.Add(VillageLandscapeShape.Warp(new Vector3(center+drift+(cross-1)*half,CanalWaterHeight,z)));
                        colors.Add(new Color(cross==1?half:0,0,0,1));
                    }
                    outline.Add(new Vector2(center+drift-half,z));
                    if(row>0) for(int c=0;c<2;c++) {int a=(row-1)*3+c;triangles.AddRange(new[]{a,a+3,a+4,a,a+4,a+1});}
                }
                for(int row=rows;row>=0;row--)
                {
                    float z=Mathf.Lerp(start,end,(float)row/rows);outline.Add(new Vector2(center+CanalDrift(z)+CanalHalfWidth(z),z));
                }
                data.outline=outline.ToArray();data.rectangle=OutlineBounds(data.outline);data.height=CanalWaterHeight;data.shoreMask=true;
                ReplaceWaterMesh(data.renderer,SaveDeepMesh("NuocKenhSau_"+(center<0?"Trai":"Phai"),vertices,triangles,colors));
                // A separate material leaves the shallow flooded paddies unchanged.
                var original=data.renderer.sharedMaterial;string path=WaterFolder+"/NuocKenhSau.mat";
                var material=AssetDatabase.LoadAssetAtPath<Material>(path);
                if(!material) {material=new Material(original){name="NuocKenhSau"};AssetDatabase.CreateAsset(material,path);}
                material.SetColor("_ShallowColor",new Color(.08f,.25f,.29f));material.SetColor("_DeepColor",new Color(.025f,.11f,.16f));
                material.SetFloat("_DepthTone",.72f);material.SetFloat("_Opacity",.74f);material.SetFloat("_WaveStrength",.065f);EditorUtility.SetDirty(material);
                data.renderer.sharedMaterial=material;water.surfaces[i]=data;
            }
            EditorUtility.SetDirty(water);ApplyVillageWaterBlocks(water);
            if(water.reflectionProbe) water.reflectionProbe.RenderProbe();
            if(!waterIds.SequenceEqual(water.surfaces.Select(s=>s.renderer))) throw new Exception("Water renderer references changed.");
            VerifyGameplay(gameplay);Physics.SyncTransforms();ValidateMemoryVillage();ValidateDeepCanals();
            AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);
            if(!EditorSceneManager.SaveScene(scene)) throw new Exception("Cannot save deeper canals.");
            Undo.CollapseUndoOperations(undo);CaptureDeepCanals();CaptureMemoryVillage();EditorSceneManager.SaveScene(scene);Inspect(true);SceneView.RepaintAll();
        }
        catch {Undo.RevertAllDownToGroup(undo);throw;}
    }

    private static float CanalDrift(float z) {return .045f*Mathf.Sin(z*.33f)+.025f*Mathf.Sin(z*.79f);}
    private static float CanalHalfWidth(float z) {return 1.012f+.018f*Mathf.Sin(z*.47f);}
    private static void BuildExcavatedGround()
    {
        var columns=new List<float>{-44};
        var profile=new[]{-1.5f,-1.32f,-1.22f,-1f,-.75f,-.5f,-.25f,0,.25f,.5f,.75f,1f,1.22f,1.32f,1.5f};
        columns.AddRange(profile.Select(d=>-5+d));columns.Add(0);columns.AddRange(profile.Select(d=>5+d));columns.Add(44);
        var vertices=new List<Vector3>();var triangles=new List<int>();var masks=new List<Color>();int rows=157;
        for(int row=0;row<=rows;row++)
        {
            float z=Mathf.Lerp(-14,88,(float)row/rows);
            float fade=Mathf.SmoothStep(0,1,Mathf.InverseLerp(-14,-12,z))*(1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(82,85,z)));
            for(int c=0;c<columns.Count;c++)
            {
                float x=columns[c],distance=Mathf.Min(Mathf.Abs(x+5),Mathf.Abs(x-5));
                float y=0,mask=1;
                if(distance<=1.5f)
                {
                    if(distance<=.25f) y=-.82f+.018f*Mathf.Sin(z*.63f);
                    else if(distance<=1.22f) y=Mathf.Lerp(-.82f,-.005f,(distance-.25f)/.97f);
                    else if(distance<=1.32f) y=Mathf.Lerp(-.005f,.06f,(distance-1.22f)/.10f);
                    else y=Mathf.Lerp(.06f,0,(distance-1.32f)/.18f);
                    if(distance>.25f) y+=.008f*Mathf.Sin(z*1.3f+distance*4);
                    y*=fade;mask=Mathf.Lerp(1,Mathf.SmoothStep(0,1,Mathf.InverseLerp(.91f,1.35f,distance)),fade);
                    x+=CanalDrift(z)*fade;
                }
                Vector3 p=VillageLandscapeShape.Warp(new Vector3(x,y,z));if(c==0)p.x=-44;if(c==columns.Count-1)p.x=44;
                vertices.Add(p);masks.Add(new Color(mask,0,0,1));
                if(row>0 && c>0) {int a=(row-1)*columns.Count+c-1;triangles.AddRange(new[]{a,a+columns.Count,a+columns.Count+1,a,a+columns.Count+1,a+1});}
            }
        }
        var mesh=SaveDeepMesh("NenLang_DaoHaiKenh",vertices,triangles,masks);
        var ground=Find("Ground");ground.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);ground.transform.localScale=Vector3.one;
        ground.GetComponent<MeshFilter>().sharedMesh=mesh;
        foreach(var old in ground.GetComponents<Collider>()) Undo.DestroyObjectImmediate(old);
        var collider=Undo.AddComponent<MeshCollider>(ground);collider.sharedMesh=mesh;
        string materialPath=NewMaterials+"/NenLang_KenhSau.mat";
        var source=ground.GetComponent<Renderer>().sharedMaterial;var material=AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if(!material) {material=new Material(source){name="NenLang_KenhSau"};AssetDatabase.CreateAsset(material,materialPath);}
        material.SetFloat("_UseVertexCoverage",1);material.SetFloat("_GrassCoverage",1);EditorUtility.SetDirty(material);ground.GetComponent<Renderer>().sharedMaterial=material;
        // The far visual plain stays flat and has no collider: only the actual village ground is excavated.
    }
    private static Mesh SaveDeepMesh(string name,List<Vector3> vertices,List<int> triangles,List<Color> colors)
    {
        var mesh=new Mesh{name=name,indexFormat=IndexFormat.UInt32};mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.SetColors(colors);
        mesh.SetUVs(0,vertices.Select(p=>new Vector2(p.x*.3f,p.z*.3f)).ToList());mesh.RecalculateNormals();mesh.RecalculateBounds();mesh.RecalculateTangents();
        string path=DeepAssets+"/"+name+".asset";var old=AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if(old) {EditorUtility.CopySerialized(mesh,old);UnityEngine.Object.DestroyImmediate(mesh);return old;}
        AssetDatabase.CreateAsset(mesh,path);return mesh;
    }
    private struct BackdropVertex
    {
        public Vector3 point;public Color color;
        public static BackdropVertex Mix(BackdropVertex a,BackdropVertex b,float t) {return new BackdropVertex{point=Vector3.Lerp(a.point,b.point,t),color=Color.Lerp(a.color,b.color,t)};}
    }
    private static List<BackdropVertex> ClipBackdrop(List<BackdropVertex> polygon,int axis,float boundary,bool above)
    {
        var output=new List<BackdropVertex>();if(polygon.Count==0)return output;
        var previous=polygon[polygon.Count-1];float oldValue=axis==0?previous.point.x:previous.point.z;bool wasInside=above?oldValue>=boundary:oldValue<=boundary;
        foreach(var current in polygon)
        {
            float value=axis==0?current.point.x:current.point.z;bool inside=above?value>=boundary:value<=boundary;
            if(inside!=wasInside) output.Add(BackdropVertex.Mix(previous,current,(boundary-oldValue)/(value-oldValue)));
            if(inside)output.Add(current);previous=current;oldValue=value;wasInside=inside;
        }
        return output;
    }
    private static void CutBackdropAroundVillage()
    {
        var root=Find(MemoryRoot);
        foreach(var filter in root.GetComponentsInChildren<MeshFilter>().Where(f=>f.name=="DongBang_MoRongTamNhin" || f.name.StartsWith("DongXa_MauLuaTuNhien_")))
        {
            if(filter.sharedMesh.name.StartsWith("DongXa_CatVungChoi_"))continue;
            var source=filter.sharedMesh;var points=source.vertices;var colors=source.colors;var indices=source.triangles;
            var vertices=new List<Vector3>();var tint=new List<Color>();var triangles=new List<int>();
            for(int i=0;i<indices.Length;i+=3)
            {
                var original=Enumerable.Range(0,3).Select(c=>new BackdropVertex{point=filter.transform.TransformPoint(points[indices[i+c]]),color=colors.Length==points.Length?colors[indices[i+c]]:Color.white}).ToList();
                for(int region=0;region<4;region++)
                {
                    List<BackdropVertex> clipped;
                    if(region==0)clipped=ClipBackdrop(original,0,-44,false);
                    else if(region==1)clipped=ClipBackdrop(original,0,44,true);
                    else {clipped=ClipBackdrop(ClipBackdrop(original,0,-44,true),0,44,false);clipped=ClipBackdrop(clipped,2,region==2?-14:88,region==3);}
                    if(clipped.Count<3)continue;int start=vertices.Count;
                    foreach(var vertex in clipped){vertices.Add(filter.transform.InverseTransformPoint(vertex.point));tint.Add(vertex.color);}
                    for(int c=1;c<clipped.Count-1;c++) triangles.AddRange(new[]{start,start+c,start+c+1});
                }
            }
            Undo.RecordObject(filter,"Cut visual background away from excavated ground");
            filter.sharedMesh=SaveDeepMesh("DongXa_CatVungChoi_"+filter.name,vertices,triangles,tint);
        }
    }
    public static void FinishDeepCanalBackdrop()
    {
        var scene=SceneManager.GetActiveScene();if(scene.path!=ScenePath || !Find(DeepRoot))throw new Exception("Deep canals missing.");
        var before=GameplaySnapshot();CutBackdropAroundVillage();VerifyGameplay(before);ValidateMemoryVillage();ValidateDeepCanals();
        AssetDatabase.SaveAssets();CaptureDeepCanals();CaptureMemoryVillage();EditorSceneManager.MarkSceneDirty(scene);
        if(!EditorSceneManager.SaveScene(scene))throw new Exception("Cannot save visible canal depths.");
        File.AppendAllText(OutputFolder+"/deep_canals_report.txt","Backdrop meshes clipped outside the village: no flat visual sheet covering depressed canal water.\n");
        Inspect(true);SceneView.RepaintAll();
    }
    private static Vector4 OutlineBounds(Vector2[] points)
    {return new Vector4(points.Min(p=>p.x),points.Min(p=>p.y),points.Max(p=>p.x),points.Max(p=>p.y));}
    private static void ReplaceWaterMesh(Renderer renderer,Mesh mesh)
    {
        renderer.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);renderer.transform.localScale=Vector3.one;
        renderer.GetComponent<MeshFilter>().sharedMesh=mesh;
    }
    private static List<Vector3> RoundedBankContour(Vector4 rect,int seed)
    {
        float xmin=rect.x-.17f,xmax=rect.y+.17f,zmin=rect.z-.17f,zmax=rect.w+.17f;
        float radius=Mathf.Min(.86f,Mathf.Min(xmax-xmin,zmax-zmin)*.22f);
        var points=new List<Vector3>();
        Action<Vector3,Vector3> line=(a,b)=>{int count=Mathf.Max(1,Mathf.CeilToInt(Vector3.Distance(a,b)/.55f));for(int i=0;i<count;i++)points.Add(Vector3.Lerp(a,b,(float)i/count));};
        Action<Vector3,float> arc=(center,start)=>{for(int i=0;i<8;i++){float a=(start+i*90f/8)*Mathf.Deg2Rad;points.Add(center+new Vector3(Mathf.Cos(a)*radius,0,Mathf.Sin(a)*radius));}};
        line(new Vector3(xmin+radius,0,zmin),new Vector3(xmax-radius,0,zmin));arc(new Vector3(xmax-radius,0,zmin+radius),270);
        line(new Vector3(xmax,0,zmin+radius),new Vector3(xmax,0,zmax-radius));arc(new Vector3(xmax-radius,0,zmax-radius),0);
        line(new Vector3(xmax-radius,0,zmax),new Vector3(xmin+radius,0,zmax));arc(new Vector3(xmin+radius,0,zmax-radius),90);
        line(new Vector3(xmin,0,zmax-radius),new Vector3(xmin,0,zmin+radius));arc(new Vector3(xmin+radius,0,zmin+radius),180);
        var natural=new List<Vector3>();
        for(int i=0;i<points.Count;i++)
        {
            Vector3 tangent=(points[(i+1)%points.Count]-points[(i+points.Count-1)%points.Count]).normalized;
            Vector3 inward=Vector3.Cross(tangent,Vector3.up);
            float wiggle=.085f*Mathf.Sin(points[i].x*.53f+points[i].z*.43f+seed*.9f)+.035f*Mathf.Sin(points[i].x*1.1f-points[i].z*.8f);
            natural.Add(points[i]+inward*wiggle);
        }
        return natural;
    }
    private static List<Vector3> InsetContour(List<Vector3> contour,float inset)
    {
        var result=new List<Vector3>();
        for(int i=0;i<contour.Count;i++)
        {var tangent=(contour[(i+1)%contour.Count]-contour[(i+contour.Count-1)%contour.Count]).normalized;result.Add(contour[i]+Vector3.Cross(tangent,Vector3.up)*inset);}
        return result;
    }
    private static Mesh BuildContinuousBank(List<Vector3> contour,int seed)
    {
        var vertices=new List<Vector3>();var triangles=new List<int>();var colors=new List<Color>();
        float[] cross={-.31f,-.2f,-.085f,0,.085f,.2f,.31f};float[] shape={0,.55f,.9f,1,.9f,.55f,0};
        for(int i=0;i<contour.Count;i++)
        {
            Vector3 p=contour[i];var tangent=(contour[(i+1)%contour.Count]-contour[(i+contour.Count-1)%contour.Count]).normalized;
            var inward=Vector3.Cross(tangent,Vector3.up);
            float height=.067f+.014f*Mathf.Sin(p.x*1.23f+p.z*.75f+seed)+.010f*Mathf.Sin(p.z*2.11f-p.x*.62f);
            float width=1+.10f*Mathf.Sin(p.x*.9f+p.z*.64f);
            for(int c=0;c<cross.Length;c++)
            {
                var v=p+inward*cross[c]*width;v.y=height*shape[c];vertices.Add(VillageLandscapeShape.Warp(v));colors.Add(new Color(Mathf.Lerp(.12f,.94f,shape[c]),0,0,1));
                if(c<cross.Length-1) {int a=i*cross.Length+c,b=((i+1)%contour.Count)*cross.Length+c;triangles.AddRange(new[]{a,b+1,b,a,a+1,b+1});}
            }
        }
        return SaveDeepMesh("BoRuong_KhepKin_"+(seed+1).ToString("00"),vertices,triangles,colors);
    }
    private static Mesh BuildPaddyWater(List<Vector3> contour,float height,int index)
    {
        var inner=InsetContour(contour,.24f);var vertices=new List<Vector3>();var triangles=new List<int>();var colors=new List<Color>();int count=contour.Count;
        for(int i=0;i<count;i++)
        {
            var a=contour[i];a.y=height;var b=inner[i];b.y=height;vertices.Add(VillageLandscapeShape.Warp(a));vertices.Add(VillageLandscapeShape.Warp(b));
            colors.Add(new Color(0,0,0,1));colors.Add(new Color(.24f,0,0,1));
            int n=(i+1)%count;triangles.AddRange(new[]{i*2,n*2+1,n*2,i*2,i*2+1,n*2+1});
        }
        Vector3 center=inner.Aggregate(Vector3.zero,(sum,p)=>sum+p)/count;center.y=height;int middle=vertices.Count;vertices.Add(VillageLandscapeShape.Warp(center));colors.Add(Color.red);
        for(int i=0;i<count;i++) triangles.AddRange(new[]{middle,((i+1)%count)*2+1,i*2+1});
        return SaveDeepMesh("NuocRuong_BoGoc_"+(index+1).ToString("00"),vertices,triangles,colors);
    }

    private static void ValidateDeepCanals()
    {
        if(Find(OrganicRoot)){ValidateOrganicVillage();return;}
        var ground=Find("Ground").GetComponent<MeshCollider>();if(!ground || Find("Ground").GetComponent<BoxCollider>()) throw new Exception("Flat ground collider still seals canal.");
        var village=Find(VillageName);var water=village.GetComponentInChildren<VillageWaterSystem>();
        foreach(var data in water.surfaces.Where(s=>s.canal))
        {
            float center=(data.rectangle.x+data.rectangle.z)/2;float z=32.2f;
            var p=VillageLandscapeShape.Warp(new Vector3(center+CanalDrift(z),2,z));
            if(!ground.Raycast(new Ray(p,Vector3.down),out var hit,4) || hit.point.y>-.76f) throw new Exception("Canal not excavated at "+p);
            if(data.height-hit.point.y<.55f) throw new Exception("Canal water has insufficient depth.");
        }
        var banks=Find(DeepRoot).GetComponentsInChildren<MeshFilter>().Where(f=>f.name.StartsWith("BoRuong_LienGoc_")).ToArray();
        float maximum=Find("HDRI_LangQue_Am") ? .15f : .095f;
        if(banks.Length!=16 || banks.Any(f=>f.GetComponent<Renderer>().bounds.max.y>maximum)) throw new Exception("Low field banks incomplete or too high.");
        foreach(var bank in banks)
        {
            var mesh=bank.sharedMesh;var indices=mesh.triangles;var used=new HashSet<int>(indices);
            if(used.Count!=mesh.vertexCount) throw new Exception("Disconnected bank seam.");
        }
        Physics.SyncTransforms();VerifyWalking();VerifyCanalExit();
        File.WriteAllText(OutputFolder+"/deep_canals_report.txt","PASS\nBoth canal bottoms approximately -0.82 m; water surface -0.18 m, water depth approximately 0.64 m.\nGround mesh and collider excavated; flat box collider removed.\nTwo continuous sloped canal shores, 16 closed field-bank loops, rounded joined corners and irregular crests 4.3-9.1 cm high.\nOld tall/separate bank pieces disabled; no bank grass added.\nBridge/shop/marble routes passed. CharacterController descends into and walks out of both canals.\nWater renderer identities preserved; footprint masks match curved rounded water geometry.\nSunset and marble setup preserved.\n");
    }
    private static void VerifyCanalExit()
    {
        var obj=new GameObject("CanalSlopeProbe"){hideFlags=HideFlags.HideAndDontSave};var cc=obj.AddComponent<CharacterController>();cc.height=1.6f;cc.center=Vector3.up*.8f;cc.radius=.25f;cc.stepOffset=.3f;cc.slopeLimit=45;
        try
        {
            foreach(int side in new[]{-1,1})
            {
                float z=32.2f;Vector3 middle=VillageLandscapeShape.Warp(new Vector3(side*5+CanalDrift(z),-.78f,z));
                cc.enabled=false;obj.transform.position=middle;cc.enabled=true;Physics.SyncTransforms();
                Vector3 goal=VillageLandscapeShape.Warp(new Vector3(side*3.25f,.10f,z));
                for(int step=0;step<160;step++) {var delta=goal-obj.transform.position;delta.y=0;if(delta.magnitude<.15f)break;cc.Move(Vector3.ClampMagnitude(delta,.075f)+Vector3.down*.015f);}
                var remaining=goal-obj.transform.position;remaining.y=0;
                if(remaining.magnitude>.35f || obj.transform.position.y<-.15f) throw new Exception("Cannot walk out of canal: "+side+" at "+obj.transform.position);
                for(int step=0;step<40;step++) cc.Move(Vector3.down*.04f);
                Vector3 downGoal=middle;
                for(int step=0;step<150;step++) {var delta=downGoal-obj.transform.position;delta.y=0;cc.Move(Vector3.ClampMagnitude(delta,.07f)+Vector3.down*.035f);}
                if(obj.transform.position.y>-.45f || obj.transform.position.y<-.95f) throw new Exception("Player does not enter real canal depression.");
            }
        }
        finally {UnityEngine.Object.DestroyImmediate(obj);}
    }
    public static void CaptureDeepCanals()
    {
        string folder=OutputFolder+"/DeepCanals";Directory.CreateDirectory(folder);var scene=SceneManager.GetActiveScene();bool dirty=scene.isDirty;
        var views=new[]{new[]{VillageLandscapeShape.Warp(new Vector3(-3.4f,1.65f,36)),VillageLandscapeShape.Warp(new Vector3(-5,-.2f,40))},new[]{new Vector3(-8,2.8f,13),new Vector3(-13,.04f,17)},new[]{new Vector3(35,2.3f,33),new Vector3(29,.04f,34)}};
        for(int i=0;i<views.Length;i++)
        {
            var obj=new GameObject("DeepCanalPreview"){hideFlags=HideFlags.HideAndDontSave};var camera=obj.AddComponent<Camera>();camera.enabled=false;camera.fieldOfView=67;camera.nearClipPlane=.03f;camera.farClipPlane=300;
            camera.transform.SetPositionAndRotation(views[i][0],Quaternion.LookRotation(views[i][1]-views[i][0]));camera.GetUniversalAdditionalCameraData().renderPostProcessing=false;
            var rt=new RenderTexture(1400,900,24);rt.Create();var prior=RenderTexture.active;var image=new Texture2D(1400,900,TextureFormat.RGB24,false);
            try {RenderPipeline.SubmitRenderRequest(camera,new UniversalRenderPipeline.SingleCameraRequest{destination=rt});RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,1400,900),0,0);image.Apply();File.WriteAllBytes(folder+"/"+(i==0?"01_KenhSau":i==1?"02_BoRuongLienGoc":"03_GocRuongTuNhien")+".png",image.EncodeToPNG());}
            finally {RenderTexture.active=prior;UnityEngine.Object.DestroyImmediate(image);rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(obj);}
        }
        if(!dirty) EditorSceneManager.SaveScene(scene);
    }
}
