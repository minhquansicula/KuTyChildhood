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
    private const string House02Textures="Assets/_Project/Art/Textures/Scene3/TencentHouse02";
    private const string House02Model="Assets/_Project/Art/Models/Scene3/TencentHouse02/NhaQue_Tencent_02.fbx";
    private const string House02Prefab="Assets/_Project/Prefabs/Scene3/TencentHouse02/NhaQue_Tencent_02.prefab";

    [MenuItem("KuTy/Scene 3/Install downloaded L house 02")]
    public static void InstallTencentHouse02()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("Install house 02 in Edit Mode.");
        var scene=SceneManager.GetActiveScene();if(scene.path!=ScenePath) throw new Exception("Expected village scene.");
        var village=Find(VillageName);if(!village) throw new Exception("Village missing.");
        var site=village.transform.Find("NhaDan_VaSanVuon/SanNha_Gon_02");
        var existing=site?site.Find(HomeNames[1]):null;if(!existing) throw new Exception("Existing second home missing.");
        Directory.CreateDirectory(OutputFolder);var backup=OutputFolder+"/before-tencent-house02.unity";
        if(!File.Exists(backup) && !EditorSceneManager.SaveScene(scene,backup,true)) throw new Exception("Cannot back up scene.");
        var gameplay=GameplaySnapshot();
        AssetDatabase.ImportAsset(House02Model,ImportAssetOptions.ForceSynchronousImport);
        var importer=(ModelImporter)AssetImporter.GetAtPath(House02Model);if(importer==null) throw new Exception("House 02 FBX missing.");
        importer.importAnimation=false;importer.materialImportMode=ModelImporterMaterialImportMode.None;importer.globalScale=1;
        importer.isReadable=false;importer.addCollider=false;importer.importNormals=ModelImporterNormals.Import;
        importer.importTangents=ModelImporterTangents.CalculateMikk;importer.SaveAndReimport();
        var materials=Enumerable.Range(0,3).Select(CreateHouse02Material).ToArray();
        var model=AssetDatabase.LoadAssetAtPath<GameObject>(House02Model);if(!model) throw new Exception("House 02 not imported.");
        EnsureFolder("Assets/_Project/Prefabs/Scene3/TencentHouse02");
        var temporary=(GameObject)PrefabUtility.InstantiatePrefab(model);GameObject prefab;
        try
        {
            temporary.name="NhaQue_Tencent_02";
            var renderers=temporary.GetComponentsInChildren<MeshRenderer>();
            var lods=Enumerable.Range(0,3).Select(i=>renderers.Single(r=>r.name=="Nha02_LOD"+i)).ToArray();
            var counts=lods.Select(r=>r.GetComponent<MeshFilter>().sharedMesh.GetIndexCount(0)/3).ToArray();
            if(counts[0]>90000 || counts[1]>36000 || counts[2]>12000) throw new Exception("House 02 exceeds validated LOD budgets.");
            for(var i=0;i<3;i++) {lods[i].sharedMaterial=materials[i];lods[i].shadowCastingMode=ShadowCastingMode.On;lods[i].receiveShadows=true;}
            var collider=lods[2].gameObject.AddComponent<MeshCollider>();collider.sharedMesh=lods[2].GetComponent<MeshFilter>().sharedMesh;
            var group=temporary.GetComponent<LODGroup>() ?? temporary.AddComponent<LODGroup>();group.fadeMode=LODFadeMode.None;
            group.SetLODs(new[]{new LOD(.24f,new Renderer[]{lods[0]}),new LOD(.09f,new Renderer[]{lods[1]}),new LOD(.003f,new Renderer[]{lods[2]})});
            group.RecalculateBounds();prefab=PrefabUtility.SaveAsPrefabAsset(temporary,House02Prefab);
            if(!prefab) throw new Exception("Cannot save house 02 prefab.");
        }
        finally {UnityEngine.Object.DestroyImmediate(temporary);}
        Undo.IncrementCurrentGroup();var undo=Undo.GetCurrentGroup();Undo.SetCurrentGroupName("Install L house 02");
        Undo.RegisterFullObjectHierarchyUndo(site.gameObject,"Install L house 02");
        try
        {
            if(existing.GetComponent<LODGroup>()) Undo.DestroyObjectImmediate(existing.gameObject);
            else {existing.name=HomeNames[1]+"_Cu_DaTat";existing.gameObject.SetActive(false);}
            var oldCollision=site.Find("VaChamNha");if(oldCollision) oldCollision.gameObject.SetActive(false);
            var house=(GameObject)PrefabUtility.InstantiatePrefab(prefab,scene);Undo.RegisterCreatedObjectUndo(house,"Downloaded L house");
            house.transform.SetParent(site,false);house.name=HomeNames[1];
            house.transform.SetPositionAndRotation(new Vector3(18,.1f,44),Quaternion.Euler(0,270,0));house.transform.localScale=Vector3.one;
            house.transform.position+=Vector3.up*(.1f-RendererBounds(house.transform).min.y);
            var bounds=RendererBounds(house.transform);
            if(bounds.min.x<10 || bounds.max.x>26 || bounds.min.z<37 || bounds.max.z>51) throw new Exception("House 02 exceeds the dry yard: "+bounds);
            VerifyGameplay(gameplay);Physics.SyncTransforms();ValidateScene();AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(scene);if(!EditorSceneManager.SaveScene(scene)) throw new Exception("Cannot save house 02 scene.");
            Undo.CollapseUndoOperations(undo);Inspect(true);Capture();CaptureHouse02(house.transform);
            var counts=house.GetComponent<LODGroup>().GetLODs().Select(l=>l.renderers[0].GetComponent<MeshFilter>().sharedMesh.GetIndexCount(0)/3);
            File.WriteAllText(OutputFolder+"/tencent_house02_report.txt","PASS\nSecond house at (18, 0.1, 44), facade towards the road.\nLOD triangles: "+string.Join(" / ",counts)+
                "\nHouse dimensions: "+bounds.size+"\nUniform scale only; independently baked 2048 / 1024 / 512 PBR atlases.\nLow-LOD static mesh collider.\nOriginal gameplay components preserved: "+gameplay.Count+
                "\nVillage layout and walking tests passed; bank grass remains removed.\nOriginal source OBJ unchanged; see geometry_report.json for sampled surface errors.\n");
            SceneView.RepaintAll();
        }
        catch {Undo.RevertAllDownToGroup(undo);throw;}
    }

    private static Material CreateHouse02Material(int lod)
    {
        var prefix="Nha02_LOD"+lod;var resolution=new[]{2048,1024,512}[lod];
        var color=ImportHouseTexture(prefix+"_BaseColor.png",true,false,resolution,House02Textures);
        var normal=ImportHouseTexture(prefix+"_Normal.png",false,true,resolution,House02Textures);
        var packed=ImportHouseTexture(prefix+"_MetallicSmoothness.png",false,false,resolution,House02Textures);
        EnsureFolder("Assets/_Project/Art/Materials/Scene3/TencentHouse02");
        var path="Assets/_Project/Art/Materials/Scene3/TencentHouse02/"+prefix+"_PBR.mat";
        var material=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(!material) {material=new Material(Shader.Find("Universal Render Pipeline/Lit")){name=prefix+"_PBR"};AssetDatabase.CreateAsset(material,path);}
        material.SetTexture("_BaseMap",color);material.SetColor("_BaseColor",Color.white);
        material.SetTexture("_BumpMap",normal);material.SetFloat("_BumpScale",1);material.EnableKeyword("_NORMALMAP");
        material.SetTexture("_MetallicGlossMap",packed);material.EnableKeyword("_METALLICSPECGLOSSMAP");
        material.SetFloat("_Smoothness",.6f);material.SetFloat("_SmoothnessTextureChannel",0);material.SetFloat("_WorkflowMode",1);
        material.SetFloat("_Surface",0);material.SetFloat("_ReceiveShadows",1);material.enableInstancing=true;
        EditorUtility.SetDirty(material);return material;
    }

    private static void CaptureHouse02(Transform house)
    {
        var folder="Tools/Scene3/TencentHouse02/Unity";Directory.CreateDirectory(folder);
        var obj=new GameObject("House02Preview"){hideFlags=HideFlags.HideAndDontSave};var camera=obj.AddComponent<Camera>();camera.enabled=false;
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
