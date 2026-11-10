using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.AI;

/// <summary>Additive integration. Existing model, standing clips, room layout and Blender sources are never overwritten.</summary>
public static class Scene2MotherHomeSetup
{
    public const string Home = "Assets/_Project/Scenes/Act2_MemoryWorld_Home.unity";
    public const string Folder = "Assets/_Project/Characters/MomUserHome";
    public const string Walk = Folder + "/MomUser_Walk_Home.fbx";
    public const string Controller = Folder + "/MotherHome.controller";

    [MenuItem("KuTy/Scene 2/Inspect unsaved mother home changes (copy only)")]
    public static void InspectUnsaved()
    {
        var scene = SceneManager.GetActiveScene();
        if (EditorApplication.isPlayingOrWillChangePlaymode || scene.path != Home) throw new InvalidOperationException("Home Edit Mode required.");
        Directory.CreateDirectory("Builds/MotherHome-20261108");
        EditorSceneManager.SaveScene(scene, "Builds/MotherHome-20261108/Home-unsaved-copy.unity", true);
        Debug.Log("[MotherHome] Saved inspection COPY only; current scene/dirty edits retained.");
    }

    [MenuItem("KuTy/Scene 2/Rebake mother indoor navigation (keep layout)")]
    public static void RebakeNavigation()
    {
        var scene = SceneManager.GetActiveScene();
        if (EditorApplication.isPlayingOrWillChangePlaymode || scene.path != Home || scene.isDirty)
            throw new InvalidOperationException("Open saved Home in Edit Mode first.");
        var actor = UnityEngine.Object.FindObjectOfType<Scene2MotherHomeMovement>();
        if (!actor) throw new InvalidOperationException("Bind mother's home movement first.");
        var sources = new List<NavMeshBuildSource>();
        var bounds = new Bounds(actor.transform.position + Vector3.up * 1.5f, new Vector3(38, 7, 30));
        NavMeshBuilder.CollectSources(bounds, ~0, NavMeshCollectGeometry.RenderMeshes, 0, new List<NavMeshBuildMarkup>(), sources);
        var player = UnityEngine.Object.FindObjectOfType<FirstPersonController>();
        sources.RemoveAll(s => s.component && (s.component.transform.IsChildOf(actor.transform) || player && s.component.transform.IsChildOf(player.transform)));
        var capsule = actor.GetComponent<CapsuleCollider>();
        var settings = NavMesh.GetSettingsByIndex(0);
        settings.agentRadius = capsule.radius * Mathf.Max(actor.transform.lossyScale.x, actor.transform.lossyScale.z) + .035f;
        settings.agentHeight = capsule.height * actor.transform.lossyScale.y;
        settings.agentClimb = .08f; settings.agentSlope = 20;
        settings.overrideVoxelSize = true; settings.voxelSize = .08f; settings.minRegionArea = .1f;
        var baked = NavMeshBuilder.BuildNavMeshData(settings, sources, bounds, Vector3.zero, Quaternion.identity);
        if (!baked) throw new InvalidOperationException("Navigation bake failed.");
        string path = Folder + "/MotherIndoorNavigation.asset";
        var asset = AssetDatabase.LoadAssetAtPath<NavMeshData>(path);
        if (!asset) { baked.name = "Mother indoor navigation - authored layout"; AssetDatabase.CreateAsset(baked, path); asset = baked; }
        else { EditorUtility.CopySerialized(baked, asset); UnityEngine.Object.DestroyImmediate(baked); EditorUtility.SetDirty(asset); }
        Undo.RecordObject(actor, "Bind own refreshed navigation data");
        var serialized = new SerializedObject(actor); serialized.FindProperty("navigation").objectReferenceValue = asset; serialized.ApplyModifiedProperties();
        Undo.FlushUndoRecordObjects();
        AssetDatabase.SaveAssets(); EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        Debug.Log($"[MotherHome] Refreshed own navigation from {sources.Count} render sources, radius={settings.agentRadius}, height={settings.agentHeight}; no transforms changed.");
    }

    [MenuItem("KuTy/Scene 2/Prepare reviewed mother walk assets")]
    public static void Prepare()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode first.");
        const string source = "Tools/MotherHome/MomUser_Walk_Home.fbx";
        if (!File.Exists(source) || !File.Exists("Tools/MotherHome/walk-export.json")) throw new InvalidOperationException("Review and export the correct MomUser walk first.");
        Directory.CreateDirectory(Folder);
        if (!File.Exists(Walk)) File.Copy(source, Walk);
        else if (!File.ReadAllBytes(source).SequenceEqual(File.ReadAllBytes(Walk))) throw new InvalidOperationException("Existing walk differs; refusing overwrite.");
        AssetDatabase.ImportAsset(Walk, ImportAssetOptions.ForceSynchronousImport);
        var importer = AssetImporter.GetAtPath(Walk) as ModelImporter;
        var avatar = AssetDatabase.LoadAllAssetsAtPath("Assets/_Project/Characters/MomUserDemo/SK_MomUser.fbx").OfType<Avatar>().Single();
        importer.animationType = ModelImporterAnimationType.Generic;
        importer.importAnimation = true; importer.avatarSetup = ModelImporterAvatarSetup.CopyFromOther; importer.sourceAvatar = avatar;
        importer.importCameras = false; importer.importLights = false; importer.materialImportMode = ModelImporterMaterialImportMode.None;
        importer.globalScale = 1; importer.useFileScale = true; importer.optimizeGameObjects = false;
        importer.animationCompression = ModelImporterAnimationCompression.Off; importer.resampleCurves = true;
        var takes = importer.defaultClipAnimations;
        if (takes.Length != 1) throw new InvalidOperationException("Walk must contain exactly one take.");
        takes[0].name = "Mother_Walk"; takes[0].loopTime = true; takes[0].loopPose = false;
        takes[0].lockRootRotation = true; takes[0].lockRootPositionXZ = true; takes[0].lockRootHeightY = false;
        takes[0].keepOriginalOrientation = true; takes[0].keepOriginalPositionY = true; takes[0].keepOriginalPositionXZ = true;
        importer.clipAnimations = takes; importer.SaveAndReimport();
        var clip = AssetDatabase.LoadAllAssetsAtPath(Walk).OfType<AnimationClip>().Single(c => !c.name.StartsWith("__"));
        if (Mathf.Abs(clip.length - 1.6f) > .04f) throw new InvalidOperationException("Wrong walk duration: " + clip.length);
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(Controller);
        if (!controller)
        {
            if (!AssetDatabase.CopyAsset("Assets/_Project/Characters/MomUserDemo/MomUser.controller", Controller)) throw new IOException("Cannot clone the standing controller.");
            controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(Controller);
            var walk = controller.layers[0].stateMachine.AddState("Mother_Walk");
            walk.motion = clip; walk.writeDefaultValues = false;
            EditorUtility.SetDirty(controller); AssetDatabase.SaveAssets();
        }
        if (!controller.layers[0].stateMachine.states.Any(s => s.state.name == "Mother_Walk" && s.state.motion == clip))
            throw new InvalidOperationException("Existing home controller has unexpected walk state; not overwritten.");
        Debug.Log("[MotherHome] Reviewed walk imported with the existing Generic avatar; separate controller, original model/prefab untouched.");
    }

    [MenuItem("KuTy/Scene 2/Enable mother walking and conversations (keep layout)")]
    public static void Bind()
    {
        var scene = SceneManager.GetActiveScene();
        if (EditorApplication.isPlayingOrWillChangePlaymode || scene.path != Home || scene.isDirty)
            throw new InvalidOperationException("Open saved Home in Edit Mode; unrelated edits are never silently saved.");
        var companion = UnityEngine.Object.FindObjectOfType<Scene2MotherCompanion>();
        var actor = companion ? companion.Actor : null;
        var presentation = actor ? actor.GetComponent<Scene2MotherDemo>() : null;
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(Controller);
        if (!presentation || !presentation.Animator || !controller) throw new InvalidOperationException("Verified actor and reviewed home controller required.");
        if (actor.GetComponent<Scene2MotherHomeMovement>()) throw new InvalidOperationException("Movement already bound; inspect rather than replace authored configuration.");
        var poses = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Transform>(true))
            .ToDictionary(t => t, t => (t.localPosition, t.localRotation, t.localScale));
        Directory.CreateDirectory("Builds/MotherHome-20261108");
        string backup = "Builds/MotherHome-20261108/Home-before-bind-" + DateTime.Now.ToString("HHmmss") + ".unity";
        File.Copy(Home, backup, false);
        var points = new List<Scene2MotherHomeMovement.ActivityPoint>();
        var simulator = companion.GetComponent<Scene2HouseholdSimulator>();
        if (simulator)
        {
            var stations = new SerializedObject(simulator).FindProperty("stations");
            for (int i = 0; i < stations.arraySize; i++)
            {
                var station = stations.GetArrayElementAtIndex(i);
                var anchor = station.FindPropertyRelative("surface").objectReferenceValue as Transform;
                if (anchor) points.Add(new Scene2MotherHomeMovement.ActivityPoint { chore = (HouseholdChore)station.FindPropertyRelative("chore").intValue, anchor = anchor });
            }
        }
        var kitchen = companion.GetComponent<Scene2KitchenHandsOn>();
        if (kitchen)
        {
            points.Add(new Scene2MotherHomeMovement.ActivityPoint { chore = HouseholdChore.WashRice, anchor = kitchen.washRoot });
            points.Add(new Scene2MotherHomeMovement.ActivityPoint { chore = HouseholdChore.CookRice, anchor = kitchen.hearth });
        }
        string navigationPath = Folder + "/MotherIndoorNavigation.asset";
        var navigation = AssetDatabase.LoadAssetAtPath<NavMeshData>(navigationPath);
        if (!navigation)
        {
            var sources = new List<NavMeshBuildSource>();
            var bounds = new Bounds(actor.position + Vector3.up * 1.5f, new Vector3(38, 7, 30));
            NavMeshBuilder.CollectSources(bounds, ~0, NavMeshCollectGeometry.RenderMeshes, 0, new List<NavMeshBuildMarkup>(), sources);
            var player = UnityEngine.Object.FindObjectOfType<FirstPersonController>();
            sources.RemoveAll(s => s.component && (s.component.transform.IsChildOf(actor) || player && s.component.transform.IsChildOf(player.transform)));
            var settings = NavMesh.GetSettingsByIndex(0);
            var capsule = actor.GetComponent<CapsuleCollider>();
            settings.agentRadius = capsule.radius * Mathf.Max(actor.lossyScale.x, actor.lossyScale.z) + .035f;
            settings.agentHeight = capsule.height * actor.lossyScale.y;
            settings.agentClimb = .08f; settings.agentSlope = 20f;
            settings.overrideVoxelSize = true; settings.voxelSize = .08f; settings.minRegionArea = .1f;
            navigation = NavMeshBuilder.BuildNavMeshData(settings, sources, bounds, Vector3.zero, Quaternion.identity);
            if (!navigation) throw new InvalidOperationException("No navigation geometry built; room was not changed.");
            navigation.name = "Mother indoor navigation - authored layout";
            AssetDatabase.CreateAsset(navigation, navigationPath); AssetDatabase.SaveAssets();
        }
        Undo.IncrementCurrentGroup(); int group = Undo.GetCurrentGroup(); Undo.SetCurrentGroupName("Mother indoor walking; preserve layout");
        try
        {
            var movement = Undo.AddComponent<Scene2MotherHomeMovement>(actor.gameObject);
            movement.Configure(companion, points.ToArray(), presentation.Animator.transform.lossyScale.y / 6f, navigation);
            Undo.RecordObject(presentation.Animator, "Use separate mother home controller");
            presentation.Animator.runtimeAnimatorController = controller;
            PrefabUtility.RecordPrefabInstancePropertyModifications(presentation.Animator);
            foreach (var pose in poses)
                if (pose.Key.localPosition != pose.Value.localPosition || pose.Key.localRotation != pose.Value.localRotation || pose.Key.localScale != pose.Value.localScale)
                    throw new InvalidOperationException("An authored transform changed; refusing save.");
            EditorUtility.SetDirty(movement); EditorUtility.SetDirty(presentation.Animator);
            Undo.FlushUndoRecordObjects();
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Could not save mother bindings.");
            Undo.CollapseUndoOperations(group);
            Directory.CreateDirectory("Tools/MotherHome");
            File.WriteAllText("Tools/MotherHome/bind-report.txt", $"Home={Home}\nActor={actor.name}\nScale={actor.localScale}\nSpeed={movement.WalkSpeed}\nActivityAnchors={points.Count}\nExistingTransformsUnchanged={poses.Count}\nBackup={backup}\n");
            Debug.Log("[MotherHome] Bound safe runtime navigation and reviewed walk. All " + poses.Count + " existing transforms preserved.");
        }
        catch { Undo.RevertAllDownToGroup(group); throw; }
    }
}
