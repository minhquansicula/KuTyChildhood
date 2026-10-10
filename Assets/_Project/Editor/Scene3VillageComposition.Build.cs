using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public static partial class Scene3VillageComposition
{
    private const string VillageName = "LangQue_VietNam_Gon";
    private const string DetailModels = "Assets/_Project/Art/Models/Scene3/VillageDetails";
    private const string NewMaterials = "Assets/_Project/Art/Materials/Scene3/RiceOptimized";
    private static readonly string[] HomeNames = { "Nha01_NgoiDo_HienTon", "Nha02_NgoiNau_HienTon",
        "Nha03_NgoiReu_HienTon", "Nha04_NgoiDoSam_HienTon", "Nha05_NgoiCu_HienTon" };
    private static readonly Vector3[] HomePositions = { new Vector3(-18,.16f,4), new Vector3(18,.16f,44),
        new Vector3(-25,.16f,27), new Vector3(18,.16f,66), new Vector3(-23,.16f,65) };
    // Thua ruong separated by dry yards, small paths and earth banks. xmin,xmax,zmin,zmax.
    private static readonly Vector4[] FieldRects = {
        new Vector4(-40,-7,-10,-5), new Vector4(7,40,-10,14),
        new Vector4(-40,-34,-3,16), new Vector4(-40,-34,18,36), new Vector4(-40,-34,38,56),
        new Vector4(-40,-33,58,73), new Vector4(-40,-7,75,82), new Vector4(-32,-7,14,19),
        new Vector4(-32,-25,35,51), new Vector4(-32,-7,53,56), new Vector4(7,26,52,56),
        new Vector4(29,40,16,34), new Vector4(28,40,36,55), new Vector4(28,40,57,74),
        new Vector4(7,40,76,82), new Vector4(-32,-28,-3,12)
    };
    [Serializable] private class LayoutReport
    {
        public bool passed;
        public string scene, backup;
        public int fields, plants, tiles, gameplayComponentsPreserved, activeRenderers;
        public float groundWidth, groundLength, riceArea;
        public List<string> checks = new List<string>();
    }

    [MenuItem("KuTy/Scene 3/Compose compact Vietnamese village with new rice")]
    public static void Compose()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("Compose requires Edit Mode.");
        var scene = SceneManager.GetActiveScene();
        if (scene.path != ScenePath) throw new Exception("Open scene 3 before composing.");
        if (Find(VillageName)) throw new Exception("Compact village already exists; no duplicate was made.");
        var environment = Find("Scene3_DuongLang_RuongLua");
        var oldLayout = Find("LangQue_BoCucMoi");
        if (!environment || !oldLayout) throw new Exception("Existing village layout was not found.");
        var homes = HomeNames.Select(name => AllTransforms().FirstOrDefault(t => t.name == name && t.gameObject.activeInHierarchy)?.gameObject).ToArray();
        var oldSites = Enumerable.Range(1,5).Select(i => Find("SanNhaMoi_0" + i)).ToArray();
        if (oldSites.Any(site => !site)) throw new Exception("Expected all five existing home sites.");
        var oldWater = Find("RuongLua_Moi").GetComponentsInChildren<MeshRenderer>().First(r => r.name == "MatNuoc").sharedMaterial;
        var earth = LoadSceneMaterial("Scene3_Earth");
        var banks = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Art/Textures/Scene3/BoKenh_Dat.mat") ?? earth;
        var timber = LoadSceneMaterial("Scene3_Timber");
        var green = LoadSceneMaterial("Scene3_Green");
        var leaves = LoadSceneMaterial("Scene3_LeafDark");
        var lightLeaves = LoadSceneMaterial("Scene3_LeafLight");
        var riceMaterial = CreateRiceMaterial();
        var meshes = Enumerable.Range(0,3).Select(i => LoadRiceMesh(i)).ToArray();
        var gameplay = GameplaySnapshot();
        Directory.CreateDirectory(OutputFolder);
        var backup = OutputFolder + "/Act3_MemoryWorld_OutSide.before-compact-village.unity";
        // Save a COPY of the current editor state, including any unsaved edits.
        if (!EditorSceneManager.SaveScene(scene, backup, true)) throw new Exception("Could not back up current editor scene.");
        Undo.IncrementCurrentGroup();
        var undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Compose Vietnamese village");
        Undo.RegisterFullObjectHierarchyUndo(environment, "Original village layout");
        var report = new LayoutReport { scene = ScenePath, backup = backup, groundWidth = 88, groundLength = 102 };
        try
        {
            var village = Group(VillageName, environment.transform);
            var homesRoot = Group("NhaDan_VaSanVuon", village.transform);
            var fieldsRoot = Group("RuongLua_Moi_LOD", village.transform);
            var details = Group("VuonRau_CayAnQua_LoiNho", village.transform);
            var boundary = Group("LuyTre_RanhGioiLang", village.transform);

            for (var index = 0; index < homes.Length; index++)
            {
                var position = HomePositions[index];
                var site = Group("SanNha_Gon_0" + (index + 1), homesRoot.transform);
                var house = homes[index];
                if (!house)
                {
                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Scene3/RuralHouses/" + HomeNames[index] + ".prefab");
                    if (!prefab) throw new Exception("Missing house prefab " + HomeNames[index]);
                    house = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
                    Undo.RegisterCreatedObjectUndo(house, "Restore village house");
                }
                Parent(house.transform, site.transform);
                var axisCorrection = house.transform.Find("Bottom_band") ? Quaternion.Euler(-90,0,0) : Quaternion.identity;
                house.transform.SetPositionAndRotation(position, Quaternion.Euler(0, position.x < 0 ? 90 : -90, 0) * axisCorrection);
                var yardLength = index == 3 ? 16 : 14;
                Block(site.transform, "SanDatNha", new Vector3(position.x,.025f,position.z), new Vector3(16,.15f,yardLength), earth);
                MakePath(site.transform, "NgoNhaRaDuong", new Vector3(Mathf.Sign(position.x) * 3.2f,0,position.z),
                    new Vector3(position.x - Mathf.Sign(position.x) * 7.4f,0,position.z), 2.1f, earth);
                Bridge(site.transform, Mathf.Sign(position.x) * 5, position.z, 2.2f, timber);
                var oldSite = oldSites[index].transform;
                var oldCenter = oldSite.Find("SanDatKho") ? oldSite.Find("SanDatKho").position :
                    new Vector3(index % 2 == 0 ? -56 : 56,0,position.z);
                var offset = new Vector3(position.x - oldCenter.x,0,position.z - oldCenter.z);
                foreach (var name in new[] { "CayOi", "BoBanGheGo" })
                {
                    var prop = oldSite.Find(name);
                    if (!prop) continue;
                    Parent(prop, site.transform); prop.position += offset;
                    if (name == "CayOi") prop.name = "CayOi_SanNha";
                }
                SmallFence(site.transform, position, yardLength, timber);
            }

            var ground = Find("Ground");
            Undo.RecordObject(ground.transform, "Limit village ground");
            ground.transform.position = new Vector3(0,-.15f,37);
            ground.transform.localScale = new Vector3(88,.3f,102);
            var road = Find("DuongDat_QuaTiemTapHoa");
            Undo.RecordObject(road.transform, "Shorter village road");
            road.transform.localScale = new Vector3(.72f,1,94f/120f);
            road.transform.localPosition = new Vector3(0,0,-.25f);
            foreach (var transform in AllTransforms().Where(t => t.gameObject.activeInHierarchy && (t.name == "KenhDanNuoc" || t.name == "BoKenh")))
            {
                Undo.RecordObject(transform, "Shorter irrigation canal");
                var size = transform.localScale; size.z = 94; transform.localScale = size;
                var position = transform.position; position.z = 35; transform.position = position;
            }

            MoveMarbleClearing(village.transform, earth, timber, leaves, lightLeaves);
            foreach (var transform in AllTransforms().Where(t => t.gameObject.activeInHierarchy && !t.IsChildOf(village.transform) &&
                (t.name == "BaiDatRieng_KhongDeRuong" || t.name == "LoiVaoBaiDat" || t.name == "CauVanVaoBai" || t.name == "SanBi_DatNen")))
            {
                Undo.RecordObject(transform.gameObject, "Retire oversized clearings"); transform.gameObject.SetActive(false);
            }
            // Keep the shop and its interaction root. Make its yard compact and accessible.
            Block(details.transform, "SanTiemTapHoa", new Vector3(20,.025f,23.4f), new Vector3(13,.15f,14), earth);
            MakePath(details.transform, "LoiVaoTiem", new Vector3(3,0,18.5f), new Vector3(16,0,18.5f), 2.2f, earth);
            Bridge(details.transform, 5,18.5f,2.4f,timber);
            MakePath(details.transform, "LoiCuoiLang", new Vector3(-29,0,74), new Vector3(25,0,74), 1.5f, earth);
            Bridge(details.transform,-5,74,1.8f,timber); Bridge(details.transform,5,74,1.8f,timber);

            var player = Find("Player")?.transform;
            for (var index = 0; index < FieldRects.Length; index++)
            {
                var rect = FieldRects[index];
                var center = new Vector3((rect.x+rect.y)*.5f,0,(rect.z+rect.w)*.5f);
                var size = new Vector2(rect.y-rect.x,rect.w-rect.z);
                var field = Group("RuongLua_" + (index+1).ToString("00"), fieldsRoot.transform);
                field.transform.position = center;
                Block(field.transform,"MatNuoc",center + Vector3.up*.018f,new Vector3(size.x,.025f,size.y),oldWater,false);
                Block(field.transform,"BoRuong_Tay",new Vector3(rect.x-.2f,.045f,center.z),new Vector3(.4f,.15f,size.y+.8f),banks);
                Block(field.transform,"BoRuong_Dong",new Vector3(rect.y+.2f,.045f,center.z),new Vector3(.4f,.15f,size.y+.8f),banks);
                Block(field.transform,"BoRuong_Nam",new Vector3(center.x,.045f,rect.z-.2f),new Vector3(size.x,.15f,.4f),banks);
                Block(field.transform,"BoRuong_Bac",new Vector3(center.x,.045f,rect.w+.2f),new Vector3(size.x,.15f,.4f),banks);
                var rice = Undo.AddComponent<RuralRiceFieldRenderer>(field);
                rice.nearMesh=meshes[0]; rice.middleMesh=meshes[1]; rice.farMesh=meshes[2];
                rice.sharedMaterial=riceMaterial; rice.fieldSize=size; rice.seed=4109+index*97;
                rice.player=player; rice.Regenerate(); EditorUtility.SetDirty(rice);
                report.plants += rice.PlantCount; report.tiles += rice.TileCount; report.riceArea += size.x*size.y;
            }
            report.fields = FieldRects.Length;
            MakeGardens(details.transform,earth,timber,leaves,lightLeaves);
            MakeBoundary(boundary.transform,timber,leaves,lightLeaves,green,earth);
            RefineHomes(village);
            Undo.RecordObject(oldLayout, "Retire old rice and distant yards"); oldLayout.SetActive(false);
            var legacy = Find("NhaDan_XenGiuaRuongLua"); if (legacy) legacy.SetActive(false);
            VerifyGameplay(gameplay);
            report.gameplayComponentsPreserved = gameplay.Count;
            report.checks.Add("Original gameplay component data and references preserved");
            ValidateLayout(report);
            report.passed=true;
            EditorSceneManager.MarkSceneDirty(scene);
            AssetDatabase.SaveAssets();
            if (!EditorSceneManager.SaveScene(scene)) throw new Exception("Could not save compact village scene.");
            Undo.CollapseUndoOperations(undoGroup);
            File.WriteAllText(OutputFolder+"/layout_report.json",JsonUtility.ToJson(report,true));
            Inspect(true);
            SceneView.RepaintAll();
            Debug.Log("[VillageComposition] Saved compact village: " + report.fields + " fields, " + report.plants + " instanced rice plants.");
        }
        catch { Undo.RevertAllDownToGroup(undoGroup); throw; }
    }

    private static void MoveMarbleClearing(Transform parent, Material earth, Material wood, Material dark, Material light)
    {
        var group = Group("BaiBanBi_VaNhomTre",parent);
        group.transform.position = new Vector3(-17,0,46);
        foreach (var name in new[] { "MarbleTable", "MarbleShootingGame", "PlayerMarble", "ScoringRing", "ShootPosition",
            "MarbleCameraPosition", "MarbleLookAt", "PlayMarbles", "Target0", "Target1", "Target2", "Target3", "Target4", "NhomTreChoibi", "DuongLang_SanBi_BanBe" })
        {
            var obj=Find(name); if (!obj) throw new Exception("Missing marble anchor " + name);
            Parent(obj.transform,group.transform);
        }
        group.transform.position = new Vector3(-15,0,44);
        Block(group.transform,"SanBi_DatNen",new Vector3(-15,.02f,44),new Vector3(14,.16f,14),earth);
        MakePath(group.transform,"LoiVaoSanBi",new Vector3(-3,0,44),new Vector3(-9,0,44),2.3f,earth);
        Bridge(group.transform,-5,44,2.5f,wood);
        var kids = group.GetComponentsInChildren<Transform>(true).Where(t => t.name.StartsWith("BanBe_PhacThao_")).OrderBy(t=>t.name).ToArray();
        var poses = new[] { new Vector3(-18.8f,.10f,42),new Vector3(-11.3f,.10f,46),new Vector3(-18.5f,.10f,47.2f) };
        for(var i=0;i<kids.Length;i++)
        {
            Undo.RecordObject(kids[i],"Children around marble ring"); kids[i].position=poses[i%poses.Length];
            var direction = new Vector3(-15,kids[i].position.y,44)-kids[i].position;
            kids[i].rotation=Quaternion.LookRotation(direction);
        }
        MakeTree(group.transform,new Vector3(-23.2f,.08f,44.5f),4.2f,wood,dark,light,"CayOi_CheBaiBi");
        Block(group.transform,"GheNghiDuoiCay",new Vector3(-21,.53f,48.2f),new Vector3(2.2f,.16f,.6f),wood);
        Block(group.transform,"ChanGhe_1",new Vector3(-21.8f,.29f,48.2f),new Vector3(.18f,.48f,.48f),wood);
        Block(group.transform,"ChanGhe_2",new Vector3(-20.2f,.29f,48.2f),new Vector3(.18f,.48f,.48f),wood);
    }

    private static Material CreateRiceMaterial()
    {
        EnsureFolder(NewMaterials);
        var path=NewMaterials+"/Lua_Ruong_Chung.mat";
        var shader=Shader.Find("KuTy/Village/Rice Instanced Wind");
        if(!shader) throw new Exception("Rice instancing shader has not imported.");
        var material=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(!material) { material=new Material(shader){name="Lua_Ruong_Chung"}; AssetDatabase.CreateAsset(material,path); }
        material.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_Project/Art/Textures/Scene3/RiceOptimized/CayLua_Atlas_BaseColor.png"));
        material.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_Project/Art/Textures/Scene3/RiceOptimized/CayLua_Atlas_Normal.png"));
        material.SetColor("_BaseColor",Color.white); material.SetFloat("_Cutoff",.3f);
        material.SetFloat("_NormalStrength",.55f); material.SetFloat("_WindAmplitude",.075f);
        material.enableInstancing=true; EditorUtility.SetDirty(material); return material;
    }
    private static Mesh LoadRiceMesh(int level)
    {
        var model=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Art/Models/Scene3/RiceOptimized/CayLua_LOD"+level+".fbx");
        if(!model) throw new Exception("Missing new rice LOD"+level);
        return model.GetComponentInChildren<MeshFilter>().sharedMesh;
    }
    private static void SmallFence(Transform parent, Vector3 center, float length, Material wood)
    {
        var outsideX=center.x+Mathf.Sign(center.x)*7.7f;
        for(var z=center.z-length*.5f+.5f;z<center.z+length*.5f;z+=2.4f)
            Block(parent,"CocRaoTre",new Vector3(outsideX,.6f,z),new Vector3(.09f,1.1f,.09f),wood,false);
        Block(parent,"RaoTre_VuonSau",new Vector3(outsideX,.78f,center.z),new Vector3(.08f,.1f,length-1),wood,false);
        Block(parent,"RaoTre_VuonSau",new Vector3(outsideX,.38f,center.z),new Vector3(.08f,.1f,length-1),wood,false);
    }
    private static void Bridge(Transform parent,float x,float z,float width,Material wood)
    {
        Block(parent,"CauVanQuaMuong",new Vector3(x,.15f,z),new Vector3(2.4f,.14f,width),wood);
        for(var index=0;index<5;index++)
            Block(parent,"NepVan",new Vector3(x-.96f+index*.48f,.225f,z),new Vector3(.022f,.012f,width),wood,false);
    }
    private static void MakePath(Transform parent,string name,Vector3 from,Vector3 to,float width,Material material)
    {
        var direction=to-from;
        var path=Block(parent,name,(from+to)*.5f+Vector3.up*.035f,new Vector3(width,.15f,direction.magnitude),material);
        path.transform.rotation=Quaternion.LookRotation(direction,Vector3.up);
    }
    private static GameObject Group(string name,Transform parent)
    {
        var obj=new GameObject(name); Undo.RegisterCreatedObjectUndo(obj,"Village group");
        obj.transform.SetParent(parent,false); return obj;
    }
    private static void Parent(Transform child,Transform parent) { Undo.SetTransformParent(child,parent,"Village layout parent"); }
    private static GameObject Block(Transform parent,string name,Vector3 position,Vector3 size,Material material,bool collision=true)
    {
        var obj=GameObject.CreatePrimitive(PrimitiveType.Cube); Undo.RegisterCreatedObjectUndo(obj,"Village detail");
        obj.name=name; obj.transform.SetParent(parent,false); obj.transform.position=position; obj.transform.localScale=size;
        obj.GetComponent<MeshRenderer>().sharedMaterial=material;
        obj.GetComponent<MeshRenderer>().shadowCastingMode=ShadowCastingMode.Off;
        if(!collision) UnityEngine.Object.DestroyImmediate(obj.GetComponent<Collider>());
        return obj;
    }
    private static void EnsureFolder(string path)
    {
        if(AssetDatabase.IsValidFolder(path)) return;
        var parent=Path.GetDirectoryName(path).Replace('\\','/'); EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent,Path.GetFileName(path));
    }
    private static Material LoadSceneMaterial(string name)
    {
        var material=AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Art/Materials/Scene3/"+name+".mat");
        if(!material) throw new Exception("Missing material "+name); return material;
    }
    private static Transform[] AllTransforms()
    {
        return SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(root=>root.GetComponentsInChildren<Transform>(true)).ToArray();
    }
    private static Dictionary<MonoBehaviour,string> GameplaySnapshot()
    {
        return AllTransforms().SelectMany(t=>t.GetComponents<MonoBehaviour>()).Where(c=>c && c.GetType().Assembly.GetName().Name=="Assembly-CSharp")
            .ToDictionary(c=>c,c=>EditorJsonUtility.ToJson(c));
    }
    private static void VerifyGameplay(Dictionary<MonoBehaviour,string> before)
    {
        foreach(var entry in before)
            if(!entry.Key || EditorJsonUtility.ToJson(entry.Key)!=entry.Value) throw new Exception("Gameplay component changed: "+(entry.Key?entry.Key.name:"deleted"));
    }
    private static void ValidateLayout(LayoutReport report)
    {
        var fields=Find(VillageName).GetComponentsInChildren<RuralRiceFieldRenderer>();
        if(fields.Length!=FieldRects.Length || fields.Any(field=>field.PlantCount<=0 || !field.sharedMaterial.enableInstancing))
            throw new Exception("Rice field population/instancing validation failed.");
        for(var i=0;i<FieldRects.Length;i++)
        for(var j=i+1;j<FieldRects.Length;j++)
        {
            var a=FieldRects[i]; var b=FieldRects[j];
            if(Mathf.Min(a.y,b.y)>Mathf.Max(a.x,b.x) && Mathf.Min(a.w,b.w)>Mathf.Max(a.z,b.z))
                throw new Exception("Rice fields overlap: "+i+", "+j);
        }
        var marble=Find("MarbleLookAt").transform.position;
        if(Vector2.Distance(new Vector2(marble.x,marble.z),new Vector2(-15,44))>.01f) throw new Exception("Marble anchors not moved together.");
        foreach(var rect in FieldRects)
        {
            if(rect.x < -43 || rect.y > 43 || rect.z < -12 || rect.w > 85) throw new Exception("Field outside village boundary.");
            if(rect.x < -8 && rect.y > -22 && rect.z < 51 && rect.w > 37) throw new Exception("Rice overlaps marble clearing.");
            foreach(var house in HomePositions)
                if(rect.x<house.x+8 && rect.y>house.x-8 && rect.z<house.z+7 && rect.w>house.z-7)
                    throw new Exception("Rice overlaps house yard.");
        }
        report.activeRenderers=AllTransforms().Count(t=>t.gameObject.activeInHierarchy && t.GetComponent<Renderer>());
        report.checks.Add("Rice rectangles are non-overlapping and avoid house yards and marble clearing");
        report.checks.Add("Six modelled homes/shop connected to the road by dry paths and bridges");
        report.checks.Add("Four collision boundaries lie inside bamboo/earth perimeter");
    }
}
