using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public static class KuTyWalkingHandsSetup
{
    private const string Folder = "Assets/_Project/Art/Characters/KuTiChildHands";

    [MenuItem("Tools/KuTy/Player/Setup Walking Hands")]
    public static void Setup() => SetupModel("KuTiWalkingHands", null);

    [MenuItem("Tools/KuTy/Player/Setup Scene 2 Walking Hands")]
    public static void SetupHome()
    {
        const string name = "HomeWalkingHands";
        var normalPath = $"{Folder}/{name}_Normal.png";
        var normalImporter = (TextureImporter)AssetImporter.GetAtPath(normalPath);
        if (normalImporter.textureType != TextureImporterType.NormalMap)
        {
            normalImporter.textureType = TextureImporterType.NormalMap;
            normalImporter.SaveAndReimport();
        }
        var materialPath = $"{Folder}/{name}.mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if (material == null)
        {
            material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            AssetDatabase.CreateAsset(material, materialPath);
        }
        material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>($"{Folder}/{name}_BaseColor.png"));
        material.SetColor("_BaseColor", new Color(.69f, .72f, .77f));
        material.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath));
        material.SetFloat("_BumpScale", .7f);
        material.SetFloat("_Smoothness", .32f);
        material.EnableKeyword("_NORMALMAP");
        EditorUtility.SetDirty(material);
        SetupModel(name, material);
    }

    private static void SetupModel(string modelName, Material texturedMaterial)
    {
        if (EditorApplication.isPlaying) throw new System.InvalidOperationException("Exit Play Mode first.");
        string modelPath = $"{Folder}/{modelName}.fbx";
        var importer = (ModelImporter)AssetImporter.GetAtPath(modelPath);
        importer.animationType = ModelImporterAnimationType.Legacy;
        importer.importAnimation = true;
        importer.isReadable = true;
        importer.materialImportMode = ModelImporterMaterialImportMode.None;
        importer.animationCompression = ModelImporterAnimationCompression.Off;
        importer.SaveAndReimport();
        var model = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
        var clip = AssetDatabase.LoadAllAssetsAtPath(modelPath).OfType<AnimationClip>()
            .First(c => !c.name.StartsWith("__preview__"));
        var skin = AssetDatabase.LoadAssetAtPath<Material>(Folder + "/ChildHands.mat");
        var shirt = AssetDatabase.LoadAssetAtPath<Material>(Folder + "/ChildShirt.mat");
        var root = new GameObject(modelName);
        try
        {
            root.transform.localScale = Vector3.one * 1.8f;
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(model);
            instance.transform.SetParent(root.transform, false);
            instance.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            foreach (var animation in instance.GetComponentsInChildren<Animation>()) animation.enabled = false;
            foreach (var animator in instance.GetComponentsInChildren<Animator>()) animator.enabled = false;
            foreach (var renderer in instance.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                var mesh = renderer.sharedMesh;
                if (texturedMaterial != null)
                {
                    renderer.sharedMaterials = Enumerable.Repeat(texturedMaterial, mesh.subMeshCount).ToArray();
                }
                else
                {
                    var vertices = mesh.vertices;
                    var heights = new float[mesh.subMeshCount];
                    for (int sub = 0; sub < heights.Length; sub++)
                    {
                        var triangles = mesh.GetTriangles(sub);
                        Vector3 center = Vector3.zero;
                        foreach (int index in triangles) center += vertices[index];
                        heights[sub] = renderer.transform.TransformPoint(center / Mathf.Max(1, triangles.Length)).y;
                    }
                    int shirtSlot = System.Array.IndexOf(heights, heights.Max());
                    renderer.sharedMaterials = Enumerable.Range(0, mesh.subMeshCount).Select(i => i == shirtSlot ? shirt : skin).ToArray();
                }
                renderer.updateWhenOffscreen = true;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }
            var walking = root.AddComponent<ChildWalkingHands>();
            var serialized = new SerializedObject(walking);
            serialized.FindProperty("modelRoot").objectReferenceValue = instance;
            serialized.FindProperty("cycle").objectReferenceValue = clip;
            var transforms = instance.GetComponentsInChildren<Transform>();
            foreach (var binding in new[] { ("rightArmRoot", "L_UpperArm"), ("rightWrist", "L_Hand"),
                         ("leftArmRoot", "R_UpperArm"), ("leftWrist", "R_Hand") })
                serialized.FindProperty(binding.Item1).objectReferenceValue = transforms.First(t => t.name == binding.Item2);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(root, $"Assets/_Project/Resources/Cutscenes/{modelName}.prefab");
            AssetDatabase.SaveAssets();
            Debug.Log("[WalkingHands] Created separate walking-hand prefab.");
        }
        finally { Object.DestroyImmediate(root); }
    }
}
