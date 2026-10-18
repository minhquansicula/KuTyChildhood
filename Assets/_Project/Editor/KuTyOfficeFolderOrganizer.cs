using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class KuTyOfficeFolderOrganizer
{
    public const string Root="Assets/_Project/Art/Office/RetroOffice";
    static readonly string[,] Moves={
        {"Assets/_Project/Art/Models/OfficeSceneNew/Office_Workstation.fbx",Root+"/Models/Office_Workstation.fbx"},
        {"Assets/_Project/Art/Materials/RetroOfficeOriginal",Root+"/Materials"},
        {"Assets/_Project/Art/Textures/OfficeSceneNew/RetroOfficeOriginal",Root+"/Textures"},
        {"Assets/_Project/Prefabs/Office/RetroOffice",Root+"/Prefabs"},
        {"Assets/_Project/Generated/OfficeBlockout/ImportedMaterials",Root+"/Legacy/PlaceholderMaterials"}
    };
    static void Ensure(string path)
    {
        if(AssetDatabase.IsValidFolder(path)) return;
        string parent=Path.GetDirectoryName(path).Replace('\\','/'); Ensure(parent);
        AssetDatabase.CreateFolder(parent,Path.GetFileName(path));
    }
    [MenuItem("KuTy/Office Intro/Organize Asset Library")]
    public static void Organize()
    {
        if(EditorApplication.isPlaying) throw new Exception("Stop Play first.");
        var tracked=new Dictionary<string,string>();
        for(int i=0;i<Moves.GetLength(0);i++)
        {
            string from=Moves[i,0],to=Moves[i,1];
            if(AssetDatabase.LoadMainAssetAtPath(to)) throw new Exception("Destination already exists: "+to);
            if(!AssetDatabase.LoadMainAssetAtPath(from)) throw new Exception("Missing source: "+from);
            if(Directory.Exists(from))
                foreach(string guid in AssetDatabase.FindAssets("",new[]{from})) tracked[guid]=AssetDatabase.GUIDToAssetPath(guid);
            tracked[AssetDatabase.AssetPathToGUID(from)]=from;
        }
        foreach(var folder in new[]{Root+"/Models",Root+"/Legacy",Root+"/Documentation"}) Ensure(folder);
        for(int i=0;i<Moves.GetLength(0);i++)
        {
            string error=AssetDatabase.MoveAsset(Moves[i,0],Moves[i,1]); if(!string.IsNullOrEmpty(error)) throw new Exception(error);
        }
        foreach(string file in new[]{"README.md","Asset_Catalog.csv"})
        {
            string error=AssetDatabase.MoveAsset(Root+"/Prefabs/"+file,Root+"/Documentation/"+file);
            if(!string.IsNullOrEmpty(error)) throw new Exception(error);
        }
        File.WriteAllText(Root+"/Documentation/README.md",
            "# Retro Office — thư viện asset\n\n"+
            "- Prefabs/Furniture: bàn, ghế, tủ, vách ngăn.\n- Prefabs/Electronics: CRT, máy tính, chuột, bàn phím.\n- Prefabs/Props: đạo cụ nhỏ.\n- Prefabs/Parts: mesh chưa xác định hoặc bộ phận rời.\n"+
            "- Models: FBX nguồn.\n- Materials: material URP khôi phục từ nguồn.\n- Textures: ảnh gốc và UnityPacked (map chuyển kênh).\n- Documentation/Asset_Catalog.csv: đối chiếu tên prefab và tên nguồn.\n- Legacy/PlaceholderMaterials: màu tạm trước khi có texture, không dùng cho bộ Imported hiện tại.\n\n"+
            "Kéo prefab từ Prefabs vào scene. Pivot ở đáy giữa, kích thước mét từ nguồn, chưa có collider. Máy in/quạt/đồng hồ có bộ phận rời cần ghép. Không đổi tên file image_N trực tiếp vì manifest và công cụ restore dùng chỉ số này.\n");
        AssetDatabase.Refresh();
        foreach(var pair in tracked) if(string.IsNullOrEmpty(AssetDatabase.GUIDToAssetPath(pair.Key))) throw new Exception("Lost GUID: "+pair.Value);
        string[] prefabs=AssetDatabase.FindAssets("t:Prefab",new[]{Root+"/Prefabs"});
        int renderers=0;
        foreach(string guid in prefabs)
        {
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));
            foreach(var renderer in prefab.GetComponentsInChildren<MeshRenderer>(true))
            {
                if(!renderer.GetComponent<MeshFilter>()?.sharedMesh || renderer.sharedMaterials.Any(m=>!m)) throw new Exception("Missing mesh/material: "+prefab.name);
                renderers++;
            }
        }
        File.WriteAllText(Root+"/Documentation/Organization_Report.txt","GUIDs preserved: "+tracked.Count+"\nPrefabs checked: "+prefabs.Length+"\nRenderers with valid mesh/material: "+renderers+"\n");
        AssetDatabase.Refresh(); Selection.activeObject=AssetDatabase.LoadAssetAtPath<DefaultAsset>(Root+"/Prefabs");
        EditorGUIUtility.PingObject(Selection.activeObject); Debug.Log("OFFICE_ORGANIZED: "+prefabs.Length+" prefabs; all GUIDs retained.");
    }
}
