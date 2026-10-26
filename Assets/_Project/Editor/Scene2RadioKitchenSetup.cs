using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class Scene2RadioKitchenSetup
{
    const string Root = "Assets/_Project/Generated/RadioKitchen";
    const string ScenePath = "Assets/_Project/Scenes/Act2_MemoryWorld_Home.unity";

    [MenuItem("KuTy/Scene 2/Preview radio to kitchen")]
    public static void Preview() => StartRun(false);

    [MenuItem("KuTy/Tests/Check Scene 2 radio to kitchen")]
    public static void CheckSequence() => StartRun(true);

    static void StartRun(bool check)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play Mode first.");
        if (SceneManager.GetActiveScene().path != ScenePath) EditorSceneManager.OpenScene(ScenePath);
        if (!UnityEngine.Object.FindObjectOfType<Scene2RadioKitchenSequence>()) throw new InvalidOperationException("Install the sequence first.");
        KuTyCheckpointTestScope.Capture();
        Scene2Checkpoint.SaveHome(1);
        SessionState.SetString("KuTy.RadioKitchen.Run", check ? "check" : "preview");
        EditorApplication.EnterPlaymode();
    }

    [MenuItem("KuTy/Scene 2/Install radio to kitchen sequence")]
    public static void Install()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play Mode first.");
        if (SceneManager.GetActiveScene().path != ScenePath) throw new InvalidOperationException("Open Scene 2 first.");
        var flow = UnityEngine.Object.FindObjectOfType<HouseholdChoreFlow>();
        var radio = GameObject.Find("BedroomArea/BedroomDecor/Radio");
        var basket = GameObject.Find("HouseLayout/YardProps/BambooBasket");
        var stove = GameObject.Find("HouseLayout/KitchenArea/FirewoodStove");
        if (!flow || !radio || !basket || !stove) throw new InvalidOperationException("Missing radio, basket, stove or chore flow.");
        Directory.CreateDirectory(Root);
        Directory.CreateDirectory("Tools/Scene2Radio");
        WriteAudio();
        AssetDatabase.Refresh();
        var old = GameObject.Find("Scene2RadioKitchenWorld");
        if (old) Undo.DestroyObjectImmediate(old);
        var world = new GameObject("Scene2RadioKitchenWorld").transform;
        Undo.RegisterCreatedObjectUndo(world.gameObject, "Radio kitchen sequence");
        var sequence = flow.GetComponent<Scene2RadioKitchenSequence>();
        if (!sequence) sequence = Undo.AddComponent<Scene2RadioKitchenSequence>(flow.gameObject);
        Undo.RecordObject(sequence, "Configure radio kitchen sequence");
        var dark = Material("Walnut", new Color(.065f, .044f, .024f));
        var brass = Material("Brass", new Color(.74f, .52f, .24f));
        var ivory = Material("Ivory", new Color(.93f, .84f, .6f));
        var wood = Material("Firewood", new Color(.24f, .105f, .038f));
        var end = Material("WoodEnd", new Color(.57f, .36f, .16f));
        var rope = Material("Rope", new Color(.66f, .55f, .33f));

        Bounds rb = radio.GetComponent<Renderer>().bounds;
        Vector3 outward = radio.transform.forward;
        var rig = new GameObject("RadioControls").transform;
        rig.SetParent(world, false);
        rig.position = rb.center + outward * (rb.extents.z + .012f);
        rig.rotation = Quaternion.LookRotation(-outward, Vector3.up);
        Cube("Tuning plate", rig, new Vector3(-.035f, .005f, 0f), new Vector3(.38f, .09f, .018f), dark);
        Cube("Frequency rail", rig, new Vector3(-.04f, 0f, -.012f), new Vector3(.34f, .003f, .002f), brass);
        for (int i = 0; i <= 10; i++)
            Cube("Frequency tick", rig, new Vector3(-.20f + i * .032f, .011f, -.013f),
                new Vector3(.0015f, i % 5 == 0 ? .018f : .009f, .002f), ivory);
        var needle = Cube("Station needle", rig, new Vector3(-.16f, .01f, -.017f), new Vector3(.003f, .025f, .003f), brass);
        WorldText("FM   88      92      96      100      108", rig, new Vector3(-.035f, .033f, -.016f), .011f, .37f);
        WorldText("NHÀ MÌNH", rig, new Vector3(-.035f, -.025f, -.016f), .012f, .26f);
        var knob = new GameObject("Tuning knob").transform;
        knob.SetParent(rig, false);
        knob.localPosition = new Vector3(.185f, -.155f, -.03f);
        var disc = Primitive("Brass tuning dial", PrimitiveType.Cylinder, knob, Vector3.zero,
            new Vector3(.067f, .012f, .067f), brass);
        disc.localRotation = Quaternion.Euler(90f, 0f, 0f);
        var hit = knob.gameObject.AddComponent<SphereCollider>();
        hit.radius = .045f;
        hit.isTrigger = true;
        Cube("Dial indicator", knob, new Vector3(0f, .021f, -.014f), new Vector3(.004f, .014f, .003f), ivory);
        var close = new GameObject("Radio close view").transform;
        close.SetParent(rig, false);
        close.position = rb.center + outward * 1.02f + Vector3.up * .12f;
        close.rotation = Quaternion.LookRotation(rb.center - Vector3.up * .07f - close.position, Vector3.up);

        var pickup = new GameObject("WoodPickup").transform;
        pickup.SetParent(world, false);
        Bounds bb = VisualBounds(basket);
        // The old basket is surrounded by grain sacks. Put a small wood basket at
        // the open edge of the storage area so the bundle has a clear approach.
        pickup.position = new Vector3(bb.max.x + 1.35f, .18f, bb.min.z - .65f);
        Primitive("Giỏ củi nhỏ", PrimitiveType.Cylinder, pickup,
            new Vector3(0f, -.09f, 0f), new Vector3(.55f, .045f, .55f), rope);
        var bundle = new GameObject("Bó củi của mẹ").transform;
        bundle.SetParent(pickup, false);
        foreach (var task in UnityEngine.Object.FindObjectsOfType<HouseholdChoreInteractable>(true))
            if (task.Chore == HouseholdChore.CarryWood)
            {
                Undo.RecordObject(task, "Replace basket interaction with actual wood pickup");
                task.enabled = false;
            }
        bundle.gameObject.layer = LayerMask.NameToLayer("Interactable");
        var pickupHit = bundle.gameObject.AddComponent<BoxCollider>();
        pickupHit.isTrigger = true;
        pickupHit.center = Vector3.up * .025f;
        pickupHit.size = new Vector3(.25f, .17f, .53f);
        var pickupTask = bundle.gameObject.AddComponent<HouseholdChoreInteractable>();
        pickupTask.Configure(HouseholdChore.CarryWood, bundle);
        for (int i = 0; i < 5; i++)
        {
            Vector3 p = new Vector3((i % 3 - 1) * .068f, i >= 3 ? .06f : 0f, (i % 2) * .025f);
            var log = Primitive("Khúc củi " + i, PrimitiveType.Cylinder, bundle, p, new Vector3(.064f, .23f, .064f), wood);
            log.localRotation = Quaternion.Euler(90f, 0f, (i - 2) * 2f);
            var cap = Primitive("Wood grain", PrimitiveType.Cylinder, bundle, p + Vector3.back * .232f,
                new Vector3(.057f, .002f, .057f), end);
            cap.localRotation = Quaternion.Euler(90f, 0f, 0f);
        }
        foreach (float z in new[] { -.1f, .1f })
            Cube("Dây buộc", bundle, new Vector3(0f, .026f, z), new Vector3(.22f, .12f, .015f), rope);

        Bounds sb = VisualBounds(stove);
        var rest = new GameObject("WoodRestByStove").transform;
        rest.SetParent(world, false);
        // A small point in front of the stove, above its solid floor and outside its collider.
        rest.position = new Vector3(sb.max.x + .5f, sb.min.y + .06f, sb.center.z);
        rest.rotation = Quaternion.identity;
        var place = rest.gameObject.AddComponent<Scene2WoodPlaceInteractable>();
        place.sequence = sequence;
        var mat = Primitive("Nia đặt củi", PrimitiveType.Cylinder, rest,
            new Vector3(0f, -.045f, 0f), new Vector3(.54f, .008f, .54f), rope);
        var mother = new GameObject("Mother kitchen voice anchor").transform;
        mother.SetParent(world, false);
        mother.position = sb.center + Vector3.up * .65f;

        sequence.radioRig = rig;
        sequence.tuningKnob = knob;
        sequence.knobCollider = hit;
        sequence.needle = needle;
        sequence.closeView = close;
        sequence.kitchenSpot = mother;
        sequence.woodBundle = bundle;
        sequence.woodPickup = pickup;
        sequence.pickupCollider = pickupHit;
        sequence.woodRest = rest;
        sequence.stationClip = AssetDatabase.LoadAssetAtPath<AudioClip>(Root + "/MorningRadio.wav");
        sequence.staticClip = AssetDatabase.LoadAssetAtPath<AudioClip>(Root + "/RadioStatic.wav");
        sequence.motherClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Project/Audio/Voice/vi/Act2/act2_mom_wakeup_001.wav");
        sequence.motherText = HouseholdChoreFlow.WakeupText;
        Scene2BlenderKitchenIntegration.ReplaceRadioWood();
        EditorUtility.SetDirty(sequence);
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
        File.WriteAllText("Tools/Scene2Radio/setup.txt", "Radio=" + rb + "\nBasket=" + bb + "\nStove=" + sb +
            "\nWood drop=" + rest.position + "\nClose view=" + close.position + "\n");
        CaptureReference(sequence);
        Debug.Log("[Radio kitchen] Installed physical tuning dial, spatial radio/mother audio, carried wood and stove destination.");
    }

    static Material Material(string name, Color color)
    {
        string path = Root + "/" + name + ".mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (!mat)
        {
            mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            AssetDatabase.CreateAsset(mat, path);
        }
        mat.color = color;
        mat.SetFloat("_Smoothness", .22f);
        EditorUtility.SetDirty(mat);
        return mat;
    }

    static Transform Primitive(string name, PrimitiveType type, Transform parent, Vector3 pos, Vector3 scale, Material mat)
    {
        var go = GameObject.CreatePrimitive(type);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.transform.localScale = scale;
        go.GetComponent<Renderer>().sharedMaterial = mat;
        UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
        return go.transform;
    }
    static Transform Cube(string n, Transform p, Vector3 v, Vector3 s, Material m) => Primitive(n, PrimitiveType.Cube, p, v, s, m);

    static void WorldText(string text, Transform parent, Vector3 pos, float height, float width)
    {
        var label = new GameObject("Radio label", typeof(TextMeshPro)).GetComponent<TextMeshPro>();
        label.transform.SetParent(parent, false);
        label.transform.localPosition = pos;
        label.font = GameUiTheme.Current.font;
        label.text = text;
        label.fontSize = height * 10f;
        label.color = new Color(.92f, .8f, .54f);
        label.alignment = TextAlignmentOptions.Center;
        label.rectTransform.sizeDelta = new Vector2(width, .04f);
        label.enableWordWrapping = false;
    }

    static Bounds VisualBounds(GameObject go)
    {
        var renderers = go.GetComponentsInChildren<Renderer>().Where(r => r.enabled && r.gameObject.activeInHierarchy && !(r is TMPro.TMP_SubMesh)).ToArray();
        if (renderers.Length == 0) throw new InvalidOperationException("No visible meshes: " + go.name);
        Bounds bounds = renderers[0].bounds;
        foreach (var r in renderers.Skip(1)) bounds.Encapsulate(r.bounds);
        return bounds;
    }

    static void WriteAudio()
    {
        const int rate = 22050;
        // Original pentatonic music-box phrase; no network or third-party audio dependency.
        int[] notes = { 60, 64, 67, 69, 67, 64, 62, 64, 60, 62, 64, 67, 64, 62, 60, 55 };
        WriteWav(Root + "/MorningRadio.wav", rate, 16f, t =>
        {
            int beat = Mathf.FloorToInt(t / .5f);
            float age = t % .5f;
            float freq = 440f * Mathf.Pow(2f, (notes[beat % notes.Length] - 69) / 12f);
            float env = (1f - Mathf.Exp(-age * 120f)) * Mathf.Exp(-age * 5f);
            return .35f * env * (Mathf.Sin(2f * Mathf.PI * freq * t) + .22f * Mathf.Sin(4f * Mathf.PI * freq * t));
        });
        var random = new System.Random(213);
        float filtered = 0f;
        WriteWav(Root + "/RadioStatic.wav", rate, 3f, t =>
        {
            filtered = Mathf.Lerp(filtered, (float)random.NextDouble() * 2f - 1f, .45f);
            return filtered * .38f;
        });
    }

    static void WriteWav(string path, int rate, float seconds, Func<float, float> sample)
    {
        if (File.Exists(path)) return;
        int count = (int)(rate * seconds);
        using (var writer = new BinaryWriter(File.Create(path)))
        {
            writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF")); writer.Write(36 + count * 2);
            writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt ")); writer.Write(16);
            writer.Write((short)1); writer.Write((short)1); writer.Write(rate); writer.Write(rate * 2);
            writer.Write((short)2); writer.Write((short)16);
            writer.Write(System.Text.Encoding.ASCII.GetBytes("data")); writer.Write(count * 2);
            for (int i = 0; i < count; i++) writer.Write((short)(Mathf.Clamp(sample(i / (float)rate), -1f, 1f) * 32767));
        }
    }

    static void CaptureReference(Scene2RadioKitchenSequence s)
    {
        var go = new GameObject("Radio QA camera", typeof(Camera));
        var camera = go.GetComponent<Camera>();
        go.transform.SetPositionAndRotation(s.closeView.position, s.closeView.rotation);
        camera.fieldOfView = 48f;
        camera.nearClipPlane = .025f;
        var target = RenderTexture.GetTemporary(1280, 720, 24);
        var previous = RenderTexture.active;
        try
        {
            camera.targetTexture = target;
            camera.Render();
            RenderTexture.active = target;
            var texture = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            texture.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); texture.Apply();
            File.WriteAllBytes("Tools/Scene2Radio/radio-reference.png", texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);
        }
        finally
        {
            camera.targetTexture = null;
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(target);
            UnityEngine.Object.DestroyImmediate(go);
        }
    }
}
