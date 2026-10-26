using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class Scene2KitchenSetup
{
    const string Root = "Assets/_Project/Generated/KitchenHandsOn";
    const string ScenePath = "Assets/_Project/Scenes/Act2_MemoryWorld_Home.unity";

    [MenuItem("KuTy/Scene 2/Install hands-on rice and cooking")]
    public static void Install()
    {
        if (Scene2BlenderKitchenIntegration.Available && UnityEngine.Object.FindObjectOfType<Scene2KitchenHandsOn>())
        { Scene2BlenderKitchenIntegration.InstallScene(); return; }
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play Mode first.");
        if (SceneManager.GetActiveScene().path != ScenePath) throw new InvalidOperationException("Open Scene 2 first.");
        var flow = UnityEngine.Object.FindObjectOfType<HouseholdChoreFlow>();
        var rice = Need("SM_RiceWashingPot");
        var fire = Need("StovePotVisual");
        var cook = Need("beplua_naucom");
        if (!flow) throw new InvalidOperationException("Missing chore flow.");
        Directory.CreateDirectory(Root); Directory.CreateDirectory("Tools/Scene2Kitchen");
        var previous = GameObject.Find("Scene2KitchenHandsOnWorld");
        if (previous) Undo.DestroyObjectImmediate(previous);
        var world = Group("Scene2KitchenHandsOnWorld", null, Vector3.zero);
        Undo.RegisterCreatedObjectUndo(world.gameObject, "Hands-on kitchen");
        var controller = flow.GetComponent<Scene2KitchenHandsOn>();
        if (!controller) controller = Undo.AddComponent<Scene2KitchenHandsOn>(flow.gameObject);
        Undo.RecordObject(controller, "Configure physical kitchen");
        var waterMat = Mat("RiceWater", new Color(.35f, .54f, .54f));
        var wood = Mat("Wood", new Color(.24f, .105f, .038f));
        var straw = Mat("Bamboo", new Color(.61f, .43f, .21f));
        var metal = Mat("PotLid", new Color(.16f, .17f, .16f));
        var stone = Mat("StoveStone", new Color(.34f, .29f, .24f));
        var riceMat = Mat("RiceGrains", new Color(.9f, .85f, .65f));
        Bounds rb = BoundsOf(rice), fb = BoundsOf(fire), cb = BoundsOf(cook);

        var wash = Group("Wash rice in the real pot", world, Vector3.zero);
        Vector3 waterPosition = rb.center;
        waterPosition.y = Mathf.Lerp(rb.min.y, rb.max.y, .87f);
        float diameter = Mathf.Min(rb.size.x, rb.size.z) * .8f;
        var surface = Prop("Rice water", PrimitiveType.Cylinder, wash, waterPosition,
            new Vector3(diameter, .003f, diameter), waterMat);
        var waterHit = Trigger(surface, new Vector3(1f, 8f, 1f));
        var spoon = Prop("Wooden rice paddle", PrimitiveType.Cube, wash,
            waterPosition + new Vector3(diameter * .25f, .018f, 0f), new Vector3(.025f, .015f, .19f), straw);
        var handle = Prop("Grip to tilt the pot", PrimitiveType.Cylinder, wash,
            waterPosition + Vector3.right * (diameter * .64f) + Vector3.up * .025f,
            new Vector3(.035f, .09f, .035f), wood);
        handle.localRotation = Quaternion.Euler(0, 0, 90);
        var drainHit = Trigger(handle, new Vector3(2f, 1.4f, 2f));
        var grains = Group("Grains under water", wash, waterPosition - Vector3.up * .008f);
        for (int i = 0; i < 32; i++)
        {
            float a = i * 2.39996f, r = Mathf.Sqrt((i + .5f) / 32f) * diameter * .39f;
            var grain = Prop("Rice grain", PrimitiveType.Sphere, grains,
                new Vector3(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r), new Vector3(.009f, .003f, .02f), riceMat, false);
            grain.localRotation = Quaternion.Euler(0, i * 31f, 0);
        }
        var washCamera = Pose("Wash close view", world, waterPosition + new Vector3(.72f, .82f, .65f), waterPosition);

        var fireRoot = Group("Fire tending", world, Vector3.zero);
        Vector3 mouth = new Vector3(fb.max.x - .16f, fb.min.y + .2f, fb.center.z);
        var hearth = Group("Put kindling into the hearth", fireRoot, mouth);
        var hearthHit = Trigger(hearth, new Vector3(.36f, .28f, .42f));
        var logTransforms = new Transform[3]; var logHits = new Collider[3];
        Vector3 tray = mouth + new Vector3(.42f, -.04f, .4f);
        Prop("Nia củi nhóm bếp", PrimitiveType.Cylinder, fireRoot, tray - Vector3.up * .05f,
            new Vector3(.42f, .012f, .42f), straw);
        for (int i = 0; i < 3; i++)
        {
            logTransforms[i] = Prop("Kindling " + (i + 1), PrimitiveType.Cylinder, fireRoot,
                tray + new Vector3((i - 1) * .105f, 0, 0), new Vector3(.048f, .14f, .048f), wood);
            logTransforms[i].rotation = Quaternion.Euler(90, 0, 0);
            logHits[i] = Trigger(logTransforms[i], new Vector3(1.7f, 1.1f, 1.7f));
        }
        var tube = Group("Blow tube pivot", fireRoot, mouth + new Vector3(.46f, .06f, -.22f));
        var tubeMesh = Prop("Bamboo blowing tube", PrimitiveType.Cylinder, tube, Vector3.zero,
            new Vector3(.037f, .2f, .037f), straw, false);
        tubeMesh.localRotation = Quaternion.Euler(0, 0, -70);
        var blowHit = Trigger(tube, new Vector3(.4f, .13f, .13f));
        var glow = hearth.gameObject.AddComponent<Light>(); glow.type = LightType.Point;
        glow.color = new Color(1f, .46f, .12f); glow.range = 1.8f; glow.intensity = 0f; glow.shadows = LightShadows.None;
        var fireCamera = Pose("Fire close view", world, mouth + new Vector3(1.45f, .85f, .4f), mouth + new Vector3(.15f, .1f, .1f));

        var cookRoot = Group("Tend the rice pot", world, Vector3.zero);
        var originalCook = cook.GetComponent<MeshRenderer>();
        originalCook.enabled = false;
        Vector3 potBase = new Vector3(cb.center.x, cb.min.y + cb.size.y * .31f, cb.center.z);
        MakeStoveStand(cookRoot, potBase, stone, wood);
        Prop("Stone stove top", PrimitiveType.Cube, cookRoot, potBase - Vector3.up * .025f,
            new Vector3(.95f, .05f, .95f), stone);
        MakePot(cookRoot, potBase, metal);
        Prop("Cooked rice surface", PrimitiveType.Cylinder, cookRoot, potBase + Vector3.up * .3f,
            new Vector3(.53f, .004f, .53f), riceMat);
        Vector3 lidPoint = potBase + Vector3.up * .357f;
        var lidPivot = Group("Lift the pot lid", cookRoot, lidPoint);
        Prop("Lid", PrimitiveType.Cylinder, lidPivot, Vector3.zero,
            new Vector3(.59f, .012f, .59f), metal, false);
        var lidKnob = Prop("Lid knob", PrimitiveType.Sphere, lidPivot, new Vector3(0, .025f, 0),
            new Vector3(.06f, .04f, .06f), wood, false);
        var lidHit = Trigger(lidKnob, new Vector3(1.6f, 1.7f, 1.6f));
        var fuel = Prop("Slide firewood to control heat", PrimitiveType.Cylinder, cookRoot,
            new Vector3(cb.max.x + .17f, cb.min.y + .19f, cb.center.z), new Vector3(.065f, .27f, .065f), wood);
        fuel.rotation = Quaternion.Euler(0, 0, 90);
        // The root remains in world axes so screen-horizontal dragging has a predictable direction.
        var fuelPivot = Group("Firewood handle", cookRoot, fuel.position);
        fuel.SetParent(fuelPivot, true);
        var fuelHit = Trigger(fuelPivot, new Vector3(.55f, .14f, .14f));
        var cookCamera = Pose("Cooking close view", world, cb.center + new Vector3(1.65f, .85f, .48f), Vector3.Lerp(lidPoint, fuelPivot.position, .45f));

        controller.worldRoot = world; controller.washRoot = wash; controller.fireRoot = fireRoot; controller.cookRoot = cookRoot;
        controller.ricePot = rice; controller.water = surface; controller.waterRenderer = surface.GetComponent<Renderer>();
        controller.spoon = spoon; controller.drainHandle = handle;
        controller.washView = washCamera; controller.fireView = fireCamera; controller.cookView = cookCamera;
        controller.waterHit = waterHit; controller.drainHit = drainHit;
        controller.hearth = hearth; controller.hearthHit = hearthHit;
        controller.blowTube = tube; controller.blowHit = blowHit;
        controller.kindling = logTransforms; controller.kindlingHits = logHits;
        controller.fireGlow = glow;
        controller.fireStoveRenderer = fire.GetComponent<Renderer>();
        controller.fuelLog = fuelPivot; controller.fuelHit = fuelHit; controller.lid = lidPivot; controller.lidHit = lidHit;
        controller.motherLines = WriteMotherLines();
        WriteSounds(); AssetDatabase.Refresh();
        controller.waterClip = AssetDatabase.LoadAssetAtPath<AudioClip>(Root + "/RiceWater.wav");
        controller.fireClip = AssetDatabase.LoadAssetAtPath<AudioClip>(Root + "/HearthCrackle.wav");
        controller.simmerClip = AssetDatabase.LoadAssetAtPath<AudioClip>(Root + "/RiceSimmer.wav");
        EditorUtility.SetDirty(controller); AssetDatabase.SaveAssets();
        File.WriteAllText("Tools/Scene2Kitchen/setup.txt", "Rice=" + rb + "\nFire=" + fb + "\nCook=" + cb +
            "\nWash surface=" + waterPosition + " diameter=" + diameter + "\nHearth=" + mouth + "\nLid=" + lidPoint);
        bool riceActive = rice.gameObject.activeSelf; rice.gameObject.SetActive(true);
        Capture(washCamera, "wash-reference.png"); Capture(fireCamera, "fire-reference.png"); Capture(cookCamera, "cook-reference.png");
        rice.gameObject.SetActive(riceActive);
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene()); EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        Debug.Log("[Hands-on kitchen] Installed physical rice washing, kindling/breathing, simmering and mother's contextual dialogue.");
    }

    [MenuItem("KuTy/Scene 2/Preview hands-on rice and cooking")]
    public static void Preview() => Run(false);
    [MenuItem("KuTy/Tests/Check hands-on rice and cooking")]
    public static void Check() => Run(true);
    static void Run(bool check)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play Mode first.");
        if (SceneManager.GetActiveScene().path != ScenePath) EditorSceneManager.OpenScene(ScenePath);
        if (!UnityEngine.Object.FindObjectOfType<Scene2KitchenHandsOn>()) throw new InvalidOperationException("Install kitchen interactions first.");
        KuTyCheckpointTestScope.Capture(); Scene2Checkpoint.SaveHome(5);
        SessionState.SetString("KuTy.KitchenHandsOn.Run", check ? "check" : "preview");
        EditorApplication.EnterPlaymode();
    }

    static DialogueSequence WriteMotherLines()
    {
        string path = Root + "/MotherKitchenLines.asset";
        var asset = AssetDatabase.LoadAssetAtPath<DialogueSequence>(path);
        if (!asset) { asset = ScriptableObject.CreateInstance<DialogueSequence>(); AssetDatabase.CreateAsset(asset, path); }
        string[][] authored = {
            new[]{"wash_start", "Con vo nhẹ tay thôi nhé. Xoay quanh lòng nồi, đừng chà mạnh kẻo gãy hạt gạo."},
            new[]{"wash_drain", "Nước đục rồi đấy. Con nắm quai, nghiêng nồi chắt từ từ, giữ lại hạt gạo nhé."},
            new[]{"wash_again", "Ừ, khéo rồi đó. Mẹ thêm nước rồi, con vo nhẹ thêm một lượt nữa nhé."},
            new[]{"wash_done", "Gạo sạch rồi, cảm ơn con. Qua đây nhóm bếp cùng mẹ nào."},
            new[]{"fire_start", "Đặt từng thanh củi vào miệng bếp, chừa chút khe cho gió vào nhé con."},
            new[]{"fire_blow", "Giỏi quá. Cầm ống tre thổi từng hơi ngắn thôi, để than bắt lửa dần."},
            new[]{"fire_short", "Hơi ngắn quá, than chưa kịp hồng. Con thử thổi chậm thêm một chút nhé."},
            new[]{"fire_gentle", "Nhẹ thôi con, thổi lâu quá lại bay tro. Nghỉ một nhịp rồi mình thử lại."},
            new[]{"fire_done", "Bếp ấm rồi! Mẹ đặt nồi lên bếp bên này, con canh cơm giúp mẹ nhé."},
            new[]{"cook_start", "Cơm mới sôi thì hạ lửa xuống con nhé. Đẩy củi vào thì lửa lớn, rút ra thì lửa dịu."},
            new[]{"cook_hot", "Lửa hơi to rồi con. Rút bớt củi ra, để cơm sôi lăn tăn thôi, không lại bén nồi."},
            new[]{"cook_low", "Bếp yếu rồi, con đẩy củi vào thêm một chút nhé. Mẹ đứng ngay đây mà."},
            new[]{"cook_good", "Đúng rồi, cứ giữ lửa thế này. Hồi nhỏ mẹ cũng được bà chỉ nấu cơm như vậy đấy."},
            new[]{"cook_check", "Nghe tiếng sôi nhỏ lại rồi. Con nhấc vung lên một chút xem cơm cạn chưa, cẩn thận hơi nóng nhé."},
            new[]{"cook_done", "Cơm chín thơm quá. Có con phụ, mẹ đỡ vất vả hẳn. Mình dọn mâm ăn cơm thôi con."}
        };
        var old = asset.Lines.ToDictionary(line => line.id, line => line.voiceClip);
        asset.ReplaceLinesForEditor(authored.Select(row => new DialogueSequence.DialogueLine {
            id = row[0], speakerId = "MẸ", text = row[1], spokenText = row[1], fallbackDuration = 7f,
            voiceClip = old.TryGetValue(row[0], out var clip) ? clip : null
        }));
        EditorUtility.SetDirty(asset); return asset;
    }

    static Transform Need(string name) => UnityEngine.Object.FindObjectsOfType<Transform>(true).FirstOrDefault(t => t.name == name)
        ?? throw new InvalidOperationException("Missing kitchen prop: " + name);
    static Bounds BoundsOf(Transform root)
    {
        var visuals = root.GetComponentsInChildren<Renderer>(true).Where(r => r.enabled && !(r is ParticleSystemRenderer)).ToArray();
        if (visuals.Length == 0 && root.GetComponent<Renderer>()) return root.GetComponent<Renderer>().bounds;
        if (visuals.Length == 0) throw new InvalidOperationException("No visual bounds: " + root.name);
        Bounds b = visuals[0].bounds; foreach (var r in visuals.Skip(1)) b.Encapsulate(r.bounds); return b;
    }
    static Transform Group(string name, Transform parent, Vector3 pos)
    {
        var t = new GameObject(name).transform; t.SetParent(parent, false); t.localPosition = pos; return t;
    }
    static Transform Pose(string name, Transform parent, Vector3 position, Vector3 at)
    {
        var t = Group(name, parent, position); t.rotation = Quaternion.LookRotation(at - position); return t;
    }
    static Transform Prop(string name, PrimitiveType type, Transform parent, Vector3 pos, Vector3 scale, Material mat, bool worldPosition = true)
    {
        var go = GameObject.CreatePrimitive(type); go.name = name; go.transform.SetParent(parent, false);
        if (worldPosition) go.transform.position = pos; else go.transform.localPosition = pos;
        go.transform.localScale = scale; go.GetComponent<Renderer>().sharedMaterial = mat;
        UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>()); return go.transform;
    }
    static Collider Trigger(Transform t, Vector3 size)
    {
        var box = t.gameObject.AddComponent<BoxCollider>(); box.isTrigger = true; box.size = size; return box;
    }
    static Material Mat(string name, Color color)
    {
        string path = Root + "/" + name + ".mat"; var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (!mat) { mat = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(mat, path); }
        mat.color = color; mat.SetFloat("_Smoothness", name == "RiceWater" ? .8f : .2f);
        if (name == "RiceWater")
        {
            mat.SetFloat("_Surface", 1f); mat.SetFloat("_Blend", 0f); mat.SetFloat("_ZWrite", 0f);
            mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); mat.SetOverrideTag("RenderType", "Transparent"); mat.renderQueue = 3000;
            mat.color = new Color(.6f, .72f, .69f, .34f);
        }
        EditorUtility.SetDirty(mat); return mat;
    }
    static void WriteSounds()
    {
        WriteWav("RiceWater", 17, .16f, .25f); WriteWav("HearthCrackle", 28, .08f, .6f); WriteWav("RiceSimmer", 41, .13f, .08f);
    }

    static void MakeStoveStand(Transform parent, Vector3 top, Material stone, Material wood)
    {
        float height = top.y - .1f;
        Vector3 center = new Vector3(top.x, .05f + height * .5f, top.z);
        Prop("Hearth back wall", PrimitiveType.Cube, parent, center + Vector3.left * .39f,
            new Vector3(.12f, height, .88f), stone);
        foreach (float side in new[] { -1f, 1f })
            Prop("Hearth side wall", PrimitiveType.Cube, parent, center + Vector3.forward * (.38f * side),
                new Vector3(.88f, height, .12f), stone);
        Prop("Hearth floor", PrimitiveType.Cube, parent, new Vector3(top.x, .065f, top.z),
            new Vector3(.88f, .04f, .88f), stone);
        for (int i = 0; i < 3; i++)
        {
            var log = Prop("Cooking hearth wood", PrimitiveType.Cylinder, parent,
                new Vector3(top.x, .14f, top.z + (i - 1) * .11f), new Vector3(.075f, .32f, .075f), wood);
            log.rotation = Quaternion.Euler(0, 0, 90);
        }
    }

    static void MakePot(Transform parent, Vector3 position, Material mat)
    {
        // A separate hollow pot and lid replace the fused lid in the imported mesh.
        Vector2[] profile = { new Vector2(.22f, 0), new Vector2(.27f, .12f), new Vector2(.29f, .33f),
            new Vector2(.29f, .35f), new Vector2(.275f, .35f), new Vector2(.255f, .13f), new Vector2(.21f, .025f) };
        const int segments = 48; var vertices = new Vector3[profile.Length * segments];
        var triangles = new int[(profile.Length - 1) * segments * 6]; int cursor = 0;
        for (int ring = 0; ring < profile.Length; ring++)
        for (int i = 0; i < segments; i++)
        {
            float a = i * Mathf.PI * 2f / segments;
            vertices[ring * segments + i] = new Vector3(Mathf.Cos(a) * profile[ring].x, profile[ring].y, Mathf.Sin(a) * profile[ring].x);
            if (ring == profile.Length - 1) continue;
            int v = ring * segments + i, n = ring * segments + (i + 1) % segments;
            triangles[cursor++] = v; triangles[cursor++] = v + segments; triangles[cursor++] = n + segments;
            triangles[cursor++] = v; triangles[cursor++] = n + segments; triangles[cursor++] = n;
        }
        var mesh = new Mesh { name = "Hollow rice pot", vertices = vertices, triangles = triangles }; mesh.RecalculateNormals(); mesh.RecalculateBounds();
        string path = Root + "/HollowRicePot.asset"; var saved = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (saved) { EditorUtility.CopySerialized(mesh, saved); UnityEngine.Object.DestroyImmediate(mesh); mesh = saved; }
        else AssetDatabase.CreateAsset(mesh, path);
        var go = new GameObject("Rice pot with separate lid", typeof(MeshFilter), typeof(MeshRenderer));
        go.transform.SetParent(parent, false); go.transform.position = position;
        go.GetComponent<MeshFilter>().sharedMesh = mesh; go.GetComponent<MeshRenderer>().sharedMaterial = mat;
        foreach (float side in new[] { -1f, 1f })
            Prop("Pot handle", PrimitiveType.Cube, go.transform, new Vector3(0f, .27f, side * .33f),
                new Vector3(.17f, .025f, .08f), mat, false);
    }
    static void WriteWav(string name, int seed, float gain, float smoothing)
    {
        string path = Root + "/" + name + ".wav"; if (File.Exists(path)) return;
        const int rate = 22050, count = rate * 4; var random = new System.Random(seed); float value = 0f;
        using (var w = new BinaryWriter(File.Create(path)))
        {
            w.Write(System.Text.Encoding.ASCII.GetBytes("RIFF")); w.Write(36 + count * 2);
            w.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt ")); w.Write(16); w.Write((short)1); w.Write((short)1);
            w.Write(rate); w.Write(rate * 2); w.Write((short)2); w.Write((short)16);
            w.Write(System.Text.Encoding.ASCII.GetBytes("data")); w.Write(count * 2);
            for (int i = 0; i < count; i++)
            {
                float t = i / (float)rate; value = Mathf.Lerp(value, (float)random.NextDouble() * 2f - 1f, smoothing);
                float envelope = .45f + .25f * Mathf.Sin(t * 17f) + .15f * Mathf.Sin(t * 31f);
                w.Write((short)(Mathf.Clamp(value * gain * envelope, -1, 1) * 32767));
            }
        }
    }
    static void Capture(Transform pose, string name)
    {
        var go = new GameObject("Kitchen QA camera", typeof(Camera)); var camera = go.GetComponent<Camera>();
        go.transform.SetPositionAndRotation(pose.position, pose.rotation); camera.fieldOfView = 48f; camera.nearClipPlane = .025f;
        var target = RenderTexture.GetTemporary(1280, 720, 24); var previous = RenderTexture.active;
        try
        {
            camera.targetTexture = target; camera.Render(); RenderTexture.active = target;
            var tex = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); tex.Apply();
            File.WriteAllBytes("Tools/Scene2Kitchen/" + name, tex.EncodeToPNG()); UnityEngine.Object.DestroyImmediate(tex);
        }
        finally { camera.targetTexture = null; RenderTexture.active = previous; RenderTexture.ReleaseTemporary(target); UnityEngine.Object.DestroyImmediate(go); }
    }
}
