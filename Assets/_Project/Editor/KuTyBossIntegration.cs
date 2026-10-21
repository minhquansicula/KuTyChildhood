using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class KuTyBossIntegration
{
    public const string Root = "Assets/_Project/Art/Characters/Boss";
    public const string Office = "Assets/_Project/Scenes/Act1_Office_Blockout.unity";
    [MenuItem("KuTy/Office/Install animated boss only")]
    public static void Install()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Use Edit Mode.");
        var scene = SceneManager.GetActiveScene();
        var pending = scene.GetRootGameObjects().FirstOrDefault(g => g.name == "BossCharacter" &&
            PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(g) == Root + "/Models/Boss_Character.fbx" &&
            !g.GetComponent<BossOfficeAnimation>());
        if (scene.isDirty && !pending) throw new InvalidOperationException("Save your scene edits before installing the boss.");
        if (scene.path != Office) scene = EditorSceneManager.OpenScene(Office);
        Directory.CreateDirectory("Tools/BossAnimation");
        if (!File.Exists("Tools/BossAnimation/before-boss.unity")) File.Copy(Office, "Tools/BossAnimation/before-boss.unity");
        foreach (var folder in new[] { "Materials", "Controllers", "Prefabs" }) Directory.CreateDirectory(Root + "/" + folder);
        AssetDatabase.Refresh();
        var modelPath = Root + "/Models/Boss_Character.fbx";
        var importer = (ModelImporter)AssetImporter.GetAtPath(modelPath);
        importer.animationType = ModelImporterAnimationType.Generic;
        importer.importCameras = importer.importLights = false;
        importer.optimizeGameObjects = false;
        importer.importBlendShapes = false;
        importer.isReadable = false;
        importer.SaveAndReimport();
        var clips = AssetDatabase.LoadAllAssetsAtPath(modelPath).OfType<AnimationClip>()
            .Where(c => c.name.StartsWith("Boss_")).ToDictionary(c => c.name);
        if (clips.Count != 5) throw new InvalidOperationException("Expected five authored boss clips.");
        string controllerPath = Root + "/Controllers/BossOffice.controller";
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
        if (!controller)
        {
            controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
            foreach (string name in new[] { "Boss_Idle", "Boss_Talk_Stern", "Boss_DeskSlam", "Boss_Talk_Command", "Boss_ReturnIdle" })
                controller.layers[0].stateMachine.AddState(name).motion = clips[name];
        }
        var slot = GameObject.Find("OFFICE_BLOCKOUT/02_BossOffice/BossPlaceholder_SLOT");
        var visual = slot.transform.Find("Visual_REPLACE");
        var oldBoss = visual.Find("BossCharacter");
        if (oldBoss) throw new InvalidOperationException("Animated boss is already installed.");
        var model = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
        var instance = pending ? pending : (GameObject)PrefabUtility.InstantiatePrefab(model, scene);
        instance.name = "BossCharacter";
        var animator = instance.GetComponent<Animator>();
        if (!animator) animator = instance.AddComponent<Animator>();
        animator.runtimeAnimatorController = controller;
        animator.applyRootMotion = false;
        animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        instance.AddComponent<BossOfficeAnimation>();
        ApplyMaterials(instance);
        foreach (var renderer in instance.GetComponentsInChildren<SkinnedMeshRenderer>()) renderer.updateWhenOffscreen = true;
        var prefab = PrefabUtility.SaveAsPrefabAsset(instance, Root + "/Prefabs/BossCharacter.prefab");
        UnityEngine.Object.DestroyImmediate(instance);
        foreach (Transform child in visual) { Undo.RecordObject(child.gameObject, "Replace boss placeholder"); child.gameObject.SetActive(false); }
        instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
        Undo.RegisterCreatedObjectUndo(instance, "Install animated boss");
        instance.transform.SetParent(visual, false);
        var desktop = GameObject.Find("OFFICE_BLOCKOUT/02_BossOffice/BossDesk_SLOT/Collision/Desktop_Collider").GetComponent<Collider>();
        instance.transform.localPosition = new Vector3(0f, desktop.bounds.max.y - .781f, 0f);
        var intro = UnityEngine.Object.FindObjectOfType<OfficeIntroController>();
        Undo.RecordObject(intro, "Connect boss to office dialogue");
        intro.bossAnimation = instance.GetComponent<BossOfficeAnimation>();
        intro.bossAnimation.intro = intro;
        var head = instance.GetComponentsInChildren<Transform>().Single(t => t.name == "head");
        var serialized = new SerializedObject(intro);
        serialized.FindProperty("openingLookTarget").objectReferenceValue = head;
        serialized.ApplyModifiedProperties();
        clips["Boss_Idle"].SampleAnimation(instance, 0f);
        EditorUtility.SetDirty(intro.bossAnimation);
        PrefabUtility.RecordPrefabInstancePropertyModifications(intro.bossAnimation);
        PrefabUtility.RecordPrefabInstancePropertyModifications(instance.transform);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Inspect();
        Debug.Log("[Boss] Installed character only; original desk and chair retained. Five clips follow the existing voice sequence.");
    }

    private static void ApplyMaterials(GameObject character)
    {
        foreach (var renderer in character.GetComponentsInChildren<Renderer>())
        {
            string n = renderer.name.ToLowerInvariant();
            string texture = n.Contains("elegantsuit") ? "male_elegantsuit01_diffuse" :
                n.Contains("male_generic") ? "young_lightskinned_male_diffuse" :
                n.Contains("low-poly") ? "Boss_Eyes_Black_BaseColor" :
                n.Contains("eyebrow009") ? "eyebrow009" :
                n.Contains("short04") ? "short04_diffuse" : n.Contains("shoes03") ? "shoes03_diffuse" : null;
            if (texture == null) throw new InvalidOperationException("Unexpected mesh in character: " + renderer.name);
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(Root + "/Textures/" + texture + ".png");
            string path = Root + "/Materials/" + texture + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (!material) { material = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(material, path); }
            material.SetTexture("_BaseMap", tex);
            material.SetColor("_BaseColor", Color.white);
            material.SetFloat("_Smoothness", .22f);
            if (n.Contains("short04") || n.Contains("eyebrow009"))
            {
                material.SetFloat("_AlphaClip", 1f); material.SetFloat("_Cutoff", .35f);
                material.EnableKeyword("_ALPHATEST_ON"); material.renderQueue = 2450;
                material.SetFloat("_Cull", 0f);
            }
            renderer.sharedMaterials = Enumerable.Repeat(material, renderer.sharedMaterials.Length).ToArray();
            EditorUtility.SetDirty(material);
        }
    }
    [MenuItem("KuTy/Office/Inspect boss animation placement")]
    public static void Inspect()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Use Edit Mode.");
        var scene = SceneManager.GetActiveScene();
        if (scene.path != Office)
        {
            if (scene.isDirty) throw new InvalidOperationException("Current scene has unsaved edits.");
            scene = EditorSceneManager.OpenScene(Office);
        }
        var report = new StringBuilder();
        foreach (var root in scene.GetRootGameObjects())
        foreach (var t in root.GetComponentsInChildren<Transform>(true))
        {
            if (!t.name.Contains("Boss") && !t.name.Contains("boss") &&
                !ParentPath(t).Contains("02_BossOffice")) continue;
            report.AppendLine(ParentPath(t) + " pos=" + t.position.ToString("F3") + " rot=" + t.eulerAngles.ToString("F1") + " scale=" + t.lossyScale.ToString("F3"));
            var renderer = t.GetComponent<Renderer>();
            if (renderer) report.AppendLine("  bounds " + renderer.bounds.ToString("F3"));
            var collider = t.GetComponent<Collider>();
            if (collider) report.AppendLine("  collider " + collider.bounds.ToString("F3"));
        }
        var model = AssetDatabase.LoadAssetAtPath<GameObject>(Root + "/Models/Boss_Character.fbx");
        foreach (var t in model.GetComponentsInChildren<Transform>(true))
            if (t.GetComponent<Renderer>() || t.name.ToLower().Contains("head") || t.name.ToLower().Contains("hand"))
                report.AppendLine("MODEL " + ParentPath(t) + " pos=" + t.position.ToString("F3"));
        foreach (var clip in AssetDatabase.LoadAllAssetsAtPath(Root + "/Models/Boss_Character.fbx").OfType<AnimationClip>())
            report.AppendLine("CLIP " + clip.name + " " + clip.length + " loop=" + clip.isLooping);
        Directory.CreateDirectory("Tools/BossAnimation");
        File.WriteAllText("Tools/BossAnimation/placement.txt", report.ToString());
    }
    private static string ParentPath(Transform t) => t.parent ? ParentPath(t.parent) + "/" + t.name : t.name;
}
