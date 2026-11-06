using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public static class KuTyChildWakeHandsSetup
{
    private const string Folder = "Assets/_Project/Art/Characters/KuTiChildHands";

    [MenuItem("Tools/KuTy/Cutscene/Setup Child Wake Hands")]
    public static void Setup()
    {
        if (EditorApplication.isPlaying) throw new System.InvalidOperationException("Exit Play Mode first.");
        var importer = (ModelImporter)AssetImporter.GetAtPath(Folder + "/KuTiChildHands_Rigged.fbx");
        importer.animationType = ModelImporterAnimationType.Legacy;
        importer.importAnimation = true;
        importer.isReadable = true;
        importer.materialImportMode = ModelImporterMaterialImportMode.None;
        importer.animationCompression = ModelImporterAnimationCompression.Off;
        importer.SaveAndReimport();
        var material = AssetDatabase.LoadAssetAtPath<Material>(Folder + "/ChildHands.mat");
        if (material == null)
        {
            material = new Material(Shader.Find("KuTy/FirstPersonSkin"));
            AssetDatabase.CreateAsset(material, Folder + "/ChildHands.mat");
        }
        material.shader = Shader.Find("KuTy/FirstPersonSkin");
        material.SetColor("_BaseColor", new Color(.72f, .53f, .39f, 1f));
        material.SetFloat("_Fill", .38f);
        EditorUtility.SetDirty(material);
        var shirt = AssetDatabase.LoadAssetAtPath<Material>(Folder + "/ChildShirt.mat");
        if (shirt == null)
        {
            shirt = new Material(Shader.Find("KuTy/FirstPersonSkin"));
            AssetDatabase.CreateAsset(shirt, Folder + "/ChildShirt.mat");
        }
        shirt.shader = Shader.Find("KuTy/FirstPersonSkin");
        shirt.SetColor("_BaseColor", new Color(.81f, .77f, .65f, 1f));
        shirt.SetFloat("_Fill", .38f);
        EditorUtility.SetDirty(shirt);
        var model = AssetDatabase.LoadAssetAtPath<GameObject>(Folder + "/KuTiChildHands_Rigged.fbx");
        var clip = AssetDatabase.LoadAllAssetsAtPath(Folder + "/KuTiChildHands_Rigged.fbx")
            .OfType<AnimationClip>().First(c => !c.name.StartsWith("__preview__"));
        var root = new GameObject("KuTiChildWakeHands");
        try
        {
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(model);
            instance.transform.SetParent(root.transform, false);
            instance.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            foreach (var animator in instance.GetComponentsInChildren<Animator>()) animator.enabled = false;
            foreach (var animation in instance.GetComponentsInChildren<Animation>()) animation.enabled = false;
            foreach (var renderer in instance.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                var mesh = renderer.sharedMesh;
                var vertices = mesh.vertices;
                var heights = new float[mesh.subMeshCount];
                for (int sub = 0; sub < heights.Length; sub++)
                {
                    var triangles = mesh.GetTriangles(sub);
                    Vector3 center = Vector3.zero;
                    foreach (int vertex in triangles) center += vertices[vertex];
                    center /= Mathf.Max(1, triangles.Length);
                    heights[sub] = renderer.transform.TransformPoint(center).y;
                }
                int shirtSlot = System.Array.IndexOf(heights, heights.Max());
                renderer.sharedMaterials = Enumerable.Range(0, mesh.subMeshCount)
                    .Select(sub => sub == shirtSlot ? shirt : material).ToArray();
                renderer.updateWhenOffscreen = true;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }
            var hands = root.AddComponent<ChildWakeHands>();
            var serialized = new SerializedObject(hands);
            serialized.FindProperty("modelRoot").objectReferenceValue = instance;
            serialized.FindProperty("motion").objectReferenceValue = clip;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(root, "Assets/_Project/Resources/Cutscenes/KuTiChildWakeHands.prefab");
            AssetDatabase.SaveAssets();
            Debug.Log("[ChildWakeHands] Created rigged first-person hands. Clip duration: " + clip.length);
        }
        finally { Object.DestroyImmediate(root); }
    }
}
