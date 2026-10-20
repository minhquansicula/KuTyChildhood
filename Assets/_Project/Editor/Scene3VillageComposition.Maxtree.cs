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
    private const string MaxtreeModels="Assets/_Project/Art/Models/Scene3/MaxtreeOptimized";
    private const string MaxtreeTextures="Assets/_Project/Art/Textures/Scene3/MaxtreeOptimized";
    private const string MaxtreeMaterials="Assets/_Project/Art/Materials/Scene3/MaxtreeOptimized";
    private const string MaxtreeRoot="Maxtree_VungNguoiChoi";
    private static readonly string[] GrassNames={"CoThap","CoManTrau","CoLac","CoDuoiChon"};

    [MenuItem("KuTy/Scene 3/Install Maxtree in playable village and lower paddies")]
    public static void InstallMaxtreeVillage()
    {
        var scene=SceneManager.GetActiveScene();
        if(scene.path!=ScenePath || EditorApplication.isPlayingOrWillChangePlaymode || !Find(DeepRoot))throw new Exception("Open the deep-canal village in Edit Mode.");
        if(Find(MaxtreeRoot)) {ValidateMaxtreeVillage();CaptureMaxtreeVillage();return;}
        string backup=OutputFolder+"/before-maxtree-playable-village.unity";
        if(!File.Exists(backup) && !EditorSceneManager.SaveScene(scene,backup,true))throw new Exception("Cannot back up current village.");
        PrepareMaxtreeImports();
        var village=Find(VillageName);var water=village.GetComponentInChildren<VillageWaterSystem>();var fields=village.GetComponentsInChildren<RuralRiceFieldRenderer>();
        var before=GameplaySnapshot();before.Remove(water);foreach(var field in fields)before.Remove(field);
        var populations=fields.Select(f=>f.PlantCount).ToArray();
        Undo.IncrementCurrentGroup();int undo=Undo.GetCurrentGroup();Undo.SetCurrentGroupName("Playable Maxtree vegetation and lower rice paddies");
        Undo.RegisterFullObjectHierarchyUndo(village,"Before Maxtree planting");Undo.RegisterFullObjectHierarchyUndo(Find("Ground"),"Before lower paddy beds");
        try
        {
            EnsureFolder(MaxtreeModels+"/Terrain");EnsureFolder(MaxtreeMaterials);
            var root=Group(MaxtreeRoot,village.transform).transform;
            LowerPaddyBeds(water);
            foreach(var field in fields) {var p=field.transform.position;p.y=-.17f;field.transform.position=p;field.Regenerate();EditorUtility.SetDirty(field);}
            foreach(int i in Enumerable.Range(0,water.surfaces.Length))
            {
                var data=water.surfaces[i];if(data.canal)continue;
                var renderer=data.renderer;var source=renderer.GetComponent<MeshFilter>().sharedMesh;
                var mesh=UnityEngine.Object.Instantiate(source);mesh.name="RuongHaThap_"+renderer.transform.parent.name;
                var vertices=mesh.vertices;for(int v=0;v<vertices.Length;v++){vertices[v]=renderer.transform.TransformPoint(vertices[v]);vertices[v].y=-.12f;}
                mesh.vertices=vertices;mesh.RecalculateBounds();
                string path=MaxtreeModels+"/Terrain/"+mesh.name+".asset";AssetDatabase.CreateAsset(mesh,path);
                ReplaceWaterMesh(renderer,mesh);data.height=-.12f;water.surfaces[i]=data;
            }
            EditorUtility.SetDirty(water);ApplyVillageWaterBlocks(water);Physics.SyncTransforms();
            var firstPaddy=water.surfaces.First(s=>!s.canal);var probePoint=VillageLandscapeShape.Warp(new Vector3((firstPaddy.rectangle.x+firstPaddy.rectangle.z)*.5f,3,(firstPaddy.rectangle.y+firstPaddy.rectangle.w)*.5f));
            bool groundHit=Find("Ground").GetComponent<MeshCollider>().Raycast(new Ray(probePoint,Vector3.down),out var probeHit,5);
            bool globalHit=Physics.Raycast(probePoint,Vector3.down,out var globalProbe,5,~0,QueryTriggerInteraction.Ignore);
            File.WriteAllText(OutputFolder+"/maxtree_terrain_debug.txt","Paddy center "+probePoint+" directGround="+groundHit+" at "+probeHit.point+" global="+globalHit+" "+(globalHit?globalProbe.collider.name:"")+" at "+globalProbe.point+"\n");
            int trees=ReplacePlayableTrees(root);
            var cover=Undo.AddComponent<VillageGroundCoverRenderer>(root.gameObject);
            cover.prototypes=GrassNames.Select(name=>
            {
                var filters=MaxtreeFilters(name);
                return new VillageGroundCoverRenderer.Prototype{name=name,near=BakedMaxtreeMesh(filters[0]),middle=BakedMaxtreeMesh(filters[1]),far=BakedMaxtreeMesh(filters[2]),
                    materials=MakeMaxtreeMaterials(filters[0].GetComponent<Renderer>().sharedMaterials,name)};
            }).ToArray();
            cover.plants=PlantMaxtreeGrass(water);cover.Rebuild();EditorUtility.SetDirty(cover);
            if(!populations.SequenceEqual(fields.Select(f=>f.PlantCount)))throw new Exception("Rice population changed while lowering paddies.");
            VerifyGameplay(before);ValidateMaxtreeVillage();ValidateMemoryVillage();ValidateDeepCanals();
            AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);
            if(!EditorSceneManager.SaveScene(scene))throw new Exception("Cannot save Maxtree village.");
            Undo.CollapseUndoOperations(undo);if(water.reflectionProbe)water.reflectionProbe.RenderProbe();
            CaptureMaxtreeVillage();CaptureMemoryVillage();EditorSceneManager.SaveScene(scene);Inspect(true);SceneView.RepaintAll();
            File.AppendAllText(OutputFolder+"/maxtree_report.txt","\nOriginal gameplay payloads preserved: "+before.Count+". Rice population retained: "+populations.Sum()+". Replaced trees: "+trees+". Scene saved.\n");
        }
        catch {Undo.RevertAllDownToGroup(undo);throw;}
    }

    private static void PrepareMaxtreeImports()
    {
        EnsureFolder(MaxtreeModels+"/Geometry");
        var shader=Shader.Find("KuTy/Village/Maxtree Matte Wind");if(!shader)throw new Exception("Maxtree wind shader is not imported.");
        foreach(var path in Directory.GetFiles(MaxtreeTextures,"*.png"))
        {
            AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);var texture=(TextureImporter)AssetImporter.GetAtPath(path);
            texture.textureType=TextureImporterType.Default;texture.sRGBTexture=true;texture.alphaSource=TextureImporterAlphaSource.FromInput;
            texture.alphaIsTransparency=true;texture.mipmapEnabled=true;texture.mipMapsPreserveCoverage=true;texture.alphaTestReferenceValue=.35f;
            texture.wrapMode=TextureWrapMode.Repeat;texture.filterMode=FilterMode.Trilinear;texture.anisoLevel=4;texture.isReadable=false;
            int size=path.Contains("_Trunk_")?2048:1024;texture.maxTextureSize=size;
            texture.SetPlatformTextureSettings(new TextureImporterPlatformSettings{name="Standalone",overridden=true,maxTextureSize=size,format=TextureImporterFormat.DXT5,compressionQuality=100});texture.SaveAndReimport();
        }
        foreach(string name in GrassNames.Concat(new[]{"CayTanRong","CayVuon"}))
        {
            string path=MaxtreeModels+"/"+name+".fbx";AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
            var importer=(ModelImporter)AssetImporter.GetAtPath(path);if(importer==null)throw new Exception("Missing optimized model: "+path);
            importer.importAnimation=false;importer.materialImportMode=ModelImporterMaterialImportMode.ImportStandard;
            importer.isReadable=false;importer.addCollider=false;importer.importNormals=ModelImporterNormals.Import;importer.importTangents=ModelImporterTangents.None;
            importer.bakeAxisConversion=true;importer.SaveAndReimport();
            var model=AssetDatabase.LoadAssetAtPath<GameObject>(path);
            foreach(var filter in model.GetComponentsInChildren<MeshFilter>())BakeMaxtreeMesh(filter);
        }
    }
    private static Mesh BakedMaxtreeMesh(MeshFilter filter)
    {
        var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(MaxtreeModels+"/Geometry/"+filter.name+".asset");
        if(!mesh)throw new Exception("Baked upright Maxtree mesh missing: "+filter.name);return mesh;
    }
    private static void BakeMaxtreeMesh(MeshFilter filter)
    {
        var mesh=UnityEngine.Object.Instantiate(filter.sharedMesh);mesh.name=filter.name;
        var matrix=filter.transform.localToWorldMatrix;var vertices=mesh.vertices;var normals=mesh.normals;var normalMatrix=matrix.inverse.transpose;
        for(int i=0;i<vertices.Length;i++){vertices[i]=matrix.MultiplyPoint3x4(vertices[i]);if(normals.Length==vertices.Length)normals[i]=normalMatrix.MultiplyVector(normals[i]).normalized;}
        if(filter.name.StartsWith("Cay"))RestoreMaxtreeCanopyCoverage(mesh,vertices,filter.name);
        mesh.vertices=vertices;if(normals.Length==vertices.Length)mesh.normals=normals;
        if(matrix.determinant<0)for(int sub=0;sub<mesh.subMeshCount;sub++){var indices=mesh.GetTriangles(sub);for(int i=0;i<indices.Length;i+=3){int swap=indices[i+1];indices[i+1]=indices[i+2];indices[i+2]=swap;}mesh.SetTriangles(indices,sub);}
        mesh.RecalculateBounds();string path=MaxtreeModels+"/Geometry/"+filter.name+".asset";
        var existing=AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if(existing){EditorUtility.CopySerialized(mesh,existing);UnityEngine.Object.DestroyImmediate(mesh);EditorUtility.SetDirty(existing);}
        else AssetDatabase.CreateAsset(mesh,path);
    }
    private static void RestoreMaxtreeCanopyCoverage(Mesh mesh,Vector3[] vertices,string name)
    {
        // Keep whole small leaf components readable as their population is reduced
        // for LOD. Root/trunk positions and the overall tree height are unchanged.
        var colors=mesh.colors;if(colors.Length!=vertices.Length)return;
        var parent=Enumerable.Range(0,vertices.Length).ToArray();
        Func<int,int> find=null;find=i=>{while(parent[i]!=i){parent[i]=parent[parent[i]];i=parent[i];}return i;};
        Action<int,int> union=(a,b)=>{a=find(a);b=find(b);if(a!=b)parent[b]=a;};
        var welded=new Dictionary<Vector3Int,int>();
        for(int i=0;i<vertices.Length;i++)
        {
            if(colors[i].g<.65f)continue;var v=vertices[i];var key=new Vector3Int(Mathf.RoundToInt(v.x*20000),Mathf.RoundToInt(v.y*20000),Mathf.RoundToInt(v.z*20000));
            if(welded.TryGetValue(key,out int other))union(i,other);else welded.Add(key,i);
        }
        var indices=mesh.triangles;
        for(int i=0;i<indices.Length;i+=3)
        {
            int a=indices[i],b=indices[i+1],c=indices[i+2];
            if(colors[a].g>.65f && colors[b].g>.65f && colors[c].g>.65f){union(a,b);union(a,c);}
        }
        var groups=new Dictionary<int,List<int>>();
        for(int i=0;i<vertices.Length;i++)if(colors[i].g>.65f){int key=find(i);if(!groups.TryGetValue(key,out var list)){list=new List<int>();groups.Add(key,list);}list.Add(i);}
        float scale=name.EndsWith("LOD0")?2.2f:name.EndsWith("LOD1")?3.2f:4.5f;float height=name.StartsWith("CayTanRong")?6.4f:4.8f;
        foreach(var group in groups.Values)
        {
            var bounds=new Bounds(vertices[group[0]],Vector3.zero);Vector3 center=Vector3.zero;
            foreach(int i in group){bounds.Encapsulate(vertices[i]);center+=vertices[i];}
            if(Mathf.Max(bounds.size.x,Mathf.Max(bounds.size.y,bounds.size.z))>.65f)continue;center/=group.Count;
            foreach(int i in group){vertices[i]=center+(vertices[i]-center)*scale;vertices[i].y=Mathf.Clamp(vertices[i].y,0,height);}
        }
    }
    private static MeshFilter[] MaxtreeFilters(string name)
    {
        var model=AssetDatabase.LoadAssetAtPath<GameObject>(MaxtreeModels+"/"+name+".fbx");
        var filters=model.GetComponentsInChildren<MeshFilter>().OrderBy(f=>f.name).ToArray();
        if(filters.Length!=3)throw new Exception("Expected three Maxtree LODs for "+name);
        // Bake any remaining import-node transform into a mesh asset before direct instancing.
        var result=new MeshFilter[3];
        for(int i=0;i<3;i++)
        {
            var f=filters[i];var mesh=BakedMaxtreeMesh(f);
            var bounds=mesh.bounds;float expected=name=="CayTanRong"?6.4f:name=="CayVuon"?4.8f:name=="CoLac"?.43f:name=="CoManTrau"?.23f:name=="CoThap"?.16f:.38f;
            if(Mathf.Abs(bounds.size.y-expected)>expected*.10f)throw new Exception("Model is not upright after FBX node baking: "+f.name+" "+bounds.size);
            result[i]=f;
        }
        return result;
    }
    private static Material[] MakeMaxtreeMaterials(Material[] sources,string prototype)
    {
        EnsureFolder(MaxtreeMaterials);var result=new Material[sources.Length];
        for(int i=0;i<sources.Length;i++)
        {
            if(!sources[i])throw new Exception("FBX material name missing for "+prototype);
            string original=sources[i].name;string path=MaxtreeMaterials+"/"+original+".mat";
            var material=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(!material){material=new Material(Shader.Find("KuTy/Village/Maxtree Matte Wind")){name=original};AssetDatabase.CreateAsset(material,path);}
            var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(MaxtreeTextures+"/"+original+".png");
            if(!texture)throw new Exception("Packed Maxtree maps missing: "+original);
            material.SetTexture("_BaseMap",texture);material.SetFloat("_HasTexture",1);material.SetFloat("_Cutoff",.35f);
            material.SetFloat("_WindAmplitude",prototype.StartsWith("Cay")?.028f:.035f);material.SetFloat("_Flexibility",prototype.StartsWith("Cay")?0:1);
            material.enableInstancing=true;EditorUtility.SetDirty(material);result[i]=material;
        }
        return result;
    }

    private static float ContourDistance(Vector2 p,Vector2[] polygon)
    {
        bool inside=false;float closest=float.MaxValue;
        for(int i=0,j=polygon.Length-1;i<polygon.Length;j=i++)
        {
            Vector2 a=polygon[j],b=polygon[i],edge=b-a;float t=Mathf.Clamp01(Vector2.Dot(p-a,edge)/Mathf.Max(.000001f,edge.sqrMagnitude));
            closest=Mathf.Min(closest,(p-(a+edge*t)).sqrMagnitude);
            if((a.y>p.y)!=(b.y>p.y) && p.x<(b.x-a.x)*(p.y-a.y)/(b.y-a.y)+a.x)inside=!inside;
        }
        return Mathf.Sqrt(closest)*(inside?1:-1);
    }
    private static float PaddyLowering(Vector2 p,VillageWaterSystem.Surface[] paddies)
    {
        float amount=0;
        foreach(var data in paddies)
        {
            var r=data.rectangle;if(p.x<r.x-.6f || p.x>r.z+.6f || p.y<r.y-.6f || p.y>r.w+.6f)continue;
            float distance=ContourDistance(p,data.outline);
            amount=Mathf.Max(amount,Mathf.SmoothStep(0,1,Mathf.InverseLerp(-.4f,.3f,distance)));
        }
        return amount;
    }
    private static void LowerPaddyBeds(VillageWaterSystem water,string assetName="NenLang_RuongHaThap")
    {
        var paddies=water.surfaces.Where(s=>!s.canal).ToArray();var xs=new SortedSet<float>();
        for(int i=0;i<=176;i++)xs.Add(-44+i*.5f);
        foreach(float center in new[]{-5,5})foreach(float d in new[]{-1.5f,-1.32f,-1.22f,-1f,-.75f,-.5f,-.25f,0,.25f,.5f,.75f,1f,1.22f,1.32f,1.5f})xs.Add(center+d);
        var columns=xs.ToArray();var vertices=new List<Vector3>();var colors=new List<Color>();var triangles=new List<int>();
        for(int row=0;row<=204;row++)
        {
            float z=-14+row*.5f;float fade=Mathf.SmoothStep(0,1,Mathf.InverseLerp(-14,-12,z))*(1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(82,85,z)));
            for(int c=0;c<columns.Length;c++)
            {
                float x=columns[c],distance=Mathf.Min(Mathf.Abs(x+5),Mathf.Abs(x-5));float y=0,mask=1;
                if(distance<=1.5f)
                {
                    if(distance<=.25f)y=-.82f+.018f*Mathf.Sin(z*.63f);
                    else if(distance<=1.22f)y=Mathf.Lerp(-.82f,-.005f,(distance-.25f)/.97f);
                    else if(distance<=1.32f)y=Mathf.Lerp(-.005f,.06f,(distance-1.22f)/.10f);
                    else y=Mathf.Lerp(.06f,0,(distance-1.32f)/.18f);
                    if(distance>.25f)y+=.008f*Mathf.Sin(z*1.3f+distance*4);
                    y*=fade;mask=Mathf.Lerp(1,Mathf.SmoothStep(0,1,Mathf.InverseLerp(.91f,1.35f,distance)),fade);x+=CanalDrift(z)*fade;
                }
                else
                {
                    float lower=PaddyLowering(new Vector2(x,z),paddies);y=-.22f*lower;
                    y+=.007f*Mathf.Sin(x*3.1f+z*1.7f)*lower;mask=1-lower;
                }
                var p=VillageLandscapeShape.Warp(new Vector3(x,y,z));if(c==0)p.x=-44;if(c==columns.Length-1)p.x=44;
                vertices.Add(p);colors.Add(new Color(mask,0,0,1));
                if(row>0 && c>0){int a=(row-1)*columns.Length+c-1;triangles.AddRange(new[]{a,a+columns.Length,a+columns.Length+1,a,a+columns.Length+1,a+1});}
            }
        }
        var mesh=new Mesh{name=assetName,indexFormat=IndexFormat.UInt32};mesh.SetVertices(vertices);mesh.SetColors(colors);mesh.SetTriangles(triangles,0);
        mesh.SetUVs(0,vertices.Select(p=>new Vector2(p.x*.3f,p.z*.3f)).ToList());mesh.RecalculateNormals();mesh.RecalculateBounds();
        string meshPath=MaxtreeModels+"/Terrain/"+mesh.name+".asset";var existing=AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
        if(existing){EditorUtility.CopySerialized(mesh,existing);UnityEngine.Object.DestroyImmediate(mesh);mesh=existing;EditorUtility.SetDirty(mesh);}
        else AssetDatabase.CreateAsset(mesh,meshPath);
        var ground=Find("Ground");ground.GetComponent<MeshFilter>().sharedMesh=mesh;
        var collider=ground.GetComponent<MeshCollider>();collider.sharedMesh=null;Physics.BakeMesh(mesh.GetInstanceID(),false);collider.sharedMesh=mesh;
    }

    private static int ReplacePlayableTrees(Transform root)
    {
        string[] names={"CayOi_SanNha","CayAnQuaCanhTiem","CayAnQua_VuonSau","CayKyUc_CheSanBi"};
        var originals=Find(VillageName).GetComponentsInChildren<Transform>().Where(t=>names.Contains(t.name)).ToArray();int count=0;
        foreach(var old in originals)
        {
            var renderers=old.GetComponentsInChildren<Renderer>().Where(r=>r.enabled && r.gameObject.activeInHierarchy).ToArray();if(renderers.Length==0)continue;
            var collider=old.GetComponentsInChildren<Collider>().OrderBy(c=>c.bounds.min.y).FirstOrDefault();var bounds=renderers[0].bounds;foreach(var r in renderers)bounds.Encapsulate(r.bounds);
            Vector3 position=collider?collider.bounds.center:bounds.center;position.y=Mathf.Max(.025f,bounds.min.y);
            if(!WithinPlayableVegetation(position))continue;
            bool hero=old.name=="CayKyUc_CheSanBi";string prototype=hero?"CayTanRong":"CayVuon";
            var filters=MaxtreeFilters(prototype);var node=Group("CayMaxtree_"+(++count).ToString("00")+(hero?"_SanBi":""),root).transform;
            node.position=position;node.rotation=Quaternion.Euler(0,(count*137)%360,0);node.localScale=Vector3.one*(hero?1:Mathf.Lerp(.85f,1.05f,Mathf.PerlinNoise(count,19)));
            var group=node.gameObject.AddComponent<LODGroup>();var lods=new LOD[3];
            for(int level=0;level<3;level++)
            {
                var leaf=Group("LOD"+level,node);var filter=leaf.AddComponent<MeshFilter>();filter.sharedMesh=BakedMaxtreeMesh(filters[level]);
                var renderer=leaf.AddComponent<MeshRenderer>();renderer.sharedMaterials=MakeMaxtreeMaterials(filters[level].GetComponent<Renderer>().sharedMaterials,prototype);
                renderer.shadowCastingMode=level==0?ShadowCastingMode.On:ShadowCastingMode.Off;renderer.receiveShadows=true;
                lods[level]=new LOD(level==0?.32f:level==1?.14f:.035f,new Renderer[]{renderer});
            }
            group.SetLODs(lods);group.RecalculateBounds();
            foreach(var renderer in renderers)renderer.enabled=false;
            if(!collider){var trunk=node.gameObject.AddComponent<CapsuleCollider>();trunk.radius=hero?.23f:.16f;trunk.height=2.8f;trunk.center=Vector3.up*1.4f;}
        }
        return count;
    }
    public static void FinishUprightMaxtreeVillage()
    {
        var scene=SceneManager.GetActiveScene();var root=Find(MaxtreeRoot);
        if(scene.path!=ScenePath || !root)throw new Exception("Installed Maxtree village missing.");
        var before=GameplaySnapshot();var cover=root.GetComponent<VillageGroundCoverRenderer>();before.Remove(cover);
        PrepareMaxtreeImports();
        for(int i=0;i<cover.prototypes.Length;i++)
        {
            var prototype=cover.prototypes[i];var filters=MaxtreeFilters(prototype.name);
            prototype.near=BakedMaxtreeMesh(filters[0]);prototype.middle=BakedMaxtreeMesh(filters[1]);prototype.far=BakedMaxtreeMesh(filters[2]);cover.prototypes[i]=prototype;
        }
        foreach(var tree in root.GetComponentsInChildren<LODGroup>())
        {
            var filters=MaxtreeFilters(tree.name.EndsWith("_SanBi")?"CayTanRong":"CayVuon");
            for(int level=0;level<3;level++)tree.transform.Find("LOD"+level).GetComponent<MeshFilter>().sharedMesh=BakedMaxtreeMesh(filters[level]);tree.RecalculateBounds();
        }
        cover.Rebuild();EditorUtility.SetDirty(cover);VerifyGameplay(before);ValidateMaxtreeVillage();ValidateMemoryVillage();ValidateDeepCanals();
        AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);if(!EditorSceneManager.SaveScene(scene))throw new Exception("Cannot save upright Maxtree meshes.");
        var water=Find(VillageName).GetComponentInChildren<VillageWaterSystem>();if(water.reflectionProbe)water.reflectionProbe.RenderProbe();
        CaptureMaxtreeVillage();CaptureMemoryVillage();EditorSceneManager.SaveScene(scene);Inspect(true);SceneView.RepaintAll();
        File.AppendAllText(OutputFolder+"/maxtree_report.txt","Upright FBX node transforms baked into mesh vertices/normals for all 18 LOD meshes. Trees and grass updated and saved.\n");
    }
    private static bool WithinPlayableVegetation(Vector3 p)
    {
        return p.x>-40.8f && p.x<40.8f && p.z>-8.5f && p.z<81 && !(Mathf.Abs(p.x)>36 && (p.z< -3 || p.z>75));
    }
    private static bool ClearForGrass(Vector3 p,bool organic=false)
    {
        if(!WithinPlayableVegetation(p))return false;
        if(Mathf.Pow((p.x+15)/7.7f,2)+Mathf.Pow((p.z-44)/6.8f,2)<1)return false;
        var canonical=VillageLandscapeShape.Unwarp(p);
        if(Mathf.Abs(canonical.x)<(organic?2.6f:3.0f))return false;
        if(Mathf.Abs(Mathf.Abs(canonical.x)-5)<1.28f)return false;
        // Shop and marble access routes remain visibly bare.
        if(Mathf.Abs(p.z-18.5f)<1.3f && p.x>-3 && p.x<20)return false;
        if(Mathf.Abs(p.z-44)<1.5f && p.x>-12 && p.x<3)return false;
        return true;
    }
    private static VillageGroundCoverRenderer.Plant[] PlantMaxtreeGrass(VillageWaterSystem water)
    {
        var plants=new List<VillageGroundCoverRenderer.Plant>();var random=new System.Random(601510);var bins=new HashSet<Vector2Int>();
        bool organic=Find(OrganicRoot);
        var paddies=water.surfaces.Where(s=>!s.canal).ToArray();var rejected=new Dictionary<string,int>();
        Action<Vector3,int,bool> add=(canonical,prototype,paddy)=>
        {
            Vector3 p=VillageLandscapeShape.Warp(canonical);if(!ClearForGrass(p,organic))return;
            var key=new Vector2Int(Mathf.FloorToInt(p.x/.34f),Mathf.FloorToInt(p.z/.34f));if(bins.Contains(key))return;
            if(!Physics.Raycast(p+Vector3.up*3,Vector3.down,out var hit,5,~0,QueryTriggerInteraction.Ignore))
            {string keyName=paddy?"NoHitPaddy":"NoHitDry";if(!rejected.ContainsKey(keyName))rejected[keyName]=0;rejected[keyName]++;return;}
            if(hit.collider.name!="Ground" && !hit.collider.name.StartsWith("BoRuong_"))
            {if(!rejected.ContainsKey(hit.collider.name))rejected[hit.collider.name]=0;rejected[hit.collider.name]++;return;}
            if(!paddy && hit.point.y<-.06f)return;
            p.y=hit.point.y-.009f;bins.Add(key);
            plants.Add(new VillageGroundCoverRenderer.Plant{position=p,yaw=(float)random.NextDouble()*360,scale=.76f+(float)random.NextDouble()*.42f,prototype=prototype,paddy=paddy});
        };
        foreach(var data in paddies)
        {
            var polygon=data.outline;float distance=0;
            for(int i=0;i<polygon.Length;i++)
            {
                var a=polygon[i];var b=polygon[(i+1)%polygon.Length];float length=Vector2.Distance(a,b);distance+=length;
                if(distance<1.25f)continue;distance=0;if(random.NextDouble()<.22)continue;
                var edge=b-a;var outward=new Vector2(edge.y,-edge.x).normalized;var p=(a+b)*.5f+outward*(.24f+(float)random.NextDouble()*.16f);
                add(new Vector3(p.x,0,p.y),random.NextDouble()<.77?0:3,false);
            }
            int target=Mathf.CeilToInt((data.rectangle.z-data.rectangle.x)*(data.rectangle.w-data.rectangle.y)*.065f);
            for(int attempt=0,accepted=0;attempt<target*12 && accepted<target;attempt++)
            {
                Vector2 p=new Vector2(Mathf.Lerp(data.rectangle.x,data.rectangle.z,(float)random.NextDouble()),Mathf.Lerp(data.rectangle.y,data.rectangle.w,(float)random.NextDouble()));
                float inside=ContourDistance(p,polygon);if(inside<.3f || (inside>1.3f && random.NextDouble()>.08))continue;
                int old=plants.Count;add(new Vector3(p.x,0,p.y),random.NextDouble()<.75?2:1,true);if(plants.Count>old)accepted++;
            }
        }
        int dry=0;
        for(int attempt=0;attempt<18000 && dry<850;attempt++)
        {
            var p=new Vector2(Mathf.Lerp(-40,40,(float)random.NextDouble()),Mathf.Lerp(-8,80,(float)random.NextDouble()));
            if(Mathf.PerlinNoise(p.x*.25f+71,p.y*.23f)<.49f || PaddyLowering(p,paddies)>.03f)continue;
            int old=plants.Count;add(new Vector3(p.x,0,p.y),random.NextDouble()<.7?0:random.NextDouble()<.75?1:3,false);if(plants.Count>old)dry++;
        }
        File.WriteAllText(OutputFolder+"/maxtree_placement_debug.txt","Clumps: "+plants.Count+", paddy: "+plants.Count(p=>p.paddy)+"\n"+string.Join("\n",rejected.Select(k=>k.Key+": "+k.Value)));
        return plants.ToArray();
    }

    public static void ValidateMaxtreeVillage()
    {
        if(Find(OrganicRoot)){ValidateOrganicVillage();return;}
        var root=Find(MaxtreeRoot);if(!root)throw new Exception("Maxtree planting is missing.");
        var cover=root.GetComponent<VillageGroundCoverRenderer>();if(!cover || cover.plants.Length<300 || cover.plants.Length>2400)throw new Exception("Groundcover budget or density invalid: component="+(bool)cover+", plants="+(cover?cover.plants.Length:-1));
        if(cover.plants.Any(p=>!ClearForGrass(p.position)))throw new Exception("Grass outside the playable zone or inside the marble clearing.");
        if(cover.plants.Count(p=>p.paddy)<30)throw new Exception("Rice field weeds missing.");
        var shader=Shader.Find("KuTy/Village/Maxtree Matte Wind");
        if(ShaderUtil.GetShaderMessages(shader).Any(m=>m.severity==UnityEditor.Rendering.ShaderCompilerMessageSeverity.Error))throw new Exception("Maxtree shader failed to compile.");
        foreach(var prototype in cover.prototypes)
        {
            if(!prototype.near || !prototype.middle || !prototype.far || prototype.near.subMeshCount!=prototype.materials.Length || prototype.materials.Any(m=>!m || !m.enableInstancing || m.GetFloat("_HasTexture")<.9f))throw new Exception("Groundcover LOD or texture material incomplete: "+prototype.name);
            if(prototype.near.triangles.Length>3600 || prototype.far.triangles.Length>400)throw new Exception("Grass triangle budget exceeded.");
        }
        var ground=Find("Ground").GetComponent<MeshCollider>();var water=Find(VillageName).GetComponentInChildren<VillageWaterSystem>();
        foreach(var data in water.surfaces.Where(s=>!s.canal))
        {
            var center=VillageLandscapeShape.Warp(new Vector3((data.rectangle.x+data.rectangle.z)*.5f,2,(data.rectangle.y+data.rectangle.w)*.5f));
            if(Mathf.Abs(data.height+.12f)>.001f || !ground.Raycast(new Ray(center,Vector3.down),out var hit,4) || hit.point.y>-.18f || data.height-hit.point.y<.06f)throw new Exception("Paddy bed, water and collision height disagree.");
            if(!data.Contains(hit.point))throw new Exception("Lowered paddy no longer triggers ripples at the player's feet.");
        }
        VerifyPaddyWalking();
        var trees=root.GetComponentsInChildren<LODGroup>();if(trees.Length<5)throw new Exception("Replacement Maxtree trees missing.");
        foreach(var tree in trees)if(tree.GetLODs().Length!=3 || !WithinPlayableVegetation(tree.transform.position))throw new Exception("Tree LOD or playable bounds mismatch.");
        File.WriteAllText(OutputFolder+"/maxtree_report.txt","PASS\nPlayable village only: "+cover.plants.Length+" instanced grass clumps, "+cover.plants.Count(p=>p.paddy)+" paddy weeds; 4 grass species.\nTree count: "+trees.Length+" with three LODs; distant scenery left intact.\nGrass LOD 10 / 24 m, culled beyond 48 m; no grass GameObjects, scripts per clump or grass colliders.\nPaddy beds -0.22 m, water -0.12 m, rice shifted down 0.17 m. Real sloping mesh colliders and water feet contact verified.\nOriginal Maxtree albedo + opacity, 1K leaves/grass and 2K bark, mipmapped compressed maps, matte wind shader.\n");
    }
    private static void VerifyPaddyWalking()
    {
        var obj=new GameObject("LoweredPaddyProbe"){hideFlags=HideFlags.HideAndDontSave};var cc=obj.AddComponent<CharacterController>();cc.height=1.6f;cc.radius=.25f;cc.center=Vector3.up*.8f;cc.stepOffset=.3f;cc.slopeLimit=45;
        try
        {
            foreach(int side in new[]{-1,1})
            {
                Vector3 start=VillageLandscapeShape.Warp(new Vector3(side*7.1f,.16f,-7));Vector3 goal=VillageLandscapeShape.Warp(new Vector3(side*10,-.2f,-7));
                cc.enabled=false;obj.transform.position=start;cc.enabled=true;Physics.SyncTransforms();
                for(int step=0;step<100;step++){var delta=goal-obj.transform.position;delta.y=0;cc.Move(Vector3.ClampMagnitude(delta,.07f)+Vector3.down*.03f);}
                float contactY=obj.transform.position.y-cc.skinWidth;
                if(contactY>-.14f || contactY<-.32f)throw new Exception("Cannot enter lowered paddy: "+side+" "+obj.transform.position);
                for(int step=0;step<100;step++){var delta=start-obj.transform.position;delta.y=0;cc.Move(Vector3.ClampMagnitude(delta,.07f)+Vector3.down*.03f);}
                var remaining=start-obj.transform.position;remaining.y=0;if(remaining.magnitude>.35f || obj.transform.position.y<-.08f)throw new Exception("Cannot step back onto field bank.");
            }
        }
        finally {UnityEngine.Object.DestroyImmediate(obj);}
    }
    public static void CaptureMaxtreeVillage()
    {
        string folder=OutputFolder+"/Maxtree";Directory.CreateDirectory(folder);var scene=SceneManager.GetActiveScene();bool dirty=scene.isDirty;
        var views=new[]{new[]{new Vector3(-7.7f,1.65f,10),new Vector3(-16,.15f,15)},new[]{new Vector3(-8,1.7f,39),new Vector3(-18,3,47)},new[]{new Vector3(8,1.65f,3),new Vector3(15,-.03f,8)},new[]{new Vector3(62,69,-25),new Vector3(0,0,35)}};
        for(int i=0;i<views.Length;i++)
        {
            var obj=new GameObject("MaxtreePreview"){hideFlags=HideFlags.HideAndDontSave};var camera=obj.AddComponent<Camera>();camera.enabled=false;
            camera.transform.SetPositionAndRotation(views[i][0],Quaternion.LookRotation(views[i][1]-views[i][0]));camera.fieldOfView=i==3?58:66;camera.nearClipPlane=.03f;camera.farClipPlane=400;
            camera.GetUniversalAdditionalCameraData().renderPostProcessing=false;var rt=new RenderTexture(1400,900,24);rt.Create();var previous=RenderTexture.active;var image=new Texture2D(1400,900,TextureFormat.RGB24,false);
            try {RenderPipeline.SubmitRenderRequest(camera,new UniversalRenderPipeline.SingleCameraRequest{destination=rt});RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,1400,900),0,0);image.Apply();File.WriteAllBytes(folder+"/"+(i+1).ToString("00")+".png",image.EncodeToPNG());
                if(i==2){var cover=Find(MaxtreeRoot).GetComponent<VillageGroundCoverRenderer>();File.AppendAllText(OutputFolder+"/maxtree_report.txt","Paddy view: "+cover.LastVisiblePlants+" visible grass clumps in "+cover.LastDrawCalls+" grass draw calls.\n");}}
            finally {RenderTexture.active=previous;UnityEngine.Object.DestroyImmediate(image);rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(obj);}
        }
        if(!dirty)EditorSceneManager.SaveScene(scene);
    }
}
