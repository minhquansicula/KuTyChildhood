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
    private const string TencentModel="Assets/_Project/Art/Models/Scene3/TencentHouse01/NhaQue_Tencent_01.fbx";
    private const string TencentTextures="Assets/_Project/Art/Textures/Scene3/TencentHouse01";
    private const string TencentPrefab="Assets/_Project/Prefabs/Scene3/TencentHouse01/NhaQue_Tencent_01.prefab";
    [MenuItem("KuTy/Scene 3/Install downloaded Tencent house 01")]
    public static void InstallTencentHouse()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("Install house requires Edit Mode.");
        var scene=SceneManager.GetActiveScene();
        if(scene.path!=ScenePath) throw new Exception("Open scene 3 before installing the house.");
        var site=Find(VillageName).transform.Find("NhaDan_VaSanVuon/SanNha_Gon_01");
        var existing=site.Find(HomeNames[0]);
        if(!existing) throw new Exception("First village home is missing.");
        Directory.CreateDirectory(OutputFolder);
        var backup=OutputFolder+(existing.GetComponent<LODGroup>()?"/before-tencent-house01-wall-repair.unity":"/before-tencent-house01.unity");
        if(!File.Exists(backup) && !EditorSceneManager.SaveScene(scene,backup,true)) throw new Exception("Cannot back up scene.");
        var gameplay=GameplaySnapshot();
        AssetDatabase.ImportAsset(TencentModel,ImportAssetOptions.ForceSynchronousImport);
        var importer=(ModelImporter)AssetImporter.GetAtPath(TencentModel);
        if(importer==null) throw new Exception("Downloaded house FBX is not ready.");
        importer.importAnimation=false;importer.materialImportMode=ModelImporterMaterialImportMode.None;
        importer.globalScale=1;importer.isReadable=false;importer.addCollider=false;
        importer.importNormals=ModelImporterNormals.Import;importer.importTangents=ModelImporterTangents.CalculateMikk;
        importer.SaveAndReimport();
        var repaired=File.Exists(TencentTextures+"/Nha01_LOD2_MetallicSmoothness.png");
        var materials=Enumerable.Range(0,3).Select(i=>CreateTencentHouseMaterial(repaired?i:-1)).ToArray();
        var model=AssetDatabase.LoadAssetAtPath<GameObject>(TencentModel);
        if(!model) throw new Exception("House FBX did not import.");
        EnsureFolder("Assets/_Project/Prefabs/Scene3/TencentHouse01");
        var temporary=(GameObject)PrefabUtility.InstantiatePrefab(model);
        GameObject prefab;
        try
        {
            temporary.name="NhaQue_Tencent_01";
            var renderers=temporary.GetComponentsInChildren<MeshRenderer>();
            var lods=Enumerable.Range(0,3).Select(i=>renderers.Single(r=>r.name=="Nha01_LOD"+i)).ToArray();
            var counts=lods.Select(r=>r.GetComponent<MeshFilter>().sharedMesh.GetIndexCount(0)/3).ToArray();
            if(counts[0]>32000 || counts[1]>13000 || counts[2]>3500) throw new Exception("House mesh reduction did not meet budget.");
            for(var i=0;i<lods.Length;i++) {lods[i].sharedMaterial=materials[i];lods[i].shadowCastingMode=ShadowCastingMode.On;lods[i].receiveShadows=true;}
            // The collider uses the medium mesh and stays active when only renderers change LOD.
            var collider=lods[1].gameObject.AddComponent<MeshCollider>();collider.sharedMesh=lods[1].GetComponent<MeshFilter>().sharedMesh;
            var group=temporary.GetComponent<LODGroup>() ?? temporary.AddComponent<LODGroup>();group.fadeMode=LODFadeMode.None;
            group.SetLODs(new[]{new LOD(.22f,new Renderer[]{lods[0]}),new LOD(.085f,new Renderer[]{lods[1]}),new LOD(.003f,new Renderer[]{lods[2]})});
            group.RecalculateBounds();
            var bounds=RendererBounds(temporary.transform);
            if(bounds.size.x<6 || bounds.size.x>8.5f || bounds.size.y<3 || bounds.size.y>6) throw new Exception("Unexpected FBX dimensions: "+bounds.size);
            prefab=PrefabUtility.SaveAsPrefabAsset(temporary,TencentPrefab);
            if(!prefab) throw new Exception("Failed to save house prefab.");
        }
        finally {UnityEngine.Object.DestroyImmediate(temporary);}
        Undo.IncrementCurrentGroup();var undo=Undo.GetCurrentGroup();Undo.SetCurrentGroupName("Replace first home with downloaded model");
        Undo.RegisterFullObjectHierarchyUndo(site.gameObject,"Replace first home");
        try
        {
            if(existing.GetComponent<LODGroup>()) Undo.DestroyObjectImmediate(existing.gameObject);
            else {Undo.RecordObject(existing.gameObject,"Keep previous house disabled");existing.name=HomeNames[0]+"_Cu_DaTat";existing.gameObject.SetActive(false);}
            var oldCollision=site.Find("VaChamNha");
            if(oldCollision) {Undo.RecordObject(oldCollision.gameObject,"Use downloaded house collider");oldCollision.gameObject.SetActive(false);}
            var house=(GameObject)PrefabUtility.InstantiatePrefab(prefab,scene);
            Undo.RegisterCreatedObjectUndo(house,"Downloaded Vietnamese home");
            house.transform.SetParent(site,false);house.name=HomeNames[0];
            house.transform.SetPositionAndRotation(new Vector3(-18,.1f,4),Quaternion.Euler(0,90,0));house.transform.localScale=Vector3.one;
            var bounds=RendererBounds(house.transform);
            house.transform.position+=Vector3.up*(.1f-bounds.min.y);
            bounds=RendererBounds(house.transform);
            if(bounds.min.x<-26 || bounds.max.x>-10 || bounds.min.z<-3 || bounds.max.z>11) throw new Exception("New house extends beyond its existing yard.");
            VerifyGameplay(gameplay);Physics.SyncTransforms();ValidateScene();
            AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);
            if(!EditorSceneManager.SaveScene(scene)) throw new Exception("Failed to save house in scene.");
            Undo.CollapseUndoOperations(undo);Inspect(true);Capture();CaptureTencentHouse(house.transform);if(repaired) CaptureRepairedHouseWalls(house.transform);
            var counts=house.GetComponent<LODGroup>().GetLODs().Select(l=>l.renderers[0].GetComponent<MeshFilter>().sharedMesh.GetIndexCount(0)/3).ToArray();
            File.WriteAllText(OutputFolder+"/tencent_house_report.txt","PASS\nReplaced first house at (-18, 0.1, 4), facing the main road.\nLOD triangles: "+string.Join(" / ",counts)+"\nHouse dimensions: "+bounds.size+"\n"+(repaired?"Corrected UVs, planar plaster normals, individual baked URP/Lit atlases: 2048 / 1024 / 512.\n":"Shared URP/Lit PBR material, imported textures limited to 2048.\n")+"Medium-mesh static collider, original gameplay components preserved: "+gameplay.Count+"\nVillage layout and walking validation passed.\nOriginal house kept disabled; original downloaded OBJ unchanged.\n");
            SceneView.RepaintAll();
        }
        catch {Undo.RevertAllDownToGroup(undo);throw;}
    }
    private static Texture2D ImportHouseTexture(string filename,bool color,bool normal=false,int resolution=2048,string folder=null)
    {
        var path=(folder??TencentTextures)+"/"+filename;AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
        var importer=(TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType=normal?TextureImporterType.NormalMap:TextureImporterType.Default;
        importer.convertToNormalmap=false;importer.sRGBTexture=color;importer.maxTextureSize=resolution;
        importer.mipmapEnabled=true;importer.wrapMode=TextureWrapMode.Repeat;importer.filterMode=FilterMode.Trilinear;importer.anisoLevel=4;
        importer.textureCompression=TextureImporterCompression.Compressed;importer.alphaSource=TextureImporterAlphaSource.FromInput;
        importer.SaveAndReimport();return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }
    private static Material CreateTencentHouseMaterial(int lod=-1)
    {
        var prefix=lod<0?"Nha01":"Nha01_LOD"+lod;
        var resolution=lod<0?2048:new[]{2048,1024,512}[lod];
        var color=ImportHouseTexture(prefix+"_BaseColor.png",true,false,resolution);
        var normal=ImportHouseTexture(prefix+"_Normal.png",false,true,resolution);
        var packed=ImportHouseTexture(prefix+"_MetallicSmoothness.png",false,false,resolution);
        if(lod<0) {ImportHouseTexture("Nha01_Metallic.png",false);ImportHouseTexture("Nha01_Roughness.png",false);}
        EnsureFolder("Assets/_Project/Art/Materials/Scene3/TencentHouse01");
        var path="Assets/_Project/Art/Materials/Scene3/TencentHouse01/"+prefix+"_Tencent_PBR.mat";
        var material=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(!material) {material=new Material(Shader.Find("Universal Render Pipeline/Lit")){name=prefix+"_Tencent_PBR"};AssetDatabase.CreateAsset(material,path);}
        material.SetTexture("_BaseMap",color);material.SetColor("_BaseColor",Color.white);
        material.SetTexture("_BumpMap",normal);material.SetFloat("_BumpScale",lod<0?.65f:1);material.EnableKeyword("_NORMALMAP");
        material.SetTexture("_MetallicGlossMap",packed);material.EnableKeyword("_METALLICSPECGLOSSMAP");
        material.SetFloat("_Smoothness",.6f);material.SetFloat("_SmoothnessTextureChannel",0);material.SetFloat("_WorkflowMode",1);
        material.SetFloat("_Surface",0);material.SetFloat("_ReceiveShadows",1);material.enableInstancing=true;
        EditorUtility.SetDirty(material);return material;
    }
    private static void CaptureRepairedHouseWalls(Transform house)
    {
        var group=house.GetComponent<LODGroup>();var folder="Tools/Scene3/TencentHouse01/Repair/Unity";Directory.CreateDirectory(folder);
        var obj=new GameObject("WallRepairPreview"){hideFlags=HideFlags.HideAndDontSave};var camera=obj.AddComponent<Camera>();camera.enabled=false;
        var target=RendererBounds(house).center;
        camera.transform.position=target+new Vector3(-1,.4f,-10);camera.transform.LookAt(target);
        camera.orthographic=true;camera.orthographicSize=4.1f;camera.nearClipPlane=.1f;camera.farClipPlane=180;
        camera.GetUniversalAdditionalCameraData().renderPostProcessing=false;
        var rt=new RenderTexture(1200,1000,24,RenderTextureFormat.ARGB32);rt.Create();var previous=RenderTexture.active;var image=new Texture2D(1200,1000,TextureFormat.RGB24,false);
        try
        {
            for(var angle=0;angle<4;angle++)
            {
                camera.transform.position=target+new Vector3(angle==3?-1:(angle-1)*3,.4f,angle==3?10:-10);camera.transform.LookAt(target);
                for(var lod=0;lod<3;lod++)
                {
                    group.ForceLOD(lod);RenderPipeline.SubmitRenderRequest(camera,new UniversalRenderPipeline.SingleCameraRequest{destination=rt});
                    RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,1200,1000),0,0);image.Apply();File.WriteAllBytes(folder+"/Wall_LOD"+lod+(angle==1?"":"_Angle"+angle)+".png",image.EncodeToPNG());
                }
            }
        }
        finally {group.ForceLOD(-1);RenderTexture.active=previous;UnityEngine.Object.DestroyImmediate(image);rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(obj);}
    }
    private static void CaptureTencentHouse(Transform house)
    {
        var obj=new GameObject("TencentHousePreview"){hideFlags=HideFlags.HideAndDontSave};var camera=obj.AddComponent<Camera>();camera.enabled=false;
        var target=RendererBounds(house).center;
        camera.transform.SetPositionAndRotation(target+new Vector3(10,2.6f,-10),Quaternion.LookRotation(new Vector3(-10,-2.6f,10)));
        camera.fieldOfView=55;camera.nearClipPlane=.1f;camera.farClipPlane=180;
        camera.GetUniversalAdditionalCameraData().renderPostProcessing=false;
        var rt=new RenderTexture(1400,1000,24,RenderTextureFormat.ARGB32);rt.Create();var previous=RenderTexture.active;var image=new Texture2D(1400,1000,TextureFormat.RGB24,false);
        try {RenderPipeline.SubmitRenderRequest(camera,new UniversalRenderPipeline.SingleCameraRequest{destination=rt});RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,1400,1000),0,0);image.Apply();File.WriteAllBytes(OutputFolder+"/After/05_NhaTencent01.png",image.EncodeToPNG());}
        finally {RenderTexture.active=previous;UnityEngine.Object.DestroyImmediate(image);rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(obj);}
    }
}
