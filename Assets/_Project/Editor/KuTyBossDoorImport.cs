using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class KuTyBossDoorImport
{
    private const string QueueFlag = "Library/KuTyRunBossDoorImport.flag";
    private const string ModelPath = "Assets/_Project/Art/Office/Door/Models/BossOfficeDoor.fbx";
    private const string TexturePath = "Assets/_Project/Art/Office/Door/Textures/T_BossDoor_Walnut_BaseColor.png";
    private const string MaterialPath = "Assets/_Project/Art/Office/Door/Materials/MAT_BossDoor_Walnut.mat";
    private const string HingePath = "OFFICE_BLOCKOUT/02_BossOffice/BossDoor_Hinge";
    private const string VisualRootPath = HingePath + "/DoorLeaf_SLOT/Visual_REPLACE";
    private const string ColliderPath = HingePath + "/DoorLeaf_SLOT/Collision/Leaf_Collider";

    [InitializeOnLoadMethod]
    private static void RunQueuedImport()
    {
        if (!File.Exists(QueueFlag))
            return;
        EditorApplication.delayCall += () =>
        {
            if (!File.Exists(QueueFlag) || EditorApplication.isPlayingOrWillChangePlaymode)
                return;
            try
            {
                Apply();
                File.Delete(QueueFlag);
                Debug.Log("BOSS_DOOR_QUEUE_COMPLETED");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        };
    }

    [MenuItem("KuTy/Office Intro/Import Textured Boss Door")]
    public static void Apply()
    {
        var scene = SceneManager.GetActiveScene();
        if (scene.path != KuTyOfficeBlockoutBuilder.ScenePath || EditorApplication.isPlaying)
            throw new InvalidOperationException("Open the office scene in Edit Mode.");

        var hinge = GameObject.Find(HingePath);
        var visualRoot = GameObject.Find(VisualRootPath);
        var leafColliderObject = GameObject.Find(ColliderPath);
        if (!hinge || !visualRoot || !leafColliderObject)
            throw new InvalidOperationException("Boss door hinge, visual root, or collider is missing.");

        var interaction = hinge.GetComponent<OfficeDoorInteractable>();
        var leafCollider = leafColliderObject.GetComponent<BoxCollider>();
        if (!interaction || !leafCollider)
            throw new InvalidOperationException("Boss door interaction or leaf collider is missing.");

        var importer = AssetImporter.GetAtPath(ModelPath) as ModelImporter;
        if (!importer)
            throw new InvalidOperationException("Boss door FBX importer is missing.");
        importer.importAnimation = false;
        importer.importCameras = false;
        importer.importLights = false;
        importer.materialImportMode = ModelImporterMaterialImportMode.None;
        importer.SaveAndReimport();

        Directory.CreateDirectory("Assets/_Project/Art/Office/Door/Materials");
        AssetDatabase.Refresh();

        var textureImporter = AssetImporter.GetAtPath(TexturePath) as TextureImporter;
        if (!textureImporter)
            throw new InvalidOperationException("Boss door walnut texture is missing.");
        textureImporter.textureType = TextureImporterType.Default;
        textureImporter.sRGBTexture = true;
        textureImporter.wrapMode = TextureWrapMode.Repeat;
        textureImporter.filterMode = FilterMode.Bilinear;
        textureImporter.maxTextureSize = 2048;
        textureImporter.SaveAndReimport();

        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(TexturePath);
        var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        if (!material)
        {
            material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            AssetDatabase.CreateAsset(material, MaterialPath);
        }
        material.name = "MAT_BossDoor_Walnut";
        material.SetTexture("_BaseMap", texture);
        material.SetColor("_BaseColor", Color.white);
        material.SetFloat("_Metallic", 0f);
        material.SetFloat("_Smoothness", 0.24f);
        EditorUtility.SetDirty(material);

        while (visualRoot.transform.childCount > 0)
            Undo.DestroyObjectImmediate(visualRoot.transform.GetChild(0).gameObject);

        var model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
        var modelInstance = PrefabUtility.InstantiatePrefab(model, scene) as GameObject;
        if (!modelInstance)
            throw new InvalidOperationException("Could not instantiate the boss door FBX.");
        Undo.RegisterCreatedObjectUndo(modelInstance, "Import textured boss door");
        modelInstance.name = "ImportedBossDoorVisual";
        modelInstance.transform.SetParent(visualRoot.transform, false);

        var chosenRenderer = modelInstance.GetComponentsInChildren<Renderer>(true)
            .FirstOrDefault(renderer => renderer.name == "D1-Door_D1_0");
        if (!chosenRenderer)
            throw new InvalidOperationException("D1 door mesh was not found in the FBX.");
        foreach (var renderer in modelInstance.GetComponentsInChildren<Renderer>(true))
            renderer.gameObject.SetActive(renderer == chosenRenderer);
        chosenRenderer.sharedMaterial = material;

        var targetBounds = leafCollider.bounds;
        var sourceBounds = chosenRenderer.bounds;
        modelInstance.transform.localScale = Vector3.Scale(
            modelInstance.transform.localScale,
            new Vector3(
                targetBounds.size.x / Mathf.Max(sourceBounds.size.x, 0.0001f),
                targetBounds.size.y / Mathf.Max(sourceBounds.size.y, 0.0001f),
                targetBounds.size.z / Mathf.Max(sourceBounds.size.z, 0.0001f)));
        sourceBounds = chosenRenderer.bounds;
        modelInstance.transform.position += targetBounds.center - sourceBounds.center;

        // The FBX's D1 variant places its handle on the hinge edge in this scene.
        // Turn the visual around its fitted center so the handle sits on the free edge.
        modelInstance.transform.RotateAround(targetBounds.center, Vector3.up, 180f);
        sourceBounds = chosenRenderer.bounds;
        modelInstance.transform.position += targetBounds.center - sourceBounds.center;

        interaction.leaf = leafCollider;
        var closedRotation = hinge.transform.localRotation;
        var closedVisualRotation = chosenRenderer.transform.rotation;
        hinge.transform.localRotation = closedRotation * Quaternion.Euler(0f, interaction.openAngle, 0f);
        var visualRotationDelta = Quaternion.Angle(closedVisualRotation, chosenRenderer.transform.rotation);
        hinge.transform.localRotation = closedRotation;
        var openable = visualRotationDelta > 90f;

        EditorUtility.SetDirty(interaction);
        EditorSceneManager.MarkSceneDirty(scene);
        AssetDatabase.SaveAssets();
        EditorSceneManager.SaveScene(scene);

        Selection.activeGameObject = modelInstance;
        SceneView.lastActiveSceneView?.FrameSelected();
        Debug.Log($"BOSS_DOOR_IMPORTED: variant=D1, textured=true, openable={openable}, rotationTest={visualRotationDelta:F1}, bounds={chosenRenderer.bounds}");
    }

    [MenuItem("KuTy/Office Intro/Inspect Boss Door Model")]
    public static void InspectModel()
    {
        var model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
        if (!model)
            throw new InvalidOperationException("Boss door FBX is missing.");

        var report = new StringBuilder("BOSS_DOOR_MODEL_INSPECTION");
        foreach (var transform in model.GetComponentsInChildren<Transform>(true))
        {
            var depth = 0;
            for (var parent = transform.parent; parent != null; parent = parent.parent)
                depth++;

            report.Append("\n").Append(new string(' ', depth * 2)).Append(transform.name)
                .Append(" pos=").Append(transform.localPosition)
                .Append(" rot=").Append(transform.localEulerAngles)
                .Append(" scale=").Append(transform.localScale);

            var renderer = transform.GetComponent<Renderer>();
            if (renderer)
            {
                report.Append(" renderer=").Append(renderer.GetType().Name)
                    .Append(" bounds=").Append(renderer.bounds)
                    .Append(" materials=[")
                    .Append(string.Join(",", renderer.sharedMaterials.Select(m => m ? m.name : "null")))
                    .Append("]");
            }
        }

        foreach (var material in AssetDatabase.LoadAllAssetsAtPath(ModelPath).OfType<Material>())
        {
            report.Append("\nMAT ").Append(material.name)
                .Append(" shader=").Append(material.shader ? material.shader.name : "null")
                .Append(" base=").Append(material.HasProperty("_BaseMap") && material.GetTexture("_BaseMap")
                    ? material.GetTexture("_BaseMap").name
                    : "none");
        }

        File.WriteAllText("Library/KuTyBossDoorInspection.txt", report.ToString());
        Debug.Log("BOSS_DOOR_MODEL_INSPECTION_WRITTEN: Library/KuTyBossDoorInspection.txt");
    }
}
