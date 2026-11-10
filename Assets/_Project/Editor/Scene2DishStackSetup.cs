using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Additive set dressing only; never moves the existing kitchen or changes washing count.</summary>
public static class Scene2DishStackSetup
{
    public const string Output = "Tools/DishStacks";
    private static Transform Need(string name) => UnityEngine.Object.FindObjectsOfType<Transform>(true)
        .Single(t => t.gameObject.scene == SceneManager.GetActiveScene() && t.name == name);

    [MenuItem("KuTy/Scene 2/Add dish stacks beside washing area (keep layout)")]
    public static void Place()
    {
        var scene = SceneManager.GetActiveScene();
        if (EditorApplication.isPlayingOrWillChangePlaymode || scene.path != "Assets/_Project/Scenes/Act2_MemoryWorld_Home.unity")
            throw new InvalidOperationException("Open Home in Edit Mode first.");
        if (GameObject.Find("Scene2_DishStacks")) throw new InvalidOperationException("Dish stacks already exist; refusing to duplicate or overwrite them.");
        Transform bowl = Need("New_Bowl"), plate = Need("New_Plate"), small = Need("DishBowl");
        Transform fixture = Need("New_WashStation"), left = Need("kecchen"), right = Need("DishCabinetVisual");
        Directory.CreateDirectory(Output);
        Undo.IncrementCurrentGroup(); int undo = Undo.GetCurrentGroup(); Undo.SetCurrentGroupName("Add kitchen dish stacks");
        var root = new GameObject("Scene2_DishStacks").transform;
        Undo.RegisterCreatedObjectUndo(root.gameObject, "Dish stack dressing");
        Transform waiting = Group(root, "01_DirtyWaiting_BesideBasin");
        Transform cleanLeft = Group(root, "02_CleanStorage_LeftShelf");
        Transform cleanRight = Group(root, "03_CleanStorage_RightCabinet");
        var report = new StringBuilder("Additive dish stack dressing — original transforms and gameplay bindings unchanged.\n");
        Stack(waiting, "Waiting_Bowls", bowl, fixture, new Vector2(-3.05f, .29f), .42f, .23f, 4, .037f, true, report);
        Stack(waiting, "Waiting_Plates", plate, fixture, new Vector2(-3.08f, -.58f), .42f, .28f, 6, .012f, true, report);
        Stack(waiting, "Waiting_SmallBowls", small, fixture, new Vector2(-2.97f, -.08f), .42f, .13f, 3, .019f, true, report);
        // Extend existing piles rather than intersecting new piles with the fused original dishes.
        Stack(cleanLeft, "Clean_Plates", plate, left, new Vector2(-6.25f, .36f), 1.23f, .24f, 3, .011f, false, report);
        Stack(cleanLeft, "Clean_Bowls", bowl, left, new Vector2(-6.25f, .38f), 1.63f, .20f, 2, .032f, false, report);
        Stack(cleanRight, "Clean_Bowls", bowl, right, new Vector2(.29f, .20f), 2.12f, .22f, 3, .035f, false, report);
        Stack(cleanRight, "Clean_Plates", plate, right, new Vector2(1.59f, .20f), 2.12f, .28f, 5, .012f, false, report);
        // Editable authoring anchors, not fake interaction buttons or a new automatic reward route.
        var storage = Group(cleanRight, "FreeSpace_ForPuttingAway");
        for (int i = 0; i < 3; i++)
        {
            var slot = Group(storage, new[] { "Bowls_Slot", "Plates_Slot", "SmallBowls_Slot" }[i]);
            slot.position = new Vector3(.68f + i * .28f, 2.06f, .20f);
        }
        report.AppendLine("26 additional ceramic instances; 13 waiting, 13 clean. Original 3 washing items remain separate.");
        report.AppendLine("Free cupboard-top space remains between clean piles. Anchors are authoring-only; PutAwayDishes mechanics are not rebound by this operation.");
        report.AppendLine("Washing order/count, faucet/soap/sponge/rack, source materials and original furniture poses unchanged.");
        Undo.CollapseUndoOperations(undo);
        EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        Capture(new Vector3(-3.1f, 3.0f, -3.3f), new Vector3(-3.6f, .45f, .10f), "WashingStacks-After.png");
        Capture(new Vector3(-6.02f, 1.10f, -1.65f), new Vector3(-6.02f, .88f, .35f), "LeftShelf-After.png");
        Capture(new Vector3(.97f, 2.8f, -2.3f), new Vector3(.97f, 1.35f, .28f), "RightCabinet-After.png");
        EditorSceneManager.SaveScene(scene);
        File.WriteAllText(Output + "/placement-report.txt", report.ToString());
        Selection.activeGameObject = root.gameObject;
        Debug.Log(report.ToString());
    }
    private static Transform Group(Transform parent, string name)
    {
        var t = new GameObject(name).transform; t.SetParent(parent, false); return t;
    }
    private static void Stack(Transform parent, string name, Transform source, Transform support, Vector2 xz, float rayY,
        float width, int count, float pitch, bool dirty, StringBuilder report)
    {
        float y = SupportHeight(support, xz, rayY, width);
        var group = Group(parent, name);
        group.position = new Vector3(xz.x, y, xz.y);
        var sourceBounds = Scene2DishwashingLayout.VisualBounds(source);
        float factor = width / Mathf.Max(sourceBounds.size.x, sourceBounds.size.z);
        for (int i = 0; i < count; i++)
        {
            var clone = UnityEngine.Object.Instantiate(source.gameObject, group).transform;
            clone.name = (dirty ? "Waiting_" : "Clean_") + source.name + "_" + (i + 1).ToString("00");
            foreach (var behaviour in clone.GetComponentsInChildren<MonoBehaviour>(true)) UnityEngine.Object.DestroyImmediate(behaviour);
            foreach (var collider in clone.GetComponentsInChildren<Collider>(true)) UnityEngine.Object.DestroyImmediate(collider);
            clone.gameObject.SetActive(true);
            clone.localScale = source.lossyScale * factor;
            clone.rotation = Quaternion.AngleAxis(i * 17f, Vector3.up) * source.rotation;
            clone.position = source.position;
            var b = Scene2DishwashingLayout.VisualBounds(clone);
            clone.position += new Vector3(xz.x - b.center.x, y + .0015f + i * pitch - b.min.y, xz.y - b.center.z);
            if (dirty && i == count - 1) AddResidue(clone, source, factor);
        }
        report.AppendLine(name + ": " + count + " pieces, base=" + group.position.ToString("F4") + ", width=" + width + ", pitch=" + pitch);
    }
    private static float SupportHeight(Transform source, Vector2 xz, float startY, float width)
    {
        var preview = EditorSceneManager.NewPreviewScene();
        try
        {
            var mesh = source.GetComponent<MeshFilter>();
            var go = new GameObject("Stack support query"); SceneManager.MoveGameObjectToScene(go, preview);
            go.transform.SetPositionAndRotation(mesh.transform.position, mesh.transform.rotation); go.transform.localScale = mesh.transform.lossyScale;
            var query = go.AddComponent<MeshCollider>(); query.sharedMesh = mesh.sharedMesh; Physics.SyncTransforms();
            float maximum = float.NegativeInfinity;
            for (int i = 0; i < 9; i++)
            {
                float a = i * Mathf.PI * 2 / 8;
                Vector3 from = new Vector3(xz.x + (i == 8 ? 0 : Mathf.Cos(a) * width * .34f), startY,
                    xz.y + (i == 8 ? 0 : Mathf.Sin(a) * width * .34f));
                if (query.Raycast(new Ray(from, Vector3.down), out var hit, .6f) && hit.normal.y > .45f)
                    maximum = Mathf.Max(maximum, hit.point.y);
            }
            if (float.IsNegativeInfinity(maximum)) throw new InvalidOperationException("No support surface under " + xz + " at " + source.name);
            return maximum;
        }
        finally { EditorSceneManager.ClosePreviewScene(preview); }
    }
    private static void AddResidue(Transform clone, Transform source, float factor)
    {
        var layout = UnityEngine.Object.FindObjectOfType<Scene2DishwashingLayout>();
        var probe = layout.dishes.First(d => d.source == source);
        string folder = "Assets/_Project/Art/Scene2/DishStackDressing";
        string path = folder + "/ReserveDishResidue.mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (!mat)
        {
            Directory.CreateDirectory(folder); AssetDatabase.Refresh();
            mat = new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = new Color(.24f, .11f, .035f), name = "Reserve dish residue" };
            AssetDatabase.CreateAsset(mat, path);
        }
        for (int i = 0; i < 3; i++)
        {
            var stain = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            UnityEngine.Object.DestroyImmediate(stain.GetComponent<Collider>());
            stain.name = "Reserve food mark";
            stain.transform.position = clone.TransformPoint(probe.points[i]) + clone.TransformDirection(probe.normals[i]) * .0008f;
            stain.transform.rotation = Quaternion.FromToRotation(Vector3.up, clone.TransformDirection(probe.normals[i]));
            stain.transform.localScale = new Vector3(.017f, .0012f, .012f) * factor;
            stain.GetComponent<Renderer>().sharedMaterial = mat;
            stain.transform.SetParent(clone, true);
        }
    }

    [MenuItem("KuTy/Scene 2/Inspect dish stack space")]
    public static void Inspect()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Use Edit Mode.");
        Directory.CreateDirectory(Output);
        var report = new StringBuilder();
        foreach (string name in new[] { "kecchen", "DishCabinetVisual", "New_WashStation" })
        {
            var source = Need(name); Bounds b = Scene2DishwashingLayout.VisualBounds(source);
            report.AppendLine(name + " " + b);
            var mesh = source.GetComponent<MeshFilter>();
            var scene = EditorSceneManager.NewPreviewScene();
            try
            {
                var go = new GameObject("Surface query"); SceneManager.MoveGameObjectToScene(go, scene);
                go.transform.SetPositionAndRotation(mesh.transform.position, mesh.transform.rotation);
                go.transform.localScale = mesh.transform.lossyScale;
                var query = go.AddComponent<MeshCollider>(); query.sharedMesh = mesh.sharedMesh;
                Physics.SyncTransforms();
                foreach (float fx in new[] { -.65f, 0f, .65f }) foreach (float fz in new[] { -.7f, -.3f, .2f })
                {
                    Vector3 start = new Vector3(b.center.x + fx * b.extents.x, b.max.y + .1f, b.center.z + fz * b.extents.z);
                    report.Append("  x=" + start.x.ToString("F3") + " z=" + start.z.ToString("F3") + " surfaces:");
                    for (int i = 0; i < 20 && query.Raycast(new Ray(start, Vector3.down), out var hit, b.size.y + .2f); i++)
                    {
                        report.Append(" " + hit.point.y.ToString("F4") + "(nY=" + hit.normal.y.ToString("F2") + ")");
                        start = hit.point + Vector3.down * .008f;
                    }
                    report.AppendLine();
                }
            }
            finally { EditorSceneManager.ClosePreviewScene(scene); }
            Capture(b.center + new Vector3(0, .25f, -1.9f) * Mathf.Max(1, b.extents.y), b.center, name + "-before.png");
        }
        File.WriteAllText(Output + "/space-inspection.txt", report.ToString());
        Debug.Log(report.ToString());
    }
    private static void Capture(Vector3 position, Vector3 focus, string name)
    {
        var go = new GameObject("Temporary dish stack review") { hideFlags = HideFlags.HideAndDontSave };
        var camera = go.AddComponent<Camera>(); camera.transform.position = position; camera.transform.LookAt(focus);
        camera.fieldOfView = 55; camera.nearClipPlane = .02f; camera.farClipPlane = 40;
        var target = RenderTexture.GetTemporary(1400, 1000, 24);
        var image = new Texture2D(1400, 1000, TextureFormat.RGB24, false); var previous = RenderTexture.active;
        try
        {
            camera.targetTexture = target; camera.Render(); RenderTexture.active = target;
            image.ReadPixels(new Rect(0, 0, 1400, 1000), 0, 0); image.Apply();
            File.WriteAllBytes(Output + "/" + name, image.EncodeToPNG());
        }
        finally { RenderTexture.active = previous; UnityEngine.Object.DestroyImmediate(image); UnityEngine.Object.DestroyImmediate(go); RenderTexture.ReleaseTemporary(target); }
    }
}
