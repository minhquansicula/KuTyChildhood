using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

/// <summary>
/// Replaces the heavy Scene 2 village mesh with one unlit image plane behind the wooden window.
/// </summary>
public static class Scene2WindowVillageBackdropSetup
{
    const string SceneName = "Act2_MemoryWorld_Home";
    const string WindowPath = "Act2_RusticWalls/Wall_02_WoodenShutters";
    const string TexturePath = "Assets/_Project/Art/Scene2/Textures/Architecture/T_WindowVillage_Backdrop.png";
    const string MaterialPath = "Assets/_Project/Art/Scene2/Materials/Architecture/MAT_WindowVillageBackdrop.mat";
    const string BackdropName = "WindowVillageBackdrop";
    const string PreviewPath = "Temp/scene2-window-village-backdrop.png";
    const string CycloramaMeshFolder = "Assets/_Project/Art/Scene2/Models/Architecture";

    [MenuItem("KuTy/Scene 2/Build lightweight window village backdrop")]
    public static void Build()
    {
        var scene = SceneManager.GetActiveScene();
        if (EditorApplication.isPlayingOrWillChangePlaymode || scene.name != SceneName)
            throw new InvalidOperationException("Open Scene 2 in Edit Mode first.");

        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(TexturePath);
        if (!texture) throw new InvalidOperationException("Missing backdrop texture: " + TexturePath);

        // Preserve any unrelated unsaved user changes before replacing the old exterior.
        if (!EditorSceneManager.SaveScene(scene))
            throw new InvalidOperationException("Could not save Scene 2 before replacement.");
        string backup = Path.Combine(".unity-validation", "scene2-window-backdrop-" + DateTime.Now.ToString("yyyyMMdd-HHmmss"));
        Directory.CreateDirectory(backup);
        File.Copy(scene.path, Path.Combine(backup, "Before.unity"));

        var oldVillage = GameObject.Find("VillageExterior");
        if (oldVillage) Undo.DestroyObjectImmediate(oldVillage);
        var oldBackdrop = GameObject.Find(BackdropName);
        if (oldBackdrop) Undo.DestroyObjectImmediate(oldBackdrop);

        var wall = GameObject.Find(WindowPath);
        if (!wall) throw new InvalidOperationException("Missing window group: " + WindowPath);
        var frames = wall.GetComponentsInChildren<Renderer>(true)
            .Where(r => r.name.StartsWith("WindowFrame_", StringComparison.Ordinal)).ToArray();
        if (frames.Length == 0) throw new InvalidOperationException("No window frame renderers found under " + WindowPath);

        Bounds opening = BoundsOf(frames);
        Bounds house = BoundsOf(GameObject.Find("HouseLayout"));
        Vector3 outside = ExteriorDirection(opening, house);

        var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        if (!material)
        {
            var shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Texture");
            if (!shader) throw new InvalidOperationException("No unlit shader is available.");
            material = new Material(shader) { name = "MAT_WindowVillageBackdrop" };
            AssetDatabase.CreateAsset(material, MaterialPath);
        }
        Undo.RecordObject(material, "Configure window village backdrop");
        if (material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap", texture);
        if (material.HasProperty("_MainTex")) material.SetTexture("_MainTex", texture);
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", Color.white);
        if (material.HasProperty("_Color")) material.SetColor("_Color", Color.white);
        if (material.HasProperty("_Cull")) material.SetFloat("_Cull", (float)CullMode.Off);
        material.renderQueue = (int)RenderQueue.Geometry;
        EditorUtility.SetDirty(material);

        var root = new GameObject(BackdropName);
        Undo.RegisterCreatedObjectUndo(root, "Create lightweight window village backdrop");
        root.transform.position = opening.center + outside * Mathf.Max(0.75f, OpeningWidth(opening) * 0.15f);
        root.transform.rotation = Quaternion.LookRotation(-outside, Vector3.up);

        var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        quad.name = "VillageBackdropQuad";
        Undo.RegisterCreatedObjectUndo(quad, "Create lightweight window village backdrop");
        quad.transform.SetParent(root.transform, false);
        var collider = quad.GetComponent<Collider>();
        if (collider) UnityEngine.Object.DestroyImmediate(collider);

        float width = OpeningWidth(opening) * 1.18f;
        float height = Mathf.Max(1.2f, opening.size.y * 1.18f);
        float imageAspect = (float)texture.width / texture.height;
        if (width / height < imageAspect) width = height * imageAspect;
        else height = width / imageAspect;
        quad.transform.localScale = new Vector3(width, height, 1f);

        var renderer = quad.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.lightProbeUsage = LightProbeUsage.Off;
        renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
        renderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
        GameObjectUtility.SetStaticEditorFlags(root, StaticEditorFlags.BatchingStatic);
        GameObjectUtility.SetStaticEditorFlags(quad, StaticEditorFlags.BatchingStatic);

        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Scene save failed.");

        Selection.activeGameObject = root;
        ShowComposition();
        Debug.Log("SCENE2_WINDOW_VILLAGE_BACKDROP_BUILT opening=" + opening +
                  " outside=" + outside.ToString("F3") + " plane=" + width.ToString("F2") + "x" +
                  height.ToString("F2") + " oldVillageRemoved=" + (oldVillage != null) + " backup=" + backup);
    }

    [MenuItem("KuTy/Scene 2/Show window village composition")]
    public static void ShowComposition()
    {
        ViewGeometry(out var opening, out var outside, out var viewPosition);
        var sceneView = SceneView.lastActiveSceneView;
        if (sceneView)
        {
            sceneView.orthographic = false;
            sceneView.LookAt(opening.center, Quaternion.LookRotation(opening.center - viewPosition, Vector3.up),
                Vector3.Distance(viewPosition, opening.center), false, true);
            sceneView.Repaint();
        }
        var backdrop = GameObject.Find(BackdropName);
        if (backdrop) Selection.activeGameObject = backdrop;
        SceneView.RepaintAll();
    }

    [MenuItem("KuTy/Scene 2/Capture window village composition")]
    public static void CaptureComposition()
    {
        ViewGeometry(out var opening, out var outside, out var viewPosition);
        var source = GameObject.Find("Player/PlayerCamera")?.GetComponent<Camera>() ?? Camera.main;
        var cameraObject = new GameObject("Scene2WindowBackdropPreviewCamera");
        var camera = cameraObject.AddComponent<Camera>();
        if (source) camera.CopyFrom(source);
        camera.enabled = false;
        camera.fieldOfView = 52f;
        camera.nearClipPlane = 0.05f;
        camera.farClipPlane = 250f;
        camera.aspect = 16f / 9f;
        camera.transform.position = viewPosition;
        camera.transform.rotation = Quaternion.LookRotation(opening.center - viewPosition, Vector3.up);

        var oldActive = RenderTexture.active;
        var target = RenderTexture.GetTemporary(1280, 720, 24);
        var image = new Texture2D(1280, 720, TextureFormat.RGB24, false);
        try
        {
            camera.targetTexture = target;
            camera.Render();
            RenderTexture.active = target;
            image.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
            image.Apply();
            File.WriteAllBytes(PreviewPath, image.EncodeToPNG());
        }
        finally
        {
            RenderTexture.active = oldActive;
            RenderTexture.ReleaseTemporary(target);
            UnityEngine.Object.DestroyImmediate(image);
            UnityEngine.Object.DestroyImmediate(cameraObject);
        }
        Debug.Log("SCENE2_WINDOW_VILLAGE_BACKDROP_CAPTURED path=" + PreviewPath +
                  " camera=" + viewPosition.ToString("F3") + " outside=" + outside.ToString("F3"));
    }

    [MenuItem("KuTy/Scene 2/Capture curved backdrop overview")]
    public static void CaptureCurvedBackdropOverview()
    {
        var root = GameObject.Find(BackdropName);
        if (!root) throw new InvalidOperationException("Missing " + BackdropName + ".");
        Bounds house = BoundsOf(GameObject.Find("HouseLayout"));
        Bounds composition = house;
        foreach (var renderer in root.GetComponentsInChildren<Renderer>(true)) composition.Encapsulate(renderer.bounds);

        float span = Mathf.Max(composition.size.x, composition.size.z);
        var cameraObject = new GameObject("Scene2CurvedBackdropOverviewCamera") { hideFlags = HideFlags.HideAndDontSave };
        var camera = cameraObject.AddComponent<Camera>();
        var source = GameObject.Find("Player/PlayerCamera")?.GetComponent<Camera>() ?? Camera.main;
        if (source) camera.CopyFrom(source);
        camera.enabled = false;
        camera.fieldOfView = 58f;
        camera.nearClipPlane = 0.05f;
        camera.farClipPlane = 500f;
        camera.aspect = 16f / 9f;
        camera.transform.position = house.center + new Vector3(-span * 0.36f, span * 0.58f, -span * 0.72f);
        camera.transform.rotation = Quaternion.LookRotation(house.center + Vector3.up * 1.5f - camera.transform.position, Vector3.up);

        const string path = "Temp/scene2-window-village-curved-overview.png";
        var oldActive = RenderTexture.active;
        var target = RenderTexture.GetTemporary(1280, 720, 24);
        var image = new Texture2D(1280, 720, TextureFormat.RGB24, false);
        try
        {
            camera.targetTexture = target;
            camera.Render();
            RenderTexture.active = target;
            image.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
            image.Apply();
            File.WriteAllBytes(path, image.EncodeToPNG());
        }
        finally
        {
            RenderTexture.active = oldActive;
            RenderTexture.ReleaseTemporary(target);
            UnityEngine.Object.DestroyImmediate(image);
            UnityEngine.Object.DestroyImmediate(cameraObject);
        }
        Debug.Log("SCENE2_CURVED_BACKDROP_OVERVIEW_CAPTURED path=" + path + " bounds=" + composition);
    }

    [MenuItem("KuTy/Scene 2/Log window frame geometry")]
    public static void LogWindowFrameGeometry()
    {
        var wall = GameObject.Find(WindowPath);
        if (!wall) throw new InvalidOperationException("Missing window group: " + WindowPath);
        foreach (var renderer in wall.GetComponentsInChildren<Renderer>(true)
                     .Where(r => r.name.StartsWith("WindowFrame_", StringComparison.Ordinal))
                     .OrderBy(r => r.bounds.center.x).ThenBy(r => r.bounds.center.z).ThenBy(r => r.bounds.center.y))
            Debug.Log("WINDOW_FRAME_GEOMETRY name=" + renderer.name + " center=" +
                      renderer.bounds.center.ToString("F3") + " size=" + renderer.bounds.size.ToString("F3"));
    }

    [MenuItem("KuTy/Scene 2/Curve placed village backdrops")]
    public static void CurvePlacedBackdrops()
    {
        var scene = SceneManager.GetActiveScene();
        if (EditorApplication.isPlayingOrWillChangePlaymode || scene.name != SceneName)
            throw new InvalidOperationException("Open Scene 2 in Edit Mode first.");

        // Save first so the user's hand-positioning and unrelated scene edits are preserved verbatim.
        if (!EditorSceneManager.SaveScene(scene))
            throw new InvalidOperationException("Could not save the user's current Scene 2 changes.");
        string backup = Path.Combine(".unity-validation", "scene2-curved-backdrop-" + DateTime.Now.ToString("yyyyMMdd-HHmmss"));
        Directory.CreateDirectory(backup);
        File.Copy(scene.path, Path.Combine(backup, "Before.unity"));

        var root = GameObject.Find(BackdropName);
        if (!root) throw new InvalidOperationException("Missing " + BackdropName + ".");
        var filters = root.GetComponentsInChildren<MeshFilter>(true)
            .Where(f => f.name.StartsWith("VillageBackdropQuad", StringComparison.Ordinal))
            .OrderBy(f => f.name).ToArray();
        if (filters.Length == 0) throw new InvalidOperationException("No placed village backdrop panels were found.");

        Bounds house = BoundsOf(GameObject.Find("HouseLayout"));
        var report = new System.Text.StringBuilder();
        for (int i = 0; i < filters.Length; i++)
        {
            var filter = filters[i];
            var renderer = filter.GetComponent<MeshRenderer>();
            if (!renderer) throw new InvalidOperationException("Backdrop renderer missing on " + filter.name);

            Vector3 position = filter.transform.localPosition;
            Quaternion rotation = filter.transform.localRotation;
            Vector3 scale = filter.transform.localScale;
            Vector3 toHouse = house.center - renderer.bounds.center;
            float facingSign = Mathf.Sign(Vector3.Dot(filter.transform.forward, toHouse));
            if (Mathf.Approximately(facingSign, 0f)) facingSign = 1f;

            float widthWorld = Mathf.Abs(filter.transform.lossyScale.x);
            float zWorldScale = Mathf.Max(0.0001f, Mathf.Abs(filter.transform.lossyScale.z));
            float floorDepthWorld = Mathf.Clamp(widthWorld * 0.42f, 6f, 12f);
            float horizontalBowWorld = Mathf.Clamp(widthWorld * 0.07f, 0.8f, 2f);
            var generated = CreateCycloramaMesh(facingSign, floorDepthWorld / zWorldScale,
                horizontalBowWorld / zWorldScale);
            string meshPath = CycloramaMeshFolder + "/SM_WindowVillageCyclorama_" + (i + 1).ToString("D2") + ".asset";
            var meshAsset = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
            if (meshAsset)
            {
                EditorUtility.CopySerialized(generated, meshAsset);
                UnityEngine.Object.DestroyImmediate(generated);
                EditorUtility.SetDirty(meshAsset);
            }
            else
            {
                generated.name = "SM_WindowVillageCyclorama_" + (i + 1).ToString("D2");
                AssetDatabase.CreateAsset(generated, meshPath);
                meshAsset = generated;
            }

            Undo.RecordObject(filter, "Curve village backdrop");
            filter.sharedMesh = meshAsset;
            EditorUtility.SetDirty(filter);
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            renderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
            EditorUtility.SetDirty(renderer);

            // Mesh replacement must never overwrite the placement the user just authored.
            if (filter.transform.localPosition != position || filter.transform.localRotation != rotation ||
                filter.transform.localScale != scale)
                throw new InvalidOperationException("Backdrop transform changed unexpectedly: " + filter.name);

            report.AppendLine(filter.name + " mesh=" + meshPath + " position=" + position.ToString("F3") +
                              " rotation=" + rotation.eulerAngles.ToString("F1") + " scale=" + scale.ToString("F3") +
                              " bow=" + horizontalBowWorld.ToString("F2") + " floorDepth=" + floorDepthWorld.ToString("F2") +
                              " towardHouseSign=" + facingSign);
        }

        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Scene save failed.");
        Selection.activeGameObject = root;
        SceneView.RepaintAll();
        File.WriteAllText(Path.Combine(backup, "curved-backdrops.txt"), report.ToString());
        Debug.Log("SCENE2_CURVED_BACKDROPS_SUCCESS count=" + filters.Length + " backup=" + backup + "\n" + report);
    }

    static Mesh CreateCycloramaMesh(float facingSign, float floorDepth, float horizontalBow)
    {
        const int horizontalSegments = 32;
        const int verticalSegments = 20;
        const float floorImageShare = 0.30f;
        var vertices = new List<Vector3>((horizontalSegments + 1) * (verticalSegments + 1));
        var uvs = new List<Vector2>(vertices.Capacity);
        var triangles = new List<int>(horizontalSegments * verticalSegments * 6);

        for (int yIndex = 0; yIndex <= verticalSegments; yIndex++)
        {
            float v = yIndex / (float)verticalSegments;
            float localY;
            float floorSweep;
            if (v <= floorImageShare)
            {
                float t = v / floorImageShare;
                float smooth = t * t * (3f - 2f * t);
                localY = Mathf.Lerp(-0.50f, -0.34f, smooth);
                floorSweep = Mathf.Lerp(floorDepth, 0f, smooth);
            }
            else
            {
                float t = (v - floorImageShare) / (1f - floorImageShare);
                localY = Mathf.Lerp(-0.34f, 0.50f, t);
                floorSweep = 0f;
            }

            for (int xIndex = 0; xIndex <= horizontalSegments; xIndex++)
            {
                float u = xIndex / (float)horizontalSegments;
                float localX = u - 0.5f;
                float edge = Mathf.Abs(localX * 2f);
                float bow = horizontalBow * edge * edge;
                vertices.Add(new Vector3(localX, localY, facingSign * (floorSweep + bow)));
                uvs.Add(new Vector2(u, v));
            }
        }

        int stride = horizontalSegments + 1;
        for (int y = 0; y < verticalSegments; y++)
        for (int x = 0; x < horizontalSegments; x++)
        {
            int a = y * stride + x;
            int b = a + 1;
            int c = a + stride;
            int d = c + 1;
            triangles.Add(a); triangles.Add(c); triangles.Add(b);
            triangles.Add(b); triangles.Add(c); triangles.Add(d);
        }

        var mesh = new Mesh { name = "SM_WindowVillageCyclorama_Generated" };
        mesh.SetVertices(vertices);
        mesh.SetUVs(0, uvs);
        mesh.SetTriangles(triangles, 0, true);
        mesh.RecalculateNormals();
        mesh.RecalculateTangents();
        mesh.RecalculateBounds();
        return mesh;
    }

    static void ViewGeometry(out Bounds opening, out Vector3 outside, out Vector3 viewPosition)
    {
        var wall = GameObject.Find(WindowPath);
        if (!wall) throw new InvalidOperationException("Missing window group: " + WindowPath);
        var frames = wall.GetComponentsInChildren<Renderer>(true)
            .Where(r => r.name.StartsWith("WindowFrame_", StringComparison.Ordinal)).ToArray();
        if (frames.Length == 0) throw new InvalidOperationException("No window frame renderers found.");
        opening = BoundsOf(frames);
        outside = ExteriorDirection(opening, BoundsOf(GameObject.Find("HouseLayout")));
        float distance = Mathf.Max(3.5f, OpeningWidth(opening) * 0.9f);
        viewPosition = opening.center - outside * distance;
        viewPosition.y = Mathf.Clamp(opening.center.y, opening.min.y + 0.55f, opening.max.y - 0.25f);
    }

    static Vector3 ExteriorDirection(Bounds opening, Bounds house)
    {
        bool wallRunsAlongX = opening.size.x >= opening.size.z;
        Vector3 axis = wallRunsAlongX ? Vector3.forward : Vector3.right;
        float delta = wallRunsAlongX ? opening.center.z - house.center.z : opening.center.x - house.center.x;
        if (Mathf.Abs(delta) < 0.01f) delta = 1f;
        return axis * Mathf.Sign(delta);
    }

    static float OpeningWidth(Bounds opening) => Mathf.Max(opening.size.x, opening.size.z);

    static Bounds BoundsOf(GameObject root)
    {
        if (!root) throw new InvalidOperationException("Required object is missing.");
        return BoundsOf(root.GetComponentsInChildren<Renderer>(true));
    }

    static Bounds BoundsOf(Renderer[] renderers)
    {
        if (renderers == null || renderers.Length == 0) throw new InvalidOperationException("No renderers found.");
        var bounds = renderers[0].bounds;
        foreach (var renderer in renderers.Skip(1)) bounds.Encapsulate(renderer.bounds);
        return bounds;
    }
}
