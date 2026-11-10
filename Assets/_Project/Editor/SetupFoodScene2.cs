using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class SetupFoodScene2
{
    private const string ScenePath = "Assets/_Project/Scenes/Act2_MemoryWorld_Home.unity";
    private const string ModelsFolder = "Assets/_Project/Art/Scene2/Models/Kitchen/Food";
    private const string MaterialsFolder = "Assets/_Project/Art/Scene2/Materials/Kitchen/Food";
    private const string TexturesFolder = "Assets/_Project/Art/Scene2/Textures/Kitchen/Food";
    private const string PrefabsFolder = "Assets/_Project/Art/Scene2/Prefabs/Food";

    private const float FoodScale = 32.0f; // Scale chuẩn cân đối tuyệt đối với mặt bàn tròn phòng bếp Scene 2

    [MenuItem("KuTyTools/Enable Scene 2 Food Carry")]
    public static void EnableFoodCarry()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (EditorApplication.isPlayingOrWillChangePlaymode || scene.path != ScenePath || scene.isDirty)
            throw new System.InvalidOperationException("Open the saved Scene 2 in Edit Mode first.");

        var meal = GameObject.Find("MamCom_GiaDinh");
        var player = Object.FindObjectOfType<FirstPersonController>();
        var names = new[] { "Canh", "RauXao", "ThitKho" };
        var labels = new[] { "canh", "rau xào", "thịt kho" };
        var dishes = new Transform[names.Length];
        int layer = LayerMask.NameToLayer("Interactable");
        if (meal == null || player == null || layer < 0)
            throw new System.InvalidOperationException("Scene 2 is missing the meal, player, or Interactable layer.");
        for (int i = 0; i < names.Length; i++)
        {
            dishes[i] = meal.transform.Find(names[i]);
            if (dishes[i] == null || !dishes[i].TryGetComponent<MeshCollider>(out _))
                throw new System.InvalidOperationException($"Missing {names[i]} or its MeshCollider.");
        }

        var carrier = player.GetComponent<Scene2FoodCarry>() ?? Undo.AddComponent<Scene2FoodCarry>(player.gameObject);
        for (int i = 0; i < dishes.Length; i++)
        {
            var dish = dishes[i].gameObject;
            Undo.RecordObject(dish, "Enable food interaction");
            dish.layer = layer;
            PrefabUtility.RecordPrefabInstancePropertyModifications(dish);
            var pickup = dish.GetComponent<Scene2FoodInteractable>() ?? Undo.AddComponent<Scene2FoodInteractable>(dish);
            var settings = new SerializedObject(pickup);
            settings.FindProperty("foodName").stringValue = labels[i];
            settings.FindProperty("carrier").objectReferenceValue = carrier;
            settings.ApplyModifiedProperties();
        }
        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene))
            throw new System.InvalidOperationException("Could not save Scene 2 food interactions.");
    }

    [MenuItem("KuTy/Scene 2/Thêm 3 món ăn vào Bàn Ăn (Scene 2)")]
    [MenuItem("KuTyTools/Setup Food Scene 2")]
    public static void SetupFood()
    {
        AssetDatabase.Refresh();

        // 1. Đảm bảo các thư mục tồn tại
        EnsureFolder("Assets/_Project/Art/Scene2/Materials/Kitchen", "Food");
        EnsureFolder("Assets/_Project/Art/Scene2/Prefabs", "Food");

        // 2. Cấu hình Normal maps
        ConfigureNormalMap($"{TexturesFolder}/ThitKho/T_ThitKho_Normal.png");
        ConfigureNormalMap($"{TexturesFolder}/RauXao/T_RauXao_Normal.png");
        ConfigureNormalMap($"{TexturesFolder}/Canh/T_Canh_Normal.png");

        // 3. Tạo URP Lit Materials
        Shader urpShader = Shader.Find("Universal Render Pipeline/Lit");
        if (urpShader == null) urpShader = Shader.Find("Standard");

        Material matThitKho = GetOrCreateMaterial(
            $"{MaterialsFolder}/MAT_ThitKho.mat",
            urpShader,
            $"{TexturesFolder}/ThitKho/T_ThitKho_BaseColor.png",
            $"{TexturesFolder}/ThitKho/T_ThitKho_Normal.png",
            $"{TexturesFolder}/ThitKho/T_ThitKho_MetallicSmoothness.png",
            0.85f
        );

        Material matRauXao = GetOrCreateMaterial(
            $"{MaterialsFolder}/MAT_RauXao.mat",
            urpShader,
            $"{TexturesFolder}/RauXao/T_RauXao_BaseColor.png",
            $"{TexturesFolder}/RauXao/T_RauXao_Normal.png",
            $"{TexturesFolder}/RauXao/T_RauXao_MetallicSmoothness.png",
            0.75f
        );

        Material matCanh = GetOrCreateMaterial(
            $"{MaterialsFolder}/MAT_Canh.mat",
            urpShader,
            $"{TexturesFolder}/Canh/T_Canh_BaseColor.png",
            $"{TexturesFolder}/Canh/T_Canh_Normal.png",
            $"{TexturesFolder}/Canh/T_Canh_MetallicSmoothness.png",
            0.80f
        );

        AssetDatabase.SaveAssets();

        // 4. Mở Scene 2 nếu chưa mở
        Scene activeScene = SceneManager.GetActiveScene();
        if (activeScene.path != ScenePath)
        {
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                activeScene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            }
            else
            {
                Debug.LogWarning("[FoodSetup] Đã hủy mở Scene 2.");
                return;
            }
        }

        // 5. Load model FBX
        GameObject fbxThitKho = AssetDatabase.LoadAssetAtPath<GameObject>($"{ModelsFolder}/thitkho.fbx");
        GameObject fbxRauXao = AssetDatabase.LoadAssetAtPath<GameObject>($"{ModelsFolder}/rauxao.fbx");
        GameObject fbxCanh = AssetDatabase.LoadAssetAtPath<GameObject>($"{ModelsFolder}/canh.fbx");

        if (fbxThitKho == null || fbxRauXao == null || fbxCanh == null)
        {
            Debug.LogError($"[FoodSetup] Không tìm thấy đủ 3 model FBX trong {ModelsFolder}!");
            return;
        }

        // 6. Tạo individual Prefabs cho từng món ăn
        CreateOrUpdatePrefab(fbxThitKho, matThitKho, $"{PrefabsFolder}/PF_ThitKho.prefab", FoodScale, Quaternion.identity);
        CreateOrUpdatePrefab(fbxRauXao, matRauXao, $"{PrefabsFolder}/PF_RauXao.prefab", FoodScale, Quaternion.Euler(-90f, 0f, 0f));
        CreateOrUpdatePrefab(fbxCanh, matCanh, $"{PrefabsFolder}/PF_Canh.prefab", FoodScale, Quaternion.identity);

        // 7. Tìm bàn ăn trong Scene 2
        GameObject diningTable = GameObject.Find("HouseLayout/KitchenArea/DiningTable");
        if (diningTable == null) diningTable = GameObject.Find("DiningTable");

        Vector3 tableTopCenter = new Vector3(-0.092f, 1.0934f, -2.018f);
        Transform kitchenAreaTransform = null;
        GameObject kitchenArea = GameObject.Find("HouseLayout/KitchenArea");
        if (kitchenArea == null) kitchenArea = GameObject.Find("KitchenArea");
        if (kitchenArea != null) kitchenAreaTransform = kitchenArea.transform;

        if (diningTable != null)
        {
            Renderer tableRend = diningTable.GetComponent<Renderer>();
            if (tableRend != null)
            {
                tableTopCenter = new Vector3(tableRend.bounds.center.x, tableRend.bounds.max.y, tableRend.bounds.center.z);
            }
        }

        // 8. Tạo hoặc reset group MamCom_GiaDinh
        GameObject rootFood = GameObject.Find("MamCom_GiaDinh");
        if (rootFood != null)
        {
            Undo.DestroyObjectImmediate(rootFood);
        }

        rootFood = new GameObject("MamCom_GiaDinh");
        Undo.RegisterCreatedObjectUndo(rootFood, "Create MamCom_GiaDinh");

        if (kitchenAreaTransform != null)
        {
            rootFood.transform.SetParent(kitchenAreaTransform, true);
        }
        rootFood.transform.position = tableTopCenter;
        rootFood.transform.rotation = Quaternion.identity;
        rootFood.transform.localScale = Vector3.one;

        float tableSurfaceY = tableTopCenter.y + 0.002f;

        // 9. Instantiate 3 món ăn lên mặt bàn
        // Canh (tô canh ở trung tâm phía trước)
        GameObject objCanh = (GameObject)PrefabUtility.InstantiatePrefab(fbxCanh, rootFood.transform);
        objCanh.name = "Canh";
        objCanh.transform.position = new Vector3(tableTopCenter.x, tableSurfaceY, tableTopCenter.z + 0.08f);
        objCanh.transform.rotation = Quaternion.identity;
        objCanh.transform.localScale = Vector3.one * FoodScale;
        ApplyMaterialAndCollider(objCanh, matCanh);

        // Thịt kho (đĩa/tô thịt kho ở phía trái)
        GameObject objThitKho = (GameObject)PrefabUtility.InstantiatePrefab(fbxThitKho, rootFood.transform);
        objThitKho.name = "ThitKho";
        objThitKho.transform.position = new Vector3(tableTopCenter.x - 0.35f, tableSurfaceY, tableTopCenter.z - 0.16f);
        objThitKho.transform.rotation = Quaternion.Euler(0f, 45f, 0f);
        objThitKho.transform.localScale = Vector3.one * FoodScale;
        ApplyMaterialAndCollider(objThitKho, matThitKho);

        // Rau xào (đĩa rau xào ở phía phải, xoay -90 độ X để đĩa nằm phẳng)
        GameObject objRauXao = (GameObject)PrefabUtility.InstantiatePrefab(fbxRauXao, rootFood.transform);
        objRauXao.name = "RauXao";
        objRauXao.transform.position = new Vector3(tableTopCenter.x + 0.35f, tableSurfaceY + (0.003f * FoodScale), tableTopCenter.z - 0.16f);
        objRauXao.transform.rotation = Quaternion.Euler(-90f, 30f, 0f);
        objRauXao.transform.localScale = Vector3.one * FoodScale;
        ApplyMaterialAndCollider(objRauXao, matRauXao);

        // 10. Lưu Prefab hoàn chỉnh của mâm cơm
        PrefabUtility.SaveAsPrefabAssetAndConnect(rootFood, $"{PrefabsFolder}/PF_MamCom_GiaDinh.prefab", InteractionMode.AutomatedAction);

        // 11. Lưu Scene
        EditorSceneManager.MarkSceneDirty(activeScene);
        EditorSceneManager.SaveScene(activeScene);

        Selection.activeGameObject = rootFood;
        if (SceneView.lastActiveSceneView != null)
        {
            SceneView.lastActiveSceneView.FrameSelected();
        }

        Debug.Log("🍲 [FoodSetup] Hoàn tất bài trí 3 món ăn (Thịt kho, Rau xào, Canh) chuẩn mặt bàn ăn trong Scene 2!");
    }

    private static void EnsureFolder(string parentFolder, string newFolder)
    {
        string full = $"{parentFolder}/{newFolder}";
        if (!AssetDatabase.IsValidFolder(full))
        {
            AssetDatabase.CreateFolder(parentFolder, newFolder);
        }
    }

    private static void ConfigureNormalMap(string assetPath)
    {
        TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer != null && importer.textureType != TextureImporterType.NormalMap)
        {
            importer.textureType = TextureImporterType.NormalMap;
            importer.SaveAndReimport();
        }
    }

    private static Material GetOrCreateMaterial(string matPath, Shader shader, string baseColorPath, string normalPath, string metallicSmoothnessPath, float smoothness)
    {
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        if (mat == null)
        {
            mat = new Material(shader);
            AssetDatabase.CreateAsset(mat, matPath);
        }

        mat.shader = shader;
        Texture2D baseColor = AssetDatabase.LoadAssetAtPath<Texture2D>(baseColorPath);
        Texture2D normal = AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath);
        Texture2D metSmooth = AssetDatabase.LoadAssetAtPath<Texture2D>(metallicSmoothnessPath);

        if (baseColor != null)
        {
            mat.SetTexture("_BaseMap", baseColor);
            mat.SetTexture("_MainTex", baseColor);
            mat.SetColor("_BaseColor", Color.white);
            mat.SetColor("_Color", Color.white);
        }

        if (normal != null)
        {
            mat.SetTexture("_BumpMap", normal);
            mat.EnableKeyword("_NORMALMAP");
        }

        if (metSmooth != null)
        {
            mat.SetTexture("_MetallicGlossMap", metSmooth);
            mat.EnableKeyword("_METALLICSPECGLOSSMAP");
            mat.SetFloat("_Metallic", 1.0f);
            mat.SetFloat("_Smoothness", smoothness);
        }
        else
        {
            mat.SetFloat("_Metallic", 0.1f);
            mat.SetFloat("_Smoothness", smoothness);
        }

        EditorUtility.SetDirty(mat);
        return mat;
    }

    private static void ApplyMaterialAndCollider(GameObject obj, Material mat)
    {
        var renderers = obj.GetComponentsInChildren<Renderer>(true);
        foreach (var rend in renderers)
        {
            Material[] mats = new Material[rend.sharedMaterials.Length];
            for (int i = 0; i < mats.Length; i++) mats[i] = mat;
            rend.sharedMaterials = mats;
            EditorUtility.SetDirty(rend);
        }

        var mf = obj.GetComponentInChildren<MeshFilter>();
        if (mf != null && mf.sharedMesh != null)
        {
            var col = obj.GetComponent<MeshCollider>();
            if (col == null) col = Undo.AddComponent<MeshCollider>(obj);
            col.sharedMesh = mf.sharedMesh;
            col.convex = true;
        }
    }

    private static void CreateOrUpdatePrefab(GameObject fbx, Material mat, string prefabPath, float scale, Quaternion rotation)
    {
        GameObject temp = (GameObject)PrefabUtility.InstantiatePrefab(fbx);
        temp.transform.localScale = Vector3.one * scale;
        temp.transform.localRotation = rotation;
        ApplyMaterialAndCollider(temp, mat);
        PrefabUtility.SaveAsPrefabAsset(temp, prefabPath);
        Object.DestroyImmediate(temp);
    }
}
