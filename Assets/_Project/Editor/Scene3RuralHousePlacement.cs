using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class Scene3RuralHousePlacement
{
    private const string ScenePath = "Assets/_Project/Scenes/Act3_MemoryWorld_OutSide.unity";
    private const string PrefabDir = "Assets/_Project/Prefabs/Scene3/RuralHouses";
    private const string CutRiceDir = "Assets/_Project/Art/Models/Scene3/RuralHouseClearings";
    private const string GroupName = "NhaDan_XenGiuaRuongLua";

    private static readonly (string prefab, string rice, Vector3 position, float yaw)[] Lots =
    {
        ("Nha01_NgoiDo_HienTon", "LuaNuoc_-1_-9", new Vector3(-18f, .16f, -9f), 90f),
        ("Nha02_NgoiNau_HienTon", "LuaNuoc_1_8", new Vector3(18f, .16f, 8f), -90f),
        ("Nha03_NgoiReu_HienTon", "LuaNuoc_-1_27", new Vector3(-18f, .16f, 27f), 90f),
        ("Nha04_NgoiDoSam_HienTon", "LuaNuoc_1_65", new Vector3(18f, .16f, 65f), -90f),
        ("Nha05_NgoiCu_HienTon", "LuaNuoc_-1_84", new Vector3(-18f, .16f, 84f), 90f),
    };

    [MenuItem("KuTy/Scene 3/Legacy: place five rural houses among rice fields")]
    public static void Place()
    {
        var active = SceneManager.GetActiveScene();
        if (active.path == ScenePath && Find(active, "LangQue_BoCucMoi"))
        {
            Debug.LogWarning("The new village layout is active. Legacy house placement was skipped.");
            return;
        }
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        if (Find(scene, "LangQue_BoCucMoi"))
        {
            Debug.LogWarning("The new village layout is active. Legacy house placement was skipped.");
            return;
        }
        var environment = Find(scene, "Scene3_DuongLang_RuongLua");
        if (!environment) throw new Exception("Scene-3 environment root is missing.");
        var oldGroup = Find(scene, GroupName);
        if (oldGroup) UnityEngine.Object.DestroyImmediate(oldGroup);

        var group = new GameObject(GroupName);
        SceneManager.MoveGameObjectToScene(group, scene);
        group.transform.SetParent(environment.transform, false);
        var earth = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Art/Materials/Scene3/Scene3_Earth.mat");
        if (!earth) throw new Exception("Scene3_Earth material is missing.");

        for (var index = 0; index < Lots.Length; index++)
        {
            var lot = Lots[index];
            var rice = Find(scene, lot.rice);
            var cutMesh = AssetDatabase.LoadAssetAtPath<Mesh>($"{CutRiceDir}/{lot.rice}_NhaDan.obj");
            if (!rice || !cutMesh) throw new Exception($"Missing cleared rice mesh for {lot.rice}.");
            rice.GetComponent<MeshFilter>().sharedMesh = cutMesh;

            var housePrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabDir}/{lot.prefab}.prefab");
            if (!housePrefab) throw new Exception($"Missing house prefab: {lot.prefab}");
            var site = new GameObject($"SanNha_0{index + 1}");
            site.transform.SetParent(group.transform, false);
            MakeEarth(site.transform, "NenDat", new Vector3(lot.position.x, .09f, lot.position.z),
                      new Vector3(13f, .14f, 11.2f), earth);

            var pathCenterX = lot.position.x < 0 ? -7.85f : 7.85f;
            MakeEarth(site.transform, "LoiDatRaDuong", new Vector3(pathCenterX, .09f, lot.position.z),
                      new Vector3(7.4f, .14f, 1.6f), earth);

            var house = (GameObject)PrefabUtility.InstantiatePrefab(housePrefab, scene);
            house.transform.SetParent(site.transform, true);
            house.transform.SetPositionAndRotation(lot.position, Quaternion.Euler(0, lot.yaw, 0));
            Debug.Log($"[HousePlacement] {lot.prefab} world={lot.position.ToString("F2")} yaw={lot.yaw} cleared={lot.rice}");
        }

        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene)) throw new Exception("Could not save scene 3.");
        Debug.Log("[HousePlacement] Saved scene 3 with 5 houses, earth lots, paths and cleared rice meshes.");
    }

    private static GameObject Find(Scene scene, string name)
    {
        foreach (var root in scene.GetRootGameObjects())
        foreach (var transform in root.GetComponentsInChildren<Transform>(true))
            if (transform.name == name) return transform.gameObject;
        return null;
    }

    private static void MakeEarth(Transform parent, string name, Vector3 position, Vector3 size, Material earth)
    {
        var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = name;
        cube.transform.SetParent(parent, false);
        cube.transform.position = position;
        cube.transform.localScale = size;
        cube.GetComponent<MeshRenderer>().sharedMaterial = earth;
        GameObjectUtility.SetStaticEditorFlags(cube, StaticEditorFlags.BatchingStatic);
    }

    public static void Inspect()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        foreach (var root in scene.GetRootGameObjects())
        foreach (var transform in root.GetComponentsInChildren<Transform>(true))
        {
            var name = transform.name;
            if (!name.StartsWith("LuaNuoc_") && name != "Scene3_DuongLang_RuongLua" &&
                name != "RuongLua_KenhNuoc_CayXanh" && name != "SanBi_DatNen" &&
                name != "ShopCounter" && name != "TiemTapHoa_Visual" &&
                name != "RuongNuoc_DaCatBaiDat" && name != "BoRuong_RanhDat" &&
                name != "DuongDat_QuaTiemTapHoa" && name != "Ground") continue;
            var renderer = transform.GetComponent<Renderer>();
            Debug.Log($"[HouseInspect] {name} pos={transform.position.ToString("F2")}" +
                      (renderer ? $" bounds={renderer.bounds.center.ToString("F2")} size={renderer.bounds.size.ToString("F2")}" : ""));
            if (name == "LuaNuoc_-1_-9")
            {
                var mesh = transform.GetComponent<MeshFilter>().sharedMesh;
                Debug.Log($"[HouseInspect] crop mesh vertices={mesh.vertexCount} readable={mesh.isReadable} triangles={mesh.triangles.Length / 3}");
            }
        }
        for (var i = 1; i <= 5; i++)
        {
            var guid = $"Nha0{i}_";
            var paths = AssetDatabase.FindAssets("t:Prefab " + guid, new[] { "Assets/_Project/Prefabs/Scene3/RuralHouses" });
            if (paths.Length != 1) throw new Exception("House prefab lookup failed: " + guid);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(paths[0]));
            foreach (var renderer in prefab.GetComponentsInChildren<Renderer>())
                if (renderer.name == "Front_step" || renderer.name == "Plinth")
                    Debug.Log($"[HouseInspect] {prefab.name}/{renderer.name} pos={renderer.transform.localPosition.ToString("F2")} bounds={renderer.bounds.center.ToString("F2")} size={renderer.bounds.size.ToString("F2")}");
            Debug.Log($"[HouseInspect] {prefab.name} collider={prefab.GetComponent<BoxCollider>().center.ToString("F2")} size={prefab.GetComponent<BoxCollider>().size.ToString("F2")}");
        }
    }
}
