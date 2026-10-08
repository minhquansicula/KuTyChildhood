using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class KuTyAct2WallSetup
{
    private const string ScenePath = "Assets/_Project/Scenes/Act2_MemoryWorld_Home.unity";
    private const string RootName = "Act2_RusticWalls";
    private const string TexturePath = "Assets/_Project/Art/Models/Act2Home/house_1/traditional_house_1_3d_model_tripo_part_1_basecolor.JPEG";

    [MenuItem("KuTy/Setup/Create Scene 2 Rustic Walls")]
    public static void CreateWalls()
    {
        var scene = SceneManager.GetActiveScene();
        if (EditorApplication.isPlayingOrWillChangePlaymode || scene.path != ScenePath)
            throw new InvalidOperationException("Open Act2_MemoryWorld_Home in Edit Mode first.");

        var camera = GameObject.Find("Player/PlayerCamera").transform;
        Undo.RecordObject(camera, "Raise Scene 2 camera");
        var cameraPosition = camera.localPosition;
        cameraPosition.y = 1.8f;
        camera.localPosition = cameraPosition;
        PrefabUtility.RecordPrefabInstancePropertyModifications(camera);

        var root = GameObject.Find(RootName);
        if (root == null)
        {
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(TexturePath);
            if (texture == null) throw new InvalidOperationException("Existing house wall texture is missing.");
            // Reuse clean regions of the original wall atlas, preserving its aged plaster and wood colours.
            var plaster = MakeMaterial("Act2Wall_AgedPlaster", texture, Color.white,
                new Vector2(0.26f, 0.28f), new Vector2(0.57f, 0.66f));
            var timber = MakeMaterial("Act2Wall_DarkTimber", texture, new Color(0.68f, 0.61f, 0.54f),
                new Vector2(0.20f, 0.024f), new Vector2(0.18f, 0.55f));
            var baseMaterial = MakeMaterial("Act2Wall_WeatheredBase", texture, new Color(0.66f, 0.60f, 0.51f),
                new Vector2(0.26f, 0.12f), new Vector2(0.57f, 0.68f));

            root = new GameObject(RootName);
            Undo.RegisterCreatedObjectUndo(root, "Create Scene 2 rustic walls");
            root.transform.position = new Vector3(7.75f, 0f, 12.45f);
            MakeWall(root.transform, "Wall_01_AgedPlaster", -3.1f, plaster, timber, baseMaterial);
            var window = MakeWall(root.transform, "Wall_02_WoodenShutters", 0f, plaster, timber, baseMaterial);
            var panelled = MakeWall(root.transform, "Wall_03_TimberWainscot", 3.1f, plaster, timber, baseMaterial);

            // Closed timber shutters sit on the wall face, so this is not a misleading walk-through opening.
            Part(window, "ShutterPanel", new Vector3(0f, 1.86f, -0.13f), new Vector3(1.65f, 1.42f, 0.08f), timber);
            for (int i = 0; i < 8; i++)
                Part(window, "ShutterSlat_" + (i + 1), new Vector3(-0.70f + i * 0.20f, 1.86f, -0.19f),
                    new Vector3(0.16f, 1.24f, 0.065f), timber);
            Part(window, "WindowFrame_Left", new Vector3(-0.87f, 1.86f, -0.20f), new Vector3(0.12f, 1.66f, 0.16f), timber);
            Part(window, "WindowFrame_Right", new Vector3(0.87f, 1.86f, -0.20f), new Vector3(0.12f, 1.66f, 0.16f), timber);
            Part(window, "WindowFrame_Top", new Vector3(0f, 2.63f, -0.20f), new Vector3(1.85f, 0.12f, 0.16f), timber);
            Part(window, "WindowSill", new Vector3(0f, 1.09f, -0.25f), new Vector3(1.98f, 0.12f, 0.34f), timber);
            Part(window, "ShutterCrossbar", new Vector3(0f, 1.60f, -0.24f), new Vector3(1.58f, 0.11f, 0.07f), timber);

            for (int i = 0; i < 10; i++)
                Part(panelled, "LowerTimberPanel_" + (i + 1), new Vector3(-1.25f + i * (2.5f / 9f), 0.68f, -0.14f),
                    new Vector3(0.267f, 0.84f, 0.075f), timber);
            Part(panelled, "WainscotRail", new Vector3(0f, 1.13f, -0.17f), new Vector3(2.9f, 0.12f, 0.13f), timber);
        }

        Selection.activeGameObject = root;
        EditorSceneManager.MarkSceneDirty(scene);
        AssetDatabase.SaveAssets();
        EditorSceneManager.SaveScene(scene);
        Debug.Log("ACT2_WALLS_READY: three aged plaster/timber wall modules; camera height 1.8.");
    }

    private static Material MakeMaterial(string name, Texture2D texture, Color tint, Vector2 scale, Vector2 offset)
    {
        string path = "Assets/_Project/Art/Materials/" + name + ".mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material != null) return material;
        material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = name };
        material.SetTexture("_BaseMap", texture);
        material.SetTextureScale("_BaseMap", scale);
        material.SetTextureOffset("_BaseMap", offset);
        material.SetColor("_BaseColor", tint);
        material.SetFloat("_Smoothness", 0.12f);
        material.SetFloat("_Metallic", 0f);
        AssetDatabase.CreateAsset(material, path);
        return material;
    }

    private static Transform MakeWall(Transform parent, string name, float x, Material plaster, Material timber, Material baseMaterial)
    {
        var wall = new GameObject(name);
        wall.transform.SetParent(parent, false);
        wall.transform.localPosition = new Vector3(x, 0f, 0f);
        Part(wall.transform, "PlasterWall", new Vector3(0f, 1.5f, 0f), new Vector3(3.1f, 3f, 0.22f), plaster, true);
        Part(wall.transform, "WeatheredFooting", new Vector3(0f, 0.16f, 0f), new Vector3(3.12f, 0.32f, 0.27f), baseMaterial);
        Part(wall.transform, "TimberPost_Left", new Vector3(-1.46f, 1.54f, 0f), new Vector3(0.18f, 3.08f, 0.34f), timber);
        Part(wall.transform, "TimberPost_Right", new Vector3(1.46f, 1.54f, 0f), new Vector3(0.18f, 3.08f, 0.34f), timber);
        Part(wall.transform, "TimberTopBeam", new Vector3(0f, 3.0f, 0f), new Vector3(3.12f, 0.18f, 0.34f), timber);
        Part(wall.transform, "TimberBaseRail", new Vector3(0f, 0.31f, -0.02f), new Vector3(3.02f, 0.12f, 0.29f), timber);
        return wall.transform;
    }

    private static void Part(Transform parent, string name, Vector3 position, Vector3 scale, Material material, bool solid = false)
    {
        var part = GameObject.CreatePrimitive(PrimitiveType.Cube);
        part.name = name;
        part.transform.SetParent(parent, false);
        part.transform.localPosition = position;
        part.transform.localScale = scale;
        part.GetComponent<MeshRenderer>().sharedMaterial = material;
        part.layer = LayerMask.NameToLayer("Ground");
        if (!solid) UnityEngine.Object.DestroyImmediate(part.GetComponent<BoxCollider>());
    }

    [MenuItem("KuTy/Setup/Preview Scene 2 Rustic Walls")]
    public static void PreviewWalls()
    {
        var preview = new GameObject("__Act2WallPreview") { hideFlags = HideFlags.HideAndDontSave };
        var camera = preview.AddComponent<Camera>();
        camera.CopyFrom(GameObject.Find("Player/PlayerCamera").GetComponent<Camera>());
        camera.fieldOfView = 75f;
        preview.transform.position = new Vector3(7.75f, 2.1f, 8.5f);
        preview.transform.LookAt(new Vector3(7.75f, 1.55f, 12.45f));
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
            File.WriteAllBytes(Path.GetFullPath("Temp/act2-rustic-walls.png"), image.EncodeToPNG());
        }
        finally
        {
            RenderTexture.active = previous;
            camera.targetTexture = null;
            RenderTexture.ReleaseTemporary(target);
            if (image != null) UnityEngine.Object.DestroyImmediate(image);
            UnityEngine.Object.DestroyImmediate(preview);
        }
    }

    [MenuItem("KuTy/Setup/Inspect Scene 2 Wall Sources")]
    public static void InspectSources()
    {
        var root = GameObject.Find("HouseLayout/TraditionalHouseRoom_Visual");
        var text = new StringBuilder();
        foreach (var renderer in root.GetComponentsInChildren<MeshRenderer>())
        {
            var material = renderer.sharedMaterial;
            var texture = material != null && material.HasProperty("_BaseMap") ? material.GetTexture("_BaseMap") : null;
            text.AppendLine($"{renderer.name}\tcenter={renderer.bounds.center}\tsize={renderer.bounds.size}\tmaterial={material?.name}\ttexture={AssetDatabase.GetAssetPath(texture)}");
        }
        File.WriteAllText(Path.GetFullPath("Temp/act2-wall-sources.txt"), text.ToString());
    }
}
