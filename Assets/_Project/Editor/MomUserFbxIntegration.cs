using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Explicit, guarded installation of the user's mom.fbx. Never imports Blender files.</summary>
public static class MomUserFbxIntegration
{
    const string Folder = "Assets/_Project/Characters/MomUserDemo";
    const string OldFolder = "Assets/_Project/Characters/Mother";
    const string Home = "Assets/_Project/Scenes/Act2_MemoryWorld_Home.unity";
    const string Reports = "Tools/MotherDelivery/Source";
    const string ModelPath = Folder + "/SK_MomUser.fbx";
    const string PrefabPath = Folder + "/MomUser.prefab";
    static readonly string[] Kinds = { "Idle", "CallChild", "TalkIdle", "Nod" };
    static readonly string[] Aliases = { "Mother_Idle_Standing", "Mother_CallChild", "Mother_TalkIdle", "Mother_NodApprove" };
    static readonly float[] Durations = { 5f, 3.4f, 5f, 1.8f };

    static void RequireEditMode()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play Mode first.");
    }

    [MenuItem("KuTy/Mom User FBX/1 Audit old references")]
    public static void AuditOldReferences()
    {
        RequireEditMode();
        var text = new StringBuilder("Old asset dependency audit\n");
        int references = 0;
        foreach (string path in AssetDatabase.GetAllAssetPaths())
        {
            if (!path.StartsWith("Assets/", StringComparison.Ordinal) || path.StartsWith(OldFolder + "/", StringComparison.Ordinal)) continue;
            string ext = Path.GetExtension(path).ToLowerInvariant();
            if (ext != ".unity" && ext != ".prefab" && ext != ".asset" && ext != ".controller" && ext != ".overridecontroller" && ext != ".mat") continue;
            var hits = AssetDatabase.GetDependencies(path, true).Where(p => p.StartsWith(OldFolder + "/", StringComparison.Ordinal)).ToArray();
            if (hits.Length == 0) continue;
            references++;
            text.AppendLine(path + " -> " + string.Join(", ", hits));
        }
        text.AppendLine("External referring assets=" + references);
        text.AppendLine("Scene=" + SceneManager.GetActiveScene().path + "; dirty=" + SceneManager.GetActiveScene().isDirty);
        Directory.CreateDirectory(Reports);
        File.WriteAllText(Reports + "/user_mom_unity_old_dependency_audit.txt", text.ToString());
        Debug.Log("[MomUser] " + text);
        if (references != 0) throw new InvalidOperationException("Old Mother assets still have references. Do not delete them.");
    }

    [MenuItem("KuTy/Mom User FBX/2 Configure FBX and build prefab")]
    public static void ConfigureAndBuild()
    {
        RequireEditMode();
        if (!File.Exists(ModelPath)) throw new FileNotFoundException(ModelPath);
        if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath)) throw new InvalidOperationException("New prefab already exists; will not overwrite.");
        foreach (var file in Directory.GetFiles(Folder, "*.blend*", SearchOption.AllDirectories)) throw new InvalidOperationException("Unexpected Blender source in Assets: " + file);
        var importer = (ModelImporter)AssetImporter.GetAtPath(ModelPath);
        ConfigureModel(importer, false);
        importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
        importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
        importer.SaveAndReimport();
        Directory.CreateDirectory(Folder + "/Textures");
        importer.ExtractTextures(Folder + "/Textures");
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        foreach (string path in Directory.GetFiles(Folder + "/Textures", "*.png"))
        {
            var textureImporter = (TextureImporter)AssetImporter.GetAtPath(path.Replace('\\', '/'));
            if (!textureImporter) continue;
            bool normal = path.IndexOf("normal", StringComparison.OrdinalIgnoreCase) >= 0;
            bool data = normal || path.IndexOf("roughness", StringComparison.OrdinalIgnoreCase) >= 0 || path.IndexOf("metallic", StringComparison.OrdinalIgnoreCase) >= 0;
            textureImporter.textureType = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
            textureImporter.sRGBTexture = !data;
            textureImporter.maxTextureSize = 2048;
            textureImporter.mipmapEnabled = true;
            textureImporter.textureCompression = TextureImporterCompression.CompressedHQ;
            textureImporter.SaveAndReimport();
        }
        Directory.CreateDirectory(Folder + "/Materials");
        var shader = Shader.Find("Universal Render Pipeline/Lit");
        if (!shader) throw new InvalidOperationException("URP/Lit shader missing.");
        var material = new Material(shader) { name = "MomUser_Atlas" };
        material.SetTexture("_BaseMap", RequireTexture("texture_pbr_20250901"));
        material.SetColor("_BaseColor", Color.white);
        material.SetTexture("_BumpMap", RequireTexture("texture_pbr_20250901_normal"));
        material.SetFloat("_BumpScale", 1f);
        material.EnableKeyword("_NORMALMAP");
        string packedPath = BuildMetallicSmoothness();
        material.SetTexture("_MetallicGlossMap", AssetDatabase.LoadAssetAtPath<Texture2D>(packedPath));
        material.SetFloat("_Metallic", 1f);
        material.SetFloat("_Smoothness", 1f);
        material.EnableKeyword("_METALLICSPECGLOSSMAP");
        AssetDatabase.CreateAsset(material, Folder + "/Materials/MomUser_Atlas.mat");
        var waistband = new Material(shader) { name = "MomUser_HiddenWaistband" };
        waistband.SetColor("_BaseColor", new Color(.0414f, .0315f, .0286f, 1f));
        waistband.SetFloat("_Smoothness", .12f);
        AssetDatabase.CreateAsset(waistband, Folder + "/Materials/MomUser_HiddenWaistband.mat");
        var model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
        var avatar = AssetDatabase.LoadAllAssetsAtPath(ModelPath).OfType<Avatar>().SingleOrDefault();
        if (!avatar || !avatar.isValid || avatar.isHuman) throw new InvalidOperationException("A valid Generic avatar is required; no Humanoid assumption.");
        var clips = new AnimationClip[4];
        for (int i = 0; i < Kinds.Length; i++)
        {
            string path = Folder + "/Animations/MomUser_" + Kinds[i] + "_Gameplay.fbx";
            var clipImporter = (ModelImporter)AssetImporter.GetAtPath(path);
            ConfigureModel(clipImporter, true);
            clipImporter.avatarSetup = ModelImporterAvatarSetup.CopyFromOther;
            clipImporter.sourceAvatar = avatar;
            clipImporter.materialImportMode = ModelImporterMaterialImportMode.None;
            var take = clipImporter.defaultClipAnimations;
            if (take.Length != 1) throw new InvalidOperationException("Expected one take in " + path + ", found " + take.Length);
            take[0].name = Aliases[i];
            take[0].loopTime = i == 0 || i == 2;
            take[0].loopPose = false;
            take[0].lockRootRotation = true;
            take[0].lockRootHeightY = false;
            take[0].lockRootPositionXZ = true;
            take[0].keepOriginalOrientation = true;
            take[0].keepOriginalPositionY = true;
            take[0].keepOriginalPositionXZ = true;
            clipImporter.clipAnimations = take;
            clipImporter.SaveAndReimport();
            clips[i] = AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().Single(c => !c.name.StartsWith("__", StringComparison.Ordinal));
            if (Mathf.Abs(clips[i].length - Durations[i]) > .04f || Mathf.Abs(clips[i].frameRate - 30f) > .01f)
                throw new InvalidOperationException("Unexpected duration/FPS for " + path + ": " + clips[i].length + "/" + clips[i].frameRate);
        }
        var controller = AnimatorController.CreateAnimatorControllerAtPath(Folder + "/MomUser.controller");
        for (int i = 0; i < clips.Length; i++)
        {
            var state = controller.layers[0].stateMachine.AddState(Aliases[i]);
            state.motion = clips[i];
            state.writeDefaultValues = false;
            if (i == 0) controller.layers[0].stateMachine.defaultState = state;
        }
        var instance = (GameObject)PrefabUtility.InstantiatePrefab(model);
        try
        {
            instance.name = "MomUser";
            var animator = instance.GetComponent<Animator>() ?? instance.AddComponent<Animator>();
            animator.avatar = avatar;
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            foreach (var renderer in instance.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                renderer.sharedMaterials = renderer.sharedMaterials.Select(m => m && m.name.Contains("HiddenWaistband") ? waistband : material).ToArray();
                renderer.quality = SkinQuality.Bone4;
                renderer.updateWhenOffscreen = true;
            }
            var renderers = instance.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            if (renderers.Length != 2 || renderers.Sum(r => r.sharedMesh.triangles.Length / 3) != 50662)
                throw new InvalidOperationException("Unexpected mesh identity/count; refusing to build.");
            var hand = instance.GetComponentsInChildren<Transform>(true).Single(t => t.name == "Hand.R");
            clips[0].SampleAnimation(instance, 0f);
            var baseline = hand.position;
            clips[1].SampleAnimation(instance, clips[1].length * .5f);
            float displacement = Vector3.Distance(hand.position, baseline);
            if (displacement < .08f) throw new InvalidOperationException("CallChild does not bind to the model skeleton: " + displacement);
            clips[0].SampleAnimation(instance, 0f);
            var bounds = MeshBounds(instance);
            if (Mathf.Abs(bounds.size.y - 1.14982f) > .015f) throw new InvalidOperationException("Unexpected model scale: " + bounds.size.y);
            PrefabUtility.SaveAsPrefabAsset(instance, PrefabPath);
            AssetDatabase.SetLabels(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath), new[] { "MomUser", "Source_mom_fbx", "StandingCandidate" });
            File.WriteAllText(Reports + "/user_mom_unity_import_validation.txt",
                "Model=" + ModelPath + "\nPrefab=" + PrefabPath + "\nGenericAvatar=" + avatar.isValid + "\nMeshes=" + renderers.Length +
                "\nTriangles=" + renderers.Sum(r => r.sharedMesh.triangles.Length / 3) + "\nHeight=" + bounds.size.y +
                "\nCallChildHandDisplacement=" + displacement + "\nClips=" + string.Join(", ", clips.Select(c => c.name + ":" + c.length + "s@" + c.frameRate)) +
                "\nSourceCopied=FBX only; textures extracted from embedded FBX; no .blend.\nExcluded=Walk,seated,preview.\n");
        }
        finally { UnityEngine.Object.DestroyImmediate(instance); }
        AssetDatabase.SaveAssets();
        Debug.Log("[MomUser] IMPORT_BUILD_OK: " + PrefabPath);
    }

    static void ConfigureModel(ModelImporter importer, bool animation)
    {
        if (!importer) throw new InvalidOperationException("Missing ModelImporter.");
        importer.animationType = ModelImporterAnimationType.Generic;
        importer.importAnimation = animation;
        importer.importCameras = false;
        importer.importLights = false;
        importer.globalScale = 1f;
        importer.useFileScale = true;
        importer.optimizeGameObjects = false;
        importer.importNormals = ModelImporterNormals.Import;
        importer.importTangents = ModelImporterTangents.CalculateMikk;
        importer.animationCompression = ModelImporterAnimationCompression.Off;
        importer.resampleCurves = true;
        importer.maxBonesPerVertex = 4;
        importer.skinWeights = ModelImporterSkinWeights.Custom;
        importer.isReadable = true;
        importer.addCollider = false;
    }

    static Texture2D RequireTexture(string stem)
    {
        string path = Directory.GetFiles(Folder + "/Textures", "*.png").SingleOrDefault(p => Path.GetFileNameWithoutExtension(p) == stem);
        if (path == null) throw new FileNotFoundException("Embedded texture not extracted: " + stem);
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path.Replace('\\', '/'));
    }

    static string BuildMetallicSmoothness()
    {
        var metallic = new Texture2D(2, 2, TextureFormat.RGBA32, false, true);
        var roughness = new Texture2D(2, 2, TextureFormat.RGBA32, false, true);
        Texture2D packed = null;
        try
        {
            metallic.LoadImage(File.ReadAllBytes(AssetDatabase.GetAssetPath(RequireTexture("texture_pbr_20250901_metallic"))));
            roughness.LoadImage(File.ReadAllBytes(AssetDatabase.GetAssetPath(RequireTexture("texture_pbr_20250901_roughness"))));
            if (metallic.width != roughness.width || metallic.height != roughness.height) throw new InvalidOperationException("PBR texture sizes differ.");
            var m = metallic.GetPixels32(); var r = roughness.GetPixels32();
            for (int i = 0; i < m.Length; i++) m[i] = new Color32(m[i].r, m[i].r, m[i].r, (byte)(255 - r[i].r));
            packed = new Texture2D(metallic.width, metallic.height, TextureFormat.RGBA32, false, true);
            packed.SetPixels32(m); packed.Apply();
            string path = Folder + "/Textures/MomUser_MetallicSmoothness.png";
            File.WriteAllBytes(path, packed.EncodeToPNG());
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.sRGBTexture = false; importer.maxTextureSize = 2048; importer.SaveAndReimport();
            return path;
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(metallic); UnityEngine.Object.DestroyImmediate(roughness);
            if (packed) UnityEngine.Object.DestroyImmediate(packed);
        }
    }

    [MenuItem("KuTy/Mom User FBX/3 Bind verified model in home")]
    public static void BindInHome()
    {
        RequireEditMode();
        var scene = SceneManager.GetActiveScene();
        if (scene.path != Home || scene.isDirty) throw new InvalidOperationException("Open the clean home scene; do not silently save/discard unrelated edits.");
        var actors = scene.GetRootGameObjects().Where(o => o.name == "Scene2MotherDemo").ToArray();
        if (actors.Length != 1) throw new InvalidOperationException("Expected exactly the existing mother interaction root.");
        var actor = actors[0];
        if (actor.GetComponentsInChildren<SkinnedMeshRenderer>(true).Length != 0) throw new InvalidOperationException("An existing model is present; inspect before replacing it.");
        var companion = UnityEngine.Object.FindObjectOfType<Scene2MotherCompanion>();
        var radio = UnityEngine.Object.FindObjectOfType<Scene2RadioKitchenSequence>();
        var kitchen = UnityEngine.Object.FindObjectOfType<Scene2KitchenHandsOn>();
        var presentation = actor.GetComponent<Scene2MotherDemo>();
        if (!companion || !radio || !kitchen || !presentation || companion.Actor != actor.transform) throw new InvalidOperationException("Mother bindings do not match the audited scene root.");
        string backup = "Tools/MotherDelivery/Backups/BeforeMomUserBind_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".unity";
        Directory.CreateDirectory(Path.GetDirectoryName(backup));
        File.Copy(Home, backup, false);
        var model = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath), scene);
        Undo.RegisterCreatedObjectUndo(model, "Bind user's mother FBX");
        model.name = "MomUser_Model";
        model.transform.SetParent(actor.transform, false);
        var animator = model.GetComponent<Animator>();
        var idle = animator.runtimeAnimatorController.animationClips.Single(c => c.name == Aliases[0]);
        idle.SampleAnimation(model, 0f);
        var head = model.GetComponentsInChildren<Transform>(true).Single(t => t.name == "Head");
        Vector3 localFace = actor.transform.InverseTransformDirection(head.forward); localFace.y = 0;
        float correction = Mathf.Atan2(localFace.x, localFace.z) * Mathf.Rad2Deg;
        model.transform.localRotation = Quaternion.AngleAxis(-correction, Vector3.up) * model.transform.localRotation;
        var bounds = MeshBounds(model);
        model.transform.position += Vector3.up * (actor.transform.position.y - bounds.min.y);
        Undo.RecordObject(presentation, "Bind user's animator");
        presentation.Configure(animator, companion, radio);
        Undo.RecordObject(companion, "Bind user's animator");
        var serialized = new SerializedObject(companion);
        serialized.FindProperty("animator").objectReferenceValue = animator;
        serialized.FindProperty("head").objectReferenceValue = null;
        serialized.FindProperty("showPlaceholder").boolValue = false;
        serialized.ApplyModifiedProperties();
        var collider = actor.GetComponent<CapsuleCollider>();
        Undo.RecordObject(collider, "Fit mother's interaction trigger");
        collider.height = bounds.size.y; collider.radius = .20f; collider.center = Vector3.up * (bounds.size.y * .5f);
        var voice = actor.transform.Find("Mother demo voice anchor");
        if (voice) { Undo.RecordObject(voice, "Fit mother's spatial voice"); voice.localPosition = Vector3.up * (bounds.size.y * .88f); }
        EditorUtility.SetDirty(presentation); EditorUtility.SetDirty(companion);
        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Could not save the home scene.");
        Selection.activeGameObject = actor;
        File.WriteAllText(Reports + "/user_mom_unity_scene_binding.txt", "Scene=" + Home + "\nActor=" + actor.name + "\nPosition=" + actor.transform.position +
            "\nSourcePrefab=" + PrefabPath + "\nModelHeight=" + bounds.size.y + "\nFaceYawCorrection=" + correction +
            "\nExistingInteractionAndVoiceRetained=True\nSceneBackup=" + backup + "\n");
        Debug.Log("[MomUser] SCENE_BIND_OK; existing interaction root/voice retained.");
    }

    [MenuItem("KuTy/Mom User FBX/4 Verify scene and capture")]
    public static void VerifySceneAndCapture()
    {
        RequireEditMode();
        var actor = GameObject.Find("Scene2MotherDemo");
        var presentation = actor ? actor.GetComponent<Scene2MotherDemo>() : null;
        if (!presentation || !presentation.Animator) throw new InvalidOperationException("New animator is not bound.");
        var animator = presentation.Animator;
        if (!AssetDatabase.GetAssetPath(animator.runtimeAnimatorController).StartsWith(Folder + "/", StringComparison.Ordinal))
            throw new InvalidOperationException("Wrong controller/source model.");
        if (actor.GetComponentsInChildren<SkinnedMeshRenderer>(true).Length != 2 || !animator.avatar.isValid || animator.isHuman)
            throw new InvalidOperationException("Wrong mesh count or avatar.");
        foreach (var renderer in actor.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            if (renderer.sharedMaterials.Any(m => !m || !m.shader || !m.shader.isSupported || m.shader.name != "Universal Render Pipeline/Lit"))
                throw new InvalidOperationException("Missing/incorrect URP material.");
        Scene2MotherDemoSetup.CaptureReference(presentation);
        Directory.CreateDirectory("Tools/MotherDelivery/Preview/UserMomAnimation/Unity");
        File.Copy("Library/MotherDemoReference.png", "Tools/MotherDelivery/Preview/UserMomAnimation/Unity/MomUser_InHome.png", true);
        Debug.Log("[MomUser] SCENE_VISUAL_BINDINGS_OK; captured MomUser_InHome.png");
    }

    static Vector3 callProbeHand;
    static Scene2MotherDemo RuntimeMother()
    {
        if (!Application.isPlaying) throw new InvalidOperationException("Runtime probe requires Play Mode.");
        var actor = GameObject.Find("Scene2MotherDemo");
        var presentation = actor ? actor.GetComponent<Scene2MotherDemo>() : null;
        if (!presentation || !presentation.enabled || !presentation.Animator) throw new InvalidOperationException("Runtime mother is not active/bound.");
        return presentation;
    }

    public static bool RuntimeBindingsValid()
    {
        var mother = RuntimeMother();
        return mother.Animator.avatar && mother.Animator.avatar.isValid && !mother.Animator.isHuman &&
            mother.Animator.HasState(0, Animator.StringToHash("Base Layer.Mother_Idle_Standing")) &&
            mother.Animator.HasState(0, Animator.StringToHash("Base Layer.Mother_CallChild")) &&
            mother.GetComponentsInChildren<SkinnedMeshRenderer>(true).Length == 2;
    }

    public static bool RuntimeBeginCall()
    {
        var mother = RuntimeMother();
        callProbeHand = mother.GetComponentsInChildren<Transform>(true).Single(t => t.name == "Hand.R").position;
        mother.Greet();
        return mother.IsCalling;
    }

    public static bool RuntimeCallMoved()
    {
        var mother = RuntimeMother();
        float distance = Vector3.Distance(callProbeHand, mother.GetComponentsInChildren<Transform>(true).Single(t => t.name == "Hand.R").position);
        string evidence = "IsCalling=" + mother.IsCalling + "; hand displacement=" + distance + "; state=" + mother.Animator.GetCurrentAnimatorStateInfo(0).shortNameHash;
        File.WriteAllText(Reports + "/user_mom_unity_runtime_call.txt", evidence);
        Scene2MotherDemoSetup.CaptureReference(mother);
        File.Copy("Library/MotherDemoReference.png", "Tools/MotherDelivery/Preview/UserMomAnimation/Unity/MomUser_RuntimeCall.png", true);
        return mother.IsCalling && distance > .03f && mother.Animator.GetCurrentAnimatorStateInfo(0).IsName("Mother_CallChild");
    }

    public static bool RuntimeIdleRestored()
    {
        var mother = RuntimeMother();
        bool valid = !mother.IsCalling && mother.Animator.GetCurrentAnimatorStateInfo(0).IsName("Mother_Idle_Standing");
        File.WriteAllText(Reports + "/user_mom_unity_runtime_idle.txt", "ReturnedToIdle=" + valid + "; IsCalling=" + mother.IsCalling);
        return valid;
    }

    static Bounds MeshBounds(GameObject obj)
    {
        bool found = false; var bounds = new Bounds();
        foreach (var renderer in obj.GetComponentsInChildren<SkinnedMeshRenderer>(true))
        {
            var mesh = new Mesh();
            try
            {
                renderer.BakeMesh(mesh);
                foreach (var vertex in mesh.vertices)
                {
                    var p = renderer.transform.TransformPoint(vertex);
                    if (found) bounds.Encapsulate(p); else { bounds = new Bounds(p, Vector3.zero); found = true; }
                }
            }
            finally { UnityEngine.Object.DestroyImmediate(mesh); }
        }
        if (!found) throw new InvalidOperationException("No skinned geometry found.");
        return bounds;
    }
}
