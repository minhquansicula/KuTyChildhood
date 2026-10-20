using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class KuTyOfficeFloorImport
{
    const string Folder="Assets/_Project/Art/Office/Floor";
    const string Model=Folder+"/Models/OfficeFloor.fbx";
    [MenuItem("KuTy/Office Intro/Import Textured Floor")]
    public static void Apply()
    {
        var scene=SceneManager.GetActiveScene();
        if(scene.path!=KuTyOfficeBlockoutBuilder.ScenePath || EditorApplication.isPlaying) throw new InvalidOperationException("Open office in Edit Mode.");
        var floor=GameObject.Find("OFFICE_BLOCKOUT/01_Architecture/RoomShell_SLOT/Visual_REPLACE/Floor");
        if(!floor) throw new InvalidOperationException("Original floor missing or disabled.");
        Directory.CreateDirectory(Folder+"/Textures"); Directory.CreateDirectory(Folder+"/Materials"); AssetDatabase.Refresh();
        var importer=(ModelImporter)AssetImporter.GetAtPath(Model);
        importer.importAnimation=false; importer.importCameras=false; importer.importLights=false;
        importer.SaveAndReimport(); importer.ExtractTextures(Folder+"/Textures"); AssetDatabase.Refresh();
        var textures=AssetDatabase.FindAssets("t:Texture2D",new[]{Folder+"/Textures"}).Select(AssetDatabase.GUIDToAssetPath).ToArray();
        var basePath=textures.FirstOrDefault(p=>!p.Contains("normal")&&!p.Contains("roughness")&&!p.Contains("metallic"));
        if(basePath==null) throw new InvalidOperationException("No original base-color texture extracted; floor not changed.");
        var normalPath=textures.FirstOrDefault(p=>p.Contains("normal"));
        if(normalPath!=null) { var ti=(TextureImporter)AssetImporter.GetAtPath(normalPath); ti.textureType=TextureImporterType.NormalMap; ti.SaveAndReimport(); }
        var material=new Material(Shader.Find("Universal Render Pipeline/Lit"));
        material.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(basePath)); material.SetColor("_BaseColor",Color.white);
        material.SetFloat("_Smoothness",.22f);
        if(normalPath!=null) {material.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath));material.EnableKeyword("_NORMALMAP");}
        string matPath=Folder+"/Materials/MAT_OfficeFloor.mat";
        if(AssetDatabase.LoadAssetAtPath<Material>(matPath)) throw new InvalidOperationException("Floor material exists; inspect before replacing.");
        AssetDatabase.CreateAsset(material,matPath);
        foreach(var source in AssetDatabase.LoadAllAssetsAtPath(Model).OfType<Material>())
            importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material),source.name),material);
        importer.SaveAndReimport();
        var model=AssetDatabase.LoadAssetAtPath<GameObject>(Model);
        var instance=(GameObject)PrefabUtility.InstantiatePrefab(model,scene);
        Undo.RegisterCreatedObjectUndo(instance,"Import office floor");
        instance.name="ImportedOfficeFloor"; instance.transform.SetParent(floor.transform.parent,false);
        var bounds=BoundsOf(instance); var target=floor.GetComponent<Renderer>().bounds;
        // Normalize footprint to the existing floor; keep walkable surface at its original height.
        instance.transform.localScale=Vector3.Scale(instance.transform.localScale,new Vector3(target.size.x/bounds.size.x, .12f/Mathf.Max(bounds.size.y,.001f),target.size.z/bounds.size.z));
        bounds=BoundsOf(instance);
        instance.transform.position+=new Vector3(target.center.x-bounds.center.x,target.max.y-bounds.max.y,target.center.z-bounds.center.z);
        Undo.RecordObject(floor,"Hide old floor visual"); floor.SetActive(false);
        AssetDatabase.SaveAssets(); KuTyOfficeSunsetLighting.Capture();
        EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        Debug.Log("OFFICE_FLOOR_IMPORTED: textures="+textures.Length+", bounds="+BoundsOf(instance)+"; original collision retained.");
    }
    static Bounds BoundsOf(GameObject go)
    {
        var renderers=go.GetComponentsInChildren<Renderer>();
        if(renderers.Length==0) throw new InvalidOperationException("Floor has no renderer.");
        var b=renderers[0].bounds; foreach(var r in renderers)b.Encapsulate(r.bounds); return b;
    }
}
