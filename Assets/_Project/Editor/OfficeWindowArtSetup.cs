using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using MeshBuilder = OfficeBossPartitionSetup.MeshBuilder;

public static class OfficeWindowArtSetup
{
    private const string ShellPath = "OFFICE_BLOCKOUT/01_Architecture/RoomShell_SLOT/Visual_REPLACE";
    private const string Folder = "Assets/_Project/Art/Office/PanoramicWindow";

    [MenuItem("KuTy/Office/Refine Panoramic Windows")]
    public static void Apply()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play before editing the windows.");
        var shell = GameObject.Find(ShellPath)?.transform;
        if (shell == null) throw new InvalidOperationException("Open the office scene first.");
        Undo.IncrementCurrentGroup();
        int group = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Refine panoramic office windows");
        Directory.CreateDirectory(Folder);
        AssetDatabase.Refresh();

        var aluminium = Finish("SatinAluminium", new Color(.27f, .30f, .30f), .5f, .42f);
        var gasket = Finish("RubberAndReveals", new Color(.035f, .045f, .047f), 0, .15f);
        var stone = Finish("WarmLimestoneSill", new Color(.66f, .63f, .55f), 0, .26f);
        var bronze = Finish("ChampagneHardware", new Color(.59f, .48f, .32f), .55f, .4f);
        var fabric = Finish("OatmealSolarBlind", new Color(.63f, .62f, .54f), 0, .12f);
        var glass = Finish("ClearLowIronGlass", new Color(.65f, .79f, .81f, .055f), .0f, .86f, true);
        var plaster = Finish("WindowWallMineral", new Color(.48f, .49f, .45f), 0, .2f);
        var root = Child(shell, "PanoramicWindow_Art");
        Undo.RecordObject(root, "Fit panoramic window assembly");
        root.SetPositionAndRotation(new Vector3(7, 0, 0), Quaternion.Euler(0, 90, 0));
        root.localScale = Vector3.one;

        foreach (Transform original in shell)
        {
            if (original.name != "WindowMullion") continue;
            var renderer = original.GetComponent<Renderer>();
            if (renderer == null) continue;
            Undo.RecordObject(renderer, "Replace blockout mullion");
            renderer.enabled = false;
        }
        foreach (string name in new[] { "EastSill", "EastHeader" })
        {
            var renderer = shell.Find(name)?.GetComponent<Renderer>();
            if (renderer == null) continue;
            Undo.RecordObject(renderer, "Finish window wall");
            renderer.sharedMaterial = plaster;
        }

        var frames = new MeshBuilder();
        var seals = new MeshBuilder();
        var sill = new MeshBuilder();
        var hardware = new MeshBuilder();
        var blinds = new MeshBuilder();
        var glazing = new MeshBuilder();

        // Local X runs along the facade; local -Z is the room side.
        frames.Box(new Vector3(0, .932f, -.025f), new Vector3(9.98f, .065f, .18f), .008f);
        frames.Box(new Vector3(0, 2.667f, -.025f), new Vector3(9.98f, .066f, .18f), .008f);
        seals.Box(new Vector3(0, .985f, -.018f), new Vector3(9.94f, .012f, .075f), .002f);
        seals.Box(new Vector3(0, 2.618f, -.018f), new Vector3(9.94f, .012f, .075f), .002f);
        // A projecting sill, drip-edge reveal, and wall skirting give the opening real depth.
        sill.Box(new Vector3(0, .896f, -.10f), new Vector3(9.98f, .065f, .36f), .012f);
        seals.Box(new Vector3(0, .851f, -.245f), new Vector3(9.96f, .012f, .02f), .002f);
        hardware.Box(new Vector3(0, .87f, -.28f), new Vector3(9.92f, .006f, .006f), .001f);
        frames.Box(new Vector3(0, .065f, -.11f), new Vector3(9.96f, .13f, .04f), .005f);
        for (int post = 0; post <= 5; post++)
        {
            float x = -5 + post * 2;
            frames.Box(new Vector3(x, 1.8f, -.025f), new Vector3(.058f, 1.8f, .19f), .006f);
            seals.Box(new Vector3(x - .037f, 1.8f, -.018f), new Vector3(.008f, 1.64f, .06f), .001f);
            seals.Box(new Vector3(x + .037f, 1.8f, -.018f), new Vector3(.008f, 1.64f, .06f), .001f);
            hardware.Box(new Vector3(x, 1.8f, -.124f), new Vector3(.005f, 1.66f, .005f), .001f);
            if (post > 0 && post < 5)
                seals.Box(new Vector3(x, .47f, -.098f), new Vector3(.006f, .72f, .007f), .001f);
        }

        float[] drops = { .16f, .24f, .12f, .20f, .15f };
        for (int bay = 0; bay < 5; bay++)
        {
            float x = -4 + bay * 2;
            glazing.Quad(new Vector3(x, 1.8f, .012f), new Vector2(1.918f, 1.63f));
            // Recessed cassette, a short lowered blind, and a weighted bottom rail.
            frames.Box(new Vector3(x, 2.675f, -.145f), new Vector3(1.91f, .09f, .13f), .009f);
            blinds.Box(new Vector3(x, 2.625f - drops[bay] * .5f, -.133f), new Vector3(1.855f, drops[bay], .008f), .002f);
            frames.Box(new Vector3(x, 2.625f - drops[bay], -.139f), new Vector3(1.865f, .018f, .025f), .004f);
            hardware.Box(new Vector3(x, 2.675f, -.214f), new Vector3(1.82f, .005f, .004f), .001f);
            // Restrained casement hardware on alternate panels avoids visual clutter.
            if (bay % 2 == 0)
            {
                float handleX = x + .88f;
                hardware.Box(new Vector3(handleX, 1.48f, -.148f), new Vector3(.018f, .15f, .022f), .004f);
                frames.Box(new Vector3(handleX, 1.48f, -.119f), new Vector3(.033f, .105f, .02f), .005f);
            }
            // Small under-sill ventilation slots, grouped into the same opaque mesh.
            for (int vent = 0; vent < 9; vent++)
                seals.Box(new Vector3(x - .24f + vent * .06f, .79f, -.096f), new Vector3(.032f, .009f, .005f), .001f);
        }

        Bake(root, "01_BevelledAluminium", frames, aluminium);
        Bake(root, "02_SealsAndVentDetails", seals, gasket);
        Bake(root, "03_LimestoneSill", sill, stone);
        Bake(root, "04_ChampagneHardware", hardware, bronze);
        Bake(root, "05_SolarBlinds", blinds, fabric);
        Bake(root, "06_ClearGlazing", glazing, glass, true);
        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Undo.CollapseUndoOperations(group);
        Selection.activeGameObject = root.gameObject;
        if (SceneView.lastActiveSceneView != null)
        {
            SceneView.lastActiveSceneView.drawGizmos = false;
            SceneView.lastActiveSceneView.LookAt(new Vector3(7, 1.8f, 0), Quaternion.Euler(0, 90, 0), 6f);
        }
        int triangles = root.GetComponentsInChildren<MeshFilter>().Sum(f => f.sharedMesh.triangles.Length / 3);
        Debug.Log($"OFFICE_WINDOWS_REFINED: {triangles} triangles; 6 renderers; five clear panes with low-iron tint, raised blinds and sill details.");
    }

    private static Transform Child(Transform parent, string name)
    {
        var child = parent.Find(name);
        if (child != null) return child;
        var obj = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(obj, "Create window art");
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
        Undo.RecordObject(material, "Finish window material");
        material.SetColor("_BaseColor", color);
        material.SetFloat("_Metallic", metallic);
        material.SetFloat("_Smoothness", smoothness);
        material.enableInstancing = true;
        if (transparent)
        {
            // Suppress round direct-light highlights without changing the scene lighting
            // or the glass's environment reflections.
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
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.SetOverrideTag("RenderType", "Transparent");
            material.SetShaderPassEnabled("ShadowCaster", false);
            material.SetShaderPassEnabled("DepthOnly", false);
            material.SetShaderPassEnabled("DepthNormals", false);
            material.renderQueue = 3000;
        }
        EditorUtility.SetDirty(material);
        return material;
    }

    private static void Bake(Transform root, string name, MeshBuilder data, Material material, bool transparent = false)
    {
        string path = Folder + "/" + name + ".asset";
        var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (mesh == null)
        {
            mesh = new Mesh { name = name };
            AssetDatabase.CreateAsset(mesh, path);
        }
        Undo.RecordObject(mesh, "Bake window geometry");
        mesh.Clear();
        mesh.SetVertices(data.vertices);
        mesh.SetNormals(data.normals);
        mesh.SetUVs(0, data.uv);
        mesh.SetTriangles(data.indices, 0);
        mesh.RecalculateBounds();
        mesh.RecalculateTangents();
        EditorUtility.SetDirty(mesh);
        var node = Child(root, name);
        var filter = node.GetComponent<MeshFilter>();
        if (filter == null) filter = Undo.AddComponent<MeshFilter>(node.gameObject);
        var renderer = node.GetComponent<MeshRenderer>();
        if (renderer == null) renderer = Undo.AddComponent<MeshRenderer>(node.gameObject);
        Undo.RecordObject(filter, "Assign window mesh");
        Undo.RecordObject(renderer, "Assign window material");
        filter.sharedMesh = mesh;
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = transparent ? ShadowCastingMode.Off : ShadowCastingMode.On;
        renderer.receiveShadows = !transparent;
        renderer.reflectionProbeUsage = ReflectionProbeUsage.BlendProbes;
    }

    [MenuItem("KuTy/Office/Preview Panoramic Windows")]
    public static void Preview()
    {
        OfficeBossPartitionSetup.Capture("office-window-wide", new Vector3(.7f, 1.65f, 0), new Vector3(7, 1.83f, 0), 54);
        OfficeBossPartitionSetup.Capture("office-window-angle", new Vector3(4.3f, 1.68f, -3.9f), new Vector3(7, 1.75f, .1f), 64);
        OfficeBossPartitionSetup.Capture("office-window-detail", new Vector3(5.35f, 1.6f, .2f), new Vector3(7, 1.7f, 0), 63);
    }

    [MenuItem("KuTy/Office/Refresh Window Glazing")]
    public static void RefreshGlazing()
    {
        Finish("ClearLowIronGlass", new Color(.65f, .79f, .81f, .055f), 0, .86f, true);
        AssetDatabase.SaveAssets();
        SceneView.RepaintAll();
    }
}
