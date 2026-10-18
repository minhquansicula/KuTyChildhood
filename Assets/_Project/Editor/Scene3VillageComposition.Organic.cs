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
    private const string OrganicRoot="LangQue_BoTriHuuCo";
    private const string OrganicAssets=MemoryAssets+"/OrganicVillage";
    private static readonly Vector2[][] OrganicZones={
        new[]{new Vector2(-39,-7),new Vector2(-31,-8),new Vector2(-28,-3),new Vector2(-29,8),new Vector2(-27,20),new Vector2(-29,34),new Vector2(-39,33),new Vector2(-40,16)},
        new[]{new Vector2(-39,36),new Vector2(-29,36),new Vector2(-27,45),new Vector2(-28,57),new Vector2(-25,74),new Vector2(-28,80),new Vector2(-39,78),new Vector2(-40,61)},
        new[]{new Vector2(9,-7),new Vector2(23,-8),new Vector2(38,-6),new Vector2(39,4),new Vector2(37,13),new Vector2(28,14),new Vector2(17,12),new Vector2(9,13),new Vector2(8,2)},
        new[]{new Vector2(29,17),new Vector2(38,16),new Vector2(40,28),new Vector2(38,43),new Vector2(39,58),new Vector2(38,78),new Vector2(30,80),new Vector2(27,73),new Vector2(28,58),new Vector2(27,42),new Vector2(28,29)}
    };
    private static Color Hex(string value){ColorUtility.TryParseHtmlString(value,out var color);return color;}
    private static Transform[] OrganicHomes(){var village=Find(VillageName).transform;return HomeNames.Select((name,i)=>village.Find("NhaDan_VaSanVuon/SanNha_Gon_0"+(i+1)+"/"+name)).ToArray();}

    [MenuItem("KuTy/Scene 3/Apply organic proportions, dense regional rice and terrain blending")]
    public static void ComposeOrganicVillage()
    {
        var scene=SceneManager.GetActiveScene();
        if(scene.path!=ScenePath || EditorApplication.isPlayingOrWillChangePlaymode || !Find(MaxtreeRoot))throw new Exception("Open the Maxtree village in Edit Mode.");
        if(Find(OrganicRoot)){ValidateOrganicVillage();CaptureOrganicVillage();return;}
        string backup=OutputFolder+"/before-organic-neighbourhood.unity";
        if(!File.Exists(backup) && !EditorSceneManager.SaveScene(scene,backup,true))throw new Exception("Cannot back up current scene.");
        var village=Find(VillageName);var fields=village.GetComponentsInChildren<RuralRiceFieldRenderer>().OrderBy(f=>f.name).ToArray();
        var cover=Find(MaxtreeRoot).GetComponent<VillageGroundCoverRenderer>();var water=village.GetComponentInChildren<VillageWaterSystem>();
        var before=GameplaySnapshot();before.Remove(cover);before.Remove(water);foreach(var field in fields)before.Remove(field);
        var oldHouseIds=OrganicHomes().Select(h=>h.GetInstanceID()).ToArray();
        Undo.IncrementCurrentGroup();int undo=Undo.GetCurrentGroup();Undo.SetCurrentGroupName("Organic village visual integration");
        Undo.RegisterFullObjectHierarchyUndo(village,"Before organic neighbourhood");Undo.RegisterFullObjectHierarchyUndo(Find("Ground"),"Before organic field terrain");
        try
        {
            EnsureFolder(OrganicAssets);var root=Group(OrganicRoot,village.transform).transform;
            NormalizeOrganicHouses(root);
            foreach(var old in Find(DeepRoot).GetComponentsInChildren<MeshFilter>().Where(f=>f.name.StartsWith("BoRuong_LienGoc_")))old.gameObject.SetActive(false);
            var sources=water.surfaces.Where(s=>!s.canal).ToArray();var surfaces=water.surfaces.Where(s=>s.canal).ToList();
            foreach(var source in sources)source.renderer.enabled=false;
            var bankMaterial=AssetDatabase.LoadAssetAtPath<Material>(NewMaterials+"/BoRuong_DatLomNhom.mat");
            var counts=new[]{3300,3600,4164,3500};
            for(int i=0;i<fields.Length;i++)
            {
                var field=fields[i];if(i>=4){field.enabled=false;EditorUtility.SetDirty(field);continue;}
                var contour=OrganicContour(OrganicZones[i]);var outline=InsetContour(contour,.22f);
                field.fieldPolygon=outline.Select(p=>new Vector2(p.x,p.z)).ToArray();field.targetInstances=counts[i];field.normalizedHeight=.8f;
                field.soilHeight=-.22f;field.rootBurial=.05f;field.randomTilt=.1f;field.regionalGpuBatch=true;field.meanderingField=true;
                field.nearDistance=6;field.middleDistance=22;field.farDistance=140;field.Regenerate();EditorUtility.SetDirty(field);
                var data=sources[i];data.renderer.enabled=true;data.outline=field.fieldPolygon;data.rectangle=OutlineBounds(data.outline);data.height=-.12f;data.meandering=data.shoreMask=true;
                ReplaceWaterMesh(data.renderer,OrganicWaterMesh("NuocRuong_Vung_"+i,outline,-.12f));surfaces.Add(data);
                var bank=MeshProp(root,"BoRuong_HuuCo_"+i,Vector3.zero,OrganicBankMesh(i,contour),bankMaterial);Undo.AddComponent<MeshCollider>(bank).sharedMesh=bank.GetComponent<MeshFilter>().sharedMesh;
            }
            water.surfaces=surfaces.ToArray();water.ClearInteraction();
            if(water.reflectionProbe){water.reflectionProbe.enabled=false;water.reflectionProbe=null;}
            EditorUtility.SetDirty(water);ApplyVillageWaterBlocks(water);LowerPaddyBeds(water,"NenLang_RuongHuuCo");BuildBlendedVillageRoad();Physics.SyncTransforms();NormalizeOrganicTrees();
            cover.plants=PlantMaxtreeGrass(water);cover.plants=AddOrganicSeamClusters(cover.plants,water);cover.Rebuild();EditorUtility.SetDirty(cover);
            HarmonizeOrganicMaterials(village,fields[0].sharedMaterial,water);
            BatchExistingVillageBamboo(cover);
            var regional=Undo.AddComponent<VillageRiceRegions>(root.gameObject);regional.regions=fields.Take(4).ToArray();regional.player=fields[0].player;
            regional.culling=AssetDatabase.LoadAssetAtPath<ComputeShader>(NewMaterials+"/RiceRegionCull.compute");regional.Rebuild();EditorUtility.SetDirty(regional);
            ConfigureOrganicAtmosphere(root);ConfigureOrganicShadows(village);HideVillageDebugGrid();
            if(!oldHouseIds.SequenceEqual(OrganicHomes().Select(h=>h.GetInstanceID())))throw new Exception("Existing house loaders or instances were replaced.");
            VerifyGameplay(before);ValidateOrganicVillage();AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);
            if(!EditorSceneManager.SaveScene(scene))throw new Exception("Cannot save organic village.");Undo.CollapseUndoOperations(undo);
            CaptureOrganicVillage();EditorSceneManager.SaveScene(scene);Inspect(true);SceneView.RepaintAll();
            File.AppendAllText(OutputFolder+"/organic_village_report.txt","\nPreserved gameplay components: "+before.Count+". All five original house instance identities retained. Scene saved.\n");
        }
        catch {Undo.RevertAllDownToGroup(undo);throw;}
    }
    private static List<Vector3> OrganicContour(Vector2[] points)
    {
        var result=new List<Vector3>();
        for(int i=0;i<points.Length;i++)
        {
            Vector2 a=points[(i+points.Length-1)%points.Length],b=points[i],c=points[(i+1)%points.Length],d=points[(i+2)%points.Length];
            int steps=Mathf.Max(3,Mathf.CeilToInt(Vector2.Distance(b,c)/.6f));
            for(int k=0;k<steps;k++){float t=(float)k/steps,t2=t*t,t3=t2*t;var p=.5f*(2*b+(-a+c)*t+(2*a-5*b+4*c-d)*t2+(-a+3*b-3*c+d)*t3);result.Add(new Vector3(p.x,0,p.y));}
        }
        return result;
    }
    private static Mesh SaveOrganicMesh(Mesh mesh)
    {
        string path=OrganicAssets+"/"+mesh.name+".asset";var existing=AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if(existing){EditorUtility.CopySerialized(mesh,existing);UnityEngine.Object.DestroyImmediate(mesh);EditorUtility.SetDirty(existing);return existing;}
        AssetDatabase.CreateAsset(mesh,path);return mesh;
    }
    private static Mesh OrganicMesh(string name,List<Vector3> vertices,List<int> triangles,List<Color> colors)
    {
        var mesh=new Mesh{name=name,indexFormat=IndexFormat.UInt32};mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.SetColors(colors);
        mesh.SetUVs(0,vertices.Select(p=>new Vector2(p.x*.3f,p.z*.3f)).ToList());mesh.RecalculateNormals();mesh.RecalculateBounds();return SaveOrganicMesh(mesh);
    }
    private static Mesh OrganicWaterMesh(string name,List<Vector3> outline,float height)
    {
        var inner=InsetContour(outline,.35f);var vertices=new List<Vector3>();var triangles=new List<int>();var colors=new List<Color>();int count=outline.Count;
        for(int i=0;i<count;i++)
        {
            var a=outline[i];a.y=height;var b=inner[i];b.y=height;vertices.Add(VillageLandscapeShape.Warp(a));vertices.Add(VillageLandscapeShape.Warp(b));colors.Add(new Color(0,0,0,1));colors.Add(new Color(.35f,0,0,1));
            int n=(i+1)%count;triangles.AddRange(new[]{i*2,n*2+1,n*2,i*2,i*2+1,n*2+1});
        }
        var center=inner.Aggregate(Vector3.zero,(a,b)=>a+b)/count;center.y=height;int middle=vertices.Count;vertices.Add(VillageLandscapeShape.Warp(center));colors.Add(Color.red);
        for(int i=0;i<count;i++)triangles.AddRange(new[]{middle,((i+1)%count)*2+1,i*2+1});return OrganicMesh(name,vertices,triangles,colors);
    }
    private static Mesh OrganicBankMesh(int index,List<Vector3> contour)
    {
        var vertices=new List<Vector3>();var triangles=new List<int>();var colors=new List<Color>();float[] cross={-.5f,-.35f,-.18f,0,.18f,.35f,.5f},height={-.005f,.035f,.067f,.08f,.02f,-.09f,-.20f};
        for(int i=0;i<contour.Count;i++)
        {
            var p=contour[i];var side=Vector3.Cross((contour[(i+1)%contour.Count]-contour[(i+contour.Count-1)%contour.Count]).normalized,Vector3.up);
            float noise=(Mathf.PerlinNoise(p.x*1.4f+19,p.z*1.2f+index)-.5f)*.026f;
            for(int c=0;c<7;c++)
            {
                var v=p+side*cross[c];v.y=height[c]+noise*(1-Mathf.Abs(c-3)/3f);vertices.Add(VillageLandscapeShape.Warp(v));colors.Add(new Color(c<3?.65f:c==3?.4f:0,0,0,1));
                if(c<6){int a=i*7+c,b=((i+1)%contour.Count)*7+c;triangles.AddRange(new[]{a,b+1,b,a,a+1,b+1});}
            }
        }
        return OrganicMesh("BoDat_HuuCo_"+index,vertices,triangles,colors);
    }
    private static Material BlendedEarthMaterial()
    {
        string path=OrganicAssets+"/DatLang_ChuyenCo.mat";var material=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(!material){var source=AssetDatabase.LoadAssetAtPath<Material>(NewMaterials+"/NenLang_KenhSau.mat");material=new Material(source){name="DatLang_ChuyenCo"};AssetDatabase.CreateAsset(material,path);}
        material.SetFloat("_UseVertexCoverage",1);material.SetFloat("_GrassCoverage",1);material.SetColor("_SoilColor",new Color(.53f,.44f,.30f));EditorUtility.SetDirty(material);return material;
    }
    private static void NormalizeOrganicHouses(Transform root)
    {
        var village=Find(VillageName).transform;var treePairs=new List<Tuple<Transform,Collider>>();
        var originals=village.GetComponentsInChildren<Transform>().Where(t=>t.name=="CayOi_SanNha" || t.name=="CayAnQuaCanhTiem" || t.name=="CayAnQua_VuonSau" || t.name=="CayKyUc_CheSanBi").ToArray();
        foreach(var replacement in Find(MaxtreeRoot).GetComponentsInChildren<LODGroup>())
        {
            var match=originals.SelectMany(t=>t.GetComponentsInChildren<Collider>()).OrderBy(c=>Vector2.Distance(new Vector2(c.bounds.center.x,c.bounds.center.z),new Vector2(replacement.transform.position.x,replacement.transform.position.z))).FirstOrDefault();
            if(match)treePairs.Add(Tuple.Create(replacement.transform,match));
        }
        Vector2[] centers={new Vector2(-16,5),new Vector2(16.5f,42),new Vector2(-16.5f,25.5f),new Vector2(16,66),new Vector2(-17,64)};
        float[] turns={-6,7,-5,6,8},heights={4.45f,4.7f,4.65f,4.3f,4.5f};
        for(int i=0;i<HomeNames.Length;i++)
        {
            var site=village.Find("NhaDan_VaSanVuon/SanNha_Gon_0"+(i+1));var home=site.Find(HomeNames[i]);var bounds=RendererBounds(home);
            var target=VillageLandscapeShape.Warp(new Vector3(centers[i].x,0,centers[i].y));
            site.RotateAround(new Vector3(bounds.center.x,0,bounds.center.z),Vector3.up,turns[i]);bounds=RendererBounds(home);
            site.position+=new Vector3(target.x-bounds.center.x,0,target.z-bounds.center.z);
            bounds=RendererBounds(home);home.localScale*=heights[i]/bounds.size.y;bounds=RendererBounds(home);home.position+=Vector3.up*(.05f-bounds.min.y);
            BuildBlendedYard(site.Find("SanDatNha"),RendererBounds(home),i);home.GetComponentInChildren<LODGroup>()?.RecalculateBounds();
        }
        var garden=Find("CayAnQua_VuonSau");if(garden)garden.transform.position=VillageLandscapeShape.Warp(new Vector3(-23,0,25));
        Physics.SyncTransforms();foreach(var pair in treePairs){var p=pair.Item1.position;p.x=pair.Item2.bounds.center.x;p.z=pair.Item2.bounds.center.z;pair.Item1.position=p;}
    }
    private static void BuildBlendedYard(Transform pad,Bounds house,int index)
    {
        if(!pad)throw new Exception("Existing house yard missing.");var vertices=new List<Vector3>();var colors=new List<Color>();var triangles=new List<int>();
        var center=new Vector3(house.center.x,.10f,house.center.z);float rx=house.extents.x+1.8f,rz=house.extents.z+1.7f;
        vertices.Add(center);colors.Add(new Color(0,0,0,1));
        for(int ring=1;ring<=4;ring++)for(int i=0;i<64;i++)
        {
            float a=i*Mathf.PI*2/64,r=ring/4f*(1+.07f*Mathf.Sin(a*3+index)+.035f*Mathf.Cos(a*7-index));
            var p=center+new Vector3(Mathf.Cos(a)*rx*r,0,Mathf.Sin(a)*rz*r);p.y=Mathf.Lerp(.10f,.005f,Mathf.SmoothStep(0,1,Mathf.InverseLerp(.65f,1,ring/4f)));
            vertices.Add(p);colors.Add(new Color(Mathf.SmoothStep(0,1,Mathf.InverseLerp(.55f,1,ring/4f)),0,0,1));
            int next=(i+1)%64;if(ring==1)triangles.AddRange(new[]{0,1+next,1+i});
            else {int a0=1+(ring-2)*64+i,b0=1+(ring-1)*64+i,a1=1+(ring-2)*64+next,b1=1+(ring-1)*64+next;triangles.AddRange(new[]{a0,b1,b0,a0,a1,b1});}
        }
        var mesh=OrganicMesh("SanNha_ChuyenCo_"+index,vertices,triangles,colors);pad.SetPositionAndRotation(Vector3.zero,Quaternion.identity);pad.localScale=Vector3.one;
        pad.GetComponent<MeshFilter>().sharedMesh=mesh;pad.GetComponent<Renderer>().sharedMaterial=BlendedEarthMaterial();var collider=pad.GetComponent<MeshCollider>();collider.sharedMesh=null;collider.sharedMesh=mesh;
    }
    private static void NormalizeOrganicTrees()
    {
        var ground=Find("Ground").GetComponent<MeshCollider>();int index=0;
        foreach(var tree in Find(MaxtreeRoot).GetComponentsInChildren<LODGroup>())
        {
            float target=tree.name.EndsWith("_SanBi")?6.6f:5.4f+(index++%4)*.35f;var bounds=RendererBounds(tree.transform);tree.transform.localScale*=target/bounds.size.y;
            bounds=RendererBounds(tree.transform);var p=tree.transform.position;float y=0;if(ground.Raycast(new Ray(new Vector3(p.x,3,p.z),Vector3.down),out var hit,5))y=hit.point.y;
            foreach(var contact in Physics.RaycastAll(new Ray(new Vector3(p.x,10,p.z),Vector3.down),15,~0,QueryTriggerInteraction.Ignore))
                if(contact.collider.name=="SanDatNha" || contact.collider.name.StartsWith("BoRuong_"))y=Mathf.Max(y,contact.point.y);
            tree.transform.position+=Vector3.up*(y-.05f-bounds.min.y);tree.RecalculateBounds();
        }
    }
    private static void BuildBlendedVillageRoad()
    {
        var road=Find("DuongDat_QuaTiemTapHoa");float[] cross={-2.5f,-2.18f,-1.85f,-.8f,.8f,1.85f,2.18f,2.5f};var vertices=new List<Vector3>();var colors=new List<Color>();var triangles=new List<int>();
        for(int row=0;row<=188;row++)
        {
            float z=-10+row*.5f;
            for(int c=0;c<cross.Length;c++)
            {
                float x=cross[c],edge=Mathf.SmoothStep(0,1,Mathf.InverseLerp(1.8f,2.5f,Mathf.Abs(x)));var p=VillageLandscapeShape.Warp(new Vector3(x,Mathf.Lerp(.10f,.008f,edge),z));
                vertices.Add(p);colors.Add(new Color(edge,0,0,1));if(row>0 && c>0){int a=(row-1)*8+c-1;triangles.AddRange(new[]{a,a+8,a+9,a,a+9,a+1});}
            }
        }
        var mesh=OrganicMesh("DuongDat_VienMem",vertices,triangles,colors);road.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);road.transform.localScale=Vector3.one;
        road.GetComponent<MeshFilter>().sharedMesh=mesh;road.GetComponent<Renderer>().sharedMaterial=BlendedEarthMaterial();var collider=road.GetComponent<MeshCollider>();if(!collider)collider=Undo.AddComponent<MeshCollider>(road);collider.sharedMesh=null;collider.sharedMesh=mesh;
    }
    private static VillageGroundCoverRenderer.Plant[] AddOrganicSeamClusters(VillageGroundCoverRenderer.Plant[] existing,VillageWaterSystem water)
    {
        var output=new List<VillageGroundCoverRenderer.Plant>(existing);var random=new System.Random(41542);var anchors=new List<Vector3>();
        for(float z=-5;z<80;z+=2.7f)foreach(int side in new[]{-1,1})
        {
            anchors.Add(VillageLandscapeShape.Warp(new Vector3(side*(2.8f+(float)random.NextDouble()*.3f),0,z)));
            anchors.Add(VillageLandscapeShape.Warp(new Vector3(side*5+side*1.4f+CanalDrift(z),0,z+.7f)));
            anchors.Add(VillageLandscapeShape.Warp(new Vector3(side*5-side*1.4f+CanalDrift(z),0,z+1.4f)));
        }
        foreach(var pad in Find(VillageName).GetComponentsInChildren<MeshFilter>().Where(f=>f.name=="SanDatNha"))
        {
            var bounds=pad.GetComponent<Renderer>().bounds;
            for(int i=0;i<18;i++){float a=i*Mathf.PI*2/18;anchors.Add(bounds.center+new Vector3(Mathf.Cos(a)*bounds.extents.x*.9f,0,Mathf.Sin(a)*bounds.extents.z*.9f));}
        }
        foreach(var anchor in anchors)for(int n=0;n<3;n++)
        {
            if(output.Count>=2400)return output.ToArray();
            var p=anchor+new Vector3(((float)random.NextDouble()-.5f)*.7f,0,((float)random.NextDouble()-.5f)*.7f);if(!ClearForGrass(p,true))continue;
            if(!Physics.Raycast(new Vector3(p.x,3,p.z),Vector3.down,out var hit,5,~0,QueryTriggerInteraction.Ignore) || (hit.collider.name!="Ground" && hit.collider.name!="SanDatNha" && !hit.collider.name.StartsWith("BoRuong_")))continue;
            if(hit.point.y<-.06f)continue;p.y=hit.point.y-.015f;
            output.Add(new VillageGroundCoverRenderer.Plant{position=p,yaw=(float)random.NextDouble()*360,scale=.65f+(float)random.NextDouble()*.65f,prototype=random.NextDouble()<.8?0:3,paddy=false});
        }
        return output.ToArray();
    }
    private static void HarmonizeOrganicMaterials(GameObject village,Material rice,VillageWaterSystem water)
    {
        foreach(var material in village.GetComponentsInChildren<Renderer>().SelectMany(r=>r.sharedMaterials).Where(m=>m).Distinct())
        {
            if(!AssetDatabase.GetAssetPath(material).StartsWith("Assets/_Project/Art/"))continue;
            bool glass=material.name.Contains("VienBi") || material.name.Contains("BiVe") || material.name.Contains("Marble");
            if(glass)continue;
            if(material.HasProperty("_Smoothness") && !glass)material.SetFloat("_Smoothness",Mathf.Min(.25f,material.GetFloat("_Smoothness")));
            if(material.HasProperty("_Metallic"))material.SetFloat("_Metallic",0);
            if(material.HasProperty("_BaseColor"))
            {
                var color=material.GetColor("_BaseColor");Color.RGBToHSV(color,out float h,out float s,out float v);var muted=Color.HSVToRGB(h,Mathf.Min(s,.68f),Mathf.Min(v,.92f));muted.a=color.a;material.SetColor("_BaseColor",muted);
            }
            if(material.HasProperty("_EmissionColor"))material.SetColor("_EmissionColor",Color.black);EditorUtility.SetDirty(material);
        }
        rice.SetColor("_BaseColor",Hex("#d4af37"));rice.SetFloat("_Saturation",.58f);rice.SetFloat("_Brightness",.9f);rice.SetFloat("_NormalStrength",.3f);rice.enableInstancing=true;EditorUtility.SetDirty(rice);
        string path=OrganicAssets+"/Nuoc_Bun_Hdri.mat";var muddy=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(!muddy){muddy=new Material(Shader.Find("KuTy/VillageWater")){name="Nuoc_Bun_Hdri"};AssetDatabase.CreateAsset(muddy,path);}
        muddy.SetColor("_ShallowColor",Hex("#3d3522"));muddy.SetColor("_DeepColor",Hex("#3d3522"));muddy.SetFloat("_DepthTone",.5f);muddy.SetFloat("_Opacity",.82f);
        muddy.SetFloat("_WaveStrength",.014f);muddy.SetFloat("_ReflectionRoughness",.15f);muddy.SetFloat("_EnvironmentIntensity",.8f);muddy.SetColor("_HorizonColor",Hex("#decbb1"));muddy.SetColor("_ZenithColor",new Color(.55f,.58f,.60f));EditorUtility.SetDirty(muddy);
        foreach(var surface in water.surfaces){surface.renderer.sharedMaterial=muddy;surface.renderer.shadowCastingMode=ShadowCastingMode.Off;surface.renderer.receiveShadows=true;surface.renderer.reflectionProbeUsage=ReflectionProbeUsage.Off;}
    }
    private static void BatchExistingVillageBamboo(VillageGroundCoverRenderer cover)
    {
        var bamboo=Find(MemoryRoot).GetComponentsInChildren<MeshFilter>().Where(f=>f.name=="CumTre_LechNhip" && WithinPlayableVegetation(f.transform.position)).ToArray();if(bamboo.Length==0)return;
        var source=bamboo[0];int prototype=cover.prototypes.Length;var materials=source.GetComponent<Renderer>().sharedMaterials;
        foreach(var material in materials){material.enableInstancing=true;EditorUtility.SetDirty(material);}
        var prototypes=cover.prototypes.ToList();prototypes.Add(new VillageGroundCoverRenderer.Prototype{name="Tre_HienCo",near=source.sharedMesh,middle=source.sharedMesh,far=source.sharedMesh,materials=materials});cover.prototypes=prototypes.ToArray();
        var plants=cover.plants.ToList();
        foreach(var filter in bamboo)
        {
            filter.GetComponent<Renderer>().enabled=false;
            plants.Add(new VillageGroundCoverRenderer.Plant{position=filter.transform.position,yaw=filter.transform.eulerAngles.y,scale=filter.transform.lossyScale.x,prototype=prototype});
        }
        cover.plants=plants.ToArray();cover.Rebuild();EditorUtility.SetDirty(cover);
    }
    private static void ConfigureOrganicAtmosphere(Transform root)
    {
        string path=OrganicAssets+"/Village_URP.asset";var pipeline=AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(path);
        if(!pipeline){AssetDatabase.CopyAsset("Assets/_Project/Generated/URP.asset",path);pipeline=AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(path);}
        pipeline.shadowDistance=45;pipeline.shadowCascadeCount=2;pipeline.renderScale=1;
        var serialized=new SerializedObject(pipeline);serialized.FindProperty("m_Cascade2Split").floatValue=.35f;serialized.FindProperty("m_MainLightShadowmapResolution").intValue=2048;serialized.FindProperty("m_AdditionalLightShadowsSupported").boolValue=false;serialized.FindProperty("m_SoftShadowsSupported").boolValue=true;serialized.FindProperty("m_RequireDepthTexture").boolValue=false;serialized.FindProperty("m_RequireOpaqueTexture").boolValue=false;serialized.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(pipeline);
        var settings=Undo.AddComponent<VillageVisualIntegration>(root.gameObject);settings.villagePipeline=pipeline;settings.fogColor=Hex("#decbb1");settings.fogDensity=.007f;settings.heightFalloff=.22f;settings.Apply();EditorUtility.SetDirty(settings);
        var sun=Find("Sun").GetComponent<Light>();sun.shadows=LightShadows.Soft;sun.shadowStrength=.72f;sun.shadowBias=.05f;sun.shadowNormalBias=.35f;
        if(RenderSettings.skybox.HasProperty("_HorizonColor")){RenderSettings.skybox.SetColor("_HorizonColor",Hex("#decbb1"));EditorUtility.SetDirty(RenderSettings.skybox);}
        DynamicGI.UpdateEnvironment();
    }
    public static void FinishOrganicVillage()
    {
        var root=Find(OrganicRoot);if(!root)throw new Exception("Organic village missing.");var before=GameplaySnapshot();
        // These opaque glass-marble materials use their texture as low-cost emission
        // for readability. Global HDRI intensity still applies to their reflections.
        foreach(string path in Directory.GetFiles("Assets/_Project/Art/Materials/Scene3/VienBi","*.mat"))
        {
            var material=AssetDatabase.LoadAssetAtPath<Material>(path);material.SetColor("_EmissionColor",Color.white);material.SetColor("_BaseColor",new Color(.08f,.08f,.08f,1));material.SetFloat("_Metallic",.03f);material.SetFloat("_Smoothness",.8f);material.EnableKeyword("_EMISSION");EditorUtility.SetDirty(material);
        }
        var regional=root.GetComponent<VillageRiceRegions>();
        var atlasPath=AssetDatabase.GetAssetPath(regional.regions[0].sharedMaterial.GetTexture("_BaseMap"));var atlas=(TextureImporter)AssetImporter.GetAtPath(atlasPath);
        if(atlas && (!atlas.mipMapsPreserveCoverage || atlas.anisoLevel<4))
        {atlas.mipmapEnabled=true;atlas.mipMapsPreserveCoverage=true;atlas.alphaTestReferenceValue=.3f;atlas.alphaIsTransparency=true;atlas.anisoLevel=4;atlas.filterMode=FilterMode.Trilinear;atlas.SaveAndReimport();}
        regional.Rebuild();ValidateOrganicVillage();VerifyGameplay(before);AssetDatabase.SaveAssets();
        CaptureOrganicVillage();File.AppendAllText(OutputFolder+"/organic_village_report.txt","GPU device: "+SystemInfo.graphicsDeviceName+", API "+SystemInfo.graphicsDeviceType+"\n");
        var source=regional.regions[0];foreach(var mesh in new[]{source.nearMesh,source.middleMesh,source.farMesh})File.AppendAllText(OutputFolder+"/organic_village_report.txt",mesh.name+" mesh bounds "+mesh.bounds+", triangles "+mesh.triangles.Length/3+"\n");
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());EditorSceneManager.SaveScene(SceneManager.GetActiveScene());Inspect(true);SceneView.RepaintAll();
    }
    private static void ConfigureOrganicShadows(GameObject village)
    {
        foreach(var renderer in village.GetComponentsInChildren<Renderer>())
        {
            string path=HierarchyPath(renderer.transform);bool large=path.Contains("/Nha0") || path.Contains("/CayMaxtree_") || renderer.name.StartsWith("Cau") || path.Contains("BanBe_PhacThao_") || renderer is SkinnedMeshRenderer;
            renderer.shadowCastingMode=large?ShadowCastingMode.On:ShadowCastingMode.Off;renderer.receiveShadows=true;
        }
    }
    private static void HideVillageDebugGrid()
    {
        foreach(SceneView view in SceneView.sceneViews)view.showGrid=false;
        foreach(var t in AllTransforms().Where(t=>t.gameObject.activeInHierarchy && (t.name=="DebugGrid" || t.name=="GroundGrid" || t.name=="GridHelper")))t.gameObject.SetActive(false);
    }
    public static void ValidateOrganicVillage()
    {
        if(Find(ReferenceRoot)){ValidateReferenceVillage();return;}
        var root=Find(OrganicRoot);if(!root)throw new Exception("Organic village missing.");var regional=root.GetComponent<VillageRiceRegions>();
        if(!regional || regional.regions.Length!=4 || regional.TotalInstances!=14564)throw new Exception("Expected four regional GPU rice batches and 14,564 instances.");
        var matrices=new List<Matrix4x4>();float minHeight=float.MaxValue,maxHeight=0,maxRootError=0;
        foreach(var field in regional.regions)
        {
            if(!field.regionalGpuBatch || field.fieldPolygon.Length<8 || field.PlantCount!=field.targetInstances)throw new Exception("Organic rice polygon or population failed.");
            matrices.Clear();field.CopyInstances(matrices);var source=field.nearMesh.bounds;
            foreach(var m in matrices)
            {
                var center=m.MultiplyPoint3x4(source.center);float extent=Mathf.Abs(m.m10)*source.extents.x+Mathf.Abs(m.m11)*source.extents.y+Mathf.Abs(m.m12)*source.extents.z;
                minHeight=Mathf.Min(minHeight,extent*2);maxHeight=Mathf.Max(maxHeight,extent*2);
                var canonical=VillageLandscapeShape.Unwarp(center);if(!RuralRiceFieldRenderer.InsidePolygon(new Vector2(canonical.x,canonical.z),field.fieldPolygon))throw new Exception("Rice outside field polygon.");
                var origin=VillageLandscapeShape.Unwarp(m.MultiplyPoint3x4(Vector3.zero));float ground=-.22f+.007f*Mathf.Sin(origin.x*3.1f+origin.z*1.7f);
                maxRootError=Mathf.Max(maxRootError,Mathf.Abs(center.y-extent-(ground-.05f)));
            }
        }
        if(minHeight<.699f || maxHeight>.901f || maxRootError>.002f)throw new Exception("Rice scale or bounding-box ground normalization failed.");
        foreach(var house in OrganicHomes()){var bounds=RendererBounds(house);if(bounds.size.y<4 || bounds.size.y>5 || Mathf.Abs(bounds.min.y-.05f)>.005f)throw new Exception("House normalization failed: "+house.name+" height="+bounds.size.y+", bottom="+bounds.min.y);}
        foreach(var tree in Find(MaxtreeRoot).GetComponentsInChildren<LODGroup>()){var b=RendererBounds(tree.transform);if(b.size.y<4.99f || b.size.y>8.01f)throw new Exception("Tree human scale normalization failed.");}
        var water=Find(VillageName).GetComponentInChildren<VillageWaterSystem>();if(water.surfaces.Length!=6 || water.surfaces.Count(s=>s.canal)!=2 || water.reflectionProbe)throw new Exception("Water regions or zero-runtime-reflection budget failed.");
        foreach(var surface in water.surfaces)
        {
            var p=VillageLandscapeShape.Warp(new Vector3((surface.rectangle.x+surface.rectangle.z)*.5f,surface.height,(surface.rectangle.y+surface.rectangle.w)*.5f));
            if(!surface.Contains(p))throw new Exception("Polygon water contact mismatch.");
            if(surface.renderer.shadowCastingMode!=ShadowCastingMode.Off || !surface.renderer.receiveShadows)throw new Exception("Selective water shadows failed.");
        }
        var cover=Find(MaxtreeRoot).GetComponent<VillageGroundCoverRenderer>();if(cover.plants.Any(p=>p.prototype<4 && !ClearForGrass(p.position,true)))throw new Exception("Grass invades the marble clearing or access paths.");
        var settings=root.GetComponent<VillageVisualIntegration>();if(!settings || settings.villagePipeline.shadowDistance>45.01f)throw new Exception("Scene-local render budget missing.");
        var shaders=new[]{Shader.Find("KuTy/Village/Rice Instanced Wind"),Shader.Find("KuTy/Village/Maxtree Matte Wind"),Shader.Find("KuTy/VillageWater"),Shader.Find("KuTy/Village/Matte Grass Earth")};
        foreach(var shader in shaders){var errors=ShaderUtil.GetShaderMessages(shader).Where(m=>m.severity==UnityEditor.Rendering.ShaderCompilerMessageSeverity.Error).ToArray();if(errors.Length>0)throw new Exception(shader.name+": "+string.Join("; ",errors.Select(e=>e.message)));}
        Physics.SyncTransforms();VerifyWalking();VerifyCanalExit();VerifyOrganicPaddyWalking(water);string contacts=VerifyVillageWaterInteraction(water);water.ClearInteraction();
        File.WriteAllText(OutputFolder+"/organic_village_report.txt","PASS\nUnity native integration; original model loaders, meshes and gameplay retained.\n4 large polygon field regions, 14,564 rice instances (4x previous 3,641); 0.700-0.900 m height, random yaw/tilt/scale, box bottom 0.05 m into mud.\nGPU compute frustum culling + LOD, <= 12 indirect draws (4 regions x 3 mesh LODs), no per-frame CPU matrix rebuilds or GPU readback.\nHouses 4-5 m, trees 5-8 m; bounding-box grounding. Neighbourhood paths and marble clearing retained.\nShared muddy HDRI water #3d3522, roughness .15, env .8; no runtime reflection camera/probe. Global reflection .6.\nHeight fog #decbb1, scene-specific URP, 2048 shadow atlas, 45 m shadow range, two cascades. No foliage or water shadow casting.\nBlended house yards, road borders and clustered seam weeds. Grass clumps: "+cover.plants.Length+".\n"+contacts+"\nRice root error max: "+maxRootError+"\n");
    }
    private static void VerifyOrganicPaddyWalking(VillageWaterSystem water)
    {
        var ground=Find("Ground").GetComponent<MeshCollider>();
        foreach(var surface in water.surfaces.Where(s=>!s.canal))
        {
            var center=VillageLandscapeShape.Warp(new Vector3((surface.rectangle.x+surface.rectangle.z)*.5f,3,(surface.rectangle.y+surface.rectangle.w)*.5f));
            if(!ground.Raycast(new Ray(center,Vector3.down),out var hit,5) || hit.point.y>-.18f || !surface.Contains(hit.point))throw new Exception("Paddy terrain, collision and water disagree.");
        }
    }
    public static void CaptureOrganicVillage()
    {
        string folder=OutputFolder+"/Organic";Directory.CreateDirectory(folder);var scene=SceneManager.GetActiveScene();bool dirty=scene.isDirty;
        var views=new[]{new[]{new Vector3(62,69,-25),new Vector3(0,0,35)},new[]{new Vector3(0,1.65f,-5),new Vector3(0,.7f,35)},new[]{new Vector3(-8,1.7f,39),new Vector3(-18,2,47)},new[]{new Vector3(9,1.5f,0),new Vector3(18,.4f,5)}};
        for(int i=0;i<views.Length;i++)
        {
            var obj=new GameObject("OrganicPreview"){hideFlags=HideFlags.HideAndDontSave};var camera=obj.AddComponent<Camera>();camera.enabled=false;camera.fieldOfView=i==0?58:66;camera.nearClipPlane=.03f;camera.farClipPlane=300;camera.allowHDR=true;
            camera.transform.SetPositionAndRotation(views[i][0],Quaternion.LookRotation(views[i][1]-views[i][0]));camera.GetUniversalAdditionalCameraData().renderPostProcessing=false;
            var rt=new RenderTexture(1400,900,24);rt.Create();var old=RenderTexture.active;var image=new Texture2D(1400,900,TextureFormat.RGB24,false);
            try{RenderPipeline.SubmitRenderRequest(camera,new UniversalRenderPipeline.SingleCameraRequest{destination=rt});RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,1400,900),0,0);image.Apply();File.WriteAllBytes(folder+"/"+(i+1).ToString("00")+".png",image.EncodeToPNG());var regions=Find(OrganicRoot).GetComponent<VillageRiceRegions>();int visible=regions.ReadVisibleInstanceCount();File.AppendAllText(OutputFolder+"/organic_village_report.txt","View "+i+": "+regions.LastVisibleRegions+" visible regions, "+regions.LastDrawCalls+" rice draw commands, "+visible+" visible GPU instances.\n");if(i==0 && visible<10000)throw new Exception("GPU visibility verification failed: fewer than 10,000 plants in full village view.");}
            finally{RenderTexture.active=old;UnityEngine.Object.DestroyImmediate(image);rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(obj);}
        }
        if(!dirty)EditorSceneManager.SaveScene(scene);
    }
}
