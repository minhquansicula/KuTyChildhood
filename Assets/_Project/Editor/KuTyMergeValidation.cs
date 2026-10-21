using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Checks the committed scene assets without depending on local art-pass baselines.</summary>
public static class KuTyMergeValidation
{
    [MenuItem("KuTy/Tests/Validate merged build scenes")]
    public static void ValidateScenes()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play Mode first.");
        if (SceneManager.GetActiveScene().isDirty) throw new InvalidOperationException("Save current scene first.");
        string original = SceneManager.GetActiveScene().path;
        var report = new StringBuilder();
        int issues = 0;
        try
        {
            foreach (var entry in EditorBuildSettings.scenes.Where(x => x.enabled))
            {
                Scene scene = EditorSceneManager.OpenScene(entry.path);
                int before = issues;
                foreach (Match guid in Regex.Matches(File.ReadAllText(entry.path), @"guid: ([a-fA-F0-9]{32})"))
                {
                    string value = guid.Groups[1].Value;
                    if (value.StartsWith("0000000000000000")) continue;
                    if (string.IsNullOrEmpty(AssetDatabase.GUIDToAssetPath(value)))
                    { report.AppendLine(entry.path + ": missing GUID " + value); issues++; }
                }
                foreach (var root in scene.GetRootGameObjects())
                foreach (var t in root.GetComponentsInChildren<Transform>(true))
                {
                    issues += GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject);
                    foreach (var component in t.GetComponents<Component>().Where(x => x != null))
                    {
                        using (var serialized = new SerializedObject(component))
                        {
                            var property = serialized.GetIterator();
                            while (property.NextVisible(true))
                                if (property.propertyType == SerializedPropertyType.ObjectReference &&
                                    property.objectReferenceValue == null && property.objectReferenceInstanceIDValue != 0)
                                { report.AppendLine(t.name + ": missing " + property.propertyPath); issues++; }
                        }
                    }
                }
                foreach (var cover in UnityEngine.Object.FindObjectsOfType<VillageGroundCoverRenderer>(true))
                {
                    foreach (var plant in cover.plants)
                        if (plant.prototype < 0 || plant.prototype >= cover.prototypes.Length)
                        { report.AppendLine("Invalid vegetation prototype index"); issues++; }
                    foreach (var prototype in cover.prototypes)
                        if (!prototype.near || !prototype.middle || !prototype.far || prototype.materials == null ||
                            prototype.materials.Length == 0 || prototype.materials.Any(m => !m || !m.enableInstancing))
                        { report.AppendLine("Incomplete vegetation prototype: " + prototype.name); issues++; }
                    report.AppendLine($"Vegetation: {cover.plants.Length} plants, {cover.prototypes.Length} prototypes");
                }
                report.AppendLine(entry.path + ": " + (issues == before ? "PASS" : "FAIL"));
            }
            foreach (string guid in AssetDatabase.FindAssets("t:Shader", new[] { "Assets/_Project/Art/Materials/Scene3" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var shader = AssetDatabase.LoadAssetAtPath<Shader>(path);
                foreach (var error in ShaderUtil.GetShaderMessages(shader).Where(m =>
                    m.severity == UnityEditor.Rendering.ShaderCompilerMessageSeverity.Error))
                { report.AppendLine(path + ": " + error.message); issues++; }
            }
        }
        finally
        {
            if (!string.IsNullOrEmpty(original)) EditorSceneManager.OpenScene(original);
            Directory.CreateDirectory("Tools/ProjectAudit");
            report.AppendLine("Total issues: " + issues);
            File.WriteAllText("Tools/ProjectAudit/merge-scene-validation.txt", report.ToString());
        }
        if (issues != 0) throw new InvalidOperationException(report.ToString());
        Debug.Log(report.ToString());
    }

    [MenuItem("KuTy/Tests/Validate merged bamboo LOD rendering")]
    public static void ValidateLod()
    {
        if (EditorApplication.isPlaying || SceneManager.GetActiveScene().isDirty)
            throw new InvalidOperationException("Save scene and exit Play Mode first.");
        string original = SceneManager.GetActiveScene().path;
        var report = new StringBuilder();
        try
        {
            EditorSceneManager.OpenScene("Assets/_Project/Scenes/Act3_MemoryWorld_OutSide.unity");
            var cover = UnityEngine.Object.FindObjectOfType<VillageGroundCoverRenderer>();
            if (!cover) throw new InvalidOperationException("Missing village vegetation renderer.");
            var flags = BindingFlags.NonPublic | BindingFlags.Static;
            typeof(Scene3VillageComposition).GetMethod("ValidateBambooLodLogic", flags).Invoke(null, new object[] { report });
            Directory.CreateDirectory("Tools/Scene3/BambooLod");
            typeof(Scene3VillageComposition).GetMethod("CaptureBambooLodMotion", flags).Invoke(null, new object[] { cover, report });
            var empty = new GameObject("Empty vegetation material regression");
            empty.SetActive(false);
            try
            {
                var probe = empty.AddComponent<VillageGroundCoverRenderer>();
                probe.prototypes = new[] { new VillageGroundCoverRenderer.Prototype {
                    near = cover.prototypes[0].near, materials = new Material[0] } };
                typeof(VillageGroundCoverRenderer).GetMethod("Draw", BindingFlags.NonPublic | BindingFlags.Instance)
                    .Invoke(probe, new object[] { null, 0 });
                report.AppendLine("PASS: missing vegetation materials do not throw during drawing.");
            }
            finally { UnityEngine.Object.DestroyImmediate(empty); }
            File.WriteAllText("Tools/ProjectAudit/merge-lod-validation.txt", report.ToString());
            Debug.Log(report.ToString());
        }
        finally { if (!string.IsNullOrEmpty(original)) EditorSceneManager.OpenScene(original); }
    }

    [MenuItem("KuTy/Tests/Prepare merged office smoke with checkpoint backup")]
    public static void PrepareOffice()
    {
        KuTyCheckpointTestScope.Capture();
        KuTyOfficeBlockoutGameplaySmoke.Prepare();
    }
}
