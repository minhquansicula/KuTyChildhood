// Copy this file to an isolated Unity project's Assets/Editor folder.
// It is a model import audit, not a runtime component or a scene-editing tool.
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class VerifyRiceModels
{
    [Serializable] public class MeshAudit
    {
        public string name;
        public int triangles, vertices, submeshes, materials;
        public Vector3 boundsMin, boundsMax, rootEuler, rootScale;
        public float windMin, windMax, maximumHeightMaskError;
        public bool uv0, uv1, rootAtGround, upright, rootIdentity;
    }
    [Serializable] public class Audit
    {
        public string unityVersion;
        public bool passed;
        public List<MeshAudit> models = new List<MeshAudit>();
    }

    public static void Run()
    {
        var arguments = Environment.GetCommandLineArgs();
        var reportIndex = Array.IndexOf(arguments, "-riceReportPath");
        var reportPath = reportIndex >= 0 ? arguments[reportIndex + 1] : "rice_import_report.json";
        var audit = new Audit { unityVersion = Application.unityVersion, passed = true };
        try
        {
            foreach (var name in new[] { "CayLua_LOD0", "CayLua_LOD1", "CayLua_LOD2" })
            {
                var path = "Assets/RiceModels/" + name + ".fbx";
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
                var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (!asset) throw new Exception("FBX could not be imported: " + name);
                var filters = asset.GetComponentsInChildren<MeshFilter>(true);
                if (filters.Length != 1) throw new Exception("Expected exactly one mesh: " + name);
                var renderer = filters[0].GetComponent<MeshRenderer>();
                var mesh = filters[0].sharedMesh;
                var entry = new MeshAudit
                {
                    name = name,
                    triangles = mesh.triangles.Length / 3,
                    vertices = mesh.vertexCount,
                    submeshes = mesh.subMeshCount,
                    materials = renderer.sharedMaterials.Length,
                    boundsMin = mesh.bounds.min,
                    boundsMax = mesh.bounds.max,
                    rootEuler = asset.transform.localEulerAngles,
                    rootScale = asset.transform.localScale,
                    uv0 = mesh.uv.Length == mesh.vertexCount,
                    uv1 = mesh.uv2.Length == mesh.vertexCount,
                    rootAtGround = Mathf.Abs(mesh.bounds.min.y) < .002f,
                    upright = mesh.bounds.size.y > 1.9f && mesh.bounds.size.y < 2.1f,
                    rootIdentity = Quaternion.Angle(asset.transform.localRotation, Quaternion.identity) < .01f
                        && Vector3.Distance(asset.transform.localScale, Vector3.one) < .001f,
                    windMin = 1,
                    windMax = 0
                };
                var colors = mesh.colors;
                if (colors.Length != mesh.vertexCount) throw new Exception("Missing wind colors: " + name);
                var vertices = mesh.vertices;
                for (var i = 0; i < colors.Length; i++)
                {
                    entry.windMin = Mathf.Min(entry.windMin, colors[i].r);
                    entry.windMax = Mathf.Max(entry.windMax, colors[i].r);
                    var expected = Mathf.Clamp01(vertices[i].y / 2.008863925933838f);
                    entry.maximumHeightMaskError = Mathf.Max(entry.maximumHeightMaskError, Mathf.Abs(expected - colors[i].r));
                }
                audit.models.Add(entry);
                audit.passed &= entry.rootAtGround && entry.upright && entry.rootIdentity
                    && entry.submeshes == 1 && entry.materials == 1 && entry.uv0 && entry.uv1
                    && entry.windMin < .01f && entry.windMax > .99f && entry.maximumHeightMaskError < .01f;
                Debug.Log("[RICE_IMPORT] " + JsonUtility.ToJson(entry));
            }
        }
        catch (Exception exception)
        {
            audit.passed = false;
            Debug.LogException(exception);
        }
        File.WriteAllText(reportPath, JsonUtility.ToJson(audit, true));
        EditorApplication.Exit(audit.passed ? 0 : 1);
    }
}
