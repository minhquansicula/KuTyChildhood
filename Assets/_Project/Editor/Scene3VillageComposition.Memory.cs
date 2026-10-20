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
    private const string MemoryAssets=DetailModels+"/MemoryLandscape";
    private const string MemoryRoot="LangQue_HoiUc";
    [MenuItem("KuTy/Scene 3/Compose open memory village")]
    public static void ComposeMemoryVillage()
    {
        var scene=SceneManager.GetActiveScene();
        if(scene.path!=ScenePath || EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("Open village in Edit Mode.");
        if(Find(MemoryRoot)) {ValidateMemoryVillage();CaptureMemoryVillage();return;}
        var village=Find(VillageName);var environment=Find("Scene3_DuongLang_RuongLua");
        if(!village || !environment) throw new Exception("Current village missing.");
        string backup=OutputFolder+"/before-memory-landscape.unity";
        if(!EditorSceneManager.SaveScene(scene,backup,true)) throw new Exception("Cannot back up current scene.");
        var before=GameplaySnapshot();
        var fields=village.GetComponentsInChildren<RuralRiceFieldRenderer>();
        var water=village.GetComponentInChildren<VillageWaterSystem>();
        // These are the only gameplay data changed: vegetation mapping and water contact mapping.
        foreach(var field in fields) before.Remove(field);before.Remove(water);
        var originalWater=water.surfaces.Select(s=>s.renderer).ToArray();
        Undo.IncrementCurrentGroup();int undo=Undo.GetCurrentGroup();Undo.SetCurrentGroupName("Open nostalgic village landscape");
        Undo.RegisterFullObjectHierarchyUndo(environment,"Original village");
        Undo.RegisterFullObjectHierarchyUndo(Find("Ground"),"Original village ground");
        try
        {
            EnsureFolder(MemoryAssets);
            var root=Group(MemoryRoot,village.transform).transform;
            var earth=LoadSceneMaterial("Scene3_Earth");var wood=LoadSceneMaterial("Scene3_Timber");
            var green=LoadSceneMaterial("Scene3_Green");var dark=LoadSceneMaterial("Scene3_LeafDark");var light=LoadSceneMaterial("Scene3_LeafLight");
            // Bake the deformation only into ground meshes, never into imported houses or characters.
            int meshIndex=0;
            var landscape=environment.transform.Find("RuongLua_KenhNuoc_CayXanh");
            foreach(var filter in landscape.GetComponentsInChildren<MeshFilter>()) BendGroundMesh(filter,meshIndex++);
            foreach(var field in fields)
            {
                foreach(var filter in field.GetComponentsInChildren<MeshFilter>()) BendGroundMesh(filter,meshIndex++);
                field.meanderingField=true;field.Regenerate();EditorUtility.SetDirty(field);
            }
            for(int i=0;i<water.surfaces.Length;i++) {var s=water.surfaces[i];s.meandering=true;water.surfaces[i]=s;}
            ApplyVillageWaterBlocks(water);EditorUtility.SetDirty(water);
            // Keep bridge crossing stations fixed; curve and narrow the approach paths between them.
            foreach(var path in village.GetComponentsInChildren<MeshFilter>().Where(f=>f.name.StartsWith("NgoNha") || f.name=="LoiCuoiLang" || f.name=="LoiVaoTiem"))
                BendGroundMesh(path,meshIndex++);
            foreach(var bridge in village.GetComponentsInChildren<Transform>().Where(t=>t.name=="CauVanQuaMuong" || t.name=="NepVan"))
                bridge.position=VillageLandscapeShape.Warp(bridge.position);
            float[] turns={-12,17,9,-14,11};float[] along={.8f,1.6f,-.8f,.7f,-.6f};
            for(int i=0;i<HomeNames.Length;i++)
            {
                var site=village.transform.Find("NhaDan_VaSanVuon/SanNha_Gon_0"+(i+1));
                var home=site.Find(HomeNames[i]);var pivot=home.position;
                home.position+=new Vector3(VillageLandscapeShape.RoadBend(pivot.z),0,along[i]);home.Rotate(0,turns[i],0,Space.World);
                var yard=site.Find("SanDatNha");
                if(yard) ReplaceEarthPad(yard,new Vector3(HomePositions[i].x+VillageLandscapeShape.RoadBend(HomePositions[i].z),.1f,HomePositions[i].z),8,6.7f,i+17,earth);
                foreach(var propName in new[]{"CayOi_SanNha","BoBanGheGo"})
                {
                    var prop=site.Find(propName);if(!prop) continue;
                    var center=RendererBounds(prop).center;prop.position+=Vector3.right*VillageLandscapeShape.RoadBend(center.z);
                    prop.RotateAround(center,Vector3.up,i%2==0?7:-11);
                }
                foreach(var fence in site.GetComponentsInChildren<Transform>().Where(t=>t.name.StartsWith("RaoTre_") || t.name=="CocRaoTre")) fence.gameObject.SetActive(false);
            }
            var gardens=village.transform.Find("VuonRau_CayAnQua_LoiNho");
            foreach(var t in gardens.Cast<Transform>().Where(t=>t.name.StartsWith("BuiChuoi") || t.name.StartsWith("CayAnQua")))
            {t.position=VillageLandscapeShape.Warp(t.position);t.Rotate(0,(t.GetSiblingIndex()%3)*21,0);}
            // Remove the continuous geometric perimeter; retain collision limits and village entrance.
            var edge=village.transform.Find("LuyTre_RanhGioiLang");
            foreach(Transform child in edge)
                if(!child.name.StartsWith("RanhGioi_") && !child.name.Contains("CongLang")) child.gameObject.SetActive(false);
            BuildOpenHorizon(root,green,wood,dark,light);
            BuildMemoryClearing(village.transform.Find("BaiBanBi_VaNhomTre"),root,earth,wood,dark,light);
            // Local shade has only one low-poly caster; dense rice and horizon remain shadow-free.
            var sun=Find("Sun").GetComponent<Light>();Undo.RecordObject(sun,"Gentle late afternoon");
            sun.color=new Color(1,.87f,.7f);sun.intensity=1.05f;sun.shadows=LightShadows.Hard;sun.shadowStrength=.65f;sun.shadowNormalBias=.16f;
            var skyPath="Assets/_Project/Art/Materials/Scene3/MemorySky.mat";
            var sky=AssetDatabase.LoadAssetAtPath<Material>(skyPath);
            if(!sky) {sky=new Material(Shader.Find("KuTy/VillageMemorySky")){name="MemorySky"};AssetDatabase.CreateAsset(sky,skyPath);}
            RenderSettings.skybox=sky;RenderSettings.sun=sun;
            RenderSettings.ambientMode=AmbientMode.Trilight;RenderSettings.ambientSkyColor=new Color(.43f,.49f,.52f);
            RenderSettings.ambientEquatorColor=new Color(.34f,.32f,.27f);RenderSettings.ambientGroundColor=new Color(.18f,.20f,.16f);
            RenderSettings.fog=true;RenderSettings.fogMode=FogMode.ExponentialSquared;RenderSettings.fogDensity=.0038f;
            RenderSettings.fogColor=new Color(.66f,.65f,.55f);
            DynamicGI.UpdateEnvironment();
            VerifyGameplay(before);
            if(!originalWater.SequenceEqual(water.surfaces.Select(s=>s.renderer))) throw new Exception("Water renderer identities changed.");
            Physics.SyncTransforms();ValidateMemoryVillage();
            AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);
            if(!EditorSceneManager.SaveScene(scene)) throw new Exception("Cannot save memory village.");
            Undo.CollapseUndoOperations(undo);
            CaptureMemoryVillage();EditorSceneManager.SaveScene(scene);Inspect(true);SceneView.RepaintAll();
        }
        catch {Undo.RevertAllDownToGroup(undo);throw;}
    }

    private struct GroundVertex
    {
        public Vector3 p;public Vector2 uv;public Color color;
        public static GroundVertex Mid(GroundVertex a,GroundVertex b) {return new GroundVertex{p=(a.p+b.p)/2,uv=(a.uv+b.uv)/2,color=Color.Lerp(a.color,b.color,.5f)};}
    }
    private static void BendGroundMesh(MeshFilter filter,int number)
    {
        // Unity permits mesh reads in Edit Mode even for models whose runtime Read/Write is off.
        var source=filter.sharedMesh;if(!source) throw new Exception("Ground mesh missing: "+filter.name);
        var positions=source.vertices;var uvs=source.uv;var colors=source.colors;
        var vertices=new List<Vector3>();var tex=new List<Vector2>();var tint=new List<Color>();
        var triangles=new List<int>[source.subMeshCount];
        Action<GroundVertex,GroundVertex,GroundVertex,int,int> add=null;
        add=(a,b,c,sub,depth)=>
        {
            float ab=(a.p-b.p).sqrMagnitude,bc=(b.p-c.p).sqrMagnitude,ca=(c.p-a.p).sqrMagnitude;
            if(depth<12 && Mathf.Max(ab,Mathf.Max(bc,ca))>16)
            {
                if(ab>=bc && ab>=ca) {var m=GroundVertex.Mid(a,b);add(a,m,c,sub,depth+1);add(m,b,c,sub,depth+1);}
                else if(bc>=ca) {var m=GroundVertex.Mid(b,c);add(a,b,m,sub,depth+1);add(a,m,c,sub,depth+1);}
                else {var m=GroundVertex.Mid(c,a);add(a,b,m,sub,depth+1);add(m,b,c,sub,depth+1);}return;
            }
            foreach(var v in new[]{a,b,c}) {triangles[sub].Add(vertices.Count);vertices.Add(filter.transform.InverseTransformPoint(VillageLandscapeShape.Warp(v.p)));tex.Add(v.uv);tint.Add(v.color);}
        };
        Func<int,GroundVertex> read=i=>new GroundVertex{p=filter.transform.TransformPoint(positions[i]),uv=uvs.Length==positions.Length?uvs[i]:Vector2.zero,color=colors.Length==positions.Length?colors[i]:Color.white};
        for(int sub=0;sub<source.subMeshCount;sub++)
        {
            triangles[sub]=new List<int>();var indices=source.GetTriangles(sub);
            for(int i=0;i<indices.Length;i+=3) add(read(indices[i]),read(indices[i+1]),read(indices[i+2]),sub,0);
        }
        var mesh=new Mesh{name="MemoryGround_"+number,indexFormat=IndexFormat.UInt32};mesh.SetVertices(vertices);mesh.SetUVs(0,tex);mesh.SetColors(tint);mesh.subMeshCount=triangles.Length;
        for(int i=0;i<triangles.Length;i++) mesh.SetTriangles(triangles[i],i);
        mesh.RecalculateNormals();mesh.RecalculateBounds();mesh.RecalculateTangents();
        AssetDatabase.CreateAsset(mesh,MemoryAssets+"/"+mesh.name+".asset");filter.sharedMesh=mesh;
        var collider=filter.GetComponent<MeshCollider>();if(collider) collider.sharedMesh=mesh;
        // The old box under a long cuboid path must follow the new curve too.
        var box=filter.GetComponent<BoxCollider>();
        if(box) {Undo.DestroyObjectImmediate(box);var curved=Undo.AddComponent<MeshCollider>(filter.gameObject);curved.sharedMesh=mesh;}
    }

    private static Mesh MemoryMesh(string name,List<Vector3> vertices,List<int> triangles)
    {
        var mesh=new Mesh{name=name,indexFormat=IndexFormat.UInt32};mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);
        mesh.SetUVs(0,vertices.Select(v=>new Vector2(v.x*.18f,v.z*.18f)).ToList());mesh.RecalculateNormals();mesh.RecalculateBounds();mesh.RecalculateTangents();
        AssetDatabase.CreateAsset(mesh,MemoryAssets+"/"+name+".asset");return mesh;
    }
    private static void ReplaceEarthPad(Transform pad,Vector3 center,float radiusX,float radiusZ,int seed,Material earth)
    {
        pad.SetPositionAndRotation(Vector3.zero,Quaternion.identity);pad.localScale=Vector3.one;
        var vertices=new List<Vector3>{center};var triangles=new List<int>();
        for(int i=0;i<48;i++)
        {
            float a=i*Mathf.PI*2/48;float r=1+.055f*Mathf.Sin(a*3+seed)+.035f*Mathf.Sin(a*7+seed*.7f);
            vertices.Add(center+new Vector3(Mathf.Cos(a)*radiusX*r,0,Mathf.Sin(a)*radiusZ*r));
            triangles.Add(0);triangles.Add((i+1)%48+1);triangles.Add(i+1);
        }
        var mesh=MemoryMesh("DatNenTuNhien_"+seed,vertices,triangles);pad.GetComponent<MeshFilter>().sharedMesh=mesh;pad.GetComponent<Renderer>().sharedMaterial=earth;
        var box=pad.GetComponent<BoxCollider>();var physicsMaterial=box?box.sharedMaterial:null;float contactOffset=box?box.contactOffset:.0002f;
        if(box) Undo.DestroyObjectImmediate(box);
        var collision=pad.GetComponent<MeshCollider>();if(!collision) collision=Undo.AddComponent<MeshCollider>(pad.gameObject);collision.sharedMesh=mesh;
        collision.sharedMaterial=physicsMaterial;collision.contactOffset=contactOffset;
    }

    private static Material MemoryMaterial(string name,Color color)
    {
        string path="Assets/_Project/Art/Materials/Scene3/"+name+".mat";
        var material=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(!material) {material=new Material(Shader.Find("Universal Render Pipeline/Lit")){name=name};AssetDatabase.CreateAsset(material,path);}
        material.SetColor("_BaseColor",color);material.SetFloat("_Smoothness",.08f);EditorUtility.SetDirty(material);return material;
    }
    private static void BuildOpenHorizon(Transform parent,Material grass,Material wood,Material dark,Material light)
    {
        // The background is visual only: no gameplay area, colliders or individual grass objects.
        var vertices=new List<Vector3>();var triangles=new List<int>();
        vertices.Add(new Vector3(0,-.025f,37));
        for(int i=0;i<96;i++)
        {
            float a=i*Mathf.PI*2/96;float radius=220+10*Mathf.Sin(a*5);
            vertices.Add(new Vector3(Mathf.Cos(a)*radius,-.03f,37+Mathf.Sin(a)*radius));
            triangles.Add(0);triangles.Add((i+1)%96+1);triangles.Add(i+1);
        }
        MeshProp(parent,"DongBang_MoRongTamNhin",Vector3.zero,MemoryMesh("NenDongXa",vertices,triangles),MemoryMaterial("DongXa_XanhXam",new Color(.29f,.34f,.19f)));
        var random=new System.Random(6412);
        var tones=new[]{new Color(.46f,.45f,.19f),new Color(.36f,.41f,.19f),new Color(.54f,.49f,.22f)};
        for(int layer=0;layer<3;layer++)
        {
            vertices=new List<Vector3>();triangles=new List<int>();
            for(int patch=0;patch<18;patch++)
            {
                float a=(patch+.07f*(layer+1))*Mathf.PI*2/18;
                float b=a+.29f;float inner=layer==0?57:91+layer*22;float outer=inner+29+(float)random.NextDouble()*13;
                var points=new List<Vector3>();
                for(int i=0;i<=6;i++) {float t=Mathf.Lerp(a,b,i/6f);float r=inner+3*Mathf.Sin(t*7);points.Add(new Vector3(Mathf.Cos(t)*r,.005f,37+Mathf.Sin(t)*r));}
                for(int i=6;i>=0;i--) {float t=Mathf.Lerp(a,b,i/6f);float r=outer+5*Mathf.Sin(t*5);points.Add(new Vector3(Mathf.Cos(t)*r,.005f,37+Mathf.Sin(t)*r));}
                int start=vertices.Count;vertices.AddRange(points);
                for(int i=1;i<points.Count-1;i++) {triangles.Add(start);triangles.Add(start+i);triangles.Add(start+i+1);}
            }
            MeshProp(parent,"ThuaRuongXa_"+layer,Vector3.zero,MemoryMesh("LuaXa_MangMau_"+layer,vertices,triangles),MemoryMaterial("LuaXa_Mau_"+layer,tones[layer]));
        }
        // Scattered small copses leave broad views of the fields between them.
        Vector3[] clusters={new Vector3(-49,0,14),new Vector3(-54,0,69),new Vector3(51,0,9),new Vector3(53,0,58),new Vector3(16,0,96),new Vector3(-26,0,108),new Vector3(77,0,108),new Vector3(-89,0,39)};
        var combined=new List<CombineInstance>();var sourceTree=AssetDatabase.LoadAssetAtPath<Mesh>(DetailModels+"/CayOi_Village.asset");
        var branches=new List<CombineInstance>();var crowns=new List<CombineInstance>();
        for(int group=0;group<clusters.Length;group++)
        for(int tree=0;tree<3;tree++)
        {
            var p=clusters[group]+new Vector3((float)random.NextDouble()*9-4,0,(float)random.NextDouble()*9-4);
            float scale=1.1f+(float)random.NextDouble()*.7f;
            var matrix=Matrix4x4.TRS(p,Quaternion.Euler(0,random.Next(360),0),Vector3.one*scale);
            branches.Add(new CombineInstance{mesh=sourceTree,subMeshIndex=0,transform=matrix});crowns.Add(new CombineInstance{mesh=sourceTree,subMeshIndex=1,transform=matrix});
        }
        foreach(var part in new[]{branches,crowns})
        {
            var mesh=new Mesh{name=part==branches?"CumCayXa_Than":"CumCayXa_Tan"};mesh.CombineMeshes(part.ToArray());
            AssetDatabase.CreateAsset(mesh,MemoryAssets+"/"+mesh.name+".asset");MeshProp(parent,mesh.name,Vector3.zero,mesh,part==branches?wood:dark);
        }
        // A few closer clumps belong to yards/field corners, never a continuous fence.
        foreach(var p in new[]{new Vector3(-39,0,21),new Vector3(39,0,31),new Vector3(-37,0,72),new Vector3(36,0,76),new Vector3(27,0,-8)})
            MeshProp(parent,"CumTre_LechNhip",VillageLandscapeShape.Warp(p),BambooMesh(),wood,light);
        // Cut off the four corners of the playable perimeter so it is not a rectangular route.
        foreach(int side in new[]{-1,1}) foreach(int end in new[]{-1,1})
        {
            var limit=Group("BoCong_GioiHan",parent);limit.transform.position=new Vector3(side*40,2.5f,end<0?-9:82);
            limit.transform.rotation=Quaternion.Euler(0,side*end*45,0);limit.AddComponent<BoxCollider>().size=new Vector3(1,5,13);
        }
    }

    private static void MemoryEllipsoid(Geometry g,Vector3 center,Vector3 size,bool foliage)
    {
        Func<int,int,Vector3> p=(row,col)=>{float a=row*Mathf.PI/8,b=col*Mathf.PI*2/16;return center+Vector3.Scale(new Vector3(Mathf.Sin(a)*Mathf.Cos(b),Mathf.Cos(a),Mathf.Sin(a)*Mathf.Sin(b)),size);};
        for(int r=0;r<8;r++) for(int c=0;c<16;c++) {g.Triangle(p(r,c),p(r+1,c+1),p(r+1,c),foliage);g.Triangle(p(r,c),p(r,c+1),p(r+1,c+1),foliage);}
    }
    private static void BuildMemoryClearing(Transform clearing,Transform root,Material earth,Material wood,Material dark,Material light)
    {
        ReplaceEarthPad(clearing.Find("SanBi_DatNen"),new Vector3(-15,.10f,44),7.1f,6.3f,99,earth);
        var oldTree=clearing.Find("CayOi_CheBaiBi");if(oldTree) oldTree.gameObject.SetActive(false);
        var g=new Geometry();
        g.Cylinder(Vector3.zero,new Vector3(.2f,3.6f,0),.26f);
        g.Cylinder(new Vector3(.15f,2.4f,0),new Vector3(2.3f,4.7f,-1.9f),.12f);
        g.Cylinder(new Vector3(.15f,2.8f,0),new Vector3(-1.7f,4.9f,1.2f),.14f);
        g.Cylinder(new Vector3(.15f,2.5f,0),new Vector3(.2f,4.6f,2.4f),.10f);
        MemoryEllipsoid(g,new Vector3(.1f,5.0f,.2f),new Vector3(3.4f,1.6f,3),true);
        MemoryEllipsoid(g,new Vector3(2.2f,4.9f,-1.8f),new Vector3(2.7f,1.25f,2.4f),true);
        MemoryEllipsoid(g,new Vector3(-1.8f,5.1f,1),new Vector3(2.3f,1.3f,2.6f),true);
        MemoryEllipsoid(g,new Vector3(.3f,4.6f,2.5f),new Vector3(2.5f,1.15f,2.4f),true);
        var tree=MeshProp(root,"CayKyUc_CheSanBi",new Vector3(-18.4f,.1f,46.8f),g.Save("CayKyUc_TanRong"),wood,MemoryMaterial("TanCayKyUc_Mat",new Color(.22f,.32f,.15f)));
        tree.GetComponent<Renderer>().shadowCastingMode=ShadowCastingMode.On;
        var trunk=tree.AddComponent<CapsuleCollider>();trunk.radius=.3f;trunk.height=3.4f;trunk.center=Vector3.up*1.7f;
        var bench=clearing.Find("GheNghiDuoiCay");Vector3 benchCenter=new Vector3(-20.4f,.53f,48.1f);
        foreach(var name in new[]{"GheNghiDuoiCay","ChanGhe_1","ChanGhe_2"})
        {
            var part=clearing.Find(name);if(!part) continue;part.RotateAround(bench.position,Vector3.up,-18);
        }
        var kids=clearing.GetComponentsInChildren<Transform>().Where(t=>t.name.StartsWith("BanBe_PhacThao_")).OrderBy(t=>t.name).ToArray();
        var poses=new[]{new Vector3(-17.3f,.1f,43.7f),new Vector3(-13.1f,.1f,45.6f),new Vector3(-16.2f,.1f,46.1f)};
        for(int i=0;i<kids.Length;i++) {kids[i].position=poses[i%poses.Length];kids[i].rotation=Quaternion.LookRotation(new Vector3(-15,.1f,44)-kids[i].position);}
        // A curved, gently flaring approach announces the clearing from the main road.
        var approach=clearing.Find("LoiVaoSanBi");if(approach) approach.GetComponent<Renderer>().enabled=false;
        var points=new[]{new Vector3(-6.2f,.11f,44),new Vector3(-8.1f,.11f,43.7f),new Vector3(-10,.11f,42.8f),new Vector3(-12.5f,.11f,41.2f)};
        var vertices=new List<Vector3>();var tris=new List<int>();
        for(int i=0;i<points.Length;i++)
        {
            var tangent=(points[Mathf.Min(i+1,points.Length-1)]-points[Mathf.Max(0,i-1)]).normalized;var side=Vector3.Cross(tangent,Vector3.up);
            float width=i==0?1.05f:1.2f;vertices.Add(points[i]-side*width);vertices.Add(points[i]+side*width);
            if(i>0) {int a=(i-1)*2;tris.AddRange(new[]{a,a+1,a+2,a+1,a+3,a+2});}
        }
        var path=MeshProp(root,"LoiCong_DanVaoKyUc",Vector3.zero,MemoryMesh("LoiSanBi_Cong",vertices,tris),earth);path.AddComponent<MeshCollider>().sharedMesh=path.GetComponent<MeshFilter>().sharedMesh;
        var details=new Geometry();
        // Bench-side school bag, a wooden marble box and two small sandals: quiet story props.
        MemoryEllipsoid(details,new Vector3(-20.9f,.17f,47.3f),new Vector3(.24f,.14f,.18f),false);
        foreach(var pos in new[]{new Vector3(-16.9f,.12f,42.4f),new Vector3(-16.65f,.12f,42.48f)})
        {MemoryEllipsoid(details,pos,new Vector3(.07f,.025f,.15f),false);details.Cylinder(pos+new Vector3(-.06f,.05f,0),pos+new Vector3(.06f,.05f,.03f),.014f);}
        MeshProp(root,"DoChoiVaDep_DeBenSan",Vector3.zero,details.Save("KyUc_DoDungNho"),wood);
        Block(root,"HopBi_GoCu",new Vector3(-20.5f,.68f,48.15f),new Vector3(.27f,.1f,.21f),wood,false);
    }

    public static void ValidateMemoryVillage()
    {
        if(Find(OrganicRoot)){ValidateOrganicVillage();return;}
        if(SceneManager.GetActiveScene().path!=ScenePath || !Find(MemoryRoot)) throw new Exception("Memory composition missing.");
        var village=Find(VillageName);var fields=village.GetComponentsInChildren<RuralRiceFieldRenderer>();
        if(fields.Length!=16 || fields.Any(f=>!f.meanderingField || f.PlantCount==0 || !f.sharedMaterial.enableInstancing)) throw new Exception("Curved rice fields are incomplete.");
        for(int i=0;i<HomeNames.Length;i++)
        {
            var home=village.transform.Find("NhaDan_VaSanVuon/SanNha_Gon_0"+(i+1)+"/"+HomeNames[i]);var bounds=RendererBounds(home);
            if(bounds.min.y<.07f || bounds.size.y<3 || bounds.size.y>6) throw new Exception("House grounding failed: "+home.name);
            if(!home.GetComponentInChildren<LODGroup>()) throw new Exception("House LOD missing: "+home.name);
        }
        var area=village.transform.Find("BaiBanBi_VaNhomTre");var ring=area.Find("ScoringRing").GetComponent<Collider>().bounds;
        if(Vector2.Distance(new Vector2(ring.center.x,ring.center.z),new Vector2(-15,44))>.01f || Vector3.Distance(area.Find("PlayerMarble").position,area.Find("ShootPosition").position)>.001f)
            throw new Exception("Marble gameplay anchors changed.");
        var floor=area.Find("SanBi_DatNen").GetComponent<MeshCollider>();
        foreach(var name in MarbleNames)
        {
            var ball=area.Find(name);float bottom=ball.position.y-.05f;
            if(!floor.Raycast(new Ray(ball.position+Vector3.up,Vector3.down),out var hit,2) || Mathf.Abs(hit.point.y-bottom)>.002f)
                throw new Exception("Marble not resting on new clearing floor: "+name);
        }
        var water=village.GetComponentInChildren<VillageWaterSystem>();
        if(water.surfaces.Length!=18 || water.surfaces.Any(s=>!s.meandering || !s.renderer)) throw new Exception("Curved water incomplete.");
        foreach(var s in water.surfaces)
        {
            var p=VillageLandscapeShape.Warp(new Vector3((s.rectangle.x+s.rectangle.z)*.5f,s.height,(s.rectangle.y+s.rectangle.w)*.5f));
            if(!s.Contains(p)) throw new Exception("Curved water contact mismatch.");
            if(s.canal)
            {
                var bank=VillageLandscapeShape.Warp(new Vector3(s.rectangle.z+.22f,s.height,p.z));
                if(s.Contains(bank)) throw new Exception("Dry canal bank triggers water.");
            }
        }
        string contact=VerifyVillageWaterInteraction(water);water.ClearInteraction();
        Physics.SyncTransforms();VerifyWalking();
        // Walking into the playable ring, not merely reaching the clearing entrance.
        var probe=new GameObject("MemoryWalkProbe"){hideFlags=HideFlags.HideAndDontSave};var cc=probe.AddComponent<CharacterController>();cc.height=1.6f;cc.radius=.25f;cc.center=Vector3.up*.8f;cc.stepOffset=.3f;
        try
        {
            cc.enabled=false;probe.transform.position=new Vector3(-10,.13f,42.8f);cc.enabled=true;
            foreach(var target in new[]{new Vector3(-13.6f,.13f,41.8f),new Vector3(-15,.13f,43.2f)})
            {
                for(int step=0;step<100;step++) {var delta=target-probe.transform.position;delta.y=0;if(delta.magnitude<.15f)break;cc.Move(Vector3.ClampMagnitude(delta,.1f)+Vector3.down*.03f);}
                var remaining=target-probe.transform.position;remaining.y=0;if(remaining.magnitude>.35f) throw new Exception("Marble approach blocked.");
            }
        }
        finally {UnityEngine.Object.DestroyImmediate(probe);}
        if(AllTransforms().Any(t=>t.gameObject.activeInHierarchy && (t.name=="CoBoRuong_Gop" || t.name=="CoBoKenh_Gop"))) throw new Exception("Removed bank grass returned.");
        var errors=ShaderUtil.GetShaderMessages(Shader.Find("KuTy/VillageWater")).Where(e=>e.severity==UnityEditor.Rendering.ShaderCompilerMessageSeverity.Error).ToArray();
        if(errors.Length>0) throw new Exception("Water shader error.");
        var root=Find(MemoryRoot);
        long triangles=root.GetComponentsInChildren<MeshFilter>().Sum(f=>(long)f.sharedMesh.triangles.Length/3);
        int renders=root.GetComponentsInChildren<Renderer>().Length;
        File.WriteAllText(OutputFolder+"/memory_landscape_report.txt","PASS\n16 curved instanced rice fields; "+fields.Sum(f=>f.PlantCount)+" plants\n5 imported houses retain LOD, upright geometry and original scale\nMarble anchors preserved; approach walk test and floor rays beneath all six marbles passed\nShop and marble bridge traversal; four collision limits passed\n"+contact+"\nBackground/detail triangles: "+triangles+"; renderers: "+renders+"\nBank grass remains removed\nBackup: before-memory-landscape.unity\n");
    }

    public static void PolishMemoryVillage()
    {
        var scene=SceneManager.GetActiveScene();if(scene.path!=ScenePath || !Find(MemoryRoot)) throw new Exception("Memory village missing.");
        var root=Find(MemoryRoot).transform;var before=GameplaySnapshot();
        Undo.IncrementCurrentGroup();int undo=Undo.GetCurrentGroup();Undo.SetCurrentGroupName("Natural horizon and childhood clearing details");
        Undo.RegisterFullObjectHierarchyUndo(root.gameObject,"Memory landscape detail");
        try
        {
            var plain=root.Find("DongBang_MoRongTamNhin").GetComponent<Renderer>();plain.sharedMaterial=Find("Ground").GetComponent<Renderer>().sharedMaterial;
            foreach(var t in root.Cast<Transform>().Where(t=>t.name.StartsWith("ThuaRuongXa_")).ToArray()) Undo.DestroyObjectImmediate(t.gameObject);
            if(!root.Find("LuaXa_NhinQuaLang"))
            {
                var random=new System.Random(7301);
                var source=Find("Ground").GetComponent<Renderer>().sharedMaterial;
                for(int layer=0;layer<3;layer++)
                {
                    var vertices=new List<Vector3>();var triangles=new List<int>();var mask=new List<Color>();
                    for(int patch=0;patch<11;patch++)
                    {
                        float a=(float)random.NextDouble()*Mathf.PI*2;float distance=62+(float)random.NextDouble()*100;
                        Vector3 center=new Vector3(Mathf.Cos(a)*distance,.004f+patch*.001f,37+Mathf.Sin(a)*distance);
                        float rx=17+(float)random.NextDouble()*18,rz=14+(float)random.NextDouble()*18,rotation=(float)random.NextDouble()*Mathf.PI;
                        int start=vertices.Count;vertices.Add(center);mask.Add(Color.red);
                        for(int i=0;i<32;i++)
                        {
                            float t=i*Mathf.PI*2/32;float radius=1+.10f*Mathf.Sin(t*3+patch)+.06f*Mathf.Cos(t*5+layer);
                            var offset=new Vector3(Mathf.Cos(t)*rx*radius,0,Mathf.Sin(t)*rz*radius);
                            vertices.Add(center+Quaternion.Euler(0,rotation*Mathf.Rad2Deg,0)*offset);mask.Add(Color.black);
                            triangles.Add(start);triangles.Add(start+(i+1)%32+1);triangles.Add(start+i+1);
                        }
                    }
                    var mesh=MemoryMesh("DongXa_ThuaTuNhien_"+layer,vertices,triangles);mesh.SetColors(mask);EditorUtility.SetDirty(mesh);
                    string path="Assets/_Project/Art/Materials/Scene3/DongXa_Matte_"+layer+".mat";
                    var material=AssetDatabase.LoadAssetAtPath<Material>(path);
                    if(!material) {material=new Material(source){name="DongXa_Matte_"+layer};AssetDatabase.CreateAsset(material,path);}
                    material.SetTexture("_SoilMap",Texture2D.whiteTexture);material.SetColor("_SoilColor",source.GetColor("_GrassColor"));
                    material.SetColor("_GrassColor",new[]{new Color(.4f,.43f,.17f),new Color(.37f,.4f,.15f),new Color(.46f,.46f,.19f)}[layer]);
                    material.SetFloat("_UseVertexCoverage",1);material.SetFloat("_GrassCoverage",1);EditorUtility.SetDirty(material);
                    MeshProp(root,"DongXa_MauLuaTuNhien_"+layer,Vector3.zero,mesh,material);
                }
                // A single merged far mesh adds stalk silhouettes at the horizon at a fixed small budget.
                var plants=new List<CombineInstance>();var rice=LoadRiceMesh(2);
                for(float z=-45;z<151;z+=3.1f) for(float x=-107;x<108;x+=3.1f)
                {
                    if(x>-44 && x<44 && z>-15 && z<88) continue;
                    if(new Vector2(x,z-37).sqrMagnitude>124*124 || random.NextDouble()>.50) continue;
                    Vector3 p=new Vector3(x+(float)random.NextDouble()*1.8f,.012f,z+(float)random.NextDouble()*1.8f);
                    float size=.5f+(float)random.NextDouble()*.10f;
                    plants.Add(new CombineInstance{mesh=rice,subMeshIndex=0,transform=Matrix4x4.TRS(p,Quaternion.Euler(0,random.Next(360),0),Vector3.one*size)});
                }
                var combined=new Mesh{name="LuaXa_BongCayGop",indexFormat=IndexFormat.UInt32};combined.CombineMeshes(plants.ToArray());
                AssetDatabase.CreateAsset(combined,MemoryAssets+"/LuaXa_BongCayGop.asset");
                var far=MeshProp(root,"LuaXa_NhinQuaLang",Vector3.zero,combined,Find(VillageName).GetComponentInChildren<RuralRiceFieldRenderer>().sharedMaterial);
                far.GetComponent<Renderer>().receiveShadows=false;
            }
            var hero=root.Find("CayKyUc_CheSanBi").GetComponent<MeshFilter>();
            if(hero.sharedMesh.name!="CayKyUc_TanMem")
            {
                var mesh=UnityEngine.Object.Instantiate(hero.sharedMesh);mesh.name="CayKyUc_TanMem";
                var positions=mesh.vertices;var normals=mesh.normals;
                var centers=new[]{new Vector3(.1f,5,.2f),new Vector3(2.2f,4.9f,-1.8f),new Vector3(-1.8f,5.1f,1),new Vector3(.3f,4.6f,2.5f)};
                var sizes=new[]{new Vector3(3.4f,1.6f,3),new Vector3(2.7f,1.25f,2.4f),new Vector3(2.3f,1.3f,2.6f),new Vector3(2.5f,1.15f,2.4f)};
                int firstLeaf=4*7*2*3;
                for(int lobe=0;lobe<4;lobe++) for(int v=0;v<8*16*2*3;v++)
                {
                    int i=firstLeaf+lobe*768+v;Vector3 d=positions[i]-centers[lobe];Vector3 s=sizes[lobe];
                    normals[i]=new Vector3(d.x/(s.x*s.x),d.y/(s.y*s.y),d.z/(s.z*s.z)).normalized;
                }
                mesh.normals=normals;AssetDatabase.CreateAsset(mesh,MemoryAssets+"/CayKyUc_TanMem.asset");hero.sharedMesh=mesh;
            }
            hero.GetComponent<Renderer>().sharedMaterials=new[]{LoadSceneMaterial("Scene3_Timber"),MemoryMaterial("TanCayKyUc_Mat",new Color(.31f,.42f,.19f))};
            var area=Find(VillageName).transform.Find("BaiBanBi_VaNhomTre");
            foreach(var t in area.GetComponentsInChildren<Transform>(true).Where(t=>t.name=="BangSanBi" || t.name=="CotBangSanBi" || t.name=="TenSanBi"))
            {Undo.RecordObject(t.gameObject,"Remove prototype sign from childhood clearing");t.gameObject.SetActive(false);}
            var approach=area.Find("LoiVaoSanBi");var oldCollider=approach.GetComponent<Collider>();if(oldCollider) {Undo.RecordObject(oldCollider,"Use curved clearing path collider");oldCollider.enabled=false;}
            var sky=RenderSettings.skybox;sky.SetColor("_Top",new Color(.34f,.53f,.7f));sky.SetColor("_Horizon",new Color(.86f,.74f,.56f));EditorUtility.SetDirty(sky);
            var sun=Find("Sun").GetComponent<Light>();Undo.RecordObject(sun,"Warm memory afternoon");sun.intensity=1.17f;
            RenderSettings.ambientEquatorColor=new Color(.4f,.4f,.33f);RenderSettings.ambientSkyColor=new Color(.49f,.56f,.59f);
            DynamicGI.UpdateEnvironment();VerifyGameplay(before);Physics.SyncTransforms();ValidateMemoryVillage();
            AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);if(!EditorSceneManager.SaveScene(scene)) throw new Exception("Cannot save polished village.");
            Undo.CollapseUndoOperations(undo);CaptureMemoryVillage();EditorSceneManager.SaveScene(scene);Inspect(true);SceneView.RepaintAll();
        }
        catch {Undo.RevertAllDownToGroup(undo);throw;}
    }

    public static void FinalizeMemoryVillage()
    {
        var scene=SceneManager.GetActiveScene();if(scene.path!=ScenePath || !Find(MemoryRoot)) throw new Exception("Memory scene missing.");
        var floor=Find(VillageName).transform.Find("BaiBanBi_VaNhomTre/SanBi_DatNen").GetComponent<MeshCollider>();
        Undo.RecordObject(floor,"Preserve small marble contact offset");floor.contactOffset=.0002f;
        ValidateMemoryVillage();string physics=VerifyMemoryMarbleFloor(floor);
        CaptureMemoryVillage();EditorSceneManager.MarkSceneDirty(scene);AssetDatabase.SaveAssets();
        if(!EditorSceneManager.SaveScene(scene)) throw new Exception("Cannot save completed village.");
        File.AppendAllText(OutputFolder+"/memory_landscape_report.txt",physics+"\nScene saved in Edit Mode.\n");
        Inspect(true);SceneView.RepaintAll();
    }
    private static string VerifyMemoryMarbleFloor(MeshCollider original)
    {
        var previous=SceneManager.GetActiveScene();var scene=EditorSceneManager.NewPreviewScene();var physics=scene.GetPhysicsScene();
        try
        {
            if(!physics.IsValid() || physics.Equals(Physics.defaultPhysicsScene)) throw new Exception("Refusing to simulate real village.");
            var ground=new GameObject("ClearingFloorCopy");SceneManager.MoveGameObjectToScene(ground,scene);
            ground.transform.SetPositionAndRotation(original.transform.position,original.transform.rotation);ground.transform.localScale=original.transform.lossyScale;
            var mesh=ground.AddComponent<MeshCollider>();mesh.sharedMesh=original.sharedMesh;mesh.contactOffset=original.contactOffset;mesh.sharedMaterial=original.sharedMaterial;
            var player=Find(VillageName).transform.Find("BaiBanBi_VaNhomTre/PlayerMarble");var reference=player.GetComponent<Rigidbody>();
            var bodies=new Rigidbody[2];float targetStart=43.6f;
            for(int i=0;i<2;i++)
            {
                var ball=new GameObject("MarblePhysicsCopy"+i);SceneManager.MoveGameObjectToScene(ball,scene);ball.transform.position=new Vector3(-15,.1501f,i==0?43.36f:targetStart);
                var collider=ball.AddComponent<SphereCollider>();collider.radius=.05f;collider.contactOffset=.0002f;collider.sharedMaterial=player.GetComponent<SphereCollider>().sharedMaterial;
                var body=ball.AddComponent<Rigidbody>();body.mass=reference.mass;body.drag=reference.drag;body.angularDrag=reference.angularDrag;body.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;bodies[i]=body;
            }
            bodies[0].AddForce(Vector3.forward*3.5f,ForceMode.Impulse);float travel=0;
            for(int step=0;step<400;step++)
            {
                physics.Simulate(.02f);travel=Mathf.Max(travel,bodies[1].position.z-targetStart);
                if(bodies.Any(b=>b.position.y<.135f)) throw new Exception("Marble tunneled through new mesh floor.");
            }
            if(travel<.10f || bodies.Any(b=>b.velocity.magnitude>.02f)) throw new Exception("Marble motion/settling failed on new clearing.");
            return "Physics PASS on actual clearing mesh: sphere collision transfers motion, no tunnelling, stops within 8 seconds; target travel "+travel.ToString("F3")+" m.";
        }
        finally {EditorSceneManager.ClosePreviewScene(scene);SceneManager.SetActiveScene(previous);}
    }

    [MenuItem("KuTy/Scene 3/Restore warm sunset in memory village")]
    public static void ApplyMemorySunset()
    {
        var scene=SceneManager.GetActiveScene();
        if(scene.path!=ScenePath || !Find(MemoryRoot) || EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("Open memory village in Edit Mode.");
        var gameplay=GameplaySnapshot();var sun=Find("Sun").GetComponent<Light>();
        var sky=LoadSceneMaterial("Scene3_Sunset");
        var errors=ShaderUtil.GetShaderMessages(sky.shader).Where(e=>e.severity==UnityEditor.Rendering.ShaderCompilerMessageSeverity.Error).ToArray();
        if(errors.Length>0) throw new Exception("Sunset sky shader failed to compile.");
        Undo.RecordObjects(new UnityEngine.Object[]{sun,sun.transform},"Warm sunset over the village");
        // Align the directional light with the low sun disc already drawn by SunsetSky.
        sun.transform.rotation=Quaternion.LookRotation(-new Vector3(-.32f,.11f,.94f).normalized,Vector3.up);
        sun.color=new Color(1,.65f,.39f);sun.intensity=.95f;
        RenderSettings.sun=sun;RenderSettings.skybox=sky;
        RenderSettings.ambientMode=AmbientMode.Trilight;
        RenderSettings.ambientSkyColor=new Color(.48f,.38f,.32f);
        RenderSettings.ambientEquatorColor=new Color(.32f,.28f,.24f);
        RenderSettings.ambientGroundColor=new Color(.14f,.12f,.1f);
        RenderSettings.fogColor=new Color(.71f,.45f,.32f);RenderSettings.fogDensity=.003f;
        DynamicGI.UpdateEnvironment();
        var water=Find(VillageName).GetComponentInChildren<VillageWaterSystem>();
        if(water && water.reflectionProbe) water.reflectionProbe.RenderProbe();
        VerifyGameplay(gameplay);
        CaptureMemoryVillage();EditorSceneManager.MarkSceneDirty(scene);
        if(!EditorSceneManager.SaveScene(scene)) throw new Exception("Cannot save sunset lighting.");
        Inspect(true);SceneView.RepaintAll();
        File.WriteAllText(OutputFolder+"/memory_sunset_report.txt","PASS\nOriginal orange-purple SunsetSky restored; low sun disc and directional light aligned.\nWarm sunlight, ambient light and fog saved.\nVillage layout and all "+gameplay.Count+" gameplay component payloads preserved.\nWater reflection captured once; no per-frame reflection rendering added.\n");
    }

    public static void CaptureMemoryVillage()
    {
        var previewScene=SceneManager.GetActiveScene();bool wasDirty=previewScene.isDirty;
        string folder=OutputFolder+"/Memory";Directory.CreateDirectory(folder);
        var views=new[]{new[]{new Vector3(62,69,-25),new Vector3(0,0,35)},new[]{new Vector3(0,1.65f,-5),new Vector3(0,1.6f,32)},new[]{new Vector3(-7.4f,1.8f,41.5f),new Vector3(-16,1.3f,44.8f)},new[]{new Vector3(-15.2f,1.7f,40.7f),new Vector3(-15,.45f,44.4f)}};
        var names=new[]{"01_ToanCanh","02_DuongLang","03_SanBi","04_KyUcChoiBi"};
        for(int i=0;i<views.Length;i++)
        {
            var obj=new GameObject("MemoryPreview"){hideFlags=HideFlags.HideAndDontSave};var camera=obj.AddComponent<Camera>();camera.enabled=false;
            camera.transform.SetPositionAndRotation(views[i][0],Quaternion.LookRotation(views[i][1]-views[i][0]));camera.fieldOfView=i==0?58:68;camera.nearClipPlane=.03f;camera.farClipPlane=400;camera.clearFlags=CameraClearFlags.Skybox;
            camera.GetUniversalAdditionalCameraData().renderPostProcessing=false;
            var rt=new RenderTexture(1600,1000,24);rt.Create();var prior=RenderTexture.active;var image=new Texture2D(1600,1000,TextureFormat.RGB24,false);
            try {RenderPipeline.SubmitRenderRequest(camera,new UniversalRenderPipeline.SingleCameraRequest{destination=rt});RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,1600,1000),0,0);image.Apply();File.WriteAllBytes(folder+"/"+names[i]+".png",image.EncodeToPNG());}
            finally {RenderTexture.active=prior;UnityEngine.Object.DestroyImmediate(image);rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(obj);}
        }
        // Temporary preview cameras can mark the scene dirty even after they are destroyed.
        if(!wasDirty) EditorSceneManager.SaveScene(previewScene);
    }
}
