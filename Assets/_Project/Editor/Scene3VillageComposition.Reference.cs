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
    private const string ReferenceRoot="LangQue_TheoAnhThamChieu";
    private const string ReferenceAssets=MemoryAssets+"/ReferenceVillage";
    // Dimensions are world metres, never a warped perimeter strip.
    private static readonly Rect[] ReferenceFields={new Rect(12,-8,20,30),new Rect(32.6f,-8,20,30),new Rect(12,22.6f,20,30),new Rect(32.6f,22.6f,20,30)};
    private static readonly Vector2 PondCenter=new Vector2(-17,25);
    private static readonly Vector3[] ReferenceRoad={new Vector3(-3,0,-15),new Vector3(-8,0,2),new Vector3(-7,0,13),new Vector3(-4,0,24),new Vector3(-5,0,36),new Vector3(-3,0,48),new Vector3(-4,0,63)};

    [MenuItem("KuTy/Scene 3/Build reference village and broad paddy plots")]
    public static void ComposeReferenceVillage()
    {
        var scene=SceneManager.GetActiveScene();
        if(scene.path!=ScenePath || EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Open the outside memory scene in Edit Mode.");
        if(Find(ReferenceRoot)){ValidateReferenceVillage();CaptureReferenceVillage();return;}
        string backup=OutputFolder+"/before-reference-village.unity";
        if(!File.Exists(backup) && !EditorSceneManager.SaveScene(scene,backup,true))throw new Exception("Cannot back up scene.");
        var village=Find(VillageName);var water=village.GetComponentInChildren<VillageWaterSystem>();
        var cover=Find(MaxtreeRoot).GetComponent<VillageGroundCoverRenderer>();
        var fields=village.GetComponentsInChildren<RuralRiceFieldRenderer>().OrderBy(f=>f.name).Take(4).ToArray();
        var before=GameplaySnapshot();before.Remove(water);before.Remove(cover);
        foreach(var f in fields)before.Remove(f);
        var old=Find(OrganicRoot);if(old){before.Remove(old.GetComponent<VillageRiceRegions>());before.Remove(old.GetComponent<VillageVisualIntegration>());}
        var houseIds=OrganicHomes().Select(h=>h.GetInstanceID()).ToArray();
        Undo.IncrementCurrentGroup();int undo=Undo.GetCurrentGroup();Undo.SetCurrentGroupName("Reference village, pond and broad rice fields");
        Undo.RegisterFullObjectHierarchyUndo(village,"Before reference layout");Undo.RegisterFullObjectHierarchyUndo(Find("Ground"),"Before reference terrain");Undo.RegisterFullObjectHierarchyUndo(Find("ShopCounter"),"Move complete shop");
        try
        {
            EnsureFolder(ReferenceAssets);var root=Group(ReferenceRoot,village.transform).transform;
            // Retire every legacy crossing and ribbon bank, including their colliders.
            foreach(var t in AllTransforms().Where(t=>t.gameObject.activeInHierarchy &&
                (t.name=="CauVanQuaMuong" || t.name=="NgoNhaRaDuong" || t.name=="LoiCuoiLang" || t.name=="LoiVaoTiem" || t.name=="LoiVaoSanBi" ||
                 t.name=="DuongDat_QuaTiemTapHoa" || t.name=="LoiCong_DanVaoKyUc" || t.name=="SanTiemTapHoa" || t.name=="SanDatNha" ||
                 t.name.StartsWith("BoCong_GioiHan") || t.name.StartsWith("RanhGioi_") || t.name.StartsWith("DongXa_MauLuaTuNhien_"))).ToArray())t.gameObject.SetActive(false);
            if(old)old.SetActive(false);
            var deep=Find(DeepRoot);if(deep)deep.SetActive(false);
            foreach(var s in water.surfaces)if(s.renderer)s.renderer.gameObject.SetActive(false);
            var land=Find("DongBang_MoRongTamNhin");if(land)land.SetActive(false);
            foreach(var t in AllTransforms().Where(t=>t.gameObject.activeInHierarchy && (t.name=="GoCoSauLuyTre" || t.name=="GoDatCuoiLang" || t.name=="GoDatDauLang")).ToArray())t.gameObject.SetActive(false);
            BuildReferenceTerrain();
            PlaceReferenceHouses(root);PlaceReferenceMemoryAndShop(root);
            BuildReferencePaths(root);BuildReferenceBridges(root);BuildReferenceBoundaries(root);
            var waterMaterial=AssetDatabase.LoadAssetAtPath<Material>(OrganicAssets+"/Nuoc_Bun_Hdri.mat");
            if(!waterMaterial)throw new Exception("Existing muddy water material missing.");
            var surfaces=new List<VillageWaterSystem.Surface>();
            for(int i=0;i<4;i++)
            {
                var r=ReferenceFields[i];var polygon=ReferenceRectangle(r);
                var f=fields[i];f.fieldPolygon=polygon;f.targetInstances=3641;f.normalizedHeight=.8f;f.soilHeight=0;f.rootBurial=.05f;
                f.randomTilt=.1f;f.regionalGpuBatch=true;f.meanderingField=false;f.nearDistance=6;f.middleDistance=22;f.farDistance=150;f.Regenerate();EditorUtility.SetDirty(f);
                surfaces.Add(ReferenceSurface(root,"Nuoc_RuongRong_"+(i+1),polygon,.025f,false,waterMaterial));
            }
            surfaces.Add(ReferenceSurface(root,"Kenh_Doc_RanhLang",ReferenceRectangle(new Rect(8.8f,-13,1.6f,87)),-.06f,true,waterMaterial));
            surfaces.Add(ReferenceSurface(root,"Kenh_Ngang_DauLang",ReferenceRectangle(new Rect(-40,-11.5f,96,1.4f)),-.06f,true,waterMaterial));
            var pond=new Vector2[96];for(int i=0;i<pond.Length;i++){float a=i*Mathf.PI*2/pond.Length;float r=.94f*(1+.025f*Mathf.Sin(a*3));pond[i]=PondCenter+new Vector2(Mathf.Cos(a)*6.5f*r,Mathf.Sin(a)*7.3f*r);}
            surfaces.Add(ReferenceSurface(root,"AoLang_BenDuongDat",pond,.12f,true,waterMaterial));
            water.surfaces=surfaces.ToArray();if(water.reflectionProbe)water.reflectionProbe.enabled=false;water.reflectionProbe=null;water.ClearInteraction();ApplyVillageWaterBlocks(water);EditorUtility.SetDirty(water);
            PlantReferenceVegetation(root,cover);
            PolishReferenceDetails(root);
            var regional=Undo.AddComponent<VillageRiceRegions>(root.gameObject);regional.regions=fields;regional.player=fields[0].player;
            regional.culling=AssetDatabase.LoadAssetAtPath<ComputeShader>(NewMaterials+"/RiceRegionCull.compute");regional.Rebuild();EditorUtility.SetDirty(regional);
            ConfigureOrganicAtmosphere(root);var atmosphere=root.GetComponent<VillageVisualIntegration>();atmosphere.fogColor=Hex("#E2CFA9");atmosphere.fogDensity=.012f;atmosphere.useHeightFog=false;atmosphere.Apply();EditorUtility.SetDirty(atmosphere);
            ConfigureOrganicShadows(village);HideVillageDebugGrid();Physics.SyncTransforms();
            if(!houseIds.SequenceEqual(OrganicHomes().Select(h=>h.GetInstanceID())))throw new Exception("House model identities changed.");
            VerifyGameplay(before);ValidateReferenceVillage();AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);
            if(!EditorSceneManager.SaveScene(scene))throw new Exception("Cannot save reference scene.");Undo.CollapseUndoOperations(undo);
            CaptureReferenceVillage();EditorSceneManager.SaveScene(scene);Inspect(true);SceneView.RepaintAll();
        }
        catch{Undo.RevertAllDownToGroup(undo);throw;}
    }
    private static Vector2[] ReferenceRectangle(Rect r)
    {
        // Small clipped corners retain the full 20 x 30 m footprint.
        const float c=.22f;return new[]{new Vector2(r.xMin+c,r.yMin),new Vector2(r.xMax-c,r.yMin),new Vector2(r.xMax,r.yMin+c),new Vector2(r.xMax,r.yMax-c),new Vector2(r.xMax-c,r.yMax),new Vector2(r.xMin+c,r.yMax),new Vector2(r.xMin,r.yMax-c),new Vector2(r.xMin,r.yMin+c)};
    }
    private static Mesh ReferenceMesh(string name,List<Vector3> vertices,List<int> indices,List<Color> colors,List<Vector2> uv=null)
    {
        var mesh=new Mesh{name=name,indexFormat=IndexFormat.UInt32};mesh.SetVertices(vertices);mesh.SetTriangles(indices,0);
        if(colors!=null)mesh.SetColors(colors);mesh.SetUVs(0,uv??vertices.Select(v=>new Vector2(v.x*.3f,v.z*.3f)).ToList());mesh.RecalculateNormals();mesh.RecalculateBounds();
        string path=ReferenceAssets+"/"+name+".asset";var existing=AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if(existing){EditorUtility.CopySerialized(mesh,existing);UnityEngine.Object.DestroyImmediate(mesh);EditorUtility.SetDirty(existing);return existing;}
        AssetDatabase.CreateAsset(mesh,path);return mesh;
    }
    private static float ReferenceHeight(float x,float z)
    {
        float y=.2f*(1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(6,12,x)));
        float pond=new Vector2((x-PondCenter.x)/6.5f,(z-PondCenter.y)/7.3f).magnitude;
        if(pond<1.15f)y=Mathf.Lerp(-.55f,.2f,Mathf.SmoothStep(0,1,Mathf.InverseLerp(.35f,1.15f,pond)));
        if(x>11.39f && x<53.21f && z>-8.61f && z<53.21f)
        {
            float distance=Mathf.Min(Mathf.Abs(x-11.7f),Mathf.Abs(x-32.3f),Mathf.Abs(x-52.9f),Mathf.Abs(z+8.3f),Mathf.Abs(z-22.3f),Mathf.Abs(z-52.9f));
            y=.14f*(1-Mathf.SmoothStep(0,1,Mathf.Clamp01(distance/.3f)));
        }
        if(z>-13 && z<74){float d=Mathf.Abs(x-9.6f);if(d<1.4f)y=Mathf.Lerp(-.5f,y,Mathf.SmoothStep(0,1,Mathf.InverseLerp(.5f,1.4f,d)));}
        if(x>-40 && x<56){float d=Mathf.Abs(z+10.8f);if(d<1.35f)y=Mathf.Min(y,Mathf.Lerp(-.5f,.2f,Mathf.SmoothStep(0,1,Mathf.InverseLerp(.4f,1.35f,d))));}
        return y;
    }
    private static void BuildReferenceTerrain()
    {
        var xs=new SortedSet<float>();var zs=new SortedSet<float>();
        for(float x=-220;x<=220;x+=8)xs.Add(x);for(float z=-220;z<=220;z+=8)zs.Add(z);
        for(float x=-44;x<=60;x+=.75f)xs.Add(x);for(float z=-18;z<=88;z+=.75f)zs.Add(z);
        foreach(float x in new[]{11.7f,32.3f,52.9f})foreach(float d in new[]{-.3f,-.15f,0,.15f,.3f})xs.Add(x+d);
        foreach(float z in new[]{-8.3f,22.3f,52.9f})foreach(float d in new[]{-.3f,-.15f,0,.15f,.3f})zs.Add(z+d);
        foreach(float d in new[]{-1.4f,-1,-.8f,-.5f,0,.5f,.8f,1,1.4f})xs.Add(9.6f+d);
        foreach(float d in new[]{-1.35f,-.9f,-.7f,-.4f,0,.4f,.7f,.9f,1.35f})zs.Add(-10.8f+d);
        var xa=xs.ToArray();var za=zs.ToArray();var vertices=new List<Vector3>();var triangles=new List<int>();var colors=new List<Color>();
        foreach(float z in za)foreach(float x in xa)
        {
            float y=ReferenceHeight(x,z);bool field=x>11.39f && x<53.21f && z>-8.61f && z<53.21f;
            vertices.Add(new Vector3(x,y,z));colors.Add(new Color(field?Mathf.Clamp01(y/.14f)*.42f:y<-.05f?0:1,0,0,1));
        }
        for(int z=0;z<za.Length-1;z++)for(int x=0;x<xa.Length-1;x++){int a=z*xa.Length+x,b=a+xa.Length;triangles.AddRange(new[]{a,b,b+1,a,b+1,a+1});}
        var mesh=ReferenceMesh("Terrain_LangCao_RuongThap",vertices,triangles,colors);var ground=Find("Ground");ground.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);ground.transform.localScale=Vector3.one;
        string materialPath=ReferenceAssets+"/Dat_NenLang_ThamChieu.mat";var material=AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if(!material){material=new Material(BlendedEarthMaterial()){name="Dat_NenLang_ThamChieu"};AssetDatabase.CreateAsset(material,materialPath);}
        material.SetFloat("_ReferenceFieldMask",1);EditorUtility.SetDirty(material);
        ground.GetComponent<MeshFilter>().sharedMesh=mesh;ground.GetComponent<Renderer>().sharedMaterial=material;
        var collider=ground.GetComponent<MeshCollider>();collider.sharedMesh=null;Physics.BakeMesh(mesh.GetInstanceID(),false);collider.sharedMesh=mesh;
    }
    private static void PlaceReferenceHouses(Transform root)
    {
        Vector2[] centers={new Vector2(-23,6),new Vector2(-1,8),new Vector2(-27,28),new Vector2(-26,58),new Vector2(-2,61)};
        float[] yaw={63,248,84,111,238},height={4.45f,4.7f,4.65f,4.3f,4.5f};
        var material=ReferenceBrickMaterial();
        for(int i=0;i<5;i++)
        {
            var home=OrganicHomes()[i];var site=home.parent;var bounds=RendererBounds(home);
            site.RotateAround(new Vector3(bounds.center.x,0,bounds.center.z),Vector3.up,Mathf.DeltaAngle(home.eulerAngles.y,yaw[i]));bounds=RendererBounds(home);
            site.position+=new Vector3(centers[i].x-bounds.center.x,.1f,centers[i].y-bounds.center.z);
            bounds=RendererBounds(home);home.localScale*=height[i]/bounds.size.y;bounds=RendererBounds(home);home.position+=Vector3.up*(.15f-bounds.min.y);
            var lod=home.GetComponentInChildren<LODGroup>();if(lod)lod.RecalculateBounds();
            var forward=Quaternion.Euler(0,yaw[i],0)*Vector3.forward;var center=new Vector3(centers[i].x,.214f,centers[i].y)+forward*(Mathf.Max(2.4f,Mathf.Min(bounds.extents.x,bounds.extents.z))+1.1f);
            ReferencePad(root,"SanGach_DoCu_"+(i+1),center,new Vector2(5.8f,3.2f),yaw[i],material);
            var hay=MeshProp(root,"DongRom_BenNha_"+(i+1),new Vector3(centers[i].x-5,.2f,centers[i].y-3),ReferenceHayMesh(),ReferenceMat("RomVangCu",new Color(.39f,.30f,.11f)));
            hay.GetComponent<Renderer>().shadowCastingMode=ShadowCastingMode.On;
        }
    }
    private static void PlaceReferenceMemoryAndShop(Transform root)
    {
        // Move the whole mini-game hierarchy: camera anchors, triggers, marbles and ring stay together.
        var clearing=Find("BaiBanBi_VaNhomTre").transform;var delta=new Vector3(2,.1f,0);clearing.position+=delta;
        foreach(var line in clearing.GetComponentsInChildren<LineRenderer>(true))if(line.useWorldSpace){var p=new Vector3[line.positionCount];line.GetPositions(p);for(int i=0;i<p.Length;i++)p[i]+=delta;line.SetPositions(p);}
        foreach(string name in new[]{"DoChoiVaDep_DeBenSan","HopBi_GoCu"}){var prop=Find(name);if(prop)prop.transform.position+=delta;}
        var shop=Find("ShopCounter").transform;var b=RendererBounds(shop);shop.position+=new Vector3(1-b.center.x,.2f-b.min.y,-3-b.center.z);
        var pad=Find("NenTiem");if(pad)pad.GetComponent<Renderer>().sharedMaterial=BlendedEarthMaterial();
        var player=AllTransforms().FirstOrDefault(t=>t.GetComponent<CharacterController>() && t.GetComponent<FirstPersonController>());
        if(player){Undo.RecordObject(player,"Place player at village entrance");player.position=new Vector3(-5,.3f,-5);player.rotation=Quaternion.Euler(0,0,0);}
    }
    private static void ReferencePad(Transform root,string name,Vector3 center,Vector2 size,float yaw,Material material)
    {
        var vertices=new List<Vector3>{center};var colors=new List<Color>{Color.black};var triangles=new List<int>();var rotation=Quaternion.Euler(0,yaw,0);
        for(int ring=1;ring<=3;ring++)for(int i=0;i<48;i++)
        {
            float a=i*Mathf.PI*2/48;float r=ring/3f;var p=center+rotation*new Vector3(Mathf.Cos(a)*size.x*.5f*r,0,Mathf.Sin(a)*size.y*.5f*r);
            p.y=ring==3?.205f:center.y;vertices.Add(p);colors.Add(new Color(ring==3?1:0,0,0,1));int next=(i+1)%48;
            if(ring==1)triangles.AddRange(new[]{0,1+next,1+i});else{int a0=1+(ring-2)*48+i,b0=1+(ring-1)*48+i,a1=1+(ring-2)*48+next,b1=1+(ring-1)*48+next;triangles.AddRange(new[]{a0,b1,b0,a0,a1,b1});}
        }
        MeshProp(root,name,Vector3.zero,ReferenceMesh(name,vertices,triangles,colors),material);
    }
    private static Material ReferenceMat(string name,Color color)
    {
        string path=ReferenceAssets+"/"+name+".mat";var material=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(!material){material=new Material(Shader.Find("Universal Render Pipeline/Lit")){name=name};AssetDatabase.CreateAsset(material,path);}
        material.SetColor("_BaseColor",color);material.SetFloat("_Smoothness",0);material.SetFloat("_Metallic",0);material.enableInstancing=true;EditorUtility.SetDirty(material);return material;
    }
    private static Material ReferenceBrickMaterial()
    {
        string path=ReferenceAssets+"/SanGach_DoCu.mat";var material=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(!material){material=new Material(BlendedEarthMaterial()){name="SanGach_DoCu"};AssetDatabase.CreateAsset(material,path);}
        material.SetFloat("_BrickPattern",1);material.SetColor("_SoilColor",new Color(.46f,.22f,.13f));material.SetFloat("_UseVertexCoverage",1);EditorUtility.SetDirty(material);return material;
    }
    private static Mesh ReferenceHayMesh()
    {
        var existing=AssetDatabase.LoadAssetAtPath<Mesh>(ReferenceAssets+"/DongRom_HinhNon.asset");if(existing)return existing;
        var v=new List<Vector3>();var t=new List<int>();var c=new List<Color>();
        for(int ring=0;ring<=12;ring++)for(int i=0;i<40;i++){float h=ring/12f,a=i*Mathf.PI*2/40;float radius=Mathf.Pow(1-h,.7f)*1.15f*(1+.035f*Mathf.Sin(i*5+ring));v.Add(new Vector3(Mathf.Cos(a)*radius,h*2.7f,Mathf.Sin(a)*radius));c.Add(Color.white);if(ring<12){int n=(i+1)%40,a0=ring*40+i,b=(ring+1)*40+i;t.AddRange(new[]{a0,ring*40+n,(ring+1)*40+n,a0,(ring+1)*40+n,b});}}
        return ReferenceMesh("DongRom_HinhNon",v,t,c);
    }
    private static List<Vector3> ReferenceSpline(Vector3[] points)
    {
        var list=new List<Vector3>();for(int i=0;i<points.Length-1;i++)
        {
            var a=points[Mathf.Max(0,i-1)];var b=points[i];var c=points[i+1];var d=points[Mathf.Min(points.Length-1,i+2)];int steps=Mathf.CeilToInt(Vector3.Distance(b,c)/.4f);
            for(int j=0;j<steps;j++){float t=j/(float)steps,t2=t*t,t3=t2*t;list.Add(.5f*(2*b+(-a+c)*t+(2*a-5*b+4*c-d)*t2+(-a+3*b-3*c+d)*t3));}
        }list.Add(points[points.Length-1]);return list;
    }
    private static void ReferencePath(Transform root,string name,Vector3[] points,float width)
    {
        var path=ReferenceSpline(points);var v=new List<Vector3>();var t=new List<int>();var colors=new List<Color>();
        for(int i=0;i<path.Count;i++)
        {
            var forward=path[Mathf.Min(i+1,path.Count-1)]-path[Mathf.Max(0,i-1)];var side=Vector3.Cross(forward.normalized,Vector3.up);
            for(int c=0;c<5;c++){float offset=(c-2)*width*.25f;var p=path[i]+side*offset;p.y=ReferenceHeight(p.x,p.z)+.012f;v.Add(p);colors.Add(new Color(c==0 || c==4?1:0,0,0,1));if(i>0 && c<4){int a=(i-1)*5+c,b=i*5+c;t.AddRange(new[]{a,b+1,b,a,a+1,b+1});}}
        }
        MeshProp(root,name,Vector3.zero,ReferenceMesh(name,v,t,colors),BlendedEarthMaterial());
    }
    private static void BuildReferencePaths(Transform root)
    {
        ReferencePath(root,"DuongLang_UonQuanhAo",ReferenceRoad,2.4f);
        ReferencePath(root,"Ngo_SanNha1",new[]{new Vector3(-8,0,6),new Vector3(-13,0,8),new Vector3(-18,0,8)},1.6f);
        ReferencePath(root,"Ngo_SanNha3",new[]{new Vector3(-5,0,34),new Vector3(-11,0,35),new Vector3(-20,0,31)},1.4f);
        ReferencePath(root,"Ngo_SanNha4",new[]{new Vector3(-4,0,60),new Vector3(-15,0,62),new Vector3(-23,0,60)},1.5f);
        ReferencePath(root,"Ngo_SanBi_KyUc",new[]{new Vector3(-4,0,44),new Vector3(-8,0,43),new Vector3(-11,0,44)},1.8f);
        foreach(float z in new[]{20f,48f}){ReferencePath(root,"LoiRaCau_"+z,new[]{new Vector3(-4,0,z+2),new Vector3(2,0,z+.7f),new Vector3(8.2f,0,z)},1.65f);ReferencePath(root,"LoiTuCau_RaRuong_"+z,new[]{new Vector3(11,0,z),new Vector3(11.7f,0,z)},1.4f);}
    }
    private static Mesh ReferenceBridgeMesh()
    {
        var existing=AssetDatabase.LoadAssetAtPath<Mesh>(ReferenceAssets+"/CauVan_1m4x2m9.asset");if(existing)return existing;
        var vertices=new List<Vector3>();var triangles=new List<int>();var colors=new List<Color>();
        // Geometry is authored at human scale once. No stretching to a road or map endpoint.
        for(int board=0;board<7;board++)
        {
            float z=-1.45f+board*(2.9f/7);Vector3 min=new Vector3(-.7f,-.08f,z+.008f),max=new Vector3(.7f,0,z+2.9f/7-.008f);int start=vertices.Count;
            vertices.AddRange(new[]{new Vector3(min.x,min.y,min.z),new Vector3(max.x,min.y,min.z),new Vector3(max.x,max.y,min.z),new Vector3(min.x,max.y,min.z),new Vector3(min.x,min.y,max.z),new Vector3(max.x,min.y,max.z),new Vector3(max.x,max.y,max.z),new Vector3(min.x,max.y,max.z)});
            foreach(int j in new[]{0,2,1,0,3,2,4,5,6,4,6,7,3,7,6,3,6,2,0,1,5,0,5,4,0,4,7,0,7,3,1,2,6,1,6,5})triangles.Add(start+j);
            for(int j=0;j<8;j++)colors.Add(Color.white);
        }return ReferenceMesh("CauVan_1m4x2m9",vertices,triangles,colors);
    }
    private static void BuildReferenceBridges(Transform root)
    {
        var mesh=ReferenceBridgeMesh();var wood=ReferenceMat("VanCau_GoCu",new Color(.25f,.17f,.09f));
        Vector3[] positions={new Vector3(-3,.23f,-10.8f),new Vector3(9.6f,.23f,20),new Vector3(9.6f,.23f,48)};
        for(int i=0;i<3;i++)
        {
            var bridge=MeshProp(root,"CauNgan_QuaKenh_"+(i+1),positions[i],mesh,wood);bridge.transform.rotation=Quaternion.Euler(0,i==0?0:90,0);bridge.transform.localScale=Vector3.one;
            var collider=Undo.AddComponent<BoxCollider>(bridge);collider.center=new Vector3(0,-.04f,0);collider.size=new Vector3(1.4f,.08f,2.9f);bridge.GetComponent<Renderer>().shadowCastingMode=ShadowCastingMode.On;
        }
    }
    private static VillageWaterSystem.Surface ReferenceSurface(Transform root,string name,Vector2[] outline,float y,bool deep,Material material)
    {
        var v=new List<Vector3>();var t=new List<int>();var colors=new List<Color>();var center=outline.Aggregate(Vector2.zero,(a,b)=>a+b)/outline.Length;
        v.Add(new Vector3(center.x,y,center.y));colors.Add(Color.red);
        foreach(var p in outline){v.Add(new Vector3(p.x,y,p.y));colors.Add(Color.red);}
        for(int i=0;i<outline.Length;i++)t.AddRange(new[]{0,1+(i+1)%outline.Length,1+i});
        var obj=MeshProp(root,name,Vector3.zero,ReferenceMesh(name,v,t,colors),material);var renderer=obj.GetComponent<Renderer>();renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=true;
        return new VillageWaterSystem.Surface{renderer=renderer,rectangle=OutlineBounds(outline),height=y,canal=deep,meandering=false,shoreMask=true,outline=outline};
    }
    private static void BuildReferenceBoundaries(Transform root)
    {
        Vector3[] p={new Vector3(-44,2.5f,34),new Vector3(59,2.5f,34),new Vector3(7.5f,2.5f,-17),new Vector3(7.5f,2.5f,85)};
        for(int i=0;i<4;i++){var go=Group("GioiHan_ThamChieu_"+i,root);go.transform.position=p[i];Undo.AddComponent<BoxCollider>(go).size=i<2?new Vector3(1,6,104):new Vector3(104,6,1);}
    }
    private static void PlantReferenceVegetation(Transform root,VillageGroundCoverRenderer cover)
    {
        var prototypes=cover.prototypes.ToArray();if(prototypes.Length<5)throw new Exception("Existing grass and bamboo prototypes missing.");
        prototypes[4].maxDistance=155;cover.prototypes=prototypes;var plants=new List<VillageGroundCoverRenderer.Plant>();var random=new System.Random(314159);
        Func<float,float,float> rand=(a,b)=>Mathf.Lerp(a,b,(float)random.NextDouble());
        Action<Vector3,int,float> plant=(p,id,height)=>{var bounds=prototypes[id].near.bounds;float scale=height/bounds.size.y;p.y=ReferenceHeight(p.x,p.z)-.03f-bounds.min.y*scale;plants.Add(new VillageGroundCoverRenderer.Plant{position=p,yaw=rand(0,360),scale=scale,prototype=id});};
        // Two irregular rows create a leafy boundary, leaving both canal crossings open.
        for(float z=-6;z<71;z+=1.7f)if(Mathf.Abs(z-20)>3.5f && Mathf.Abs(z-48)>3.5f)for(int row=0;row<2;row++)plant(new Vector3(5.5f+row*1.3f+rand(-.5f,.5f),0,z+rand(-.7f,.7f)),4,rand(5.8f,7.5f));
        for(float z=-5;z<73;z+=2.4f)plant(new Vector3(-37+rand(-1.5f,1.5f),0,z),4,rand(5.8f,7.7f));
        for(float x=-35;x<5;x+=2.3f)plant(new Vector3(x,0,72+rand(-1,1)),4,rand(6,7.5f));
        // Sparse bank/foundation clusters; the rice interior stays a continuous crop.
        for(int i=0;i<1450;i++)
        {
            Vector3 p;int mode=i%4;
            if(mode==0)p=new Vector3(8.1f+rand(-.25f,.2f),0,rand(-7,69));
            else if(mode==1){float a=rand(0,Mathf.PI*2);p=new Vector3(PondCenter.x+Mathf.Cos(a)*7.3f,0,PondCenter.y+Mathf.Sin(a)*8.1f);}
            else if(mode==2){var h=OrganicHomes()[i%5];var b=RendererBounds(h);float a=rand(0,Mathf.PI*2);p=new Vector3(b.center.x+Mathf.Cos(a)*(b.extents.x+rand(.1f,.8f)),0,b.center.z+Mathf.Sin(a)*(b.extents.z+rand(.1f,.8f)));}
            else{var line=ReferenceRoad[i%ReferenceRoad.Length];p=line+new Vector3(rand(1.4f,2.1f)*(i%2==0?1:-1),0,rand(-1.3f,1.3f));}
            if(new Vector2((p.x+13)/8,(p.z-44)/6.8f).sqrMagnitude<1 || (Mathf.Abs(p.z-20)<2 && p.x>1) || (Mathf.Abs(p.z-48)<2 && p.x>1))continue;
            plant(p,i%4,rand(.12f,.31f));
        }
        cover.plants=plants.ToArray();cover.Rebuild();EditorUtility.SetDirty(cover);
        // Existing Maxtree trunks/LODs are retained and given a full green canopy.
        Vector2[] trees={new Vector2(-32,12),new Vector2(-31,38),new Vector2(-20,15),new Vector2(-30,67),new Vector2(-10,67),new Vector2(3,30),new Vector2(-26,21),new Vector2(-16.4f,46.8f)};
        int n=0;foreach(var group in Find(MaxtreeRoot).GetComponentsInChildren<LODGroup>())
        {
            var p=trees[Mathf.Min(n,7)];var b=RendererBounds(group.transform);group.transform.position+=new Vector3(p.x-group.transform.position.x,0,p.y-group.transform.position.z);b=RendererBounds(group.transform);group.transform.position+=Vector3.up*(ReferenceHeight(p.x,p.y)-.05f-b.min.y);
            AddReferenceLeafCrown(group,n++);group.RecalculateBounds();
        }
    }
    private static void AddReferenceLeafCrown(LODGroup group,int seed)
    {
        if(group.transform.Find("ThanTan_LienKhoi_LOD0"))return;
        var source=group.GetLODs();if(source.Length==0)return;
        for(int i=0;i<source.Length;i++)source[i].renderers=source[i].renderers.Where(r=>r && !r.name.StartsWith("TanLaXanh")).ToArray();
        foreach(var t in group.GetComponentsInChildren<Transform>(true).Where(t=>t.name.StartsWith("TanLaXanh")).ToArray())Undo.DestroyObjectImmediate(t.gameObject);
        var materials=source[0].renderers.SelectMany(r=>r.sharedMaterials).Where(m=>m && m.HasProperty("_HasTexture") && m.GetFloat("_HasTexture")>.5f).ToArray();
        var leaf=materials.LastOrDefault(m=>m.name.Contains("leaf") || m.name.Contains("Leaf"))??materials.LastOrDefault();if(!leaf)return;
        var world=RendererBounds(group.transform);float scale=group.transform.lossyScale.x;
        for(int lod=0;lod<source.Length;lod++)
        {
            string name="TanLaXanhNho_"+lod;var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(ReferenceAssets+"/"+name+".asset");
            if(!mesh)
            {
                var v=new List<Vector3>();var t=new List<int>();var c=new List<Color>();var uv=new List<Vector2>();var random=new System.Random(884);int count=lod==0?3000:lod==1?1200:500;
                for(int i=0;i<count;i++)
                {
                    float a=(float)random.NextDouble()*Mathf.PI*2,z=(float)random.NextDouble()*2-1,r=Mathf.Pow((float)random.NextDouble(),.333f);float disc=Mathf.Sqrt(1-z*z);
                    var center=new Vector3(Mathf.Cos(a)*disc*2.7f*r,4.25f+z*1.45f*r,Mathf.Sin(a)*disc*2.7f*r);
                    var rotation=Quaternion.Euler((float)random.NextDouble()*120-60,(float)random.NextDouble()*360,(float)random.NextDouble()*120-60);float size=lod==0?.17f:lod==1?.25f:.36f;
                    var right=rotation*Vector3.right*size;var up=rotation*Vector3.up*size;int b=v.Count;v.AddRange(new[]{center-right-up,center+right-up,center+right+up,center-right+up});t.AddRange(new[]{b,b+2,b+1,b,b+3,b+2});
                    uv.AddRange(new[]{new Vector2(.62f,.4f),new Vector2(.94f,.4f),new Vector2(.94f,.98f),new Vector2(.62f,.98f)});for(int j=0;j<4;j++)c.Add(new Color(.85f,1,0,1));
                }mesh=ReferenceMesh(name,v,t,c,uv);
            }
            var obj=MeshProp(group.transform,name,group.transform.position,mesh,leaf);obj.transform.localRotation=Quaternion.identity;
            // Authoring mesh is 5.7 m tall: use a uniform scale, never flatten the canopy.
            obj.transform.localScale=Vector3.one*(world.size.y/5.7f/scale);obj.transform.localPosition=Vector3.zero;
            obj.GetComponent<Renderer>().shadowCastingMode=ShadowCastingMode.On;source[lod].renderers=source[lod].renderers.Concat(new[]{obj.GetComponent<Renderer>()}).ToArray();
        }group.SetLODs(source);
    }
    public static void PolishReferenceVillage()
    {
        var root=Find(ReferenceRoot).transform;var before=GameplaySnapshot();var cover=Find(MaxtreeRoot).GetComponent<VillageGroundCoverRenderer>();before.Remove(cover);
        Undo.RegisterFullObjectHierarchyUndo(Find(VillageName),"Polish reference planting and marble ground");
        foreach(var tree in Find(MaxtreeRoot).GetComponentsInChildren<LODGroup>())AddReferenceLeafCrown(tree,0);
        BuildReferenceTerrain();PolishReferenceDetails(root);VerifyGameplay(before);ValidateReferenceVillage();AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());EditorSceneManager.SaveScene(SceneManager.GetActiveScene());CaptureReferenceVillage();EditorSceneManager.SaveScene(SceneManager.GetActiveScene());Inspect(true);
    }
    private static Mesh ReferenceBambooMesh(int lod)
    {
        string name="TreTanXumXe_LOD"+lod;var existing=AssetDatabase.LoadAssetAtPath<Mesh>(ReferenceAssets+"/"+name+".asset");if(existing)return existing;
        var g=new Geometry();var random=new System.Random(602);int stems=7,branches=6,leaves=lod==0?22:lod==1?18:14;
        for(int stem=0;stem<stems;stem++)
        {
            float a=stem*Mathf.PI*2/stems,h=5.4f+(float)random.NextDouble()*1.6f;var root=new Vector3(Mathf.Cos(a)*.65f,0,Mathf.Sin(a)*.65f);var tip=root+new Vector3(Mathf.Cos(a)*1.1f,h,Mathf.Sin(a)*1.1f);
            for(int k=0;k<4;k++)g.Cylinder(Vector3.Lerp(root,tip,k/4f)+Vector3.up*Mathf.Sin(k/4f*Mathf.PI)*.15f,Vector3.Lerp(root,tip,(k+1)/4f)+Vector3.up*Mathf.Sin((k+1)/4f*Mathf.PI)*.15f,.035f);
            for(int b=0;b<branches;b++)
            {
                float ba=a+b*2.399f;var start=Vector3.Lerp(root,tip,.29f+b*.115f);var dir=new Vector3(Mathf.Cos(ba),.16f,Mathf.Sin(ba));var end=start+dir*(1.5f+(float)random.NextDouble()*.65f);g.Cylinder(start,end,.012f);
                for(int l=0;l<leaves;l++)
                {
                    var p=Vector3.Lerp(start,end,.10f+.90f*l/leaves);var side=Vector3.Cross(dir,Vector3.up)*(l%2==0?1:-1);var leafEnd=p+side*(.6f+(float)random.NextDouble()*.35f)+dir*.35f+Vector3.down*(.15f+(float)random.NextDouble()*.25f);var middle=Vector3.Lerp(p,leafEnd,.4f)+Vector3.up*.10f;var width=Vector3.Cross((leafEnd-p).normalized,Vector3.up)*(lod==0?.075f:lod==1?.1f:.14f);
                    g.Triangle(p,middle+width,leafEnd,true);g.Triangle(p,leafEnd,middle-width,true);
                }
            }
        }
        var color=new Color[g.vertices.Count];foreach(int index in g.first)color[index]=new Color(.4f,0,0,1);foreach(int index in g.second)color[index]=new Color(.85f,1,0,1);
        return ReferenceMesh(name,g.vertices,g.first.Concat(g.second).ToList(),color.ToList());
    }
    private static void PolishReferenceDetails(Transform root)
    {
        var cover=Find(MaxtreeRoot).GetComponent<VillageGroundCoverRenderer>();var prototypes=cover.prototypes;
        string path=ReferenceAssets+"/TreLang_Xanh.mat";var material=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(!material){material=new Material(Shader.Find("KuTy/Village/Maxtree Matte Wind")){name="TreLang_Xanh"};AssetDatabase.CreateAsset(material,path);}
        material.SetFloat("_HasTexture",0);material.SetColor("_LeafColor",new Color(.15f,.28f,.065f));material.SetColor("_BarkColor",new Color(.22f,.27f,.08f));material.SetFloat("_WindAmplitude",.045f);material.SetFloat("_Flexibility",0);material.enableInstancing=true;EditorUtility.SetDirty(material);
        if(RenderSettings.skybox && RenderSettings.skybox.HasProperty("_HorizonColor")){RenderSettings.skybox.SetColor("_HorizonColor",Hex("#E2CFA9"));EditorUtility.SetDirty(RenderSettings.skybox);}
        float oldHeight=prototypes[4].near.bounds.size.y;var mesh=ReferenceBambooMesh(0);var plants=cover.plants;
        for(int i=0;i<plants.Length;i++)if(plants[i].prototype==4){float height=plants[i].scale*oldHeight;plants[i].scale=height/mesh.bounds.size.y;var p=plants[i].position;p.y=ReferenceHeight(p.x,p.z)-.04f-mesh.bounds.min.y*plants[i].scale;plants[i].position=p;}
        prototypes[4].near=mesh;prototypes[4].middle=ReferenceBambooMesh(1);prototypes[4].far=ReferenceBambooMesh(2);prototypes[4].materials=new[]{material};prototypes[4].maxDistance=155;cover.prototypes=prototypes;cover.plants=plants;cover.Rebuild();EditorUtility.SetDirty(cover);
        foreach(var t in AllTransforms().Where(t=>t.gameObject.activeInHierarchy && (t.name.StartsWith("CumTre_") || t.name.StartsWith("BuiTre_") || t.name=="LuyTre_CuoiLang" || t.name=="LuyTre_DauLang")).ToArray())t.gameObject.SetActive(false);
        int banana=0;foreach(var t in AllTransforms().Where(t=>t.gameObject.activeInHierarchy && t.name=="BuiChuoiBenVuon").ToArray()){var p=new Vector3(-30+banana%3*8,.2f,14+banana/3*20);t.position=p;banana++;}
        var garden=Find("VuonRauCanhNgo");if(garden)garden.SetActive(false);
        // Old hidden placeholder trunks must not leave invisible colliders at obsolete positions.
        foreach(var t in AllTransforms().Where(t=>t.name=="CayOi_SanNha" || t.name=="CayAnQuaCanhTiem" || t.name=="CayAnQua_VuonSau" || t.name=="CayKyUc_CheSanBi"))foreach(var c in t.GetComponentsInChildren<Collider>(true))c.enabled=false;
        foreach(var tree in Find(MaxtreeRoot).GetComponentsInChildren<LODGroup>())
        {
            var collider=tree.GetComponent<CapsuleCollider>();if(!collider)collider=Undo.AddComponent<CapsuleCollider>(tree.gameObject);float scale=tree.transform.lossyScale.x;
            collider.center=new Vector3(0,.85f/scale,0);collider.height=1.7f/scale;collider.radius=.18f/scale;collider.direction=1;
        }
        if(!root.Find("SanBi_DaNangMatNen"))
        {
            var clearing=Find("BaiBanBi_VaNhomTre").transform;var delta=Vector3.up*.025f;clearing.position+=delta;
            foreach(var line in clearing.GetComponentsInChildren<LineRenderer>(true))if(line.useWorldSpace){var p=new Vector3[line.positionCount];line.GetPositions(p);for(int i=0;i<p.Length;i++)p[i]+=delta;line.SetPositions(p);}
            foreach(string name in new[]{"DoChoiVaDep_DeBenSan","HopBi_GoCu"}){var prop=Find(name);if(prop)prop.transform.position+=delta;}
            Group("SanBi_DaNangMatNen",root);
        }
    }
    public static void ValidateReferenceVillage()
    {
        var root=Find(ReferenceRoot);if(!root)throw new Exception("Reference layout missing.");var regional=root.GetComponent<VillageRiceRegions>();if(!regional || regional.TotalInstances!=14564 || regional.regions.Length!=4)throw new Exception("Four regional rice batches missing.");
        var matrices=new List<Matrix4x4>();float minHeight=10,maxHeight=0;
        for(int i=0;i<4;i++)
        {
            var field=regional.regions[i];if(field.meanderingField || field.PlantCount!=3641 || field.soilHeight!=0)throw new Exception("Rice ribbon or population regression.");
            var r=ReferenceFields[i];if(r.width!=20 || r.height!=30)throw new Exception("Paddy footprint must be 20 x 30 m.");
            matrices.Clear();field.CopyInstances(matrices);var box=field.nearMesh.bounds;
            foreach(var m in matrices){var p=m.MultiplyPoint3x4(box.center);if(!r.Contains(new Vector2(p.x,p.z)))throw new Exception("Rice outside wide plot.");float h=(Mathf.Abs(m.m10)*box.extents.x+Mathf.Abs(m.m11)*box.extents.y+Mathf.Abs(m.m12)*box.extents.z)*2;minHeight=Mathf.Min(minHeight,h);maxHeight=Mathf.Max(maxHeight,h);}
        }
        if(minHeight<.699f || maxHeight>.901f)throw new Exception("Rice human scale failed.");
        foreach(var home in OrganicHomes()){var b=RendererBounds(home);if(b.center.x>5 || b.size.y<4 || b.size.y>5 || Mathf.Abs(b.min.y-.15f)>.01f)throw new Exception("Village core house proportions or grounding failed.");}
        var bridges=root.GetComponentsInChildren<MeshFilter>().Where(f=>f.name.StartsWith("CauNgan_")).ToArray();if(bridges.Length!=3)throw new Exception("Expected exactly three short crossings.");
        for(int i=0;i<bridges.Length;i++){var b=bridges[i];if((b.transform.localScale-Vector3.one).sqrMagnitude>.0001f || b.sharedMesh.bounds.size.z>3 || b.sharedMesh.bounds.size.x>1.5f)throw new Exception("Bridge stretching regression.");var f=b.transform.forward;if(i==0?Mathf.Abs(f.z)<.99f:Mathf.Abs(f.x)<.99f)throw new Exception("Bridge must be perpendicular to canal.");}
        if(AllTransforms().Any(t=>t.gameObject.activeInHierarchy && (t.name=="CauVanQuaMuong" || t.name.StartsWith("BoRuong_HuuCo_"))))throw new Exception("Legacy bridges or rice ribbons remain active.");
        var water=Find(VillageName).GetComponentInChildren<VillageWaterSystem>();if(water.surfaces.Length!=7 || water.reflectionProbe)throw new Exception("Expected four paddies, two canals and one pond, without live reflections.");
        foreach(var s in water.surfaces){var p=new Vector3((s.rectangle.x+s.rectangle.z)*.5f,s.height,(s.rectangle.y+s.rectangle.w)*.5f);if(s.meandering || !s.Contains(p) || s.renderer.shadowCastingMode!=ShadowCastingMode.Off || !s.renderer.receiveShadows)throw new Exception("Water footprint/contact/shadow mismatch.");}
        if(!RenderSettings.fog || RenderSettings.fogMode!=FogMode.ExponentialSquared || Mathf.Abs(RenderSettings.fogDensity-.012f)>.00001f || Vector4.Distance(RenderSettings.fogColor,Hex("#E2CFA9"))>.001f)throw new Exception("Reference fog settings failed.");
        Physics.SyncTransforms();var ground=Find("Ground").GetComponent<MeshCollider>();
        foreach(var p in new[]{new Vector3(-13,3,44),new Vector3(-7,3,13),new Vector3(22,3,7),new Vector3(42.6f,3,37.6f),new Vector3(32.3f,3,10)})
        {if(!ground.Raycast(new Ray(p,Vector3.down),out var hit,5) || Mathf.Abs(hit.point.y-ReferenceHeight(p.x,p.z))>.01f)throw new Exception("Terrain collider mismatch at "+p);}
        foreach(var shader in new[]{Shader.Find("KuTy/Village/Rice Instanced Wind"),Shader.Find("KuTy/Village/Maxtree Matte Wind"),Shader.Find("KuTy/VillageWater"),Shader.Find("KuTy/Village/Matte Grass Earth")})if(ShaderUtil.GetShaderMessages(shader).Any(m=>m.severity==UnityEditor.Rendering.ShaderCompilerMessageSeverity.Error))throw new Exception("Shader compile error: "+shader.name);
        string contacts=VerifyVillageWaterInteraction(water);water.ClearInteraction();
        VerifyReferenceWalking();
        File.WriteAllText(OutputFolder+"/reference_village_report.txt","PASS\nOriginal five house model identities retained; existing loaders and mini-game references preserved.\nVillage core y=0.2; four wide 20 x 30 m paddies at y=0, 0.6 m dikes.\n14,564 rice instances, heights "+minHeight+".."+maxHeight+" m; four GPU regions, frustum culling and three LODs, no foliage shadows.\nExactly 3 bridges, 1.4 x 2.9 m, scale one, perpendicular to canals. Legacy crossings and ribbon banks inactive.\nFive brick courtyards and haystacks; one pond; two canals; complete marble clearing and shop moved as intact hierarchies.\nFog ExpSquared #E2CFA9 density .012; shadow atlas 2048; no live water reflections.\n"+contacts+"\nCharacterController crossing and paddy access checks passed.\nGPU: "+SystemInfo.graphicsDeviceName+" / "+SystemInfo.graphicsDeviceType+"\n");
        var verts=ground.sharedMesh.vertices;var colors=ground.sharedMesh.colors;foreach(var p in new[]{new Vector3(60,0,60),new Vector3(80,0,20),new Vector3(-60,0,0)}){int index=Enumerable.Range(0,verts.Length).OrderBy(i=>(verts[i]-p).sqrMagnitude).First();File.AppendAllText(OutputFolder+"/reference_village_report.txt","Ground sample "+verts[index]+" color "+colors[index]+"\n");}
    }
    private static void VerifyReferenceWalking()
    {
        var obj=new GameObject("ReferenceWalkingProbe"){hideFlags=HideFlags.HideAndDontSave};var cc=obj.AddComponent<CharacterController>();cc.height=1.6f;cc.center=Vector3.up*.8f;cc.radius=.24f;cc.stepOffset=.3f;cc.slopeLimit=48;
        try
        {
            var routes=new[]{new[]{new Vector3(-3,.3f,-13),new Vector3(-3,.2f,-8)},new[]{new Vector3(7,.3f,20),new Vector3(12,.1f,20)},new[]{new Vector3(7,.3f,48),new Vector3(12,.1f,48)},new[]{new Vector3(32.3f,.3f,10),new Vector3(35,.1f,10)},new[]{new Vector3(-7,.3f,44),new Vector3(-10,.3f,44)}};
            foreach(var route in routes){cc.enabled=false;obj.transform.position=route[0];cc.enabled=true;Physics.SyncTransforms();for(int step=0;step<180;step++){var d=route[1]-obj.transform.position;d.y=0;if(d.magnitude<.12f)break;cc.Move(Vector3.ClampMagnitude(d,.06f)+Vector3.down*.025f);}var remain=route[1]-obj.transform.position;remain.y=0;if(remain.magnitude>.25f || obj.transform.position.y<-.1f)throw new Exception("Reference path blocked: "+route[0]+" -> "+route[1]+" at "+obj.transform.position);}
        }
        finally{UnityEngine.Object.DestroyImmediate(obj);}
    }
    public static void CaptureReferenceVillage()
    {
        string folder=OutputFolder+"/Reference";Directory.CreateDirectory(folder);var scene=SceneManager.GetActiveScene();bool dirty=scene.isDirty;
        var views=new[]{new[]{new Vector3(54,51,-35),new Vector3(3,0,30)},new[]{new Vector3(-8,2,12),new Vector3(-19,1,27)},new[]{new Vector3(-6,2,40),new Vector3(-16,1,46)},new[]{new Vector3(14,1.7f,18),new Vector3(34,.4f,32)}};
        for(int i=0;i<views.Length;i++)
        {
            var obj=new GameObject("ReferencePreview"){hideFlags=HideFlags.HideAndDontSave};var camera=obj.AddComponent<Camera>();camera.enabled=false;camera.fieldOfView=i==0?52:65;camera.nearClipPlane=.03f;camera.farClipPlane=300;camera.allowHDR=true;camera.transform.SetPositionAndRotation(views[i][0],Quaternion.LookRotation(views[i][1]-views[i][0]));camera.GetUniversalAdditionalCameraData().renderPostProcessing=false;
            if(i==0){camera.orthographic=true;camera.orthographicSize=43;}
            var rt=new RenderTexture(1400,900,24);rt.Create();var old=RenderTexture.active;var image=new Texture2D(1400,900,TextureFormat.RGB24,false);
            bool fog=RenderSettings.fog;
            try{if(i==0)RenderSettings.fog=false;RenderPipeline.SubmitRenderRequest(camera,new UniversalRenderPipeline.SingleCameraRequest{destination=rt});RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,1400,900),0,0);image.Apply();File.WriteAllBytes(folder+"/"+(i+1).ToString("00")+".png",image.EncodeToPNG());var regional=Find(ReferenceRoot).GetComponent<VillageRiceRegions>();File.AppendAllText(OutputFolder+"/reference_village_report.txt","View "+i+(i==0?" (layout inspection: fog temporarily hidden)":"")+": "+regional.LastVisibleRegions+" regions, "+regional.LastDrawCalls+" rice draws, "+regional.ReadVisibleInstanceCount()+" visible stalks.\n");}
            finally{RenderSettings.fog=fog;RenderTexture.active=old;UnityEngine.Object.DestroyImmediate(image);rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(obj);}
        }if(!dirty)EditorSceneManager.SaveScene(scene);
    }
}
