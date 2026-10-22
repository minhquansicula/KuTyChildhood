using System;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public static class OfficeDirectorSignSetup
{
    private const string ParentPath = "OFFICE_BLOCKOUT/02_BossOffice/BossPartitions_SLOT/Visual_REPLACE";
    private const string MaterialFolder = "Assets/_Project/Art/Office/DirectorSign";

    [MenuItem("KuTy/Office/Refine Director Sign")]
    public static void Apply()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play before editing the sign.");
        var parent = GameObject.Find(ParentPath);
        if (parent == null) throw new InvalidOperationException("Open the office scene with BossPartitions_SLOT first.");
        var label = parent.transform.Find("Label_DIRECTOR")?.GetComponent<TextMeshPro>();
        if (label == null) throw new InvalidOperationException("The original director label is missing.");

        Undo.IncrementCurrentGroup();
        int group = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Refine director office sign");
        Directory.CreateDirectory(MaterialFolder);
        AssetDatabase.Refresh();
        var brass = Material("SatinBrass", new Color(.58f, .43f, .24f), .55f, .32f);
        var enamel = Material("MidnightEnamel", new Color(.024f, .055f, .064f), .12f, .22f);
        var edge = Material("DarkBacking", new Color(.018f, .024f, .027f), .1f, .15f);
        var root = Child(parent.transform, "DirectorOfficePlaque");
        Undo.RecordObject(root, "Position plaque");
        root.SetPositionAndRotation(new Vector3(-1.895f, 2.68f, 2.1f), Quaternion.Euler(0, 270, 0));
        root.localScale = Vector3.one;

        Part(root, "Backing", Vector3.zero, new Vector3(1.29f, .45f, .025f), edge);
        Part(root, "BrassFrame", new Vector3(0, 0, -.017f), new Vector3(1.265f, .425f, .018f), brass);
        Part(root, "EnamelFace", new Vector3(0, 0, -.030f), new Vector3(1.229f, .389f, .012f), enamel);
        Part(root, "FineDivider", new Vector3(0, -.058f, -.038f), new Vector3(.82f, .003f, .002f), brass);
        foreach (int side in new[] { -1, 1 })
            Part(root, side < 0 ? "LeftInlay" : "RightInlay", new Vector3(side * .573f, 0, -.038f),
                new Vector3(.006f, .265f, .002f), brass);

        Undo.RecordObject(label, "Style Vietnamese title");
        Undo.RecordObject(label.rectTransform, "Position Vietnamese title");
        label.transform.SetPositionAndRotation(root.TransformPoint(new Vector3(0, .043f, -.040f)), root.rotation);
        label.transform.localScale = Vector3.one;
        Style(label, "PH\u00d2NG GI\u00c1M \u0110\u1ed0C", 1.05f, new Vector2(1.08f, .175f),
            new Color(.96f, .89f, .72f), FontStyles.Bold);

        var subtitleRoot = Child(root, "DepartmentCaption");
        var subtitle = subtitleRoot.GetComponent<TextMeshPro>();
        if (subtitle == null) subtitle = Undo.AddComponent<TextMeshPro>(subtitleRoot.gameObject);
        // TMP replaces an ordinary Transform with a RectTransform when added.
        subtitleRoot = subtitle.transform;
        Undo.RecordObject(subtitle, "Style department caption");
        Undo.RecordObject(subtitleRoot, "Position department caption");
        subtitleRoot.localPosition = new Vector3(0, -.116f, -.040f);
        subtitleRoot.localRotation = Quaternion.identity;
        subtitleRoot.localScale = Vector3.one;
        subtitle.font = label.font;
        Style(subtitle, "BAN GI\u00c1M \u0110\u1ed0C", .43f, new Vector2(1.04f, .082f),
            new Color(.73f, .77f, .74f), FontStyles.Normal);
        subtitle.characterSpacing = 7f;
        subtitle.ForceMeshUpdate();

        EditorUtility.SetDirty(label);
        EditorUtility.SetDirty(subtitle);
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
        Undo.CollapseUndoOperations(group);
        Selection.activeGameObject = root.gameObject;
        SceneView.lastActiveSceneView?.LookAt(root.position, Quaternion.Euler(0, 270, 0), 1.25f);
        Debug.Log($"DIRECTOR_SIGN_REFINED: title={label.text}; fits={!label.isTextOverflowing}; subtitleFits={!subtitle.isTextOverflowing}; 6 simple mesh parts, no colliders or runtime scripts.");
    }

    private static void Style(TextMeshPro text, string content, float size, Vector2 bounds, Color color, FontStyles style)
    {
        if (text.font == null) throw new InvalidOperationException("Vietnamese font is missing.");
        if (!text.font.HasCharacters(content, out uint[] missing, true, true))
            throw new InvalidOperationException("Missing Vietnamese glyphs: " + string.Join(",", missing));
        text.text = content;
        text.fontSize = size;
        text.enableAutoSizing = false;
        text.fontStyle = style;
        text.alignment = TextAlignmentOptions.Center;
        text.enableWordWrapping = false;
        text.overflowMode = TextOverflowModes.Overflow;
        text.characterSpacing = 1.5f;
        text.margin = Vector4.zero;
        text.color = color;
        text.rectTransform.sizeDelta = bounds;
        text.rectTransform.pivot = new Vector2(.5f, .5f);
        text.raycastTarget = false;
        text.ForceMeshUpdate();
        var renderer = text.GetComponent<MeshRenderer>();
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        EditorUtility.SetDirty(text.font);
    }

    private static Transform Child(Transform parent, string name)
    {
        var child = parent.Find(name);
        if (child != null) return child;
        var obj = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(obj, "Create sign element");
        obj.transform.SetParent(parent, false);
        return obj.transform;
    }

    private static Material Material(string name, Color color, float metallic, float smoothness)
    {
        string path = MaterialFolder + "/" + name + ".mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            AssetDatabase.CreateAsset(material, path);
        }
        Undo.RecordObject(material, "Style plaque material");
        material.SetColor("_BaseColor", color);
        material.SetFloat("_Metallic", metallic);
        material.SetFloat("_Smoothness", smoothness);
        EditorUtility.SetDirty(material);
        return material;
    }

    private static void Part(Transform parent, string name, Vector3 position, Vector3 scale, Material material)
    {
        var part = parent.Find(name);
        if (part == null)
        {
            var obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obj.name = name;
            Undo.RegisterCreatedObjectUndo(obj, "Create plaque mesh");
            obj.transform.SetParent(parent, false);
            Undo.DestroyObjectImmediate(obj.GetComponent<Collider>());
            part = obj.transform;
        }
        Undo.RecordObject(part, "Fit plaque mesh");
        part.localPosition = position;
        part.localRotation = Quaternion.identity;
        part.localScale = scale;
        var renderer = part.GetComponent<MeshRenderer>();
        Undo.RecordObject(renderer, "Assign plaque finish");
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = true;
    }

    [MenuItem("KuTy/Office/Preview Director Sign")]
    public static void Preview()
    {
        var root = GameObject.Find(ParentPath + "/DirectorOfficePlaque");
        if (root == null) throw new InvalidOperationException("Apply the director plaque first.");
        var obj = new GameObject("DirectorSignPreviewCamera") { hideFlags = HideFlags.HideAndDontSave };
        var camera = obj.AddComponent<Camera>();
        var target = new RenderTexture(1500, 850, 24);
        var image = new Texture2D(1500, 850, TextureFormat.RGB24, false);
        var previous = RenderTexture.active;
        try
        {
            obj.transform.SetPositionAndRotation(root.transform.TransformPoint(new Vector3(.12f, -.06f, -2.15f)), root.transform.rotation);
            camera.fieldOfView = 25f;
            camera.nearClipPlane = .02f;
            camera.farClipPlane = 30f;
            camera.targetTexture = target;
            camera.Render();
            RenderTexture.active = target;
            image.ReadPixels(new Rect(0, 0, 1500, 850), 0, 0);
            image.Apply();
            string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Temp/opencode/director-sign-review.png");
            File.WriteAllBytes(path, image.EncodeToPNG());
            Debug.Log("DIRECTOR_SIGN_PREVIEW: " + path);
        }
        finally
        {
            RenderTexture.active = previous;
            camera.targetTexture = null;
            UnityEngine.Object.DestroyImmediate(obj);
            UnityEngine.Object.DestroyImmediate(image);
            target.Release();
            UnityEngine.Object.DestroyImmediate(target);
        }
    }
}
