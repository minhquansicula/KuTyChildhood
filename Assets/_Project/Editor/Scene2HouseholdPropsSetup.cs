using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class Scene2HouseholdPropsSetup
{
    const string Root = "Assets/_Project/Art/Scene2";
    const string ScenePath = "Assets/_Project/Scenes/Act2_MemoryWorld_Home.unity";
    static readonly string[] Names = { "Radio", "Refrigerator", "PottedPlant", "Certificate", "GrainPile", "Rake", "Broom" };
    static readonly string[] Categories = { "Bedroom", "Kitchen", "Yard", "Bedroom", "Yard", "Yard", "Yard" };
    static string ModelPath(int i) => Root + "/Models/" + Categories[i] + "/SM_" + Names[i] + ".fbx";
    static string MaterialPath(int i) => Root + "/Materials/" + Categories[i] + "/MAT_" + Names[i] + ".mat";

    [MenuItem("KuTy/Scene 2/Prepare household props and inspect layout")]
    public static void Prepare()
    {
        RequireScene();
        var report = new StringBuilder();
        for (int i = 0; i < Names.Length; i++)
        {
            string textureFolder = Root + "/Textures/" + Categories[i] + "/" + Names[i];
            Directory.CreateDirectory(textureFolder);
            AssetDatabase.Refresh();
            var importer = (ModelImporter)AssetImporter.GetAtPath(ModelPath(i));
            if (!importer) throw new InvalidOperationException("Model missing: " + ModelPath(i));
            importer.ExtractTextures(textureFolder);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            var textures = AssetDatabase.FindAssets("t:Texture2D", new[] { textureFolder })
                .Select(AssetDatabase.GUIDToAssetPath).ToArray();
            var colors = textures.Where(p => {
                var n = Path.GetFileNameWithoutExtension(p).ToLowerInvariant();
                return !n.Contains("normal") && !n.Contains("metallic") && !n.Contains("roughness") && !n.Contains("occlusion") && !n.Contains("height");
            }).ToArray();
            if (colors.Length != 1) throw new InvalidOperationException("Expected one original color atlas for " + Names[i] + ", found " + colors.Length);
            var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath(i));
            if (!material)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(material, MaterialPath(i));
            }
            material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(colors[0]));
            material.SetColor("_BaseColor", Color.white);
            material.SetFloat("_Smoothness", .2f);
            string normal = textures.FirstOrDefault(p => p.ToLowerInvariant().Contains("normal"));
            if (normal != null)
            {
                var ti = (TextureImporter)AssetImporter.GetAtPath(normal);
                if (ti.textureType != TextureImporterType.NormalMap)
                {
                    ti.textureType = TextureImporterType.NormalMap;
                    ti.SaveAndReimport();
                }
                material.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>(normal));
                material.EnableKeyword("_NORMALMAP");
            }
            EditorUtility.SetDirty(material);
            foreach (var embedded in AssetDatabase.LoadAllAssetsAtPath(ModelPath(i)).OfType<Material>())
                importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), embedded.name), material);
            importer.SaveAndReimport();
            var preview = EditorSceneManager.NewPreviewScene();
            try
            {
                var go = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath(i)), preview);
                report.AppendLine("MODEL " + Names[i] + " scale=" + go.transform.localScale + " bounds=" + BoundsOf(go));
                report.AppendLine("COLOR " + colors[0]);
            }
            finally { EditorSceneManager.ClosePreviewScene(preview); }
        }
        foreach (var go in SceneManager.GetActiveScene().GetRootGameObjects())
            Inspect(go.transform, 0, report);
        File.WriteAllText("Library/Scene2HouseholdPropsReport.txt", report.ToString());
        AssetDatabase.SaveAssets();
        Debug.Log("HOUSEHOLD_PROPS_PREPARED: 7 models with original color textures. Report in Library/Scene2HouseholdPropsReport.txt");
    }

    [MenuItem("KuTy/Scene 2/Place household props")]
    public static void Place()
    {
        RequireScene();
        var bedroom = GameObject.Find("BedroomArea/BedroomDecor");
        var kitchen = GameObject.Find("HouseLayout/KitchenArea");
        var house = GameObject.Find("HouseLayout");
        var chest = GameObject.Find("BedroomArea/BedroomDecor/StorageChest");
        if (!bedroom || !kitchen || !house || !chest) throw new InvalidOperationException("Expected room anchors missing.");
        foreach (string name in Names)
            if (GameObject.Find(name)) throw new InvalidOperationException("Already exists: " + name);
        var yard = house.transform.Find("YardProps");
        if (!yard)
        {
            var go = new GameObject("YardProps");
            Undo.RegisterCreatedObjectUndo(go, "Add Scene 2 yard props");
            go.transform.SetParent(house.transform, false);
            yard = go.transform;
        }
        var chestBounds = BoundsOf(chest);
        Put(0, bedroom.transform, new Vector3(chestBounds.center.x, chestBounds.max.y + .01f, chestBounds.center.z), new Vector3(0, 180, 0), .62f);
        Put(1, kitchen.transform, new Vector3(2.32f, .02f, -1.08f), new Vector3(0, 270, 0), 1.75f);
        Put(2, yard, new Vector3(1.35f, .02f, 1.65f), Vector3.zero, 1.05f);
        Put(3, bedroom.transform, new Vector3(chestBounds.center.x, 2.48f, 10.56f), new Vector3(0, 180, 0), .85f);
        Put(4, yard, new Vector3(-5.65f, .02f, 1.65f), new Vector3(0, 20, 0), 1.05f);
        Put(5, yard, new Vector3(-4.18f, .02f, 1.17f), new Vector3(80, 0, 0), 1.65f);
        Put(6, yard, new Vector3(-2.75f, .02f, 1.14f), new Vector3(0, 0, -8), 1.45f);
        AssetDatabase.SaveAssets();
        var scene = SceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Could not save Scene 2.");
        foreach (string name in Names)
        {
            var go = GameObject.Find(name);
            foreach (var r in go.GetComponentsInChildren<Renderer>())
                foreach (var mat in r.sharedMaterials)
                    if (!mat || !mat.GetTexture("_BaseMap")) throw new InvalidOperationException("Missing color texture: " + name);
            Debug.Log("HOUSEHOLD_PROP_PLACED " + PathOf(go.transform) + " " + BoundsOf(go));
        }
        ViewBedroom();
        Debug.Log("HOUSEHOLD_PROPS_SAVED: 7 textured objects.");
    }
    static void Put(int i, Transform parent, Vector3 bottomCenter, Vector3 euler, float longestSide)
    {
        var go = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath(i)), parent);
        Undo.RegisterCreatedObjectUndo(go, "Add " + Names[i]);
        go.name = Names[i];
        go.transform.rotation = Quaternion.Euler(euler);
        var b = BoundsOf(go);
        go.transform.localScale *= longestSide / Mathf.Max(b.size.x, b.size.y, b.size.z);
        b = BoundsOf(go);
        go.transform.position += bottomCenter - new Vector3(b.center.x, b.min.y, b.center.z);
        foreach (var r in go.GetComponentsInChildren<Renderer>())
            r.sharedMaterials = r.sharedMaterials.Select(_ => AssetDatabase.LoadAssetAtPath<Material>(MaterialPath(i))).ToArray();
        if (i == 1)
        {
            var collider = Undo.AddComponent<BoxCollider>(go);
            var world = BoundsOf(go);
            var local = new Bounds(go.transform.InverseTransformPoint(world.center), Vector3.zero);
            for (int x = -1; x <= 1; x += 2)
                for (int y = -1; y <= 1; y += 2)
                    for (int z = -1; z <= 1; z += 2)
                        local.Encapsulate(go.transform.InverseTransformPoint(world.center + Vector3.Scale(world.extents, new Vector3(x, y, z))));
            collider.center = local.center;
            collider.size = local.size;
        }
    }
    [MenuItem("KuTy/Scene 2/View new bedroom props")]
    public static void ViewBedroom() => View(new Vector3(6.6f, 2.05f, 10.25f), new Vector3(6.6f, 2.4f, 7.2f), 2.3f);
    [MenuItem("KuTy/Scene 2/View new kitchen props")]
    public static void ViewKitchen() => View(new Vector3(2.3f, .9f, -1.05f), new Vector3(-.5f, 1.8f, -2.4f), 2.5f);
    [MenuItem("KuTy/Scene 2/View new yard props")]
    public static void ViewYard() => View(new Vector3(-2.15f, .8f, 1.6f), new Vector3(-2.15f, 3.7f, 8f), 5.5f);
    static void View(Vector3 target, Vector3 from, float size)
    {
        if (SceneView.lastActiveSceneView) SceneView.lastActiveSceneView.LookAt(target, Quaternion.LookRotation(target - from), size, false, true);
    }
    [MenuItem("KuTy/Scene 2/Face household props into room")]
    public static void FaceIntoRoom()
    {
        RequireScene();
        foreach (var name in new[] { "Radio", "Certificate", "Refrigerator" })
        {
            var go = GameObject.Find(name);
            if (!go) throw new InvalidOperationException("Missing " + name);
            var center = BoundsOf(go).center;
            Undo.RecordObject(go.transform, "Orient " + name);
            go.transform.rotation = Quaternion.Euler(0, name == "Refrigerator" ? 270 : 180, 0);
            go.transform.position += center - BoundsOf(go).center;
            PrefabUtility.RecordPrefabInstancePropertyModifications(go.transform);
        }
        var scene = SceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    static void Inspect(Transform t, int depth, StringBuilder report)
    {
        if (depth > 3) return;
        if (t.GetComponentsInChildren<Renderer>(true).Length > 0)
            report.AppendLine("SCENE " + PathOf(t) + " pos=" + t.position + " rotation=" + t.eulerAngles + " bounds=" + BoundsOf(t.gameObject));
        foreach (Transform child in t) Inspect(child, depth + 1, report);
    }
    static string PathOf(Transform t) => t.parent ? PathOf(t.parent) + "/" + t.name : t.name;
    static void RequireScene()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || SceneManager.GetActiveScene().path != ScenePath)
            throw new InvalidOperationException("Open Scene 2 in Edit Mode first.");
    }
    static Bounds BoundsOf(GameObject go)
    {
        var rs = go.GetComponentsInChildren<Renderer>(true);
        if (rs.Length == 0) throw new InvalidOperationException("No renderer: " + go.name);
        var b = rs[0].bounds;
        foreach (var r in rs) b.Encapsulate(r.bounds);
        return b;
    }
}
