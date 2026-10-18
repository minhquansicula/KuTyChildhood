using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public static partial class Scene3VillageComposition
{
    private const string BananaSource="Assets/_Project/Art/Materials/Scene3/buichuoi.fbx";
    private const string BananaAssets="Assets/_Project/Art/Models/Scene3/BananaVillage";
    private const string BananaTextures="Assets/_Project/Art/Textures/Scene3/BananaVillage";
    private const string BananaOutput=OutputFolder+"/Bananas";
    private const string BananaEndGroup="BuiChuoi_ChiCuoiLang";

    [MenuItem("KuTy/Scene 3/Place banana grove at village end")]
    public static void InstallVillageEndBananas()
    {
        var scene=SceneManager.GetActiveScene();if(scene.path!=ScenePath || EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Open the outside village in Edit Mode.");
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(BananaSource);if(!prefab)throw new Exception("Banana model has not imported: "+BananaSource);
        Directory.CreateDirectory(BananaOutput);EnsureFolder(BananaAssets);EnsureFolder(BananaTextures);
        string backup=BananaOutput+"/before-larger-banana-grove.unity";if(!File.Exists(backup) && !EditorSceneManager.SaveScene(scene,backup,true))throw new Exception("Cannot back up scene.");
        var before=GameplaySnapshot();var report=new StringBuilder();var material=VillageBananaMaterial();
        var village=Find(VillageName);var group=village.transform.Find(BananaEndGroup);if(!group)group=Group(BananaEndGroup,village.transform).transform;
        Undo.RegisterFullObjectHierarchyUndo(group.gameObject,"Larger banana grove only at village end");
        // Retire any old placeholder or loose instance of this model. All active
        // banana instances belong to the one explicit, bounded end-of-village group.
        foreach(var t in AllTransforms().Where(t=>t.gameObject.activeInHierarchy && !t.IsChildOf(group) &&
            (t.name=="BuiChuoiBenVuon" || PrefabUtility.GetCorrespondingObjectFromSource(t.gameObject)==prefab)).ToArray())
        {Undo.RecordObject(t.gameObject,"Consolidate banana placement at village end");t.gameObject.SetActive(false);report.AppendLine("Retired loose banana: "+HierarchyPath(t));}
        // Two loose, overlapping pockets form a grove, wholly behind the last
        // houses. The road stays to the east and the bamboo boundary stays intact.
        Vector2[] centers={new Vector2(-24.8f,65.8f),new Vector2(-21.6f,69.1f),new Vector2(-16.7f,65.2f),new Vector2(-24.9f,69.8f),new Vector2(-20.5f,65.1f),new Vector2(-17.5f,69.8f),new Vector2(-14.8f,68.3f)};
        float[] heights={4.25f,4.6f,4.4f,3.9f,4.1f,4.25f,3.8f},yaw={24,151,287,74,215,338,126};
        for(int i=0;i<centers.Length;i++)
        {
            string name="BuiChuoi_CuoiLang_"+(i+1);var holder=group.Find(name);if(!holder)holder=Group(name,group).transform;
            if(holder.childCount==0)
            {
                var instance=(GameObject)PrefabUtility.InstantiatePrefab(prefab,holder);Undo.RegisterCreatedObjectUndo(instance,"Use imported banana model");
                instance.transform.localPosition=Vector3.zero;instance.transform.localRotation=Quaternion.identity;instance.transform.localScale=prefab.transform.localScale;
            }
            foreach(var renderer in holder.GetComponentsInChildren<Renderer>()){renderer.sharedMaterials=Enumerable.Repeat(material,renderer.sharedMaterials.Length).ToArray();renderer.receiveShadows=true;renderer.shadowCastingMode=ShadowCastingMode.Off;}
            foreach(var a in holder.GetComponentsInChildren<Animator>())if(!a.runtimeAnimatorController)a.enabled=false;
            foreach(var c in holder.GetComponentsInChildren<Camera>())c.gameObject.SetActive(false);
            foreach(var l in holder.GetComponentsInChildren<Light>())l.gameObject.SetActive(false);
            var p=centers[i];FitGroceryVisual(holder,heights[i],new Vector3(p.x,ReferenceHeight(p.x,p.y)-.05f,p.y),yaw[i]);
            // A single LOD retains the original silhouette and lets Unity cull the
            // small plant at very low screen coverage; no mesh simplification.
            var lod=holder.GetComponent<LODGroup>();if(!lod)lod=Undo.AddComponent<LODGroup>(holder.gameObject);
            lod.SetLODs(new[]{new LOD(.025f,holder.GetComponentsInChildren<Renderer>())});lod.fadeMode=LODFadeMode.None;lod.RecalculateBounds();EditorUtility.SetDirty(lod);
            var bounds=RendererBounds(holder);report.AppendLine(name+" centre="+bounds.center.ToString("F3")+" height="+bounds.size.y.ToString("F2")+" grounded="+bounds.min.y.ToString("F3"));
        }
        VerifyGameplay(before);Physics.SyncTransforms();ValidateVillageEndBananas();VerifyReferenceWalking();
        AssetDatabase.SaveAssets();EditorSceneManager.MarkSceneDirty(scene);if(!EditorSceneManager.SaveScene(scene))throw new Exception("Cannot save banana placement.");
        var mesh=prefab.GetComponentsInChildren<MeshFilter>().Select(f=>f.sharedMesh).Distinct().ToArray();long tris=mesh.Sum(m=>Enumerable.Range(0,m.subMeshCount).Sum(s=>(long)m.GetIndexCount(s)/3));
        report.AppendLine("PASS: seven larger banana clumps restricted to the back garden; uniform scale, 5cm root burial, shared source mesh/material, 2K compressed mipmaps, shadows received only; original gameplay and walking routes retained. Source triangles per clump: "+tris);
        File.WriteAllText(BananaOutput+"/report.txt",report.ToString());File.WriteAllText(BananaOutput+"/result.json","{\"passed\":true,\"clumps\":7,\"sourceTriangles\":"+tris+"}");Inspect(true);
    }

    private static Material VillageBananaMaterial()
    {
        if(!Directory.GetFiles(BananaTextures).Any(p=>new[]{".png",".jpg",".tga"}.Contains(Path.GetExtension(p).ToLowerInvariant())))
        {((ModelImporter)AssetImporter.GetAtPath(BananaSource)).ExtractTextures(BananaTextures);AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);}
        var images=Directory.GetFiles(BananaTextures).Where(p=>new[]{".png",".jpg",".jpeg",".tga"}.Contains(Path.GetExtension(p).ToLowerInvariant())).Select(p=>p.Replace('\\','/')).ToArray();
        var albedo=images.FirstOrDefault(p=>!p.ToLowerInvariant().Contains("normal") && !p.ToLowerInvariant().Contains("rough") && !p.ToLowerInvariant().Contains("metal") && !p.ToLowerInvariant().Contains("ao."));
        var normal=images.FirstOrDefault(p=>p.ToLowerInvariant().Contains("normal"));if(albedo==null)throw new Exception("Banana albedo missing.");
        foreach(string image in images)
        {
            var ti=(TextureImporter)AssetImporter.GetAtPath(image);bool n=image==normal;var type=n?TextureImporterType.NormalMap:TextureImporterType.Default;
            bool changed=ti.maxTextureSize!=2048 || ti.isReadable || !ti.mipmapEnabled || ti.anisoLevel!=2 || ti.textureCompression!=TextureImporterCompression.Compressed || ti.textureType!=type || ti.sRGBTexture!=(image==albedo);
            ti.maxTextureSize=2048;ti.isReadable=false;ti.mipmapEnabled=true;ti.anisoLevel=2;ti.textureCompression=TextureImporterCompression.Compressed;ti.textureType=type;ti.sRGBTexture=image==albedo;if(changed)ti.SaveAndReimport();
        }
        string path=BananaAssets+"/Chuoi_XanhTram.mat";var material=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(!material){material=new Material(Shader.Find("Universal Render Pipeline/Lit")){name="Chuoi_XanhTram"};AssetDatabase.CreateAsset(material,path);}
        material.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(albedo));material.SetColor("_BaseColor",new Color(.88f,.93f,.81f));material.SetFloat("_Metallic",0);material.SetFloat("_Smoothness",.08f);material.SetFloat("_Cull",0);material.doubleSidedGI=true;material.enableInstancing=true;
        if(normal!=null){material.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>(normal));material.SetFloat("_BumpScale",.4f);material.EnableKeyword("_NORMALMAP");}EditorUtility.SetDirty(material);return material;
    }
    private static void ValidateVillageEndBananas()
    {
        var group=Find(BananaEndGroup).transform;if(group.childCount!=7)throw new Exception("Expected seven banana clumps.");
        Material shared=null;
        foreach(Transform clump in group)
        {
            var b=RendererBounds(clump);if(b.center.z<64 || b.center.z>70 || b.center.x< -26 || b.center.x> -13 || b.size.y<3.7f || b.size.y>4.7f || Mathf.Abs(b.min.y-(ReferenceHeight(b.center.x,b.center.z)-.05f))>.01f)throw new Exception("Banana outside the back garden or wrong scale: "+clump.name);
            var scale=clump.lossyScale;if(Mathf.Abs(scale.x-scale.y)>.001f || Mathf.Abs(scale.x-scale.z)>.001f)throw new Exception("Banana clump is stretched.");
            foreach(var r in clump.GetComponentsInChildren<Renderer>())foreach(var m in r.sharedMaterials){if(!shared)shared=m;else if(shared!=m)throw new Exception("Banana material not shared.");}
        }
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(BananaSource);
        foreach(var t in AllTransforms().Where(t=>t.gameObject.activeInHierarchy && !t.IsChildOf(group)))
            if(t.name=="BuiChuoiBenVuon" || PrefabUtility.GetCorrespondingObjectFromSource(t.gameObject)==prefab)throw new Exception("Banana still scattered outside village end: "+HierarchyPath(t));
    }
    public static void BatchInstallVillageEndBananas()
    {
        try
        {
            EditorSceneManager.OpenScene(ScenePath);InstallVillageEndBananas();DynamicGI.UpdateEnvironment();double ready=EditorApplication.timeSinceStartup+6;
            EditorApplication.CallbackFunction capture=null;capture=()=>
            {
                if(EditorApplication.timeSinceStartup<ready)return;EditorApplication.update-=capture;
                try{CaptureVillageEndBananas();Inspect(true);EditorApplication.Exit(0);}
                catch(Exception e){Debug.LogException(e);File.WriteAllText(BananaOutput+"/error.txt",e.ToString());EditorApplication.Exit(1);}
            };EditorApplication.update+=capture;
        }
        catch(Exception e){Debug.LogException(e);Directory.CreateDirectory(BananaOutput);File.WriteAllText(BananaOutput+"/error.txt",e.ToString());EditorApplication.Exit(1);}
    }
    private static void CaptureVillageEndBananas()
    {
        Directory.CreateDirectory(BananaOutput);
        GroceryShot(new Vector3(-12,3.3f,57.5f),new Vector3(-20,1.5f,67),BananaOutput+"/01_ChuoiCuoiLang.png");
        GroceryShot(new Vector3(-25,10,54),new Vector3(-19,.5f,66.5f),BananaOutput+"/02_ViTriCuoiLang.png");
    }
}
