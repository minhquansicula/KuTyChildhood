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
    private const string House05Textures="Assets/_Project/Art/Textures/Scene3/TencentHouse05";
    private const string House05Model="Assets/_Project/Art/Models/Scene3/TencentHouse05/NhaQue_Tencent_05.fbx";
    private const string House05Prefab="Assets/_Project/Prefabs/Scene3/TencentHouse05/NhaQue_Tencent_05.prefab";

    [MenuItem("KuTy/Scene 3/Install rural house 05")]
    public static void InstallTencentHouse05()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("Install house 05 in Edit Mode.");
        var scene=SceneManager.GetActiveScene();if(scene.path!=ScenePath) throw new Exception("Expected village scene.");
        var village=Find(VillageName);if(!village) throw new Exception("Village missing.");
        var site=village.transform.Find("NhaDan_VaSanVuon/SanNha_Gon_05");
        var existing=site?site.Find(HomeNames[4]):null;if(!existing) throw new Exception("Existing fifth home missing.");
        Directory.CreateDirectory(OutputFolder);var backup=OutputFolder+"/before-tencent-house05.unity";
        if(!File.Exists(backup) && !EditorSceneManager.SaveScene(scene,backup,true)) throw new Exception("Cannot back up scene.");
        var gameplay=GameplaySnapshot();
        var materials=Enumerable.Range(0,3).Select(CreateHouse05Material).ToArray();
        AssetDatabase.ImportAsset(House05Model,ImportAssetOptions.ForceSynchronousImport);
        var importer=(ModelImporter)AssetImporter.GetAtPath(House05Model);if(importer==null) throw new Exception("House 05 FBX missing.");
        importer.importAnimation=false;importer.materialImportMode=ModelImporterMaterialImportMode.None;importer.globalScale=1;
        importer.isReadable=false;importer.addCollider=false;importer.importNormals=ModelImporterNormals.Import;
        importer.importTangents=ModelImporterTangents.CalculateMikk;importer.SaveAndReimport();
        var model=AssetDatabase.LoadAssetAtPath<GameObject>(House05Model);if(!model) throw new Exception("House 05 not imported.");
        EnsureFolder("Assets/_Project/Prefabs/Scene3/TencentHouse05");
        var temporary=(GameObject)PrefabUtility.InstantiatePrefab(model);GameObject prefab;
        try
        {
            temporary.name="NhaQue_Tencent_05";
            var renderers=temporary.GetComponentsInChildren<MeshRenderer>();
            var lods=Enumerable.Range(0,3).Select(i=>renderers.Single(r=>r.name=="Nha05_LOD"+i)).ToArray();
            var counts=lods.Select(r=>r.GetComponent<MeshFilter>().sharedMesh.GetIndexCount(0)/3).ToArray();
            if(counts[0]>90000 || counts[1]>36000 || counts[2]>12000) throw new Exception("House 05 exceeds validated LOD budgets.");
            for(var i=0;i<3;i++) {lods[i].sharedMaterial=materials[i];lods[i].shadowCastingMode=ShadowCastingMode.On;lods[i].receiveShadows=true;}
            var collider=lods[2].gameObject.AddComponent<MeshCollider>();collider.sharedMesh=lods[2].GetComponent<MeshFilter>().sharedMesh;
            var group=temporary.GetComponent<LODGroup>() ?? temporary.AddComponent<LODGroup>();group.fadeMode=LODFadeMode.None;
            group.SetLODs(new[]{new LOD(.24f,new Renderer[]{lods[0]}),new LOD(.09f,new Renderer[]{lods[1]}),new LOD(.003f,new Renderer[]{lods[2]})});
            group.RecalculateBounds();prefab=PrefabUtility.SaveAsPrefabAsset(temporary,House05Prefab);
            if(!prefab) throw new Exception("Cannot save house 05 prefab.");
        }
        finally {UnityEngine.Object.DestroyImmediate(temporary);}
        Undo.IncrementCurrentGroup();var undo=Undo.GetCurrentGroup();Undo.SetCurrentGroupName("Install rural house 05");
        Undo.RegisterFullObjectHierarchyUndo(site.gameObject,"Install rural house 05");
        try
        {
            if(existing.GetComponent<LODGroup>()) Undo.DestroyObjectImmediate(existing.gameObject);
            else {existing.name=HomeNames[4]+"_Cu_DaTat";existing.gameObject.SetActive(false);}
            var oldCollision=site.Find("VaChamNha");if(oldCollision) oldCollision.gameObject.SetActive(false);
            var house=(GameObject)PrefabUtility.InstantiatePrefab(prefab,scene);Undo.RegisterCreatedObjectUndo(house,"Rural house 05");
            house.transform.SetParent(site,false);house.name=HomeNames[4];
            house.transform.SetPositionAndRotation(new Vector3(-23,.1f,65),Quaternion.Euler(0,90,0));house.transform.localScale=Vector3.one*.8f;
            house.transform.position+=Vector3.up*(.1f-RendererBounds(house.transform).min.y);
            var bounds=RendererBounds(house.transform);
            if(bounds.min.x<-31 || bounds.max.x>-15 || bounds.min.z<58 || bounds.max.z>72) throw new Exception("House 05 exceeds the dry yard: "+bounds);
            VerifyGameplay(gameplay);Physics.SyncTransforms();VerifyHouse05Collision(house.transform);ValidateScene();AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(scene);if(!EditorSceneManager.SaveScene(scene)) throw new Exception("Cannot save house 05 scene.");
            Undo.CollapseUndoOperations(undo);Inspect(true);Capture();CaptureHouse05(house.transform);
            var counts=house.GetComponent<LODGroup>().GetLODs().Select(l=>l.renderers[0].GetComponent<MeshFilter>().sharedMesh.GetIndexCount(0)/3);
            File.WriteAllText(OutputFolder+"/tencent_house05_report.txt","PASS\nFifth house at (-23, 0.1, 65), facade towards the road.\nLOD triangles: "+string.Join(" / ",counts)+
                "\nHouse dimensions: "+bounds.size+"\nUniform scale only; independently baked 2048 / 1024 / 512 PBR atlases.\nLow-LOD static mesh collider; four walls/roof raycasts and LOD collision persistence verified. Front approach walkable.\nOriginal gameplay components preserved: "+gameplay.Count+
                "\nVillage layout and walking tests passed; bank grass remains removed.\nOriginal weathered colour preserved; roughness floor baked into PBR maps. Mid/far atlases reprojected from clean near mesh, with normal maps disabled to avoid projection artifacts. Original source OBJ unchanged; see geometry_report.json for sampled surface errors.\n");
            SceneView.RepaintAll();
        }
        catch {Undo.RevertAllDownToGroup(undo);throw;}
    }

    private static void VerifyHouse05Collision(Transform house)
    {
        var group=house.GetComponent<LODGroup>();
        var collider=house.GetComponentInChildren<MeshCollider>();
        if(!collider || collider.convex || collider.isTrigger || collider.GetComponent<Rigidbody>())
            throw new Exception("House 05 requires a static solid mesh collider.");
        var bounds=RendererBounds(house);var center=bounds.center;center.y=bounds.min.y+1;
        var rays=new[]{new Ray(new Vector3(bounds.max.x+2,center.y,center.z),Vector3.left),
            new Ray(new Vector3(bounds.min.x-2,center.y,center.z),Vector3.right),
            new Ray(new Vector3(center.x,center.y,bounds.max.z+2),Vector3.back),
            new Ray(new Vector3(center.x,center.y,bounds.min.z-2),Vector3.forward),
            new Ray(new Vector3(center.x,bounds.max.y+2,center.z),Vector3.down)};
        try
        {
            group.ForceLOD(0);Physics.SyncTransforms();
            if(!collider.enabled || !collider.gameObject.activeInHierarchy) throw new Exception("House collider disabled with near LOD.");
            for(var i=0;i<rays.Length;i++)
                if(!collider.Raycast(rays[i],out var hit,20)) throw new Exception("House 05 collision ray missed wall/roof: "+i);
            group.ForceLOD(2);Physics.SyncTransforms();
            if(!collider.enabled || !collider.gameObject.activeInHierarchy) throw new Exception("House collider disabled with far LOD.");
            var approach=new Vector3(bounds.max.x+.8f,2,center.z);
            var ground=Physics.RaycastAll(approach,Vector3.down,3,~0,QueryTriggerInteraction.Ignore)
                .Where(h=>!h.rigidbody && !h.collider.GetComponent<CharacterController>()).OrderBy(h=>h.distance).ToArray();
            if(ground.Length==0 || ground[0].point.y>.5f || ground[0].point.y<-.1f)
                throw new Exception("House 05 front approach is not walkable.");
        }
        finally {group.ForceLOD(-1);}
    }

    private static Material CreateHouse05Material(int lod)
    {
        var prefix="Nha05_LOD"+lod;var resolution=new[]{2048,1024,512}[lod];
        var color=ImportHouseTexture(prefix+"_BaseColor.png",true,false,resolution,House05Textures);
        var normal=ImportHouseTexture(prefix+"_Normal.png",false,true,resolution,House05Textures);
        var packed=ImportHouseTexture(prefix+"_MetallicSmoothness.png",false,false,resolution,House05Textures);
        EnsureFolder("Assets/_Project/Art/Materials/Scene3/TencentHouse05");
        var path="Assets/_Project/Art/Materials/Scene3/TencentHouse05/"+prefix+"_PBR.mat";
        var material=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(!material) {material=new Material(Shader.Find("Universal Render Pipeline/Lit")){name=prefix+"_PBR"};AssetDatabase.CreateAsset(material,path);}
        material.SetTexture("_BaseMap",color);material.SetColor("_BaseColor",Color.white);
        material.SetTexture("_BumpMap",normal);material.SetFloat("_BumpScale",1);material.EnableKeyword("_NORMALMAP");
        material.SetTexture("_MetallicGlossMap",packed);material.EnableKeyword("_METALLICSPECGLOSSMAP");
        material.SetFloat("_Smoothness",.6f);material.SetFloat("_SmoothnessTextureChannel",0);material.SetFloat("_WorkflowMode",1);
        material.SetFloat("_Surface",0);material.SetFloat("_ReceiveShadows",1);material.enableInstancing=true;
        // Geometry normals avoid projection artifacts once the house occupies under 24% of the screen.
        if(lod>=1) {material.SetTexture("_BumpMap",null);material.DisableKeyword("_NORMALMAP");}
        EditorUtility.SetDirty(material);return material;
    }

    private static void CaptureHouse05(Transform house)
    {
        var folder="Tools/Scene3/TencentHouse05/Unity";Directory.CreateDirectory(folder);
        var obj=new GameObject("House05Preview"){hideFlags=HideFlags.HideAndDontSave};var camera=obj.AddComponent<Camera>();camera.enabled=false;
        var target=RendererBounds(house).center;camera.fieldOfView=55;camera.nearClipPlane=.1f;camera.farClipPlane=180;
        camera.GetUniversalAdditionalCameraData().renderPostProcessing=false;
        var group=house.GetComponent<LODGroup>();var rt=new RenderTexture(1400,1000,24,RenderTextureFormat.ARGB32);rt.Create();
        var previous=RenderTexture.active;var image=new Texture2D(1400,1000,TextureFormat.RGB24,false);
        try
        {
            var offsets=new[]{new Vector3(-10,3,-10),new Vector3(10,3,10),new Vector3(-10,1,0),new Vector3(0,1,-10)};
            for(var view=0;view<offsets.Length;view++)
            {
                camera.transform.position=target+offsets[view];camera.transform.LookAt(target);
                for(var lod=0;lod<3;lod++)
                {
                    group.ForceLOD(lod);RenderPipeline.SubmitRenderRequest(camera,new UniversalRenderPipeline.SingleCameraRequest{destination=rt});
                    RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,1400,1000),0,0);image.Apply();
                    File.WriteAllBytes(folder+"/LOD"+lod+"_View"+view+".png",image.EncodeToPNG());
                }
            }
        }
        finally {group.ForceLOD(-1);RenderTexture.active=previous;UnityEngine.Object.DestroyImmediate(image);rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(obj);}
    }
}
