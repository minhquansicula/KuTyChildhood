using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Replaces the dense original rice strips with instances of the artist's source FBX,
/// and lays out dry home plots and a separate marble clearing. Original scene objects stay inactive.</summary>
public static class Scene3VillageRebuild
{
    private const string ScenePath = "Assets/_Project/Scenes/Act3_MemoryWorld_OutSide.unity";
    private const string GroupName = "LangQue_BoCucMoi";
    private const string RiceModelPath = "Assets/_Project/Art/Models/Scene3/caylua.fbx";
    private const string RicePrefabPath = "Assets/_Project/Prefabs/Scene3/CayLua_Goc.prefab";
    private const string HousePrefabDir = "Assets/_Project/Prefabs/Scene3/RuralHouses";

    private static readonly (string prefab, Vector3 position)[] Homes =
    {
        ("Nha01_NgoiDo_HienTon", new Vector3(-56f, .16f, -9f)),
        ("Nha02_NgoiNau_HienTon", new Vector3(56f, .16f, 10f)),
        ("Nha03_NgoiReu_HienTon", new Vector3(-56f, .16f, 33f)),
        ("Nha04_NgoiDoSam_HienTon", new Vector3(56f, .16f, 66f)),
        ("Nha05_NgoiCu_HienTon", new Vector3(-56f, .16f, 92f)),
    };

    [MenuItem("KuTy/Scene 3/Rebuild village, rice fields and marble clearing")]
    public static void Rebuild()
    {
        var scene = SceneManager.GetActiveScene();
        if (scene.path != ScenePath) throw new Exception("Open Act3_MemoryWorld_OutSide before rebuilding.");
        if (Find(scene, GroupName)) throw new Exception("New village layout already exists; no duplicate was made.");
        var environment = Find(scene, "Scene3_DuongLang_RuongLua");
        if (!environment) throw new Exception("Scene-3 environment root is missing.");

        var earth = LoadMaterial("Scene3_Earth");
        var grass = LoadMaterial("Scene3_Green");
        var timber = LoadMaterial("Scene3_Timber");
        var leafDark = LoadMaterial("Scene3_LeafDark");
        var leafLight = LoadMaterial("Scene3_LeafLight");
        var ricePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(RicePrefabPath);
        if (!ricePrefab) throw new Exception("Original rice prefab is missing.");
        var water = Find(scene, "RuongNuoc_DaCatBaiDat")?.GetComponent<MeshRenderer>()?.sharedMaterial;
        if (!water) throw new Exception("Original rice-field water material is missing.");

        // Keep the authored source layout in the scene but deactivate it for easy comparison/restore.
        var oldHomes = Find(scene, "NhaDan_XenGiuaRuongLua");
        if (oldHomes) oldHomes.SetActive(false);
        foreach (var root in scene.GetRootGameObjects())
        foreach (var t in root.GetComponentsInChildren<Transform>(true))
            if (t.name.StartsWith("LuaNuoc_") || t.name == "RuongNuoc_DaCatBaiDat" ||
                t.name == "BoRuong_RanhDat" || t.name == "caylua")
                t.gameObject.SetActive(false);

        var group = NewGroup(GroupName, environment.transform);
        var fieldGroup = NewGroup("RuongLua_Moi", group.transform);
        var sites = NewGroup("NamSanNha_KhoRao", group.transform);
        var marble = NewGroup("BaiBanBi_Rieng", group.transform);

        // Three neighbouring left-hand strips become a broad dry play area. The shop keeps
        // its existing clearing on the right at z=27. All other strips receive new rice.
        var keptFields = 0;
        foreach (var side in new[] { -1, 1 })
        foreach (var z in new[] { -9, 8, 27, 46, 65, 84 })
        {
            if (side < 0 && (z == 27 || z == 46 || z == 65)) continue;
            if (side > 0 && z == 27) continue;
            BuildField(fieldGroup.transform, ricePrefab, water, earth, side, z);
            keptFields++;
        }

        MakeBlock(marble.transform, "BaiCo_Kho", new Vector3(-23f, .01f, 46f),
                  new Vector3(34f, .08f, 57f), grass);
        var marbleDirt = Find(scene, "SanBi_DatNen");
        if (!marbleDirt) throw new Exception("Existing marble-play ground is missing.");
        marbleDirt.transform.position = new Vector3(-17f, .07f, 46f);
        marbleDirt.transform.localScale = new Vector3(24f, .14f, 22f);
        MakeBlock(marble.transform, "LoiVaoBaiBi", new Vector3(-4.4f, .07f, 46f),
                  new Vector3(3.2f, .14f, 3.2f), earth);

        for (var i = 0; i < Homes.Length; i++)
            BuildHome(scene, sites.transform, i, earth, timber, leafDark, leafLight);

        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene)) throw new Exception("Could not save rebuilt scene 3.");
        Debug.Log($"[VillageRebuild] Saved 5 spaced dry homes, {keptFields} new rice fields and a rice-free marble clearing.");
    }

    private static void BuildField(Transform parent, GameObject ricePrefab, Material water,
                                   Material earth, int side, int z)
    {
        var centerX = side * 23f;
        var field = NewGroup($"RuongLuaMoi_{side}_{z}", parent);
        var pathZ = float.NaN;
        foreach (var home in Homes)
            if (Mathf.Sign(home.position.x) == side && Mathf.Abs(home.position.z - z) < 8.5f)
                pathZ = home.position.z;

        // A clear dry route crosses any field lying between a house and the road.
        var minZ = z - 8.5f;
        var maxZ = z + 8.5f;
        if (float.IsNaN(pathZ))
            MakeBlock(field.transform, "MatNuoc", new Vector3(centerX, .016f, z),
                      new Vector3(33f, .022f, 17f), water, false);
        else
        {
            WaterSegment(minZ, pathZ - 2.2f);
            WaterSegment(pathZ + 2.2f, maxZ);
        }

        var placed = 0;
        var rng = new System.Random(7919 + (side + 1) * 97 + z * 13);
        for (var row = 0; row < 5; row += 4)
        for (var col = 1; col < 8; col += 5)
        {
            var x = centerX - 14.4f + col * (28.8f / 7f);
            var zz = z - 6.75f + row * (13.5f / 4f);
            if (!float.IsNaN(pathZ) && Mathf.Abs(zz - pathZ) < 3.95f) continue;
            var clump = (GameObject)PrefabUtility.InstantiatePrefab(ricePrefab, field.scene);
            clump.name = $"CayLuaMoi_{row:00}_{col:00}";
            clump.transform.SetParent(field.transform, true);
            clump.transform.position = new Vector3(x + (float)(rng.NextDouble() - .5) * .18f,
                                                   0f, zz + (float)(rng.NextDouble() - .5) * .18f);
            clump.transform.rotation = Quaternion.Euler(0, rng.Next(-13, 14), 0);
            placed++;
        }
        Debug.Log($"[VillageRebuild] {field.name}: {placed} original source-rice clumps");

        void WaterSegment(float from, float to)
        {
            if (to <= from + .1f) return;
            MakeBlock(field.transform, "MatNuoc", new Vector3(centerX, .016f, (from + to) * .5f),
                      new Vector3(33f, .022f, to - from), water, false);
        }
    }

    private static void BuildHome(Scene scene, Transform parent, int index, Material earth,
                                  Material timber, Material leafDark, Material leafLight)
    {
        var data = Homes[index];
        var x = data.position.x;
        var z = data.position.z;
        var direction = Mathf.Sign(x);
        var site = NewGroup($"SanNhaMoi_0{index + 1}", parent);
        MakeBlock(site.transform, "SanDatKho", new Vector3(x, .06f, z),
                  new Vector3(23f, .16f, 20f), earth);
        var roadEdge = direction * 4.4f;
        var pathStart = direction * 44.5f;
        var pathWidth = Mathf.Abs(pathStart - roadEdge);
        MakeBlock(site.transform, "LoiDatRaDuong", new Vector3((pathStart + roadEdge) * .5f, .07f, z),
                  new Vector3(pathWidth, .14f, 3.2f), earth);

        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{HousePrefabDir}/{data.prefab}.prefab");
        if (!prefab) throw new Exception("Missing house prefab " + data.prefab);
        var house = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
        house.transform.SetParent(site.transform, true);
        house.transform.SetPositionAndRotation(data.position, Quaternion.Euler(0, direction < 0 ? 90f : -90f, 0));

        MakeTree(site.transform, new Vector3(x + direction * 5.6f, .14f, z + 5.2f),
                 timber, leafDark, leafLight);
        MakeFurniture(site.transform, new Vector3(x - direction * 5.2f, .14f, z - 5.3f), timber);
    }

    private static void MakeTree(Transform parent, Vector3 basePoint, Material wood,
                                 Material dark, Material light)
    {
        var tree = NewGroup("CayOi", parent);
        MakeBlock(tree.transform, "GocCay", basePoint + Vector3.up * 1.15f,
                  new Vector3(.38f, 2.3f, .38f), wood);
        MakeSphere(tree.transform, "TanLaChinh", basePoint + new Vector3(0, 3.05f, 0),
                   new Vector3(3.1f, 2.45f, 3.1f), dark);
        MakeSphere(tree.transform, "TanLaPhu", basePoint + new Vector3(-.9f, 2.65f, .4f),
                   new Vector3(2.1f, 1.75f, 2.1f), light);
        MakeSphere(tree.transform, "TanLaPhu", basePoint + new Vector3(.95f, 2.8f, -.45f),
                   new Vector3(2.2f, 1.8f, 2.2f), light);
    }

    private static void MakeFurniture(Transform parent, Vector3 center, Material wood)
    {
        var furniture = NewGroup("BoBanGheGo", parent);
        MakeBlock(furniture.transform, "MatBan", center + Vector3.up * .78f,
                  new Vector3(1.65f, .12f, 1.05f), wood);
        foreach (var dx in new[] { -.65f, .65f })
        foreach (var dz in new[] { -.4f, .4f })
            MakeBlock(furniture.transform, "ChanBan", center + new Vector3(dx, .35f, dz),
                      new Vector3(.12f, .7f, .12f), wood);
        foreach (var side in new[] { -1f, 1f })
        {
            var chair = center + new Vector3(side * 1.45f, 0, 0);
            MakeBlock(furniture.transform, "MatGhe", chair + Vector3.up * .43f,
                      new Vector3(.68f, .11f, .7f), wood);
            MakeBlock(furniture.transform, "TuaGhe", chair + new Vector3(side * .31f, .83f, 0),
                      new Vector3(.11f, .84f, .7f), wood);
            foreach (var dz in new[] { -.25f, .25f })
                MakeBlock(furniture.transform, "ChanGhe", chair + new Vector3(-side * .2f, .2f, dz),
                          new Vector3(.1f, .4f, .1f), wood);
        }
    }

    private static GameObject NewGroup(string name, Transform parent)
    {
        var obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        return obj;
    }

    private static GameObject MakeBlock(Transform parent, string name, Vector3 center,
                                        Vector3 size, Material material, bool collider = true)
    {
        var obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        obj.name = name;
        obj.transform.SetParent(parent, false);
        obj.transform.position = center;
        obj.transform.localScale = size;
        obj.GetComponent<MeshRenderer>().sharedMaterial = material;
        obj.GetComponent<Collider>().enabled = collider;
        return obj;
    }

    private static void MakeSphere(Transform parent, string name, Vector3 center,
                                   Vector3 size, Material material)
    {
        var obj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        obj.name = name;
        obj.transform.SetParent(parent, false);
        obj.transform.position = center;
        obj.transform.localScale = size;
        obj.GetComponent<MeshRenderer>().sharedMaterial = material;
    }

    private static Material LoadMaterial(string name)
    {
        var material = AssetDatabase.LoadAssetAtPath<Material>($"Assets/_Project/Art/Materials/Scene3/{name}.mat");
        if (!material) throw new Exception("Missing scene-3 material: " + name);
        return material;
    }

    private static GameObject Find(Scene scene, string name)
    {
        foreach (var root in scene.GetRootGameObjects())
        foreach (var t in root.GetComponentsInChildren<Transform>(true))
            if (t.name == name) return t.gameObject;
        return null;
    }
}
