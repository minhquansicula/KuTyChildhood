using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Integrates the existing exported demo, without touching Blender sources or the source prefab.</summary>
public static class Scene2MotherDemoSetup
{
    public const string PrefabPath = "Assets/_Project/Characters/Mother/Mother.prefab";
    private const string HomePath = "Assets/_Project/Scenes/Act2_MemoryWorld_Home.unity";
    public const string ActorName = "Scene2MotherDemo";

    [MenuItem("KuTy/Scene 2/Inspect mother demo placement")]
    public static void Inspect()
    {
        var kitchen = RequireHome();
        var preview = EditorSceneManager.NewPreviewScene();
        try
        {
            var model = (GameObject)PrefabUtility.InstantiatePrefab(RequirePrefab(), preview);
            SampleIdle(model);
            Bounds bounds = MeshBounds(model);
            var look = model.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "Socket_LookAt");
            var report = new StringBuilder("[MotherDemo] Exported demo audit\n");
            report.AppendLine("Bounds: " + bounds + "; feet=" + bounds.min.y);
            report.AppendLine("Model rotation: " + model.transform.eulerAngles + "; scale=" + model.transform.localScale);
            report.AppendLine("Face forward: " + (look ? look.forward.ToString() : "no look socket"));
            report.AppendLine("Kitchen fire view: " + kitchen.fireView.position + "; hearth=" + kitchen.hearth.position);
            report.AppendLine("Safe standing point: " + FindStandingPoint(kitchen, bounds.size.y));
            Debug.Log(report.ToString());
        }
        finally { EditorSceneManager.ClosePreviewScene(preview); }
    }

    [MenuItem("KuTy/Scene 2/Install mother demo in home")]
    public static void Install()
    {
        var kitchen = RequireHome();
        var scene = SceneManager.GetActiveScene();
        if (scene.isDirty) throw new InvalidOperationException("Save the current Scene 2 edits first. Installation will not discard or silently save unrelated work.");
        if (scene.GetRootGameObjects().Any(g => g.name == ActorName))
            throw new InvalidOperationException("Mother demo is already installed; keep its authored placement instead of creating a duplicate.");
        var flow = UnityEngine.Object.FindObjectOfType<HouseholdChoreFlow>();
        var existing = flow.GetComponent<Scene2MotherCompanion>();
        if (existing)
        {
            var serialized = new SerializedObject(existing);
            if (serialized.FindProperty("actor").objectReferenceValue)
                throw new InvalidOperationException("A mother actor is already assigned. It will not be replaced automatically.");
        }

        var preview = EditorSceneManager.NewPreviewScene();
        Bounds modelBounds;
        float faceYaw;
        try
        {
            var inspection = (GameObject)PrefabUtility.InstantiatePrefab(RequirePrefab(), preview);
            SampleIdle(inspection);
            modelBounds = MeshBounds(inspection);
            var look = inspection.GetComponentsInChildren<Transform>(true).First(t => t.name == "Socket_LookAt");
            faceYaw = Quaternion.LookRotation(Vector3.ProjectOnPlane(look.forward, Vector3.up)).eulerAngles.y;
        }
        finally { EditorSceneManager.ClosePreviewScene(preview); }
        Vector3 feet = FindStandingPoint(kitchen, modelBounds.size.y);

        Directory.CreateDirectory("Assets/_Project/Scenes/Backups");
        string backup = "Assets/_Project/Scenes/Backups/BeforeMotherDemo_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".unity";
        if (!EditorSceneManager.SaveScene(scene, backup, true)) throw new IOException("Could not make a safety copy of Scene 2.");
        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Install Scene 2 mother demo");
        try
        {
            var actor = new GameObject(ActorName);
            Undo.RegisterCreatedObjectUndo(actor, "Mother demo actor");
            actor.transform.position = feet;
            Vector3 facing = Vector3.ProjectOnPlane(kitchen.fireView.position - kitchen.hearth.position, Vector3.up).normalized;
            actor.transform.rotation = Quaternion.LookRotation(facing) * Quaternion.Euler(0, -faceYaw, 0);
            var model = (GameObject)PrefabUtility.InstantiatePrefab(RequirePrefab(), scene);
            Undo.RegisterCreatedObjectUndo(model, "Mother demo model");
            Quaternion importedRotation = model.transform.localRotation;
            Vector3 importedScale = model.transform.localScale;
            Undo.SetTransformParent(model.transform, actor.transform, "Keep exported model axes");
            model.transform.localPosition = Vector3.up * -modelBounds.min.y;
            model.transform.localRotation = importedRotation;
            model.transform.localScale = importedScale;
            // Preserve the FBX's authored axis conversion and unit scale.
            var animator = model.GetComponentInChildren<Animator>();
            animator.applyRootMotion = false;
            animator.updateMode = AnimatorUpdateMode.Normal;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

            var hit = Undo.AddComponent<CapsuleCollider>(actor);
            hit.isTrigger = true;
            hit.height = modelBounds.size.y;
            hit.radius = Mathf.Clamp(Mathf.Min(modelBounds.size.x, modelBounds.size.z) * .5f, .20f, .30f);
            hit.center = new Vector3(0, hit.height * .5f, 0);
            int layer = LayerMask.NameToLayer("Interactable");
            if (layer >= 0) actor.layer = layer;

            var companion = existing ? existing : Undo.AddComponent<Scene2MotherCompanion>(flow.gameObject);
            Undo.RecordObject(companion, "Bind authored mother");
            var binding = new SerializedObject(companion);
            binding.FindProperty("actor").objectReferenceValue = actor.transform;
            binding.FindProperty("animator").objectReferenceValue = animator;
            binding.FindProperty("head").objectReferenceValue = null; // Generic rig: don't assume Humanoid bone axes.
            binding.FindProperty("showPlaceholder").boolValue = false;
            binding.ApplyModifiedProperties();
            var interaction = Undo.AddComponent<Scene2MotherInteractable>(actor);
            interaction.Configure(companion);
            var presentation = Undo.AddComponent<Scene2MotherDemo>(actor);
            presentation.Configure(animator, companion, flow.GetComponent<Scene2RadioKitchenSequence>());

            var voice = new GameObject("Mother demo voice anchor");
            Undo.RegisterCreatedObjectUndo(voice, "Mother demo voice anchor");
            voice.transform.SetParent(actor.transform, false);
            voice.transform.localPosition = Vector3.up * (modelBounds.size.y * .88f);
            var radio = flow.GetComponent<Scene2RadioKitchenSequence>();
            Undo.RecordObject(radio, "Align mother's spatial voice with the actor");
            radio.kitchenSpot = voice.transform;
            Undo.RecordObject(kitchen, "Align kitchen conversation with mother demo");
            kitchen.motherVoiceAnchor = voice.transform;
            EditorUtility.SetDirty(companion);
            EditorUtility.SetDirty(radio);
            EditorUtility.SetDirty(kitchen);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Could not save the integrated home scene.");
            Undo.CollapseUndoOperations(undoGroup);
            Selection.activeGameObject = actor;
            Debug.Log("[MotherDemo] Installed existing demo prefab at " + feet + "; height=" + modelBounds.size.y +
                ". Idle/call animations, E conversation and spatial voice are bound. Source assets were not rebuilt. Backup: " + backup);
        }
        catch { Undo.RevertAllDownToGroup(undoGroup); throw; }
    }

    private static Scene2KitchenHandsOn RequireHome()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play Mode before installing the demo.");
        if (SceneManager.GetActiveScene().path != HomePath) throw new InvalidOperationException("Open Act2_MemoryWorld_Home first.");
        var kitchen = UnityEngine.Object.FindObjectOfType<Scene2KitchenHandsOn>();
        if (!kitchen || !kitchen.fireView || !kitchen.hearth) throw new InvalidOperationException("The authored kitchen is missing.");
        return kitchen;
    }

    public static void CaptureReference(Scene2MotherDemo actor)
    {
        var cameraObject = new GameObject("Mother demo QA camera", typeof(Camera));
        var camera = cameraObject.GetComponent<Camera>();
        if (Camera.main) camera.CopyFrom(Camera.main);
        camera.enabled = false;
        camera.fieldOfView = 48f;
        camera.nearClipPlane = .04f;
        cameraObject.transform.position = actor.transform.position + actor.transform.forward * 2.8f +
            actor.transform.right * .35f + Vector3.up * 1.18f;
        cameraObject.transform.LookAt(actor.transform.position + Vector3.up * .85f);
        var target = RenderTexture.GetTemporary(1280, 720, 24);
        var previous = RenderTexture.active;
        Texture2D image = null;
        try
        {
            camera.targetTexture = target;
            camera.Render();
            RenderTexture.active = target;
            image = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
            image.Apply();
            File.WriteAllBytes("Library/MotherDemoReference.png", image.EncodeToPNG());
        }
        finally
        {
            camera.targetTexture = null;
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(target);
            if (image) UnityEngine.Object.DestroyImmediate(image);
            UnityEngine.Object.DestroyImmediate(cameraObject);
        }
    }

    private static GameObject RequirePrefab()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (!prefab) throw new InvalidOperationException("Exported mother demo prefab is missing.");
        return prefab;
    }

    private static void SampleIdle(GameObject model)
    {
        var animator = model.GetComponentInChildren<Animator>();
        if (!animator || !animator.runtimeAnimatorController || !animator.avatar || !animator.avatar.isValid)
            throw new InvalidOperationException("The demo needs its valid Generic rig and controller.");
        var idle = animator.runtimeAnimatorController.animationClips.First(c => c.name == "Mother_Idle_Standing");
        idle.SampleAnimation(model, 0f);
    }

    private static Bounds MeshBounds(GameObject model)
    {
        var result = new Bounds();
        bool found = false;
        foreach (var renderer in model.GetComponentsInChildren<SkinnedMeshRenderer>(true))
        {
            var mesh = new Mesh();
            try
            {
                renderer.BakeMesh(mesh);
                foreach (var vertex in mesh.vertices)
                {
                    Vector3 point = renderer.transform.TransformPoint(vertex);
                    if (found) result.Encapsulate(point); else { result = new Bounds(point, Vector3.zero); found = true; }
                }
            }
            finally { UnityEngine.Object.DestroyImmediate(mesh); }
        }
        if (!found || result.size.y < .8f || result.size.y > 2.2f)
            throw new InvalidOperationException("Unexpected demo dimensions: " + result);
        return result;
    }

    private static Vector3 FindStandingPoint(Scene2KitchenHandsOn kitchen, float height)
    {
        Physics.SyncTransforms();
        var reference = kitchen.fireView.position;
        var downHits = Physics.RaycastAll(reference + Vector3.up, Vector3.down, 4f, ~0, QueryTriggerInteraction.Ignore)
            .Where(h => h.collider.gameObject.scene == kitchen.gameObject.scene && h.normal.y > .8f && h.point.y < reference.y - .45f)
            .OrderByDescending(h => h.point.y).ToArray();
        if (downHits.Length == 0) throw new InvalidOperationException("Cannot locate the floor beside the kitchen.");
        float floor = downHits[0].point.y;
        Vector3 side = Vector3.ProjectOnPlane(kitchen.fireView.right, Vector3.up).normalized;
        Vector3 outwards = Vector3.ProjectOnPlane(kitchen.fireView.position - kitchen.hearth.position, Vector3.up).normalized;
        foreach (float along in new[] { .95f, 1.25f, -.95f, -1.25f, 1.6f, -1.6f })
        foreach (float back in new[] { .15f, .5f, -.25f })
        {
            Vector3 candidate = reference + side * along + outwards * back;
            var floors = Physics.RaycastAll(new Vector3(candidate.x, floor + .6f, candidate.z), Vector3.down, 1f, ~0, QueryTriggerInteraction.Ignore)
                .Where(h => h.collider.gameObject.scene == kitchen.gameObject.scene && h.normal.y > .8f && Mathf.Abs(h.point.y - floor) < .12f).ToArray();
            if (floors.Length == 0) continue;
            candidate.y = floors.Max(h => h.point.y) + .015f;
            bool blocked = Physics.OverlapCapsule(candidate + Vector3.up * .34f, candidate + Vector3.up * (height - .30f),
                .30f, ~0, QueryTriggerInteraction.Ignore).Any(c => c.gameObject.scene == kitchen.gameObject.scene);
            if (blocked || Vector3.ProjectOnPlane(candidate - kitchen.cookView.position, Vector3.up).magnitude < .8f) continue;
            return candidate;
        }
        throw new InvalidOperationException("No safe standing point found. No scene objects were moved; author a clear place for mother first.");
    }
}
