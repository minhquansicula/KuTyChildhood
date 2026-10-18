using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class KuTyOfficeAssetImport
{
    public const string ModelPath="Assets/_Project/Art/Office/RetroOffice/Models/Office_Workstation.fbx";
    [MenuItem("KuTy/Office Intro/Place Imported Furniture")]
    public static void Place()
    {
        if(EditorApplication.isPlaying || UnityEngine.SceneManagement.SceneManager.GetActiveScene().path!=KuTyOfficeBlockoutBuilder.ScenePath)
            throw new Exception("Open office blockout outside Play.");
        var model=AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
        var source=UnityEngine.Object.Instantiate(model); source.name="TemporaryFurnitureSource";
        var report=new StringBuilder();
        try
        {
            foreach(string name in new[]{"StaffDesk_1_SLOT","StaffDesk_2_SLOT","StaffDesk_3_SLOT","TyDesk_SLOT"})
            {
                var slot=GameObject.Find(name).transform; var visual=slot.Find("Visual_REPLACE");
                foreach(string imported in new[]{"ImportedDesk","ImportedMonitor","ImportedKeyboard"})
                    if(visual.Find(imported)) UnityEngine.Object.DestroyImmediate(visual.Find(imported).gameObject);
                Fit(source.transform,"Desk.004_19",visual,"ImportedDesk",new Vector3(1.8f,.8f,.85f),new Vector3(0,.4f,0),report);
                foreach(Transform part in visual) if(new[]{"Desktop","Leg","Drawer","DrawerHandle"}.Contains(part.name)) part.gameObject.SetActive(false);
                Fit(source.transform,"Monitor.001_60",visual,"ImportedMonitor",new Vector3(.66f,.46f,.36f),new Vector3(0,1.05f,.19f),report);
                foreach(Transform part in visual) if(new[]{"Monitor","MonitorFoot","MonitorStand","Display"}.Contains(part.name)) part.gameObject.SetActive(false);
                Fit(source.transform,"KeyBoard_002.001_12",visual,"ImportedKeyboard",new Vector3(.5f,.04f,.17f),new Vector3(0,.83f,-.18f),report);
                visual.Find("Keyboard").gameObject.SetActive(false);
            }
            foreach(string name in new[]{"StaffChair_1_SLOT","StaffChair_2_SLOT","StaffChair_3_SLOT","TyChair_SLOT"})
            {
                var visual=GameObject.Find(name).transform.Find("Visual_REPLACE");
                if(visual.Find("ImportedChair")) UnityEngine.Object.DestroyImmediate(visual.Find("ImportedChair").gameObject);
                Fit(source.transform,"Chair 1.001_65",visual,"ImportedChair",new Vector3(.65f,1.19f,.65f),new Vector3(0,.595f,0),report);
                foreach(Transform part in visual) if(new[]{"Seat","Back","Stem","BaseA","BaseB"}.Contains(part.name)) part.gameObject.SetActive(false);
            }
            // Keep all interaction/collision anchors unchanged; retain hidden blockout geometry.
            EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        }
        finally { UnityEngine.Object.DestroyImmediate(source); }
        EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        File.WriteAllText("Assets/Screenshots/OfficeWorkstation_Placement.txt",report.ToString());
        KuTyOfficeIntroVisualChecks.Capture();
    }
    static void Fit(Transform source,string sourceName,Transform parent,string name,Vector3 size,Vector3 center,StringBuilder report)
    {
        var original=source.GetComponentsInChildren<Transform>().First(t=>t.name==sourceName);
        var container=new GameObject(name).transform;
        var piece=UnityEngine.Object.Instantiate(original.gameObject).transform;
        piece.SetPositionAndRotation(Vector3.zero,Quaternion.Euler(0,(name=="ImportedMonitor" || name=="ImportedDesk")?-90:90,0)*original.rotation); piece.localScale=original.lossyScale; piece.SetParent(container,true);
        var renderers=piece.GetComponentsInChildren<Renderer>(); var bounds=renderers[0].bounds;
        foreach(var renderer in renderers) renderer.sharedMaterials=renderer.sharedMaterials.Select(FallbackMaterial).ToArray();
        foreach(var r in renderers) bounds.Encapsulate(r.bounds);
        report.AppendLine(sourceName+" raw size="+bounds.size+" rotation="+original.eulerAngles);
        if(bounds.size.x<.00001f || bounds.size.y<.00001f || bounds.size.z<.00001f) throw new Exception("Invalid bounds "+sourceName);
        piece.position-=bounds.center;
        container.localScale=new Vector3(size.x/bounds.size.x,size.y/bounds.size.y,size.z/bounds.size.z);
        container.SetParent(parent,false); container.localPosition=center;
    }
    static Material FallbackMaterial(Material original)
    {
        if(original)
        {
            var restored=AssetDatabase.LoadAssetAtPath<Material>(KuTyOfficeOriginalMaterials.Materials+"/"+original.name.Replace('/','_')+".mat");
            if(restored) return restored;
        }
        if(!original || original.mainTexture) return original;
        const string folder="Assets/_Project/Art/Office/RetroOffice/Legacy/PlaceholderMaterials";
        Directory.CreateDirectory(folder);
        string path=folder+"/"+original.name.Replace('/','_')+".mat";
        var result=AssetDatabase.LoadAssetAtPath<Material>(path); if(result) return result;
        result=new Material(original); result.name=original.name;
        string key=original.name.ToLowerInvariant();
        Color color=new Color(.61f,.59f,.51f);
        if(key.Contains("table")) color=new Color(.38f,.25f,.14f);
        else if(key.Contains("glass")) color=new Color(.055f,.12f,.14f);
        else if(key.Contains("metal") || key.Contains("feet")) color=new Color(.16f,.18f,.19f);
        else if(key.Contains("leather") || key.Contains("plastic")) color=new Color(.09f,.12f,.13f);
        result.SetColor("_BaseColor",color); result.SetFloat("_Smoothness",.25f);
        AssetDatabase.CreateAsset(result,path); return result;
    }
    [MenuItem("KuTy/Office Intro/Inspect Imported Workstation")]
    public static void Inspect()
    {
        var model=AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
        if(!model) throw new Exception("FBX not imported.");
        var report=new StringBuilder();
        foreach(var t in model.GetComponentsInChildren<Transform>(true))
        {
            int depth=0; for(var p=t.parent;p;p=p.parent) depth++;
            report.AppendLine(new string(' ',depth*2)+t.name+" pos="+t.localPosition+" scale="+t.localScale);
        }
        foreach(var m in AssetDatabase.LoadAllAssetsAtPath(ModelPath).OfType<Material>())
            report.AppendLine("MAT "+m.name+" shader="+m.shader.name+" texture="+(m.mainTexture?AssetDatabase.GetAssetPath(m.mainTexture):"NONE"));
        File.WriteAllText("Assets/Screenshots/OfficeWorkstation_Import.txt",report.ToString());
        Debug.Log("WORKSTATION_INSPECTED: "+model.GetComponentsInChildren<MeshRenderer>(true).Length+" renderers");
    }
}
