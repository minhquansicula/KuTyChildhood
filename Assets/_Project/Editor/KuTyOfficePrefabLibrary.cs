using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class KuTyOfficePrefabLibrary
{
    const string Folder="Assets/_Project/Art/Office/RetroOffice/Prefabs";
    static string Kind(string name)
    {
        string n=name.ToLowerInvariant();
        if(n.StartsWith("binder")) return "Binder";
        if(n.Contains("lamp")) return n.Contains("black")?"DeskLamp_Black":n.Contains("creme")?"DeskLamp_Cream":"DeskLamp_White";
        if(n.StartsWith("chair")) return "OfficeChair";
        if(n.StartsWith("cubicle")) return "CubiclePartition";
        if(n.StartsWith("desk")) return "OfficeDesk";
        if(n.StartsWith("filing")) return "FilingCabinet";
        if(n.StartsWith("bottom_drawer")) return "Drawer_Bottom";
        if(n.StartsWith("top_drawer")) return "Drawer_Top";
        if(n.StartsWith("keyboard")) return "Keyboard";
        if(n.StartsWith("monitor")) return "Monitor_CRT";
        if(n.StartsWith("mouse")) return "ComputerMouse";
        if(n.StartsWith("pc")) return "ComputerTower";
        if(n.StartsWith("mug")) return "CoffeeMug";
        if(n.StartsWith("bottle")) return "Bottle";
        if(n.StartsWith("cooler")) return "WaterCooler";
        if(n.StartsWith("clock")) return "WallClock_Body";
        if(n.StartsWith("hour arm")) return "WallClock_HourHand";
        if(n.StartsWith("minute arm")) return "WallClock_MinuteHand";
        if(n.StartsWith("file box 2 lid")) return "ArchiveBox_Lid";
        if(n.StartsWith("file box")) return "ArchiveBox";
        if(n.StartsWith("files")) return "DocumentFolders";
        if(n.StartsWith("paper")) return "PaperSheet";
        if(n.StartsWith("pen holder")) return "PenHolder";
        if(n.StartsWith("phone")) return "DeskPhone";
        if(n.StartsWith("printer base")) return "Printer_Base";
        if(n.StartsWith("printer top")) return "Printer_Top";
        if(n.StartsWith("trash")) return "WasteBin";
        if(n.StartsWith("fan blades")) return "DeskFan_Blades";
        if(n.StartsWith("fan body")) return "DeskFan_Body";
        if(n.StartsWith("fan head")) return "DeskFan_Head";
        if(n.StartsWith("pot")) return "PlantPot";
        if(n.StartsWith("nurbspath")) return "Cable";
        if(n.StartsWith("center piece")) return "Cubicle_CenterPiece";
        return null;
    }
    static string Safe(string name) => System.Text.RegularExpressions.Regex.Replace(name,@"[^A-Za-z0-9_]+","_").Trim('_');
    [MenuItem("KuTy/Office Intro/Create Named Asset Library")]
    public static void Build()
    {
        if(EditorApplication.isPlaying) throw new Exception("Stop Play first.");
        Directory.CreateDirectory(Folder); AssetDatabase.Refresh();
        var source=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(KuTyOfficeAssetImport.ModelPath));
        var counters=new Dictionary<string,int>(); var report=new StringBuilder("Prefab,SourceObject,SizeMetres,Notes\n");
        int created=0;
        try
        {
            var parts=source.GetComponentsInChildren<Transform>().First(t=>t.name=="GLTF_SceneRootNode");
            foreach(Transform part in parts)
            {
                var renderers=part.GetComponentsInChildren<MeshRenderer>(); if(renderers.Length==0) continue;
                string category=Kind(part.name);
                bool unidentified=category==null;
                if(unidentified)
                {
                    string material=renderers.SelectMany(r=>r.sharedMaterials).Where(m=>m).Select(m=>m.name).FirstOrDefault()??"Unknown";
                    category="MeshPart_"+Safe(material);
                }
                if(!counters.ContainsKey(category)) counters[category]=0;
                string name="PF_Office_"+category+"_"+(++counters[category]).ToString("00");
                string subfolder=unidentified?"Parts":category.StartsWith("Office")||category.StartsWith("Filing")||category.StartsWith("Cubicle")?"Furniture":category.StartsWith("Monitor")||category.StartsWith("Computer")||category=="Keyboard"?"Electronics":"Props";
                Directory.CreateDirectory(Folder+"/"+subfolder); AssetDatabase.Refresh();
                var root=new GameObject(name);
                try
                {
                    var visual=UnityEngine.Object.Instantiate(part.gameObject).transform;
                    visual.SetPositionAndRotation(Vector3.zero,Quaternion.Euler(0,90,0)*part.rotation);
                    visual.localScale=part.lossyScale; visual.SetParent(root.transform,true); visual.name="Visual";
                    var meshes=visual.GetComponentsInChildren<MeshRenderer>();
                    Bounds bounds=meshes[0].bounds; foreach(var mesh in meshes) bounds.Encapsulate(mesh.bounds);
                    visual.position-=new Vector3(bounds.center.x,bounds.min.y,bounds.center.z);
                    int index=0;
                    foreach(var mesh in meshes)
                    {
                        string surface=string.Join("_",mesh.sharedMaterials.Where(m=>m).Select(m=>Safe(m.name)));
                        if(mesh.transform!=visual) mesh.name="Mesh_"+surface+"_"+(++index).ToString("00");
                    }
                    string path=Folder+"/"+subfolder+"/"+name+".prefab";
                    if(AssetDatabase.LoadAssetAtPath<GameObject>(path)) throw new Exception("Library already exists: "+path);
                    PrefabUtility.SaveAsPrefabAsset(root,path,out bool success);
                    if(!success) throw new Exception("Failed to save "+path);
                    var check=AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    if(!check || check.GetComponentsInChildren<MeshRenderer>().Any(r=>r.sharedMaterials.Any(m=>!m))) throw new Exception("Invalid prefab "+path);
                    report.AppendLine(name+","+part.name+",\""+bounds.size+"\","+(unidentified?"Unidentified source part; inspect preview":"Original dimensions; bottom-centre pivot; no collider"));
                    created++;
                }
                finally { UnityEngine.Object.DestroyImmediate(root); }
            }
        }
        finally { UnityEngine.Object.DestroyImmediate(source); }
        File.WriteAllText(Folder+"/Asset_Catalog.csv",report.ToString());
        File.WriteAllText(Folder+"/README.md","# Office asset library\n\nDrag PF_Office prefabs from Furniture, Electronics or Props into a scene. Parts contains source meshes with uncertain identities, named by their original material. Original FBX/material/texture names are retained for stable mapping.\n\nPrefabs preserve source metre dimensions, use a bottom-centre pivot and original materials. No collider is added. Printer, fan, clock and drawer components remain separate as in the source; combine the appropriate parts before use as a complete prop. Asset_Catalog.csv maps every prefab to its source name.\n\nValidated "+created+" prefabs: mesh renderers and material references present. Scene layout was not changed.\n");
        AssetDatabase.Refresh(); Selection.activeObject=AssetDatabase.LoadAssetAtPath<DefaultAsset>(Folder); EditorGUIUtility.PingObject(Selection.activeObject);
        Debug.Log("OFFICE_NAMED_LIBRARY_CREATED: "+created+" prefabs at "+Folder);
    }
}
