using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

public static partial class Scene3VillageComposition
{
    private const string ArtOutput = OutputFolder + "/ArtDirection";
    private const string ArtAssets = "Assets/_Project/Art/Models/Scene3/ArtDirection";
    private const string ArtRoot = "VillageArtDirection_Sample";

    private static void RequireArtScene()
    {
        if (SceneManager.GetActiveScene().path != ScenePath || EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Open Scene 3 in Edit Mode before running this art pass.");
    }

    [MenuItem("KuTy/Scene 3/Art direction/1 Capture baseline")]
    public static void CaptureArtBaseline()
    {
        RequireArtScene();
        Directory.CreateDirectory(ArtOutput);
        // Never replace the original baseline on subsequent iterations.
        if (File.Exists(ArtOutput + "/baseline-complete.txt")) return;
        if (Find(ArtRoot)) throw new InvalidOperationException("The sample is already present; restore its backup to capture a new baseline.");
        if (!File.Exists(ArtOutput + "/before.unity") && !EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), ArtOutput + "/before.unity", true))
            throw new IOException("Unable to save the scene safety copy.");
        var report = new StringBuilder("Scene 3 art direction baseline\n");
        report.AppendLine("UTC " + DateTime.UtcNow.ToString("O"));
        report.AppendLine("GPU " + SystemInfo.graphicsDeviceName);
        report.AppendLine("Editor captures are visual comparisons, not warmed-up build performance measurements.");
        report.AppendLine("Existing gameplay components: " + GameplaySnapshot().Count);
        foreach (var t in AllTransforms().Where(t => t.gameObject.activeInHierarchy &&
            (t.name.StartsWith("Nha") || t.name.StartsWith("CayMaxtree") ||
             t.name.Contains("Chum") || t.name.Contains("Rao") || t.name.Contains("Cong") ||
             t.name.Contains("Chau") || t.name.Contains("XeDap") ||
             t.name == "ShopCounter" || t.name == "SanBi_DatNen")))
            report.AppendLine(HierarchyPath(t) + " @ " + t.position);
        report.AppendLine("Asset candidates (inspect before replacing the sample geometry):");
        foreach (string guid in AssetDatabase.FindAssets("t:GameObject", new[] { "Assets/_Project" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (new[] { "chum", "xedap", "bicycle", "fence", "hangrao", "conglang", "flowerpot" }
                .Any(word => path.ToLowerInvariant().Contains(word))) report.AppendLine(path);
        }
        File.WriteAllText(ArtOutput + "/inventory.txt", report.ToString());
        // Read only: KuTyCheckpointTestScope.Capture clears progress for tests, so do not use it here.
        File.WriteAllText(ArtOutput + "/checkpoint-before.txt", ReadArtCheckpoint());
        CaptureArtViews("Before");
        File.WriteAllText(ArtOutput + "/baseline-complete.txt", "Scene, checkpoint, inventory and fixed camera views captured.\n");
    }

    private static string ReadArtCheckpoint()
    {
        var checkpoint = new StringBuilder("Read-only checkpoint backup; no PlayerPrefs writes in this art pass.\n");
        foreach (string key in new[] { "Version", "Stage", "Chores", "Step", "Money", "Candy", "Marbles", "Key", "Memories" })
        {
            string fullKey = "KuTy.Scene2Checkpoint." + key;
            checkpoint.AppendLine(fullKey + "=" + (PlayerPrefs.HasKey(fullKey) ? PlayerPrefs.GetInt(fullKey).ToString() : "<absent>"));
        }
        return checkpoint.ToString();
    }

    [MenuItem("KuTy/Scene 3/Art direction/2 Apply marble-yard sample")]
    public static void ApplyArtDirectionSample()
    {
        RequireArtScene();
        CaptureArtBaseline();
        if (!File.Exists(ArtOutput + "/baseline-complete.txt")) throw new IOException("Complete the baseline capture first.");
        if (Find(ArtRoot)) throw new InvalidOperationException("Sample already applied. Undo it or restore before.unity before applying again.");
        var before = GameplaySnapshot();
        var game = Find("BaiBanBi_VaNhomTre").GetComponentInChildren<MarbleShootingGame>();
        var data = new SerializedObject(game);
        var ring = (Collider)data.FindProperty("ringCollider").objectReferenceValue;
        var floor = (Collider)data.FindProperty("marbleGround").objectReferenceValue;
        if (!ring || !floor) throw new InvalidOperationException("The marble ring and playing surface must be assigned.");
        Vector3 center = ring.transform.position; center.y = floor.bounds.max.y;
        EnsureFolder(ArtAssets);
        Undo.IncrementCurrentGroup();
        int undo = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Scene 3 marble-yard art sample");
        try
        {
            var root = Group(ArtRoot, null).transform;
            Material lime = ArtMaterial("WarmLime", new Color(.67f, .63f, .50f));
            Material cap = ArtMaterial("OldBrick", new Color(.39f, .23f, .15f));
            Material wood = ArtMaterial("GreyTealWood", new Color(.20f, .29f, .27f));
            Material clay = ArtMaterial("Earthenware", new Color(.38f, .25f, .15f));
            // Frame the western edge. Keep the existing east-side approach and south entry open.
            ArtWall(root, center + new Vector3(-4.15f, 0, -2.9f), 3.2f, lime, cap);
            ArtWall(root, center + new Vector3(-4.15f, 0, 2.9f), 3.2f, lime, cap);
            for (int side = -1; side <= 1; side += 2)
            {
                Vector3 p = center + new Vector3(-4.15f, 0, side * 1.2f);
                Block(root, "TruCong_VoiCu", p + Vector3.up * .66f, new Vector3(.38f, 1.32f, .38f), lime);
                Block(root, "MuTru_Gach", p + Vector3.up * 1.35f, new Vector3(.46f, .09f, .46f), cap, false);
            }
            // A short open gate leaf reads as an entrance without spanning the walking route.
            var gate = Group("CongGo_Mo", root).transform;
            gate.position = center + new Vector3(-4.25f, 0, -.9f);
            gate.rotation = Quaternion.Euler(0, -55, 0);
            for (int i = 0; i < 6; i++)
            {
                var slat = Block(gate, "ThanhGo", Vector3.zero, new Vector3(.08f, .8f, .055f), wood, false);
                slat.transform.localPosition = new Vector3(0, .52f, i * .16f);
                slat.transform.localRotation = Quaternion.identity;
            }
            for (int i = 0; i < 2; i++)
            {
                var rail = Block(gate, "NanNgang", Vector3.zero, new Vector3(.07f, .065f, .94f), wood, false);
                rail.transform.localPosition = new Vector3(.015f, .33f + i * .38f, .4f);
                rail.transform.localRotation = Quaternion.identity;
            }
            // Yard objects stay behind the boundary and share one low-poly lathed mesh.
            Mesh jar = ArtJarMesh();
            for (int i = 0; i < 3; i++)
            {
                Vector3 p = center + new Vector3(-3.4f - i * .34f, 0, 1.8f + i * .42f);
                var obj = MeshProp(root, "ChumSanh_" + (i + 1), p, jar, clay);
                obj.transform.localScale = Vector3.one * (i == 0 ? 1 : .68f);
            }
            PlaceArtChildren(root, center);
            var sun = Find("Sun").GetComponent<Light>();
            Undo.RecordObject(sun, "Village afternoon light");
            Undo.RecordObject(sun.transform, "Village afternoon sun angle");
            sun.transform.rotation = Quaternion.Euler(32, -48, 0);
            sun.color = new Color(1, .91f, .77f);
            sun.intensity = 1.15f;
            Physics.SyncTransforms();
            VerifyGameplay(before);
            VerifyReferenceWalking();
            VerifyArtWalking(center);
            ValidateArtSample();
            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            if (!EditorSceneManager.SaveScene(SceneManager.GetActiveScene())) throw new IOException("Unable to save the sample scene.");
            Undo.CollapseUndoOperations(undo);
        }
        catch
        {
            Undo.RevertAllDownToGroup(undo);
            throw;
        }
        DynamicGI.UpdateEnvironment();
        SceneView.RepaintAll();
        Debug.Log("[VillageArtDirection] Marble-yard sample saved. Capture comparison after the environment probe has updated.");
    }

    private static Material ArtMaterial(string name, Color color)
    {
        string path = ArtAssets + "/" + name + ".mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material) return material;
        material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = name, enableInstancing = true };
        material.SetColor("_BaseColor", color);
        material.SetFloat("_Smoothness", .08f);
        AssetDatabase.CreateAsset(material, path);
        return material;
    }

    private static void ArtWall(Transform parent, Vector3 foot, float length, Material lime, Material cap)
    {
        Block(parent, "TuongThap_VoiCu", foot + Vector3.up * .48f, new Vector3(.24f, .96f, length), lime)
            .GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.On;
        Block(parent, "ChanTuong_Gach", foot + Vector3.up * .10f, new Vector3(.26f, .20f, length), cap, false);
        Block(parent, "MuTuong_Gach", foot + Vector3.up * .99f, new Vector3(.31f, .08f, length + .06f), cap, false);
    }

    private static Mesh ArtJarMesh()
    {
        string path = ArtAssets + "/ChumSanh.asset";
        var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (existing) return existing;
        // Open lip, inner neck and closed base; no expensive sphere primitives or colliders.
        Vector2[] profile = { new Vector2(0, 0), new Vector2(.20f, 0), new Vector2(.30f, .12f),
            new Vector2(.34f, .36f), new Vector2(.28f, .56f), new Vector2(.18f, .64f),
            new Vector2(.18f, .69f), new Vector2(.14f, .69f), new Vector2(.14f, .62f), new Vector2(0, .58f) };
        var vertices = new System.Collections.Generic.List<Vector3>();
        var indices = new System.Collections.Generic.List<int>();
        const int sides = 20;
        foreach (var p in profile)
            for (int j = 0; j <= sides; j++)
            {
                float a = j * Mathf.PI * 2 / sides;
                vertices.Add(new Vector3(Mathf.Cos(a) * p.x, p.y, Mathf.Sin(a) * p.x));
            }
        for (int i = 0; i < profile.Length - 1; i++)
            for (int j = 0; j < sides; j++)
            {
                int a = i * (sides + 1) + j, b = a + sides + 1;
                indices.AddRange(new[] { a, b, a + 1, a + 1, b, b + 1 });
            }
        var mesh = new Mesh { name = "ChumSanh_360tri" };
        mesh.SetVertices(vertices); mesh.SetTriangles(indices, 0); mesh.RecalculateNormals(); mesh.RecalculateBounds();
        AssetDatabase.CreateAsset(mesh, path);
        return mesh;
    }

    private static void PlaceArtChildren(Transform root, Vector3 center)
    {
        var old = Find("TreMoi_QuanhSanBi");
        if (!old) throw new InvalidOperationException("Existing children group missing.");
        Undo.RecordObject(old, "Retain original crowd for restoration");
        old.SetActive(false);
        Material material = AssetDatabase.LoadAssetAtPath<Material>(ActorAssets + "/trecon_Mat.mat");
        if (!material) throw new InvalidOperationException("Original children's material missing.");
        float[] angles = { 35, 120, 205, 275 };
        for (int i = 0; i < 4; i++)
        {
            float a = angles[i] * Mathf.Deg2Rad;
            Vector3 foot = center + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * (i == 3 ? 2.9f : 2.25f);
            Mesh mesh = i == 3 ? ShoulderPoseMesh(0) : ArtCrouchMesh(i);
            var child = MeshProp(root, "BanChoiBi_" + (i + 1), foot, mesh, material);
            child.transform.rotation = Quaternion.LookRotation(new Vector3(center.x - foot.x, 0, center.z - foot.z));
            child.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.On;
            // Visual actors are outside the ring; no new NPC obstacles in the aiming or walking space.
        }
    }

    private static Mesh ArtCrouchMesh(int variant)
    {
        string path = ArtAssets + "/TreNgoiXom_" + variant + ".asset";
        var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (existing) return existing;
        Mesh source = NormalizedChildMesh();
        var pose = new ChildPose();
        ArmBones(-1, false, false, out pose.leftUpper, out pose.leftLower);
        ArmBones(1, false, false, out pose.rightUpper, out pose.rightLower);
        for (int side = -1; side <= 1; side += 2)
        {
            Vector3 hip = new Vector3(side * .105f, .505f, -.045f);
            Vector3 knee = new Vector3(side * (side < 0 ? .245f : .255f), .285f, .015f);
            Vector3 ankle = new Vector3(side * (side < 0 ? .315f : .365f), .065f, -.065f);
            Vector3 targetHip = hip + Vector3.down * .235f;
            Vector3 targetKnee = targetHip + new Vector3(side * .09f, -.09f, .23f);
            var upper = new PoseBone { source = hip, target = targetHip, rotation = Quaternion.FromToRotation(knee - hip, targetKnee - targetHip) };
            var lower = new PoseBone { source = knee, target = upper.Point(knee), rotation = Quaternion.FromToRotation(ankle - knee, new Vector3(side * .025f, -.15f, -.18f)) };
            if (side < 0) { pose.leftThigh = upper; pose.leftCalf = lower; }
            else { pose.rightThigh = upper; pose.rightCalf = lower; }
        }
        var mesh = UnityEngine.Object.Instantiate(source);
        mesh.name = "TreNgoiXom_" + variant;
        var vertices = source.vertices;
        for (int i = 0; i < vertices.Length; i++)
        {
            Vector3 p = vertices[i];
            float legWeight = 1 - Smooth(.45f, .56f, p.y);
            var upper = p.x < 0 ? pose.leftThigh : pose.rightThigh;
            var lower = p.x < 0 ? pose.leftCalf : pose.rightCalf;
            Vector3 leg = Vector3.Lerp(upper.Point(p), lower.Point(p), 1 - Smooth(.23f, .35f, p.y));
            Vector3 body = pose.Vertex(p) + Vector3.down * .235f;
            // Small torso lean varies the silhouettes without touching the face or UVs.
            body.z += Smooth(.52f, .9f, p.y) * (.035f + variant * .025f);
            vertices[i] = Vector3.Lerp(body, leg, legWeight);
        }
        float bottom = vertices.Min(p => p.y);
        for (int i = 0; i < vertices.Length; i++) vertices[i].y -= bottom;
        mesh.vertices = vertices;
        mesh.RecalculateNormals(); mesh.RecalculateBounds();
        // Source tangents describe the old pose; this material uses albedo only.
        mesh.tangents = new Vector4[0];
        AssetDatabase.CreateAsset(mesh, path);
        return mesh;
    }

    [MenuItem("KuTy/Scene 3/Art direction/3 Validate and capture comparison")]
    public static void CaptureArtComparison()
    {
        RequireArtScene();
        ValidateArtSample();
        VerifyReferenceWalking();
        VerifyArtWalking(ArtRingCenter());
        CaptureArtViews("After");
    }

    private static Vector3 ArtRingCenter()
    {
        var game = Find("BaiBanBi_VaNhomTre").GetComponentInChildren<MarbleShootingGame>();
        var data = new SerializedObject(game);
        var ring = (Collider)data.FindProperty("ringCollider").objectReferenceValue;
        var floor = (Collider)data.FindProperty("marbleGround").objectReferenceValue;
        var center = ring.transform.position; center.y = floor.bounds.max.y;
        return center;
    }

    [MenuItem("KuTy/Scene 3/Art direction/4 Refine sample props and wall shadows")]
    public static void RefineArtSample()
    {
        RequireArtScene();
        var root = Find(ArtRoot);
        if (!root) throw new InvalidOperationException("Apply the sample first.");
        var before = GameplaySnapshot();
        Vector3 center = ArtRingCenter();
        Undo.IncrementCurrentGroup();
        int undo = Undo.GetCurrentGroup();
        Undo.RegisterFullObjectHierarchyUndo(root, "Refine art sample props and shadows");
        try
        {
            for (int i = 0; i < 3; i++)
                root.transform.Find("ChumSanh_" + (i + 1)).position = center + new Vector3(-3.4f - i * .34f, 0, 1.8f + i * .42f);
            foreach (var r in root.GetComponentsInChildren<Renderer>().Where(r => r.name == "TuongThap_VoiCu"))
                r.shadowCastingMode = ShadowCastingMode.On;
            Physics.SyncTransforms();
            VerifyGameplay(before); VerifyReferenceWalking(); VerifyArtWalking(center); ValidateArtSample();
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            if (!EditorSceneManager.SaveScene(SceneManager.GetActiveScene())) throw new IOException("Unable to save sample refinement.");
            Undo.CollapseUndoOperations(undo);
        }
        catch { Undo.RevertAllDownToGroup(undo); throw; }
    }

    private static void VerifyArtWalking(Vector3 center)
    {
        var probe = new GameObject("ArtWalkingProbe") { hideFlags = HideFlags.HideAndDontSave };
        var controller = probe.AddComponent<CharacterController>();
        controller.height = 1.6f; controller.center = Vector3.up * .8f;
        controller.radius = .24f; controller.stepOffset = .3f; controller.slopeLimit = 48;
        Vector3[][] routes = {
            new[] { new Vector3(0, 0, -5), new Vector3(0, 0, -2) },
            new[] { new Vector3(5, 0, 0), new Vector3(1.4f, 0, 0) },
            new[] { new Vector3(-5, 0, 0), new Vector3(-2.5f, 0, 0) }
        };
        try
        {
            foreach (var route in routes)
                for (int direction = 0; direction < 2; direction++)
                {
                    Vector3 from = center + route[direction], to = center + route[1 - direction];
                    controller.enabled = false; probe.transform.position = from + Vector3.up * .1f;
                    controller.enabled = true; Physics.SyncTransforms();
                    for (int i = 0; i < 180; i++)
                    {
                        Vector3 d = to - probe.transform.position; d.y = 0;
                        if (d.magnitude < .12f) break;
                        controller.Move(Vector3.ClampMagnitude(d, .05f) + Vector3.down * .025f);
                    }
                    Vector3 remaining = to - probe.transform.position; remaining.y = 0;
                    if (remaining.magnitude > .25f || probe.transform.position.y < center.y - .35f)
                        throw new Exception("Sample route blocked: " + from + " -> " + to + " at " + probe.transform.position);
                }
        }
        finally { UnityEngine.Object.DestroyImmediate(probe); }
        File.WriteAllText(ArtOutput + "/sample-walking.txt", "PASS: south approach, east approach and west gate, each in both directions.\nCharacterController height 1.6m, radius 0.24m, step 0.3m.\n");
    }

    private static void ValidateArtSample()
    {
        var root = Find(ArtRoot);
        if (!root || root.GetComponentsInChildren<MeshFilter>().Count(f => f.name.StartsWith("BanChoiBi_")) != 4)
            throw new Exception("Expected three crouching children and one spectator.");
        foreach (var child in root.GetComponentsInChildren<MeshFilter>().Where(f => f.name.StartsWith("BanChoiBi_")))
        {
            var bounds = child.GetComponent<Renderer>().bounds;
            if (bounds.size.y < .55f || bounds.size.y > 1.3f || Mathf.Abs(bounds.min.y - child.transform.position.y) > .01f)
                throw new Exception("Child pose scale/grounding failed: " + child.name);
        }
        if (root.GetComponentsInChildren<MonoBehaviour>().Length != 0) throw new Exception("Sample must not add per-frame behaviours.");
        var report = new StringBuilder("PASS: sample scale, child grounding and no new runtime scripts.\n");
        report.AppendLine("Reference CharacterController routes verified by Apply / Capture comparison.");
        report.AppendLine("Original game component serialization verified unchanged during Apply.");
        report.AppendLine("Original play surface, scoring colliders, camera anchors, shop and quest objects retained.");
        report.AppendLine("Rice/tree LOD, water and shadow pipeline settings retained. One existing sun adjusted.");
        report.AppendLine("Review poses and frontage in gameplay before expanding. Build CPU/GPU measurements and full quest tests pending.");
        Directory.CreateDirectory(ArtOutput);
        string checkpoint = ReadArtCheckpoint();
        File.WriteAllText(ArtOutput + "/checkpoint-after.txt", checkpoint);
        report.AppendLine("Checkpoint matches baseline: " + (checkpoint == File.ReadAllText(ArtOutput + "/checkpoint-before.txt")));
        File.WriteAllText(ArtOutput + "/sample-validation.txt", report.ToString());
    }

    private static void CaptureArtViews(string phase)
    {
        string folder = ArtOutput + "/" + phase;
        Directory.CreateDirectory(folder);
        var floor = Find("SanBi_DatNen").GetComponent<Collider>();
        Vector3 c = floor.bounds.center; c.y = floor.bounds.max.y;
        Vector3[] positions = { new Vector3(-5, 1.8f, -7), c + new Vector3(3.6f, 1.65f, -4.8f), new Vector3(-6, 1.8f, 55), c + new Vector3(10, 9, -12) };
        Vector3[] targets = { c + Vector3.up * .6f, c + new Vector3(-1, .7f, 0), new Vector3(-7, 1, 66), c + new Vector3(-1, .5f, 0) };
        string[] names = { "01_Entrance", "02_MarbleYard", "03_VillageEnd", "04_Overview" };
        for (int i = 0; i < positions.Length; i++)
        {
            var obj = new GameObject("ArtDirectionCapture") { hideFlags = HideFlags.HideAndDontSave };
            var camera = obj.AddComponent<Camera>();
            camera.enabled = false; camera.fieldOfView = 65; camera.nearClipPlane = .03f; camera.farClipPlane = 300;
            camera.allowHDR = true;
            camera.transform.SetPositionAndRotation(positions[i], Quaternion.LookRotation(targets[i] - positions[i]));
            camera.GetUniversalAdditionalCameraData().renderPostProcessing = true;
            var target = new RenderTexture(1920, 1080, 24);
            var image = new Texture2D(1920, 1080, TextureFormat.RGB24, false);
            var previous = RenderTexture.active;
            try
            {
                target.Create();
                RenderPipeline.SubmitRenderRequest(camera, new UniversalRenderPipeline.SingleCameraRequest { destination = target });
                RenderTexture.active = target;
                image.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0); image.Apply();
                File.WriteAllBytes(folder + "/" + names[i] + ".png", image.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active = previous; target.Release();
                UnityEngine.Object.DestroyImmediate(image); UnityEngine.Object.DestroyImmediate(target); UnityEngine.Object.DestroyImmediate(obj);
            }
        }
    }
}
