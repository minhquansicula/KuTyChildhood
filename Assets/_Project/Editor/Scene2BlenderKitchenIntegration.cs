using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Imports the delivered Blender kitchen without modifying its source files.</summary>
public static class Scene2BlenderKitchenIntegration
{
    public const string ArtRoot = "Assets/_Project/Art/Scene2/BlenderKitchen";
    const string Delivery = "Tools/BlenderKitchenDelivery";
    const string ReviewRoot = "Tools/Scene2Kitchen";
    public static bool Available => File.Exists(ArtRoot + "/Models/SM_CookingPot.fbx");

    [MenuItem("KuTy/Scene 2/Import Blender kitchen materials and inspect")]
    public static void ImportAndInspect()
    {
        if (Application.isPlaying) throw new InvalidOperationException("Stop Play Mode first.");
        Directory.CreateDirectory(ArtRoot + "/Models");
        Directory.CreateDirectory(ArtRoot + "/Textures");
        Directory.CreateDirectory(ArtRoot + "/Materials");
        foreach (string source in Directory.GetFiles(Delivery + "/Exports", "*.fbx"))
            CopyChanged(source, ArtRoot + "/Models/" + Path.GetFileName(source));
        foreach (string source in Directory.GetFiles(Delivery + "/Textures", "*.png"))
            if (source.EndsWith("_BaseColor.png") || source.EndsWith("_Normal.png") || source.EndsWith("_MetallicSmoothness.png"))
                CopyChanged(source, ArtRoot + "/Textures/" + Path.GetFileName(source));
        AssetDatabase.Refresh();
        foreach (string path in Directory.GetFiles(ArtRoot + "/Textures", "*.png"))
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            bool normal = path.EndsWith("_Normal.png"), color = path.EndsWith("_BaseColor.png");
            importer.textureType = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
            importer.sRGBTexture = color; importer.mipmapEnabled = true; importer.anisoLevel = 4;
            importer.maxTextureSize = 2048; importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.SaveAndReimport();
        }
        foreach (string path in Directory.GetFiles(ArtRoot + "/Textures", "*_BaseColor.png"))
            CreateMaterial(Path.GetFileNameWithoutExtension(path).Replace("_BaseColor", ""));
        CreateMaterial("M_RiceWater");
        foreach (string path in Directory.GetFiles(ArtRoot + "/Models", "*.fbx"))
        {
            var importer = (ModelImporter)AssetImporter.GetAtPath(path);
            importer.globalScale = 1f; importer.useFileScale = true;
            importer.bakeAxisConversion = true; importer.preserveHierarchy = true;
            importer.importAnimation = false; importer.importCameras = false; importer.importLights = false;
            importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
            importer.importNormals = ModelImporterNormals.Import;
            importer.importTangents = ModelImporterTangents.CalculateMikk;
            importer.SaveAndReimport();
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            foreach (var material in model.GetComponentsInChildren<Renderer>(true).SelectMany(r => r.sharedMaterials).Where(m => m).Distinct())
            {
                string name = material.name;
                var replacement = AssetDatabase.LoadAssetAtPath<Material>(ArtRoot + "/Materials/" + name + ".mat");
                if (!replacement) throw new InvalidOperationException("Missing URP material for " + name);
                importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), name), replacement);
            }
            importer.SaveAndReimport();
        }
        AssetDatabase.SaveAssets();
        Inspect();
    }

    static void CopyChanged(string source, string destination)
    {
        if (File.Exists(destination) && new FileInfo(source).Length == new FileInfo(destination).Length &&
            File.GetLastWriteTimeUtc(source) <= File.GetLastWriteTimeUtc(destination)) return;
        File.Copy(source, destination, true);
    }

    static void CreateMaterial(string name)
    {
        string path = ArtRoot + "/Materials/" + name + ".mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (!material) { material = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(material, path); }
        material.SetColor("_BaseColor", Color.white);
        Texture2D albedo = Texture(name, "BaseColor"), normal = Texture(name, "Normal"), packed = Texture(name, "MetallicSmoothness");
        material.SetTexture("_BaseMap", albedo); material.SetTexture("_BumpMap", normal); material.SetTexture("_MetallicGlossMap", packed);
        material.SetFloat("_Metallic", packed ? 1f : 0f); material.SetFloat("_Smoothness", packed ? 1f : .3f);
        if (normal) material.EnableKeyword("_NORMALMAP");
        if (packed) material.EnableKeyword("_METALLICSPECGLOSSMAP");
        material.SetFloat("_SmoothnessTextureChannel", 0f);
        if (name == "M_Ember")
        {
            material.EnableKeyword("_EMISSION"); material.SetColor("_EmissionColor", new Color(.7f, .055f, .005f));
        }
        if (name == "M_RiceWater")
        {
            material.SetColor("_BaseColor", new Color(.71f, .77f, .72f, .33f)); material.SetFloat("_Smoothness", .85f);
            material.SetFloat("_Surface", 1f); material.SetFloat("_Blend", 0f); material.SetFloat("_ZWrite", 0f);
            material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); material.SetOverrideTag("RenderType", "Transparent"); material.renderQueue = 3000;
        }
        EditorUtility.SetDirty(material);
    }
    static Texture2D Texture(string name, string map) => AssetDatabase.LoadAssetAtPath<Texture2D>(ArtRoot + "/Textures/" + name + "_" + map + ".png");

    [MenuItem("KuTy/Scene 2/Use Blender kitchen assets in game")]
    public static void InstallScene()
    {
        if (Application.isPlaying) throw new InvalidOperationException("Stop Play Mode first.");
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (scene.path != "Assets/_Project/Scenes/Act2_MemoryWorld_Home.unity") throw new InvalidOperationException("Open Scene 2 first.");
        if (!Available) throw new InvalidOperationException("Import the Blender kitchen first.");
        var flow = UnityEngine.Object.FindObjectOfType<HouseholdChoreFlow>();
        var c = flow ? flow.GetComponent<Scene2KitchenHandsOn>() : null;
        if (!c) throw new InvalidOperationException("Install the existing kitchen gameplay first.");
        Undo.RecordObject(c, "Bind Blender kitchen");
        var legacyRice = SceneObject("HouseLayout/KitchenArea/WallBasins/noivogao/SM_RiceWashingPot");
        var legacyFire = SceneObject("HouseLayout/KitchenArea/FirewoodStove/StovePotVisual");
        var legacyCook = SceneObject("HouseLayout/KitchenArea/RiceCookingStove/beplua_naucom");
        Bounds riceBounds = legacyRice.GetComponentInChildren<Renderer>(true).bounds;
        Bounds fireBounds = legacyFire.GetComponent<Renderer>().bounds, cookBounds = legacyCook.GetComponent<Renderer>().bounds;
        Vector3 riceBase = new Vector3(riceBounds.center.x + .8f, .025f, riceBounds.center.z - 1.65f);
        Vector3 fireBase = new Vector3(fireBounds.center.x, .025f, fireBounds.center.z);
        Vector3 cookBase = new Vector3(cookBounds.center.x, .025f, cookBounds.center.z);
        var old = c.worldRoot;
        if (old) Undo.DestroyObjectImmediate(old.gameObject);
        Transform world = Group("Scene2KitchenHandsOnWorld", null, Vector3.zero);
        Undo.RegisterCreatedObjectUndo(world.gameObject, "Blender kitchen installation");
        Transform wash = Group("Wash rice in the real pot", world, Vector3.zero);
        Transform rice = Model("SM_RiceWashingPot", wash, riceBase);
        Transform tilt = Part(rice, "Tilt_Pivot"), water = Part(rice, "Water_Surface"), grip = Part(rice, "Grip_Anchor");
        Transform drain = Part(rice, "Drain_Anchor"), stir = Part(rice, "Stir_Anchor");
        var solids = new System.Collections.Generic.List<Collider>();
        solids.Add(SolidMesh(Part(rice, "Pot_Body")));
        solids.Add(SolidMesh(Part(rice, "Handle_Left"))); solids.Add(SolidMesh(Part(rice, "Handle_Right")));
        AddTask(rice, HouseholdChore.WashRice);
        var waterHit = water.gameObject.AddComponent<BoxCollider>(); waterHit.isTrigger = true;
        Bounds localWater = water.GetComponent<MeshFilter>().sharedMesh.bounds;
        waterHit.center = localWater.center; waterHit.size = localWater.size + Vector3.one * .016f;
        var gripHit = grip.gameObject.AddComponent<SphereCollider>(); gripHit.isTrigger = true; gripHit.radius = .048f;
        Transform spoon = Group("Rice paddle tip", tilt, Vector3.zero); spoon.position = stir.position + Vector3.up * .015f;
        Transform paddle = Model("SM_RicePaddle", spoon, spoon.position);
        paddle.localPosition = new Vector3(0f, 0f, -.105f);
        var washView = Pose("Wash close view", world, stir.position + new Vector3(.77f, .76f, .63f), stir.position + Vector3.up * .02f);

        Transform fire = Group("Fire tending", world, fireBase);
        Transform stove = Model("SM_WoodStove", fire, fireBase); stove.rotation = Quaternion.Euler(0f, -90f, 0f);
        solids.Add(SolidMesh(Part(stove, "Stove_Body"))); solids.Add(SolidMesh(Part(stove, "Stove_Top")));
        AddTask(fire, HouseholdChore.LightFire);
        Transform hearth = Part(stove, "Hearth_Anchor");
        Transform hearthHitRoot = Group("Hearth opening target", fire, hearth.position);
        var hearthHit = hearthHitRoot.gameObject.AddComponent<BoxCollider>(); hearthHit.isTrigger = true; hearthHit.size = new Vector3(.36f, .28f, .42f);
        Transform tray = Model("SM_BambooTray", fire, fireBase + new Vector3(.72f, .015f, .49f));
        var logs = new Transform[3]; var logHits = new Collider[3]; var slots = new Transform[3];
        Vector3 inward = stove.TransformDirection(Vector3.forward);
        for (int i = 0; i < 3; i++)
        {
            logs[i] = Group("Kindling " + (i + 1), fire, tray.position + new Vector3((i - 1) * .09f, .045f, 0f));
            Transform model = Model("SM_Firewood_0" + (i + 1), logs[i], logs[i].position);
            logHits[i] = BoxForVisual(logs[i], true, .025f);
            Transform slot = Part(stove, "WoodSlot_0" + (i + 1));
            slots[i] = Group("Inserted kindling " + (i + 1), fire, Vector3.zero);
            slots[i].rotation = stove.rotation;
            Vector3 tipOffset = Part(model, "Feed_Anchor").position - logs[i].position;
            tipOffset = slots[i].rotation * tipOffset;
            slots[i].position = slot.position + inward * .15f - tipOffset;
        }
        Transform tube = Group("Blow tube grip", fire, fireBase + new Vector3(.70f, .075f, -.36f));
        Transform tubeModel = Model("SM_BlowTube", tube, tube.position);
        tube.rotation = Quaternion.LookRotation((hearth.position - tube.position).normalized, Vector3.up);
        var blowHit = BoxForVisual(tube, true, .035f);
        Transform fireAnchor = Part(stove, "Fire_Anchor");
        var glow = fireAnchor.gameObject.AddComponent<Light>(); glow.type = LightType.Point;
        glow.range = 1.8f; glow.color = new Color(1f, .42f, .12f); glow.intensity = 0f; glow.shadows = LightShadows.None;
        Transform fireView = Pose("Fire close view", world, fireBase + new Vector3(1.9f, .98f, .58f),
            Vector3.Lerp(hearth.position + Vector3.up * .08f, tray.position, .35f));

        Transform cook = Group("Tend the rice pot", world, cookBase);
        Transform cookStove = Model("SM_WoodStove", cook, cookBase); cookStove.rotation = stove.rotation;
        var cookingGlow = Part(cookStove, "Fire_Anchor").gameObject.AddComponent<Light>();
        cookingGlow.type = LightType.Point; cookingGlow.range = 1.6f; cookingGlow.intensity = 0f;
        cookingGlow.color = glow.color; cookingGlow.shadows = LightShadows.None;
        solids.Add(SolidMesh(Part(cookStove, "Stove_Body"))); solids.Add(SolidMesh(Part(cookStove, "Stove_Top")));
        Transform potRest = Part(cookStove, "PotRest_Anchor");
        Transform pot = Model("SM_CookingPot", cook, potRest.position); pot.rotation = cookStove.rotation;
        solids.Add(SolidMesh(Part(pot, "Pot_Body")));
        AddTask(cook, HouseholdChore.CookRice);
        Transform lid = Part(pot, "Lid_Pivot"), knob = Part(pot, "Lid_Knob"), lidGrip = Part(pot, "LidGrip_Anchor");
        // The source anchor marks the closed state; parenting it to the lid keeps its hit point attached while lifting.
        lidGrip.SetParent(lid, true);
        var lidHit = lidGrip.gameObject.AddComponent<SphereCollider>(); lidHit.isTrigger = true; lidHit.radius = .047f;
        Transform fuel = Group("Firewood handle", cook, Part(cookStove, "WoodSlot_02").position - inward * .12f);
        Transform fuelModel = Model("SM_Firewood_04", fuel, fuel.position); fuel.rotation = cookStove.rotation;
        var fuelHit = BoxForVisual(fuel, true, .035f);
        Transform fuelAxis = Group("Inward feed direction", cook, fuel.position); fuelAxis.rotation = Quaternion.LookRotation(inward, Vector3.up);
        foreach (int i in new[] { 1, 3 })
        {
            Transform slot = Part(cookStove, "WoodSlot_0" + i);
            Transform support = Model("SM_Firewood_05", cook, slot.position); support.rotation = cookStove.rotation;
        }
        Transform cookView = Pose("Cooking close view", world, cookBase + new Vector3(1.85f, 1.1f, .5f),
            Vector3.Lerp(lidGrip.position, fuel.position, .45f));

        c.worldRoot = world; c.washRoot = wash; c.fireRoot = fire; c.cookRoot = cook;
        c.washView = washView; c.fireView = fireView; c.cookView = cookView;
        c.ricePot = tilt; c.water = water; c.waterRenderer = water.GetComponent<Renderer>(); c.spoon = spoon;
        c.drainHandle = grip; c.drainHit = gripHit; c.waterHit = waterHit; c.drainAnchor = drain;
        c.hearth = hearth; c.hearthHit = hearthHit; c.blowTube = tube; c.blowHit = blowHit;
        c.kindling = logs; c.kindlingHits = logHits; c.kindlingSlots = slots; c.fireGlow = glow;
        c.cookGlow = cookingGlow;
        c.fuelLog = fuel; c.fuelHit = fuelHit; c.fuelAxis = fuelAxis; c.lid = lid; c.lidHit = lidHit;
        c.steamAnchor = Part(pot, "Steam_Anchor"); c.fireStoveRenderer = null;
        c.emberRenderers = new[] { Part(stove, "Ember_Bed").GetComponent<Renderer>(), Part(cookStove, "Ember_Bed").GetComponent<Renderer>() };
        c.retainedColliders = solids.ToArray(); c.importedKitchen = true;
        c.washRadius = Mathf.Min(c.waterRenderer.bounds.extents.x, c.waterRenderer.bounds.extents.z) * .94f;
        Vector3 drainDirection = Vector3.ProjectOnPlane(drain.position - stir.position, Vector3.up).normalized;
        c.potTiltAxis = tilt.parent.InverseTransformDirection(Vector3.Cross(Vector3.up, drainDirection)); c.potTiltDegrees = 23f;
        // Preserve the established chapter owner while changing its physical target.
        var serializedFlow = new SerializedObject(flow);
        serializedFlow.FindProperty("riceWashingPot").objectReferenceValue = rice;
        serializedFlow.FindProperty("riceWashingTarget").objectReferenceValue = null;
        serializedFlow.ApplyModifiedProperties();
        foreach (var source in new[] { legacyRice, legacyFire, legacyCook })
        { Undo.RecordObject(source.gameObject, "Retire placeholder kitchen mesh"); source.gameObject.SetActive(false); }
        // The old stove containers had broad boxes around their fused meshes.
        // Keep the containers, but let the new mesh colliders define their physical shape.
        foreach (var container in new[] { legacyFire.parent, legacyCook.parent })
            foreach (var collider in container.GetComponents<Collider>())
                if (!collider.isTrigger)
                { Undo.RecordObject(collider, "Retire placeholder stove collider"); collider.enabled = false; }
        ReplaceRadioWood();
        foreach (var light in UnityEngine.Object.FindObjectsOfType<Light>(true))
            if (light.name == "Kitchen_RiceStove_Ember" || light.name == "Kitchen_RiceStove_Ember (1)")
            { Undo.RecordObject(light, "Replace placeholder stove lighting"); light.enabled = false; }
        EditorUtility.SetDirty(c); EditorUtility.SetDirty(flow);
        EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
        Directory.CreateDirectory(ReviewRoot);
        File.WriteAllText(ReviewRoot + "/blender-integration.txt", "Imported models bound to Scene 2.\n" +
            "Wash=" + c.waterRenderer.bounds + "\nFire=" + VisualBounds(stove) + "\nCook=" + VisualBounds(cook) +
            "\nWashRadius=" + c.washRadius + "\nTiltAxis=" + c.potTiltAxis + "\nInward=" + inward);
        Capture(washView, "blender-wash.png"); Capture(fireView, "blender-fire.png"); Capture(cookView, "blender-cook.png");
        Debug.Log("[Blender kitchen] Replaced prototypes, bound authored pivots and retained chapter checkpoints/dialogue.");
    }

    public static void ReplaceRadioWood()
    {
        if (!Available) return;
        var radio = UnityEngine.Object.FindObjectOfType<Scene2RadioKitchenSequence>();
        if (!radio || !radio.woodBundle || !radio.woodPickup) return;
        Undo.RecordObject(radio, "Bind imported firewood bundle");
        foreach (Transform child in radio.woodBundle.Cast<Transform>().ToArray()) Undo.DestroyObjectImmediate(child.gameObject);
        for (int i = 0; i < 5; i++)
        {
            var log = Model("SM_Firewood_0" + (i + 1), radio.woodBundle, radio.woodBundle.position);
            log.localPosition = new Vector3((i % 3 - 1) * .066f, i >= 3 ? .06f : 0f, (i % 2) * .015f);
        }
        var pickupPosition = radio.woodPickup.position; pickupPosition.y = .09f;
        Undo.RecordObject(radio.woodPickup, "Lower bundle into its woven tray"); radio.woodPickup.position = pickupPosition;
        foreach (Transform child in radio.woodPickup.Cast<Transform>().ToArray())
            if (child != radio.woodBundle && (child.name == "Giỏ củi nhỏ" || child.name == "Imported pickup tray")) Undo.DestroyObjectImmediate(child.gameObject);
        var pickupTray = Model("SM_BambooTray", radio.woodPickup, radio.woodPickup.position + Vector3.down * .045f); pickupTray.name = "Imported pickup tray";
        foreach (Transform child in radio.woodRest.Cast<Transform>().ToArray())
            if (child.name == "Nia đặt củi" || child.name == "Imported delivery tray") Undo.DestroyObjectImmediate(child.gameObject);
        var restTray = Model("SM_BambooTray", radio.woodRest, radio.woodRest.position + Vector3.down * .045f); restTray.name = "Imported delivery tray";
        var hit = radio.pickupCollider as BoxCollider;
        if (hit) FitBox(hit, radio.woodBundle, .025f);
        EditorUtility.SetDirty(radio);
    }

    static Transform SceneObject(string path) => UnityEngine.Object.FindObjectsOfType<Transform>(true).FirstOrDefault(t =>
        AnimationUtility.CalculateTransformPath(t, null) == path) ?? throw new InvalidOperationException("Missing scene object: " + path);
    static Transform Part(Transform model, string name) => model.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == name)
        ?? throw new InvalidOperationException("Missing imported part " + name + " under " + model.name);
    static Transform Group(string name, Transform parent, Vector3 position)
    { var t = new GameObject(name).transform; t.SetParent(parent, false); t.SetPositionAndRotation(position, Quaternion.identity); return t; }
    static Transform Model(string name, Transform parent, Vector3 position)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ArtRoot + "/Models/" + name + ".fbx");
        if (!prefab) throw new InvalidOperationException("Missing imported model: " + name);
        var t = ((GameObject)PrefabUtility.InstantiatePrefab(prefab)).transform;
        t.SetParent(parent, false); t.position = position; return t;
    }
    static Transform Pose(string name, Transform parent, Vector3 position, Vector3 at)
    { var t = Group(name, parent, position); t.rotation = Quaternion.LookRotation(at - position, Vector3.up); return t; }
    static void AddTask(Transform target, HouseholdChore task)
    { target.gameObject.layer = LayerMask.NameToLayer("Interactable"); target.gameObject.AddComponent<HouseholdChoreInteractable>().Configure(task, target); }
    static Collider SolidMesh(Transform target)
    {
        target.gameObject.layer = LayerMask.NameToLayer("Interactable");
        var c = target.gameObject.AddComponent<MeshCollider>(); c.sharedMesh = target.GetComponent<MeshFilter>().sharedMesh; return c;
    }
    static BoxCollider BoxForVisual(Transform target, bool trigger, float padding)
    { var c = target.gameObject.AddComponent<BoxCollider>(); c.isTrigger = trigger; FitBox(c, target, padding); return c; }
    static void FitBox(BoxCollider c, Transform target, float padding)
    {
        var renderers = target.GetComponentsInChildren<Renderer>(true); bool first = true; Bounds bounds = default;
        foreach (var r in renderers)
        {
            var mesh = r.GetComponent<MeshFilter>(); if (!mesh) continue;
            Bounds b = mesh.sharedMesh.bounds;
            for (int i = 0; i < 8; i++)
            {
                Vector3 corner = b.center + Vector3.Scale(b.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                Vector3 point = target.InverseTransformPoint(r.transform.TransformPoint(corner));
                if (first) { bounds = new Bounds(point, Vector3.zero); first = false; } else bounds.Encapsulate(point);
            }
        }
        c.center = bounds.center; c.size = bounds.size + Vector3.one * padding;
    }
    static Bounds VisualBounds(Transform target)
    { var rr = target.GetComponentsInChildren<Renderer>(true); var b = rr[0].bounds; foreach (var r in rr.Skip(1)) b.Encapsulate(r.bounds); return b; }
    static void Capture(Transform pose, string filename)
    {
        var go = new GameObject("Blender kitchen review camera", typeof(Camera)); var camera = go.GetComponent<Camera>();
        go.transform.SetPositionAndRotation(pose.position, pose.rotation); camera.fieldOfView = 48f; camera.nearClipPlane = .025f;
        var rt = RenderTexture.GetTemporary(1280, 720, 24); var previous = RenderTexture.active;
        try
        {
            camera.targetTexture = rt; camera.Render(); RenderTexture.active = rt;
            var texture = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            texture.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); texture.Apply();
            File.WriteAllBytes(ReviewRoot + "/" + filename, texture.EncodeToPNG()); UnityEngine.Object.DestroyImmediate(texture);
        }
        finally { camera.targetTexture = null; RenderTexture.active = previous; RenderTexture.ReleaseTemporary(rt); UnityEngine.Object.DestroyImmediate(go); }
    }

    [MenuItem("KuTy/Scene 2/Inspect imported Blender kitchen")]
    public static void Inspect()
    {
        var report = new StringBuilder();
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        report.AppendLine("Scene=" + scene.path + " dirty=" + scene.isDirty);
        foreach (string path in Directory.GetFiles(ArtRoot + "/Models", "*.fbx"))
        {
            var model = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(path));
            try
            {
                var renderers = model.GetComponentsInChildren<Renderer>();
                Bounds bounds = renderers[0].bounds; foreach (var r in renderers.Skip(1)) bounds.Encapsulate(r.bounds);
                report.AppendLine("\n" + Path.GetFileName(path) + " " + bounds);
                foreach (var t in model.GetComponentsInChildren<Transform>())
                {
                    string relative = AnimationUtility.CalculateTransformPath(t, model.transform);
                    report.AppendLine(relative + " position=" + t.position.ToString("F4") + " local=" + t.localPosition.ToString("F4") +
                        " rotation=" + t.localEulerAngles.ToString("F2") + " scale=" + t.localScale.ToString("F4") +
                        " forward=" + t.forward.ToString("F3") + " up=" + t.up.ToString("F3"));
                }
            }
            finally { UnityEngine.Object.DestroyImmediate(model); }
        }
        Directory.CreateDirectory(ReviewRoot);
        File.WriteAllText(ReviewRoot + "/blender-import-inventory.txt", report.ToString());
        Debug.Log("[Blender kitchen] Imported 11 FBX models, remapped URP materials and wrote anchor inventory.");
    }
}
