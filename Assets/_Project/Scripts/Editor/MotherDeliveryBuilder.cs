using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace KuTy.EditorTools
{
    // Only affects the dedicated delivery folder; existing scene assets are untouched.
    public sealed class MotherDeliveryImporter : AssetPostprocessor
    {
        internal const string Folder = "Assets/_Project/Characters/Mother";

        void OnPreprocessModel()
        {
            if (!assetPath.StartsWith(Folder + "/", StringComparison.Ordinal)) return;
            var importer = (ModelImporter)assetImporter;
            importer.animationType = ModelImporterAnimationType.Generic;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.motionNodeName = "Root";
            importer.importCameras = false;
            importer.importLights = false;
            importer.materialImportMode = ModelImporterMaterialImportMode.None;
            importer.isReadable = false;
            importer.maxBonesPerVertex = 4;
            importer.skinWeights = ModelImporterSkinWeights.Custom;
            importer.animationCompression = ModelImporterAnimationCompression.Off;
            importer.importAnimation = assetPath.Contains("/Animations/");
        }

        void OnPreprocessAnimation()
        {
            if (!assetPath.StartsWith(Folder + "/Animations/", StringComparison.Ordinal)) return;
            var importer = (ModelImporter)assetImporter;
            var clips = importer.defaultClipAnimations;
            var name = Path.GetFileNameWithoutExtension(assetPath);
            foreach (var clip in clips)
            {
                clip.name = name;
                clip.loopTime = name == "Mother_Idle_Standing" || name == "Mother_Idle_Seated" ||
                                name == "Mother_PrepVegetables" || name == "Mother_Walk";
                clip.loopPose = false;
                clip.lockRootRotation = true;
                clip.lockRootHeightY = true;
                clip.lockRootPositionXZ = true;
                clip.keepOriginalOrientation = true;
                clip.keepOriginalPositionY = true;
                clip.keepOriginalPositionXZ = true;
            }
            importer.clipAnimations = clips;
        }
    }

    public static class MotherDeliveryBuilder
    {
        const string Folder = MotherDeliveryImporter.Folder;

        [MenuItem("Tools/KuTy/Build Mother Delivery")]
        public static void Build()
        {
            var source = Path.GetFullPath("Tools/MotherDelivery");
            Directory.CreateDirectory(Folder + "/Animations");
            Directory.CreateDirectory(Folder + "/Textures");
            Directory.CreateDirectory(Folder + "/Materials");
            File.Copy(source + "/Exports/SK_Mother.fbx", Folder + "/SK_Mother.fbx", true);
            foreach (var file in Directory.GetFiles(source + "/Exports/Animations", "*.fbx"))
                File.Copy(file, Folder + "/Animations/" + Path.GetFileName(file), true);
            foreach (var file in Directory.GetFiles(source + "/Textures", "*.png"))
            {
                if (file.EndsWith("_BaseColor.png") || file.EndsWith("_Normal.png") || file.EndsWith("_MetallicSmoothness.png"))
                    File.Copy(file, Folder + "/Textures/" + Path.GetFileName(file), true);
            }
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { Folder + "/Textures" }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                importer.textureType = path.EndsWith("_Normal.png") ? TextureImporterType.NormalMap : TextureImporterType.Default;
                importer.sRGBTexture = path.EndsWith("_BaseColor.png");
                importer.alphaSource = TextureImporterAlphaSource.FromInput;
                importer.alphaIsTransparency = false;
                importer.maxTextureSize = 2048;
                importer.SaveAndReimport();
            }
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) throw new InvalidOperationException("URP/Lit shader not available");
            foreach (var part in new[] { "Skin", "Blouse", "Trousers", "Hair", "Eyes" })
            {
                var name = "Mother_" + part;
                var path = Folder + "/Materials/" + name + ".mat";
                var material = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (material == null)
                {
                    material = new Material(shader);
                    AssetDatabase.CreateAsset(material, path);
                }
                material.SetTexture("_BaseMap", Texture(name + "_BaseColor"));
                material.SetColor("_BaseColor", Color.white);
                material.SetTexture("_BumpMap", Texture(name + "_Normal"));
                material.SetFloat("_BumpScale", part == "Hair" ? .18f : .65f);
                material.SetTexture("_MetallicGlossMap", Texture(name + "_MetallicSmoothness"));
                material.SetFloat("_Smoothness", 1);
                material.EnableKeyword("_NORMALMAP");
                material.EnableKeyword("_METALLICSPECGLOSSMAP");
                material.SetFloat("_Surface", 0);
                EditorUtility.SetDirty(material);
            }
            var controllerPath = Folder + "/Mother.controller";
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
            if (controller == null) controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
            var machine = controller.layers[0].stateMachine;
            foreach (var old in machine.states) machine.RemoveState(old.state);
            foreach (var file in Directory.GetFiles(Folder + "/Animations", "*.fbx").OrderBy(f => f))
            {
                var clip = AssetDatabase.LoadAllAssetsAtPath(file.Replace('\\', '/')).OfType<AnimationClip>().FirstOrDefault(c => !c.name.StartsWith("__"));
                if (clip == null) throw new InvalidOperationException("No clip: " + file);
                var state = machine.AddState(clip.name);
                state.motion = clip;
                state.writeDefaultValues = false;
                if (clip.name == "Mother_Idle_Standing") machine.defaultState = state;
            }
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(Folder + "/SK_Mother.fbx");
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(model);
            try
            {
                instance.name = "Mother";
                var animator = instance.GetComponent<Animator>() ?? instance.AddComponent<Animator>();
                animator.runtimeAnimatorController = controller;
                animator.applyRootMotion = false;
                foreach (var renderer in instance.GetComponentsInChildren<SkinnedMeshRenderer>())
                {
                    renderer.sharedMaterials = renderer.sharedMaterials.Select(m =>
                    {
                        var name = m == null ? "" : m.name;
                        var part = name.Contains("Skin") ? "Skin" : name.Contains("Hair") ? "Hair" :
                                   name.Contains("Eye") ? "Eyes" : name.Contains("Blouse") ? "Blouse" : "Trousers";
                        // Material import is disabled, so fall back to the mesh name.
                        if (m == null || !name.StartsWith("Mother_", StringComparison.Ordinal))
                            part = renderer.name.Contains("Skin") ? "Skin" : renderer.name.Contains("Hair") ? "Hair" :
                                   renderer.name.Contains("Eye") ? "Eyes" : renderer.name.Contains("Blouse") ? "Blouse" : "Trousers";
                        return AssetDatabase.LoadAssetAtPath<Material>(Folder + "/Materials/Mother_" + part + ".mat");
                    }).ToArray();
                    renderer.quality = SkinQuality.Bone4;
                }
                PrefabUtility.SaveAsPrefabAsset(instance, Folder + "/Mother.prefab");
                var renderers = instance.GetComponentsInChildren<SkinnedMeshRenderer>();
                var hips = instance.GetComponentsInChildren<Transform>().Single(t => t.name == "Hips");
                var restHips = hips.position;
                var sit = machine.states.Select(s => s.state.motion).OfType<AnimationClip>().Single(c => c.name == "Mother_SitDown");
                sit.SampleAnimation(instance, sit.length);
                var hipDrop = restHips.y - hips.position.y;
                if (hipDrop < .25f || hipDrop > .55f)
                    throw new InvalidOperationException("SitDown animation is not bound correctly: hip drop=" + hipDrop);
                var report = "Meshes=" + renderers.Length + "\nClips=" + machine.states.Length +
                             "\nAvatar=" + (animator.avatar != null && animator.avatar.isValid) +
                             "\nTriangles=" + renderers.Sum(r => r.sharedMesh.triangles.Length / 3) +
                             "\nSitDownHipDrop=" + hipDrop;
                File.WriteAllText(source + "/Source/unity_validation.txt", report);
                if (renderers.Length != 6 || machine.states.Length != 10 || animator.avatar == null || !animator.avatar.isValid)
                    throw new InvalidOperationException("Mother validation failed: " + report);
            }
            finally { UnityEngine.Object.DestroyImmediate(instance); }
            AssetDatabase.SaveAssets();
            AssetDatabase.ExportPackage(Folder, source + "/Mother_Unity_URP.unitypackage", ExportPackageOptions.Recurse);
            Debug.Log("MOTHER_DELIVERY_OK: " + Folder + "/Mother.prefab");
        }

        static Texture2D Texture(string name) => AssetDatabase.LoadAssetAtPath<Texture2D>(Folder + "/Textures/" + name + ".png");
    }
}
