using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

/// <summary>Editor-only art pass. Generated geometry is baked into shared mesh assets.</summary>
public static class OfficeBossPartitionSetup
{
    private const string ParentPath = "OFFICE_BLOCKOUT/02_BossOffice/BossPartitions_SLOT/Visual_REPLACE";
    private const string Folder = "Assets/_Project/Art/Office/BossPartition";
    private const string RootName = "ExecutivePartition_Art";

    [MenuItem("KuTy/Office/Refine Boss Partition")]
    public static void Apply()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play before applying the partition art pass.");
        var parent = GameObject.Find(ParentPath)?.transform;
        if (parent == null) throw new InvalidOperationException("Open the office scene first.");
        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Refine executive office partition");
        Directory.CreateDirectory(Folder);
        AssetDatabase.Refresh();

        var metal = Finish("GraphiteAluminium", new Color(.21f, .245f, .25f), .35f, .36f);
        var recess = Finish("ShadowRecess", new Color(.028f, .036f, .038f), .05f, .18f);
        var wood = Finish("WarmWalnut", new Color(.48f, .32f, .205f), .0f, .27f);
        wood.SetTexture("_BaseMap", WoodGrain());
        var bronze = Finish("ChampagneInlay", new Color(.56f, .43f, .27f), .4f, .34f);
        var glass = Finish("ArchitecturalGlass", new Color(.48f, .68f, .71f, .13f), .05f, .86f, true);
        var frost = Finish("EtchedPrivacyFilm", new Color(.69f, .78f, .77f, .72f), .0f, .26f, true);
        // A small ambient contribution keeps etched film legible in the existing dark office.
        frost.EnableKeyword("_EMISSION");
        frost.SetColor("_EmissionColor", new Color(.12f, .15f, .145f));
        metal.EnableKeyword("_EMISSION");
        metal.SetColor("_EmissionColor", new Color(.012f, .015f, .015f));
        var wall = Finish("WarmMineralPanel", new Color(.43f, .47f, .45f), .0f, .22f);
        var diffuser = Finish("LinearLightDiffuser", new Color(.87f, .84f, .73f), 0, .2f);
        diffuser.EnableKeyword("_EMISSION");
        diffuser.SetColor("_EmissionColor", new Color(.75f, .69f, .54f));

        var root = Child(parent, RootName);
        root.localPosition = Vector3.zero;
        root.localRotation = Quaternion.identity;
        root.localScale = Vector3.one;
        var frameMesh = new MeshBuilder();
        var shadowMesh = new MeshBuilder();
        var woodMesh = new MeshBuilder();
        var trimMesh = new MeshBuilder();
        var glassMesh = new MeshBuilder();
        var filmMesh = new MeshBuilder();
        var diffuserMesh = new MeshBuilder();

        // Replace only the original front visuals. Dedicated collision volumes remain in place.
        foreach (Transform original in parent)
        {
            if (original.name != "FrontLower" && original.name != "FrontGlass" && original.name != "GlassFrame") continue;
            var renderer = original.GetComponent<Renderer>();
            if (renderer == null) continue;
            Undo.RecordObject(renderer, "Replace blockout surface");
            renderer.enabled = false;
        }

        // The full-height structure uses real section depths and softened edges.
        frameMesh.Box(new Vector3(-4.5f, .065f, 1f), new Vector3(5.08f, .13f, .18f), .012f);
        frameMesh.Box(new Vector3(-4.5f, .91f, 1f), new Vector3(5.08f, .075f, .16f), .007f);
        frameMesh.Box(new Vector3(-4.5f, 2.92f, 1f), new Vector3(5.08f, .09f, .16f), .009f);
        frameMesh.Box(new Vector3(-4.5f, 2.73f, 1f), new Vector3(5.02f, .027f, .11f), .004f);
        shadowMesh.Box(new Vector3(-4.5f, .5f, 1f), new Vector3(5f, .77f, .115f), .004f);
        for (int i = 0; i <= 4; i++)
        {
            float x = -7f + i * 1.25f;
            frameMesh.Box(new Vector3(x, 1.51f, 1f), new Vector3(i == 0 || i == 4 ? .07f : .045f, 2.86f, .15f), .007f);
            // A restrained bronze bead on the corridor side, continuous with the sign finish.
            trimMesh.Box(new Vector3(x, 1.91f, .920f), new Vector3(.006f, 1.9f, .005f), .001f);
        }

        for (int bay = 0; bay < 4; bay++)
        {
            float x = -6.375f + bay * 1.25f;
            woodMesh.Box(new Vector3(x, .495f, 1f), new Vector3(1.176f, .69f, .132f), .009f);
            // Narrow recessed border, cabinet-like reveal, and contrasting cap.
            trimMesh.Box(new Vector3(x, .824f, .928f), new Vector3(1.13f, .006f, .005f), .001f);
            trimMesh.Box(new Vector3(x, .824f, 1.072f), new Vector3(1.13f, .006f, .005f), .001f);
            shadowMesh.Box(new Vector3(x, .18f, .928f), new Vector3(1.13f, .01f, .004f), .001f);

            // Thin glass has one surface per pane: no overlapping transparent cube faces.
            glassMesh.Quad(new Vector3(x, 1.91f, 1f), new Vector2(1.197f, 1.945f));
            filmMesh.Quad(new Vector3(x, 1.575f, .995f), new Vector2(1.192f, .43f));
            foreach (float y in new[] { 1.29f, 1.315f, 1.835f, 1.86f })
                filmMesh.Quad(new Vector3(x, y, .994f), new Vector2(1.192f, .006f));
            // Small clear/etched rhythm at the band edge, rather than a flat opaque wall.
            for (int tick = 0; tick < 23; tick++)
                filmMesh.Quad(new Vector3(x - .55f + tick * .05f, 1.355f, .994f), new Vector2(.003f, .014f));
        }

        // Wrap the existing doorway and return wall, preserving their exact opening.
        foreach (string name in new[] { "EastUpperWall", "EastSmallWall", "DoorHeader" })
        {
            var renderer = parent.Find(name)?.GetComponent<Renderer>();
            if (renderer == null) continue;
            Undo.RecordObject(renderer, "Finish return wall");
            renderer.sharedMaterial = wall;
        }
        foreach (float z in new[] { 1.37f, 2.83f })
            frameMesh.Box(new Vector3(-2f, 1.2f, z), new Vector3(.195f, 2.4f, .062f), .008f);
        frameMesh.Box(new Vector3(-2f, 2.395f, 2.1f), new Vector3(.205f, .055f, 1.51f), .007f);
        foreach (float side in new[] { -.105f, .105f })
        {
            float x = -2f + side;
            woodMesh.Box(new Vector3(x, .49f, 3.925f), new Vector3(.045f, .7f, 2.08f), .006f);
            woodMesh.Box(new Vector3(x, .49f, 1.17f), new Vector3(.045f, .7f, .32f), .006f);
            trimMesh.Box(new Vector3(x + Mathf.Sign(side) * .026f, .84f, 3.925f), new Vector3(.006f, .009f, 2.08f), .001f);
            frameMesh.Box(new Vector3(x, .07f, 3.925f), new Vector3(.065f, .14f, 2.1f), .005f);
            frameMesh.Box(new Vector3(x, .07f, 1.17f), new Vector3(.065f, .14f, .32f), .005f);
            // Vertical joints break up the plaster return at a deliberate module width.
            shadowMesh.Box(new Vector3(x + Mathf.Sign(side) * .024f, 1.92f, 3.9f), new Vector3(.003f, 2.12f, .008f), .0005f);
        }
        frameMesh.Box(new Vector3(-2f, 2.945f, 3.9f), new Vector3(.20f, .065f, 2.22f), .006f);
        frameMesh.Box(new Vector3(-2f, 2.945f, 1.2f), new Vector3(.20f, .065f, .4f), .006f);
        // A single shadowless practical wash reveals the bevels and timber in this dim corridor.
        frameMesh.Box(new Vector3(-4.5f, 2.965f, -.4f), new Vector3(3.7f, .035f, .09f), .006f);
        diffuserMesh.Box(new Vector3(-4.5f, 2.945f, -.4f), new Vector3(3.61f, .006f, .055f), .002f);

        Bake(root, "01_AluminiumStructure", frameMesh, metal);
        Bake(root, "02_ShadowReveals", shadowMesh, recess);
        Bake(root, "03_WalnutPanels", woodMesh, wood);
        Bake(root, "04_ChampagneDetails", trimMesh, bronze);
        Bake(root, "05_ClearGlazing", glassMesh, glass, true);
        Bake(root, "06_EtchedPrivacy", filmMesh, frost, true);
        Bake(root, "07_LinearLightDiffuser", diffuserMesh, diffuser);
        GlassLettering(root, parent);
        var wash = Child(root, "CorridorSoftWash");
        var light = wash.GetComponent<Light>();
        if (light == null) light = Undo.AddComponent<Light>(wash.gameObject);
        Undo.RecordObject(wash, "Aim partition wash");
        Undo.RecordObject(light, "Set partition wash");
        wash.position = new Vector3(-4.5f, 2.86f, -.4f);
        wash.rotation = Quaternion.LookRotation(new Vector3(0, -1.35f, 1.4f));
        light.type = LightType.Spot;
        light.color = new Color(1f, .91f, .77f);
        light.intensity = 2.2f;
        light.range = 6;
        light.spotAngle = 115;
        light.innerSpotAngle = 70;
        light.shadows = LightShadows.None;
        light.bounceIntensity = 0;
        light.lightmapBakeType = LightmapBakeType.Realtime;

        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Undo.CollapseUndoOperations(undoGroup);
        Selection.activeGameObject = root.gameObject;
        if (SceneView.lastActiveSceneView != null)
        {
            SceneView.lastActiveSceneView.drawGizmos = false;
            SceneView.lastActiveSceneView.LookAt(new Vector3(-4.1f, 1.5f, 1.8f), Quaternion.Euler(8, -32, 0), 5.3f);
        }
        int triangles = root.GetComponentsInChildren<MeshFilter>().Sum(f => f.sharedMesh != null ? f.sharedMesh.triangles.Length / 3 : 0);
        Debug.Log($"BOSS_PARTITION_REFINED: {triangles} triangles, {root.GetComponentsInChildren<Renderer>().Length} renderers; shared baked meshes, no new colliders or runtime scripts.");
    }

    private static Transform Child(Transform parent, string name)
    {
        var child = parent.Find(name);
        if (child != null) return child;
        var obj = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(obj, "Create partition art");
        obj.transform.SetParent(parent, false);
        return obj.transform;
    }

    private static Material Finish(string name, Color color, float metallic, float smoothness, bool transparent = false)
    {
        string path = Folder + "/" + name + ".mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = name };
            AssetDatabase.CreateAsset(material, path);
        }
        Undo.RecordObject(material, "Finish partition material");
        material.SetColor("_BaseColor", color);
        material.SetFloat("_Metallic", metallic);
        material.SetFloat("_Smoothness", smoothness);
        if (transparent)
        {
            // Thin architectural glass should not show an oversized analytic light disc.
            material.SetFloat("_SpecularHighlights", 0);
            material.EnableKeyword("_SPECULARHIGHLIGHTS_OFF");
            material.SetFloat("_Surface", 1);
            material.SetFloat("_Blend", 0);
            material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
            material.SetFloat("_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_ZWrite", 0);
            material.SetFloat("_Cull", (float)CullMode.Off);
            material.SetOverrideTag("RenderType", "Transparent");
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            material.renderQueue = name.Contains("Privacy") ? 3010 : 3000;
            material.SetShaderPassEnabled("ShadowCaster", false);
        }
        material.enableInstancing = true;
        EditorUtility.SetDirty(material);
        return material;
    }

    private static Texture2D WoodGrain()
    {
        string path = Folder + "/WalnutGrain.png";
        if (!File.Exists(path))
        {
            var texture = new Texture2D(256, 512, TextureFormat.RGB24, false);
            var pixels = new Color[256 * 512];
            for (int y = 0; y < 512; y++)
                for (int x = 0; x < 256; x++)
                {
                    float u = x / 256f, v = y / 512f;
                    float warp = Mathf.PerlinNoise(u * 5f, v * 2f) * 2.6f;
                    float grain = Mathf.PerlinNoise(u * 72f + warp, v * 3f);
                    float fine = Mathf.PerlinNoise(u * 190f + warp, v * 11f);
                    float value = .66f + grain * .26f + fine * .08f;
                    pixels[y * 256 + x] = new Color(value, value * .95f, value * .87f, 1);
                }
            texture.SetPixels(pixels);
            texture.Apply();
            File.WriteAllBytes(path, texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.wrapMode = TextureWrapMode.Mirror;
            importer.maxTextureSize = 512;
            importer.mipmapEnabled = true;
            importer.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }

    private static void Bake(Transform root, string name, MeshBuilder builder, Material material, bool transparent = false)
    {
        string path = Folder + "/" + name + ".asset";
        var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (mesh == null)
        {
            mesh = new Mesh { name = name };
            AssetDatabase.CreateAsset(mesh, path);
        }
        Undo.RecordObject(mesh, "Bake partition geometry");
        mesh.Clear();
        mesh.SetVertices(builder.vertices);
        mesh.SetNormals(builder.normals);
        mesh.SetUVs(0, builder.uv);
        mesh.SetTriangles(builder.indices, 0);
        mesh.RecalculateBounds();
        mesh.RecalculateTangents();
        EditorUtility.SetDirty(mesh);
        var node = Child(root, name);
        var filter = node.GetComponent<MeshFilter>();
        if (filter == null) filter = Undo.AddComponent<MeshFilter>(node.gameObject);
        var renderer = node.GetComponent<MeshRenderer>();
        if (renderer == null) renderer = Undo.AddComponent<MeshRenderer>(node.gameObject);
        Undo.RecordObject(filter, "Assign baked mesh");
        Undo.RecordObject(renderer, "Assign partition finish");
        filter.sharedMesh = mesh;
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = transparent ? ShadowCastingMode.Off : ShadowCastingMode.On;
        renderer.receiveShadows = !transparent;
        renderer.reflectionProbeUsage = ReflectionProbeUsage.BlendProbes;
        renderer.lightProbeUsage = LightProbeUsage.BlendProbes;
    }

    private static void GlassLettering(Transform root, Transform parent)
    {
        var font = parent.Find("Label_DIRECTOR")?.GetComponent<TMP_Text>()?.font;
        if (font == null) throw new InvalidOperationException("Director sign Vietnamese font missing.");
        var node = Child(root, "EtchedDepartmentMark");
        var text = node.GetComponent<TextMeshPro>();
        if (text == null) text = Undo.AddComponent<TextMeshPro>(node.gameObject);
        Undo.RecordObject(text, "Etch department name");
        Undo.RecordObject(text.transform, "Position glass lettering");
        text.transform.localPosition = new Vector3(-5.125f, 1.575f, .984f);
        text.transform.localRotation = Quaternion.identity;
        text.transform.localScale = Vector3.one;
        text.font = font;
        text.text = "BAN GI\u00c1M \u0110\u1ed0C";
        text.fontSize = .63f;
        text.characterSpacing = 3;
        text.fontStyle = FontStyles.Normal;
        text.alignment = TextAlignmentOptions.Center;
        text.rectTransform.sizeDelta = new Vector2(1.04f, .18f);
        text.enableWordWrapping = false;
        text.color = new Color(.85f, .88f, .81f, .95f);
        text.raycastTarget = false;
        text.ForceMeshUpdate();
        text.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
        EditorUtility.SetDirty(text);
    }

    internal sealed class MeshBuilder
    {
        public readonly List<Vector3> vertices = new List<Vector3>();
        public readonly List<Vector3> normals = new List<Vector3>();
        public readonly List<Vector2> uv = new List<Vector2>();
        public readonly List<int> indices = new List<int>();

        public void Quad(Vector3 center, Vector2 size)
        {
            int start = vertices.Count;
            foreach (var corner in new[] { new Vector2(-1, -1), new Vector2(-1, 1), new Vector2(1, 1), new Vector2(1, -1) })
            {
                vertices.Add(center + new Vector3(corner.x * size.x * .5f, corner.y * size.y * .5f, 0));
                normals.Add(Vector3.back);
                uv.Add((corner + Vector2.one) * .5f);
            }
            indices.AddRange(new[] { start, start + 1, start + 2, start, start + 2, start + 3 });
        }

        public void Box(Vector3 center, Vector3 size, float radius)
        {
            Vector3 half = size * .5f;
            float bevel = Mathf.Min(radius, Mathf.Min(half.x, Mathf.Min(half.y, half.z)) * .8f);
            Vector3 core = half - Vector3.one * bevel;
            // Four samples per axis produce tiny chamfers with continuous corner normals.
            for (int axis = 0; axis < 3; axis++)
                for (int sign = -1; sign <= 1; sign += 2)
                {
                    int a = (axis + 1) % 3, b = (axis + 2) % 3;
                    int start = vertices.Count;
                    for (int v = 0; v < 4; v++)
                        for (int u = 0; u < 4; u++)
                        {
                            Vector3 p = Vector3.zero;
                            p[axis] = sign * half[axis];
                            p[a] = Sample(u, half[a], bevel);
                            p[b] = Sample(v, half[b], bevel);
                            var closest = new Vector3(Mathf.Clamp(p.x, -core.x, core.x), Mathf.Clamp(p.y, -core.y, core.y), Mathf.Clamp(p.z, -core.z, core.z));
                            Vector3 normal = (p - closest).normalized;
                            Vector3 point = center + closest + normal * bevel;
                            vertices.Add(point);
                            normals.Add(normal);
                            uv.Add(axis == 0 ? new Vector2(point.z * 1.4f, point.y * .7f) : new Vector2(point.x * 1.4f, (axis == 1 ? point.z : point.y) * .7f));
                        }
                    for (int v = 0; v < 3; v++)
                        for (int u = 0; u < 3; u++)
                        {
                            int i = start + v * 4 + u;
                            if (sign > 0) indices.AddRange(new[] { i, i + 1, i + 5, i, i + 5, i + 4 });
                            else indices.AddRange(new[] { i, i + 5, i + 1, i, i + 4, i + 5 });
                        }
                }
        }

        private static float Sample(int index, float half, float bevel)
        {
            return index == 0 ? -half : index == 1 ? -half + bevel : index == 2 ? half - bevel : half;
        }
    }

    [MenuItem("KuTy/Office/Preview Boss Partition")]
    public static void Preview()
    {
        Capture("boss-partition-front", new Vector3(-4.5f, 1.8f, -.65f), new Vector3(-4.5f, 1.48f, 1f), 94);
        Capture("boss-partition-corner", new Vector3(.2f, 2.1f, -.65f), new Vector3(-3.9f, 1.48f, 2f), 60);
        Capture("boss-partition-detail", new Vector3(-5.1f, 1.65f, -.55f), new Vector3(-5.1f, 1.45f, 1f), 54);
    }

    internal static void Capture(string name, Vector3 position, Vector3 target, float fov)
    {
        var obj = new GameObject("PartitionReviewCamera") { hideFlags = HideFlags.HideAndDontSave };
        var camera = obj.AddComponent<Camera>();
        var rt = new RenderTexture(1600, 1000, 24);
        var image = new Texture2D(1600, 1000, TextureFormat.RGB24, false);
        var previous = RenderTexture.active;
        try
        {
            obj.transform.position = position;
            obj.transform.LookAt(target);
            camera.fieldOfView = fov;
            camera.nearClipPlane = .025f;
            camera.farClipPlane = 180f;
            camera.targetTexture = rt;
            camera.Render();
            RenderTexture.active = rt;
            image.ReadPixels(new Rect(0, 0, 1600, 1000), 0, 0);
            image.Apply();
            string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Temp/opencode/" + name + ".png");
            File.WriteAllBytes(path, image.EncodeToPNG());
            Debug.Log("BOSS_PARTITION_PREVIEW: " + path);
        }
        finally
        {
            RenderTexture.active = previous;
            camera.targetTexture = null;
            UnityEngine.Object.DestroyImmediate(obj);
            UnityEngine.Object.DestroyImmediate(image);
            rt.Release();
            UnityEngine.Object.DestroyImmediate(rt);
        }
    }
}
