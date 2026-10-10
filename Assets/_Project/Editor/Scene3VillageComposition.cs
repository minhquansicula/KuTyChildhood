using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// One-shot editor requests for the village layout. No action happens without a request file.
[InitializeOnLoad]
public static partial class Scene3VillageComposition
{
    public const string ScenePath = "Assets/_Project/Scenes/Act3_MemoryWorld_OutSide.unity";
    public const string OutputFolder = "Tools/Scene3/VillageComposition";
    private static double nextCheck;
    [Serializable] private class Request { public string command; }
    [Serializable] private class Result { public bool passed; public string command, message; }
    [Serializable] private class Node
    {
        public string name, path, parent;
        public bool active;
        public Vector3 position, rotation, scale, boundsCenter, boundsSize;
        public string[] components, materials;
    }
    [Serializable] private class SceneSnapshot
    {
        public string scene;
        public bool dirty, playing;
        public int transforms, activeRenderers;
        public List<Node> nodes = new List<Node>();
    }

    static Scene3VillageComposition() { EditorApplication.update += ReadRequest; }
    private static void ReadRequest()
    {
        if (EditorApplication.timeSinceStartup < nextCheck) return;
        nextCheck = EditorApplication.timeSinceStartup + 1;
        var requestPath = Path.Combine(OutputFolder, "request.json");
        if (!File.Exists(requestPath) || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
        var request = JsonUtility.FromJson<Request>(File.ReadAllText(requestPath));
        if(request.command=="clear_grass_v1")
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode) {if(EditorApplication.isPlaying) EditorApplication.ExitPlaymode();return;}
            File.Delete(requestPath);
            var removal=new Result{command=request.command};
            try {ClearBankGrass();removal.passed=true;removal.message="All bank grass removed";}
            catch(Exception exception) {removal.message=exception.ToString();Debug.LogException(exception);}
            File.WriteAllText(Path.Combine(OutputFolder,"command_result.json"),JsonUtility.ToJson(removal,true));
            return;
        }
        if(request.command=="thin_grass_v2") request.command="thin_grass";
        if(request.command=="house02_v1")
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode) {if(EditorApplication.isPlaying) EditorApplication.ExitPlaymode();return;}
            File.Delete(requestPath);var install=new Result{command=request.command};
            try {InstallTencentHouse02();install.passed=true;install.message="House 02 installed";}
            catch(Exception exception) {install.message=exception.ToString();Debug.LogException(exception);}
            File.WriteAllText(Path.Combine(OutputFolder,"command_result.json"),JsonUtility.ToJson(install,true));return;
        }
        if(request.command=="house03_v1")
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode) {if(EditorApplication.isPlaying) EditorApplication.ExitPlaymode();return;}
            File.Delete(requestPath);var install=new Result{command=request.command};
            try {InstallTencentHouse03();install.passed=true;install.message="Weathered house 03 installed";}
            catch(Exception exception) {install.message=exception.ToString();Debug.LogException(exception);}
            File.WriteAllText(Path.Combine(OutputFolder,"command_result.json"),JsonUtility.ToJson(install,true));return;
        }
        if(request.command=="house03_final_v2")
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode) {if(EditorApplication.isPlaying) EditorApplication.ExitPlaymode();return;}
            File.Delete(requestPath);var finish=new Result{command=request.command};
            try {FinalizeHouse03Far();finish.passed=true;finish.message="House 03 far shading finalized";}
            catch(Exception exception) {finish.message=exception.ToString();Debug.LogException(exception);}
            File.WriteAllText(Path.Combine(OutputFolder,"command_result.json"),JsonUtility.ToJson(finish,true));return;
        }
        if(request.command=="house04_v1")
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode) {if(EditorApplication.isPlaying) EditorApplication.ExitPlaymode();return;}
            File.Delete(requestPath);var install=new Result{command=request.command};
            try {InstallTencentHouse04();install.passed=true;install.message="House 04 installed";}
            catch(Exception exception) {install.message=exception.ToString();Debug.LogException(exception);}
            File.WriteAllText(Path.Combine(OutputFolder,"command_result.json"),JsonUtility.ToJson(install,true));return;
        }
        if(request.command=="house04_final_v3")
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode) {if(EditorApplication.isPlaying) EditorApplication.ExitPlaymode();return;}
            File.Delete(requestPath);var finish=new Result{command=request.command};
            try {FinalizeHouse04Shading();finish.passed=true;finish.message="House 04 shading finalized";}
            catch(Exception exception) {finish.message=exception.ToString();Debug.LogException(exception);}
            File.WriteAllText(Path.Combine(OutputFolder,"command_result.json"),JsonUtility.ToJson(finish,true));return;
        }
        // Leave a newer command pending while Unity imports the rest of this tool.
        if (request.command != "inspect" && request.command != "inspectall" && request.command != "compose" && request.command != "capture" && request.command != "validate" && request.command != "refine" && request.command != "rice_lod" && request.command != "rice_gust" && request.command != "natural_banks" && request.command != "tencent_house" && request.command != "repair_house" && request.command != "thin_grass") return;
        if((request.command == "natural_banks" || request.command == "tencent_house" || request.command == "repair_house" || request.command == "thin_grass") && EditorApplication.isPlayingOrWillChangePlaymode)
        {
            if(EditorApplication.isPlaying) EditorApplication.ExitPlaymode();
            return; // Apply to the edit scene after Unity restores it, not to the temporary play scene.
        }
        File.Delete(requestPath);
        var result = new Result { command = request.command };
        try
        {
            var scene = SceneManager.GetActiveScene();
            if (scene.path != ScenePath) throw new Exception("Expected scene 3, active scene is " + scene.path);
            if (request.command == "inspect") Inspect();
            else if (request.command == "inspectall") Inspect(true);
            else if (request.command == "compose") Compose();
            else if (request.command == "capture") Capture();
            else if (request.command == "validate") ValidateScene();
            else if (request.command == "refine") Refine();
            else if (request.command == "rice_lod") UpdateRiceLodDistances();
            else if (request.command == "rice_gust") UpdateRiceGusts();
            else if (request.command == "natural_banks") MakeNaturalBanks();
            else if (request.command == "tencent_house") InstallTencentHouse();
            else if (request.command == "repair_house") InstallTencentHouse();
            else if (request.command == "thin_grass") ThinBankGrass();
            else throw new Exception("Unsupported command: " + request.command);
            result.passed = true;
            result.message = "Completed in editor";
        }
        catch (Exception exception) { result.message = exception.ToString(); Debug.LogException(exception); }
        Directory.CreateDirectory(OutputFolder);
        File.WriteAllText(Path.Combine(OutputFolder, "command_result.json"), JsonUtility.ToJson(result, true));
    }

    public static void Inspect(bool all = false)
    {
        var scene = SceneManager.GetActiveScene();
        var transforms = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Transform>(true)).ToArray();
        var snapshot = new SceneSnapshot { scene = scene.path, dirty = scene.isDirty, playing = EditorApplication.isPlaying,
            transforms = transforms.Length, activeRenderers = transforms.Count(t => t.gameObject.activeInHierarchy && t.GetComponent<Renderer>()) };
        foreach (var transform in transforms)
        {
            if (transform.name.StartsWith("CayLua") || transform.name.StartsWith("Visual_9Cay")) continue;
            if (!all && transform.parent != null && !Important(transform.name)) continue;
            var bounds = new Bounds(transform.position, Vector3.zero);
            var renderers = transform.GetComponentsInChildren<Renderer>(true).Where(r => r.enabled && r.gameObject.activeInHierarchy).ToArray();
            if (renderers.Length > 0) { bounds = renderers[0].bounds; foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds); }
            snapshot.nodes.Add(new Node { name = transform.name, path = HierarchyPath(transform),
                parent = transform.parent ? HierarchyPath(transform.parent) : "", active = transform.gameObject.activeInHierarchy,
                position = transform.position, rotation = transform.eulerAngles, scale = transform.lossyScale,
                boundsCenter = bounds.center, boundsSize = bounds.size,
                components = transform.GetComponents<Component>().Where(c => c).Select(c => c.GetType().Name).ToArray(),
                materials = renderers.SelectMany(r => r.sharedMaterials).Where(m => m).Select(m => m.name).Distinct().ToArray() });
        }
        Directory.CreateDirectory(OutputFolder);
        File.WriteAllText(Path.Combine(OutputFolder, "scene_snapshot.json"), JsonUtility.ToJson(snapshot, true));
        Debug.Log("[VillageComposition] Scene inspected: " + snapshot.nodes.Count + " layout anchors");
    }

    private static bool Important(string name)
    {
        return name.StartsWith("RuongLuaMoi") || name.StartsWith("SanNhaMoi") || name.StartsWith("Nha0") ||
            name.StartsWith("BanBe_") || name.StartsWith("NhomDan") || name.StartsWith("DuongDat") ||
            name.StartsWith("BoKenh") || name.StartsWith("Cau") || name.StartsWith("Kenh") ||
            name.StartsWith("HangTre") || name.StartsWith("Bai") || name.StartsWith("CayOi") ||
            name.StartsWith("CayXanh") || name.StartsWith("CayDa") || name.StartsWith("HangRao") ||
            name == "SanBi_DatNen" || name == "TiemTapHoa_Visual" || name == "MatNuoc" ||
            name == "NhomTreChoibi" || name == "Scene3_DuongLang_RuongLua" || name == "RuongLua_KenhNuoc_CayXanh" ||
            name == "LangQue_BoCucMoi" || name == "NamSanNha_KhoRao" || name == "RuongLua_Moi";
    }

    private static string HierarchyPath(Transform transform)
    {
        return transform.parent ? HierarchyPath(transform.parent) + "/" + transform.name : transform.name;
    }
    private static GameObject Find(string name)
    {
        return SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Transform>(true))
            .FirstOrDefault(t => t.name == name)?.gameObject;
    }
}
