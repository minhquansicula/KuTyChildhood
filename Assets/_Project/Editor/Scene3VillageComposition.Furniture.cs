using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static partial class Scene3VillageComposition
{
    private static bool IsScene3Furniture(Transform t)
    {
        // Explicit furniture names: never match BanBe (children), BanBi (marbles)
        // or the shop's counter and its interaction hierarchy.
        string n=t.name;
        return n=="BoBanGheGo" || n=="JournalTable" || n=="MarbleTable" || n=="GheNghiDuoiCay" ||
            n=="BanGo" || n=="GheGo" || n.StartsWith("ChanBan") || n.StartsWith("MatBan") ||
            n.StartsWith("ChanGhe") || n.StartsWith("MatGhe") || n.StartsWith("TuaGhe");
    }

    [MenuItem("KuTy/Scene 3/Hide all tables and chairs")]
    public static void HideScene3Furniture()
    {
        var scene=SceneManager.GetActiveScene();
        if(scene.path!=ScenePath || EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Open Scene 3 in Edit Mode; other scenes must not be edited.");
        string folder=OutputFolder+"/Furniture";Directory.CreateDirectory(folder);string backup=folder+"/before-hide-tables-chairs.unity";
        if(!File.Exists(backup) && !EditorSceneManager.SaveScene(scene,backup,true))throw new Exception("Cannot back up current Scene 3 including unsaved edits.");
        var before=GameplaySnapshot();var furniture=new HashSet<Transform>(AllTransforms().Where(IsScene3Furniture));
        var roots=furniture.Where(t=>{for(var p=t.parent;p;p=p.parent)if(furniture.Contains(p))return false;return true;}).ToArray();
        var report=new StringBuilder();int disabledColliders=0,renderers=0;
        foreach(var root in roots)
        {
            Undo.RegisterFullObjectHierarchyUndo(root.gameObject,"Hide Scene 3 furniture including collision");
            foreach(var collider in root.GetComponentsInChildren<Collider>(true)){if(collider.enabled)disabledColliders++;collider.enabled=false;EditorUtility.SetDirty(collider);}
            renderers+=root.GetComponentsInChildren<Renderer>(true).Length;
            root.gameObject.SetActive(false);PrefabUtility.RecordPrefabInstancePropertyModifications(root.gameObject);
            report.AppendLine("HIDDEN "+HierarchyPath(root));
        }
        Physics.SyncTransforms();
        foreach(var t in furniture)
        {
            if(t.gameObject.activeInHierarchy || t.GetComponentsInChildren<Collider>(true).Any(c=>c.enabled))throw new Exception("Furniture remains active or has enabled collision: "+HierarchyPath(t));
        }
        VerifyGameplay(before);EditorSceneManager.MarkSceneDirty(scene);
        if(!EditorSceneManager.SaveScene(scene))throw new Exception("Cannot save Scene 3 furniture visibility.");
        report.AppendLine("PASS: "+roots.Length+" furniture roots hidden, "+renderers+" renderers hidden, "+disabledColliders+" colliders disabled. All gameplay components unchanged; only the active Scene 3 was saved. Shared assets/prefabs and other scenes untouched.");
        File.WriteAllText(folder+"/report.txt",report.ToString());File.WriteAllText(folder+"/result.json","{\"passed\":true,\"furnitureRoots\":"+roots.Length+",\"hiddenRenderers\":"+renderers+",\"disabledColliders\":"+disabledColliders+"}");
        Inspect(true);SceneView.RepaintAll();
    }
}
