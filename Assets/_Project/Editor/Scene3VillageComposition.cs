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
        TickVillageResume();
        var requestPath = Path.Combine(OutputFolder, "request.json");
        if (!File.Exists(requestPath) || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
        var request = JsonUtility.FromJson<Request>(File.ReadAllText(requestPath));
        if(request.command=="hide_furniture_scene3")
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode){if(EditorApplication.isPlaying)EditorApplication.ExitPlaymode();return;}
            File.Delete(requestPath);var furniture=new Result{command=request.command};
            try{HideScene3Furniture();furniture.passed=true;furniture.message="Scene 3 tables and chairs hidden with their colliders";}
            catch(Exception e){furniture.message=e.ToString();Debug.LogException(e);}
            File.WriteAllText(Path.Combine(OutputFolder,"command_result.json"),JsonUtility.ToJson(furniture,true));return;
        }
        if(request.command=="player_diagnose")
        {
            File.Delete(requestPath);DiagnoseVillagePlayer();return;
        }
        if(request.command=="player_resume")
        {
            File.Delete(requestPath);ResumeVillagePlayer();return;
        }
        if(request.command=="player_collision_fix")
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode){if(EditorApplication.isPlaying)EditorApplication.ExitPlaymode();return;}
            File.Delete(requestPath);var repair=new Result{command=request.command};
            try{RepairInvisibleJournalCollision();repair.passed=true;repair.message="Hidden journal-table collision removed and approach tested";}
            catch(Exception e){repair.message=e.ToString();Debug.LogException(e);}
            File.WriteAllText(Path.Combine(OutputFolder,"command_result.json"),JsonUtility.ToJson(repair,true));return;
        }
        if(request.command.StartsWith("grocery_"))
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode){if(EditorApplication.isPlaying)EditorApplication.ExitPlaymode();return;}
            File.Delete(requestPath);var grocery=new Result{command=request.command};
            try{if(request.command=="grocery_inspect")InspectGroceryModels();else if(request.command=="grocery_install")InstallGroceryModels();else if(request.command=="grocery_capture")CaptureGroceryScene();else if(request.command=="grocery_test")BeginGroceryPlayCheck();else throw new Exception("Unknown grocery command");grocery.passed=true;grocery.message="Grocery command completed";}
            catch(Exception exception){grocery.message=exception.ToString();Debug.LogException(exception);}
            File.WriteAllText(Path.Combine(OutputFolder,"command_result.json"),JsonUtility.ToJson(grocery,true));return;
        }
        if(request.command=="refresh_scripts")
        {
            // Refresh on the editor's main thread even when its window is unfocused.
            File.Delete(requestPath);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            return;
        }
        if(request.command=="hdri_seam_check")
        {
            File.Delete(requestPath);var check=new Result{command=request.command};
            try {CaptureHdriSeam();check.passed=true;check.message="HDRI seam rendered; see HdriSeam/capture.png and report.txt";}
            catch(Exception exception) {check.message=exception.ToString();Debug.LogException(exception);}
            File.WriteAllText(Path.Combine(OutputFolder,"command_result.json"),JsonUtility.ToJson(check,true));return;
        }
        if(request.command=="maxtree_v1" || request.command=="maxtree_finish_v2" || request.command=="maxtree_capture" || request.command=="maxtree_validate")
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode){if(EditorApplication.isPlaying)EditorApplication.ExitPlaymode();return;}
            File.Delete(requestPath);var vegetation=new Result{command=request.command};
            try {if(request.command=="maxtree_capture")CaptureMaxtreeVillage();else if(request.command=="maxtree_validate")ValidateMaxtreeVillage();else if(request.command=="maxtree_finish_v2")FinishUprightMaxtreeVillage();else InstallMaxtreeVillage();vegetation.passed=true;vegetation.message="Maxtree playable vegetation and lower paddies completed";}
            catch(Exception exception){vegetation.message=exception.ToString();Debug.LogException(exception);}
            File.WriteAllText(Path.Combine(OutputFolder,"command_result.json"),JsonUtility.ToJson(vegetation,true));return;
        }
        if(request.command.StartsWith("reference_"))
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode){if(EditorApplication.isPlaying)EditorApplication.ExitPlaymode();return;}
            File.Delete(requestPath);var reference=new Result{command=request.command};
            try{if(request.command=="reference_capture")CaptureReferenceVillage();else if(request.command=="reference_validate")ValidateReferenceVillage();else if(request.command=="reference_polish")PolishReferenceVillage();else ComposeReferenceVillage();reference.passed=true;reference.message="Reference village with broad paddies and short canal crossings saved and checked";}
            catch(Exception exception){reference.message=exception.ToString();Debug.LogException(exception);}
            File.WriteAllText(Path.Combine(OutputFolder,"command_result.json"),JsonUtility.ToJson(reference,true));return;
        }
        if(request.command=="organic_v1" || request.command=="organic_finish" || request.command=="organic_capture" || request.command=="organic_validate")
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode){if(EditorApplication.isPlaying)EditorApplication.ExitPlaymode();return;}
            File.Delete(requestPath);var organic=new Result{command=request.command};
            try{if(request.command=="organic_capture")CaptureOrganicVillage();else if(request.command=="organic_validate")ValidateOrganicVillage();else if(request.command=="organic_finish")FinishOrganicVillage();else ComposeOrganicVillage();organic.passed=true;organic.message="Organic Unity village visuals and GPU rice integration completed";}
            catch(Exception exception){organic.message=exception.ToString();Debug.LogException(exception);}
            File.WriteAllText(Path.Combine(OutputFolder,"command_result.json"),JsonUtility.ToJson(organic,true));return;
        }
        if(request.command=="water_v4" || request.command=="water_capture")
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode) {if(EditorApplication.isPlaying) EditorApplication.ExitPlaymode();return;}
            File.Delete(requestPath);var water=new Result{command=request.command};
            try {if(request.command=="water_capture") CaptureVillageWater();else InstallVillageWater();water.passed=true;water.message="Village water completed";}
            catch(Exception exception) {water.message=exception.ToString();Debug.LogException(exception);}
            File.WriteAllText(Path.Combine(OutputFolder,"command_result.json"),JsonUtility.ToJson(water,true));return;
        }
        if(request.command=="memory_layout_v1" || request.command=="memory_polish_v2" || request.command=="memory_finish_v3" || request.command=="memory_sunset_v1" || request.command=="memory_capture" || request.command=="memory_validate")
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode) {if(EditorApplication.isPlaying) EditorApplication.ExitPlaymode();return;}
            File.Delete(requestPath);var memory=new Result{command=request.command};
            try {if(request.command=="memory_capture") CaptureMemoryVillage();else if(request.command=="memory_validate") ValidateMemoryVillage();else if(request.command=="memory_sunset_v1") ApplyMemorySunset();else if(request.command=="memory_finish_v3") FinalizeMemoryVillage();else if(request.command=="memory_polish_v2") PolishMemoryVillage();else ComposeMemoryVillage();memory.passed=true;memory.message="Memory village completed and verified";}
            catch(Exception exception) {memory.message=exception.ToString();Debug.LogException(exception);}
            File.WriteAllText(Path.Combine(OutputFolder,"command_result.json"),JsonUtility.ToJson(memory,true));return;
        }
        if(request.command=="deep_canals_v1" || request.command=="deep_canals_backdrop_v2" || request.command=="deep_canals_capture")
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode) {if(EditorApplication.isPlaying) EditorApplication.ExitPlaymode();return;}
            File.Delete(requestPath);var banks=new Result{command=request.command};
            try {if(request.command=="deep_canals_capture") CaptureDeepCanals();else if(request.command=="deep_canals_backdrop_v2") FinishDeepCanalBackdrop();else BuildDeepCanalsAndLowBanks();banks.passed=true;banks.message="Deep canals and continuous low banks saved and verified";}
            catch(Exception exception) {banks.message=exception.ToString();Debug.LogException(exception);}
            File.WriteAllText(Path.Combine(OutputFolder,"command_result.json"),JsonUtility.ToJson(banks,true));return;
        }
        if(request.command=="warm_hdri_v1" || request.command=="hdri_background_v2" || request.command=="warm_hdri_capture")
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode) {if(EditorApplication.isPlaying) EditorApplication.ExitPlaymode();return;}
            File.Delete(requestPath);var hdri=new Result{command=request.command};
            try {if(request.command=="warm_hdri_capture") {CaptureDeepCanals();CaptureMemoryVillage();Inspect(true);}else if(request.command=="hdri_background_v2") InstallCompactHdriSky();else InstallWarmVillageHdri();hdri.passed=true;hdri.message="Warm HDRI, rough banks and planted rice saved";}
            catch(Exception exception) {hdri.message=exception.ToString();Debug.LogException(exception);}
            File.WriteAllText(Path.Combine(OutputFolder,"command_result.json"),JsonUtility.ToJson(hdri,true));return;
        }
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
        if(request.command=="house05_v1"||request.command=="house05_final_v2")
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode) {if(EditorApplication.isPlaying) EditorApplication.ExitPlaymode();return;}
            File.Delete(requestPath);var install=new Result{command=request.command};
            try {InstallTencentHouse05();install.passed=true;install.message="Optimized house 05 installed";}
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
        if(request.command=="marbles_v1")
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode) {if(EditorApplication.isPlaying) EditorApplication.ExitPlaymode();return;}
            File.Delete(requestPath);var install=new Result{command=request.command};
            try {InstallOptimizedMarbles();install.passed=true;install.message="Optimized marbles installed";}
            catch(Exception exception) {install.message=exception.ToString();Debug.LogException(exception);}
            File.WriteAllText(Path.Combine(OutputFolder,"command_result.json"),JsonUtility.ToJson(install,true));return;
        }
        if(request.command=="marbles_final_v2")
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode) {if(EditorApplication.isPlaying) EditorApplication.ExitPlaymode();return;}
            File.Delete(requestPath);var install=new Result{command=request.command};
            try {InstallOptimizedMarbles();install.passed=true;install.message="Final marble materials installed";}
            catch(Exception exception) {install.message=exception.ToString();Debug.LogException(exception);}
            File.WriteAllText(Path.Combine(OutputFolder,"command_result.json"),JsonUtility.ToJson(install,true));return;
        }
        if(request.command=="marbles_visual_v3")
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode) {if(EditorApplication.isPlaying) EditorApplication.ExitPlaymode();return;}
            File.Delete(requestPath);var install=new Result{command=request.command};
            try {InstallOptimizedMarbles();install.passed=true;install.message="Marble colours finalized";}
            catch(Exception exception) {install.message=exception.ToString();Debug.LogException(exception);}
            File.WriteAllText(Path.Combine(OutputFolder,"command_result.json"),JsonUtility.ToJson(install,true));return;
        }
        if(request.command=="marbles_size_v6"||request.command=="marbles_readable_v7")
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode) {if(EditorApplication.isPlaying) EditorApplication.ExitPlaymode();return;}
            File.Delete(requestPath);var sizing=new Result{command=request.command};
            try {if(request.command=="marbles_readable_v7") ResizeReadableMarbles();else ResizeRealisticMarbles();sizing.passed=true;sizing.message="Round marbles resized and physics verified";}
            catch(Exception exception) {sizing.message=exception.ToString();Debug.LogException(exception);}
            File.WriteAllText(Path.Combine(OutputFolder,"command_result.json"),JsonUtility.ToJson(sizing,true));return;
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
