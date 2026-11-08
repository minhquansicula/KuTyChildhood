using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class SetupTiepKhachScene2
{
    private const string ScenePath = "Assets/_Project/Scenes/Act2_MemoryWorld_Home.unity";
    private const string Folder = "Assets/_Project/Art/Scene2/Models/LivingRoom";
    private const string TexturesFolder = "Assets/_Project/Art/Scene2/Models/LivingRoom/Textures";
    private const string MaterialsFolder = "Assets/_Project/Art/Scene2/Models/LivingRoom/Materials";

    [MenuItem("KuTy/Scene 2/Import living room set")]
    [MenuItem("KuTy/Scene 2/Thêm bộ bàn ghế tiếp khách (Phòng khách)")]
    [MenuItem("KuTyTools/Thêm Bộ Bàn Ghế Tiếp Khách vào Scene 2")]
    public static void ApplyOriginalFurniture()
    {
        AssetDatabase.Refresh();

        // 1. Tạo thư mục Materials nếu chưa có
        if (!AssetDatabase.IsValidFolder(MaterialsFolder))
        {
            AssetDatabase.CreateFolder(Folder, "Materials");
        }

        // 2. Cấu hình Normal maps
        ConfigureNormalMap($"{TexturesFolder}/T_BanDai_Normal.png");
        ConfigureNormalMap($"{TexturesFolder}/T_GheDai_Normal.png");
        ConfigureNormalMap($"{TexturesFolder}/T_CaiGhe_Normal.png");

        // 3. Tạo/Cập nhật 3 Material chuẩn URP Lit
        Shader urpShader = Shader.Find("Universal Render Pipeline/Lit");
        if (urpShader == null) urpShader = Shader.Find("Standard");

        Material matBan = GetOrCreateMaterial($"{MaterialsFolder}/M_BanDai_Khach.mat", urpShader,
            $"{TexturesFolder}/T_BanDai_BaseColor.png",
            $"{TexturesFolder}/T_BanDai_Normal.png",
            $"{TexturesFolder}/T_BanDai_MetallicSmoothness.png");

        Material matGheDai = GetOrCreateMaterial($"{MaterialsFolder}/M_GheDai_Khach.mat", urpShader,
            $"{TexturesFolder}/T_GheDai_BaseColor.png",
            $"{TexturesFolder}/T_GheDai_Normal.png",
            $"{TexturesFolder}/T_GheDai_MetallicSmoothness.png");

        Material matCaiGhe = GetOrCreateMaterial($"{MaterialsFolder}/M_CaiGhe_Khach.mat", urpShader,
            $"{TexturesFolder}/T_CaiGhe_BaseColor.png",
            $"{TexturesFolder}/T_CaiGhe_Normal.png",
            $"{TexturesFolder}/T_CaiGhe_MetallicSmoothness.png");

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
                Debug.LogWarning("[TiepKhach] Đã hủy mở Scene 2.");
                return;
            }
        }

        // 5. Load model FBX
        GameObject banFBX = AssetDatabase.LoadAssetAtPath<GameObject>($"{Folder}/bandai_khach.fbx");
        GameObject gheDaiFBX = AssetDatabase.LoadAssetAtPath<GameObject>($"{Folder}/ghedai_khach.fbx");
        GameObject caiGheFBX = AssetDatabase.LoadAssetAtPath<GameObject>($"{Folder}/caighe2_khach.fbx");

        if (banFBX == null || gheDaiFBX == null || caiGheFBX == null)
        {
            Debug.LogError($"[TiepKhach] Không tìm thấy đủ 3 model FBX trong {Folder}!");
            return;
        }

        // 6. Tìm hoặc tạo cụm cha BoBanGhe_TiepKhach
        GameObject rootGroup = GameObject.Find("BoBanGhe_TiepKhach");
        Vector3 defaultPos = new Vector3(-0.505f, 0f, -4.69f);
        if (rootGroup == null)
        {
            rootGroup = new GameObject("BoBanGhe_TiepKhach");
            Undo.RegisterCreatedObjectUndo(rootGroup, "Create BoBanGhe_TiepKhach");
            rootGroup.transform.position = defaultPos;
        }

        // Dọn dẹp con cũ nếu có
        while (rootGroup.transform.childCount > 0)
        {
            Undo.DestroyObjectImmediate(rootGroup.transform.GetChild(0).gameObject);
        }

        // 7. Tạo Bàn Dài
        GameObject banObj = (GameObject)PrefabUtility.InstantiatePrefab(banFBX, rootGroup.transform);
        banObj.name = "bandai_khach";
        banObj.transform.localPosition = Vector3.zero;
        banObj.transform.localRotation = Quaternion.identity;
        ApplyMaterialToRenderers(banObj, matBan);

        // 8. Tạo Ghế Dài
        GameObject gheDaiObj = (GameObject)PrefabUtility.InstantiatePrefab(gheDaiFBX, rootGroup.transform);
        gheDaiObj.name = "ghedai_khach";
        gheDaiObj.transform.localPosition = new Vector3(0f, 0f, 1.25f);
        gheDaiObj.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
        ApplyMaterialToRenderers(gheDaiObj, matGheDai);

        // 9. Tạo Ghế Đơn 1
        GameObject caiGhe1Obj = (GameObject)PrefabUtility.InstantiatePrefab(caiGheFBX, rootGroup.transform);
        caiGhe1Obj.name = "caighe_khach_1";
        caiGhe1Obj.transform.localPosition = new Vector3(-1.3f, 0f, 0f);
        caiGhe1Obj.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
        ApplyMaterialToRenderers(caiGhe1Obj, matCaiGhe);

        // 10. Tạo Ghế Đơn 2
        GameObject caiGhe2Obj = (GameObject)PrefabUtility.InstantiatePrefab(caiGheFBX, rootGroup.transform);
        caiGhe2Obj.name = "caighe_khach_2";
        caiGhe2Obj.transform.localPosition = new Vector3(1.3f, 0f, 0f);
        caiGhe2Obj.transform.localRotation = Quaternion.Euler(0f, -90f, 0f);
        ApplyMaterialToRenderers(caiGhe2Obj, matCaiGhe);

        EditorSceneManager.MarkSceneDirty(activeScene);
        EditorSceneManager.SaveScene(activeScene);

        Selection.activeGameObject = rootGroup;
        if (SceneView.lastActiveSceneView != null)
        {
            SceneView.lastActiveSceneView.FrameSelected();
        }

        Debug.Log("🎉 [TiepKhach] Đã import & cấu hình 100% texture gốc cho bộ bàn ghế tiếp khách vào Scene 2 thành công!");
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

    private static Material GetOrCreateMaterial(string matPath, Shader shader, string baseColorPath, string normalPath, string metallicSmoothnessPath)
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
            mat.SetFloat("_Smoothness", 1.0f);
        }

        EditorUtility.SetDirty(mat);
        return mat;
    }

    private static void ApplyMaterialToRenderers(GameObject obj, Material mat)
    {
        var renderers = obj.GetComponentsInChildren<Renderer>();
        foreach (var rend in renderers)
        {
            Material[] mats = new Material[rend.sharedMaterials.Length];
            for (int i = 0; i < mats.Length; i++) mats[i] = mat;
            rend.sharedMaterials = mats;
            EditorUtility.SetDirty(rend);
        }
    }
}
