#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

/// <summary>
/// Authors the dual-set 3D cinematic ending inside Act_Ending ("Tỉnh Giấc Bình Minh Nơi Bàn Làm Việc"):
///   1. HomeMemorySet: Cloned from Act2_MemoryWorld_Home (Blender kitchen hearth, steaming cooking pot,
///      sunlit home interior with Mother, and Mother's letter beside the vintage radio).
///   2. OfficeDawnSet: Cloned from Act1_Office_Blockout (Quân's workstation with CRT monitor, keyboard,
///      coffee mug, East Panoramic Window, city exterior backdrop, and Mother's letter resting in morning sunlight).
/// Safe to re-run; rebuilds its own objects cleanly.
/// </summary>
public static class EndingSceneBuilder
{
    private const string ScenePath = "Assets/_Project/Scenes/Act_Ending.unity";
    private const string HomeScenePath = "Assets/_Project/Scenes/Act2_MemoryWorld_Home.unity";
    private const string OfficeScenePath = "Assets/_Project/Scenes/Act1_Office_Blockout.unity";
    private const string ProfileFolder = "Assets/_Project/Generated/Ending";
    private const string ProfilePath = ProfileFolder + "/EndingVolume.asset";
    private const string LetterMatEndingPath = ProfileFolder + "/MAT_EndingLetter.mat";
    private const string EyeMatEndingPath = ProfileFolder + "/MAT_EndingMotherEyes.mat";
    private const string SteamMatEndingPath = ProfileFolder + "/MAT_CookingSteam.mat";
    private const string Mats = "Assets/_Project/Art/Materials/Scene3/";
    private const string LetterModelPath = "Assets/_Project/Art/Scene2/Models/Bedroom/SM_Letter.fbx";
    private const string LetterTexPath = "Assets/_Project/Art/Scene2/Textures/Bedroom/Letter/T_Letter_BaseColor.png";

    private static readonly Quaternion HomeSunRotation = Quaternion.Euler(28f, 315f, 0f);
    private static readonly Quaternion OfficeSunRotation = Quaternion.Euler(13f, -96f, 0f);

    [MenuItem("KuTy/Ending/Build cinematic world")]
    public static void Build()
    {
        if (Application.isPlaying) throw new System.InvalidOperationException("Stop Play Mode first.");
        if (EditorSceneManager.GetActiveScene().path != ScenePath)
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        var scene = EditorSceneManager.GetActiveScene();

        Directory.CreateDirectory(ProfileFolder);
        KuTyEndingVoices.Import();

        foreach (string stale in new[] { "EndingWorld", "EndingDirector", "EndingVolume" })
        {
            var old = GameObject.Find(stale);
            if (old != null) Object.DestroyImmediate(old);
        }

        var cameraObject = FindCamera();
        Light sun = FindOrCreateSun();
        ConfigureRenderSettings(sun);

        var world = new GameObject("EndingWorld").transform;

        var homeSet = BuildHomeMemorySet(world, scene,
            out Vector3 potFocus, out Vector3 windowFocusPos,
            out Vector3 letterFocusPos, out Vector3 letterOutward);

        var officeSet = BuildOfficeDawnSet(world, scene,
            out Vector3 officeLetterPos, out Vector3 officeMonitorPos, out float deskTopY);

        BuildAnchors(world, potFocus, windowFocusPos, letterFocusPos, letterOutward, officeLetterPos, officeMonitorPos, deskTopY);

        // Start with HomeMemorySet active and OfficeDawnSet hidden until the eyelid transition.
        homeSet.SetActive(true);
        officeSet.SetActive(false);

        var volume = BuildVolume();
        SetupCamera(cameraObject, volume);
        SetupDirector(world, cameraObject, sun, null);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("[Ending builder] Dual-set cinematic world (HomeMemorySet + OfficeDawnSet) built and saved.");
    }

    // ---------------- core objects ----------------
    private static GameObject FindCamera()
    {
        var cam = Camera.main;
        if (cam == null) cam = Object.FindObjectOfType<Camera>(true);
        if (cam == null)
        {
            var created = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            cam = created.GetComponent<Camera>();
        }
        cam.gameObject.tag = "MainCamera";
        return cam.gameObject;
    }

    private static Light FindOrCreateSun()
    {
        Light sun = null;
        foreach (var light in Object.FindObjectsOfType<Light>(true))
            if (light.type == LightType.Directional) { sun = light; break; }
        if (sun == null)
        {
            var go = new GameObject("Sun");
            sun = go.AddComponent<Light>();
            sun.type = LightType.Directional;
        }
        sun.gameObject.name = "Sun";
        sun.transform.rotation = HomeSunRotation;
        sun.color = new Color(1f, .88f, .68f);
        sun.intensity = 2.1f;
        sun.shadows = LightShadows.Soft;
        sun.shadowStrength = .82f;
        sun.shadowBias = .03f;
        sun.shadowNormalBias = .25f;
        return sun;
    }

    private static void ConfigureRenderSettings(Light sun)
    {
        var sky = AssetDatabase.LoadAssetAtPath<Material>(Mats + "VillageGoldenAfternoon.mat");
        if (sky != null) RenderSettings.skybox = sky;
        RenderSettings.sun = sun;
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogColor = new Color(.95f, .83f, .66f);
        RenderSettings.fogDensity = .0045f;
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(.74f, .68f, .58f);
        RenderSettings.ambientEquatorColor = new Color(.56f, .49f, .42f);
        RenderSettings.ambientGroundColor = new Color(.34f, .28f, .22f);
        RenderSettings.ambientIntensity = 1.05f;
        RenderSettings.reflectionIntensity = .65f;
    }

    // ---------------- Set 1: HomeMemorySet ----------------
    private static GameObject BuildHomeMemorySet(Transform world, Scene targetScene,
        out Vector3 potFocus, out Vector3 windowFocusPos,
        out Vector3 letterFocusPos, out Vector3 letterOutward)
    {
        var homeRoot = new GameObject("HomeMemorySet");
        homeRoot.transform.SetParent(world, false);

        string[] skipNames =
        {
            "Player", "Canvas", "UI", "EventSystem", "Directional Light", "Sun",
            "Global Volume", "Act2_GlobalVolume", "GameManager", "AudioManager",
            "QuestManager", "KitchenFlowManager", "Scene2RadioKitchenWorld"
        };

        Scene homeScene = EditorSceneManager.OpenScene(HomeScenePath, OpenSceneMode.Additive);
        try
        {
            foreach (var root in homeScene.GetRootGameObjects())
            {
                if (skipNames.Contains(root.name)) continue;
                if (root.GetComponentsInChildren<Renderer>(true).Length == 0 &&
                    root.GetComponentsInChildren<Light>(true).Length == 0)
                    continue;

                var clone = Object.Instantiate(root);
                clone.name = root.name;
                SceneManager.MoveGameObjectToScene(clone, targetScene);
                clone.transform.SetParent(homeRoot.transform, true);
                StripGameplayComponents(clone, true);
            }
        }
        finally
        {
            EditorSceneManager.CloseScene(homeScene, true);
        }

        // Remove any existing Mother inside the cloned set so we place one cleanly posed Mother.
        foreach (var t in homeRoot.GetComponentsInChildren<Transform>(true))
        {
            if (t != null && (t.name == "Mother" || t.name == "MotherHolder"))
                Object.DestroyImmediate(t.gameObject);
        }

        // Focus Shot 1 on the cooking stove + steaming rice pot ("Tend the rice pot")
        Transform cookGroup = FindDescendant(homeRoot.transform, "Tend the rice pot");
        Transform cookPot = FindDescendant(homeRoot.transform, "SM_CookingPot");
        Transform bambooTray = FindDescendant(homeRoot.transform, "SM_BambooTray");
        Transform blowTube = FindDescendant(homeRoot.transform, "Blow tube grip");

        potFocus = new Vector3(-6.05f, .66f, -6.35f);
        if (cookPot != null && BoundsOf(cookPot.gameObject, out Bounds pb))
            potFocus = pb.center + Vector3.up * .02f;

        // Bring the bamboo tray & kindling closer to the cooking stove so Shot 1 frames both the hearth fire and the steaming pot
        if (bambooTray != null)
            bambooTray.position = new Vector3(potFocus.x + .55f, .025f, potFocus.z + .52f);
        if (blowTube != null)
            blowTube.gameObject.SetActive(false);

        // Crack the cooking pot lid slightly open and add rising white steam
        if (cookPot != null)
        {
            Transform lid = FindDescendant(cookPot, "Lid_Pivot");
            if (lid != null) lid.localRotation = Quaternion.Euler(-11f, 0f, 6f);
            AddCookingSteam(homeRoot.transform, potFocus + Vector3.up * .14f);
        }

        // Turn on hearth point lights & warm kitchen rim fill
        foreach (var l in homeRoot.GetComponentsInChildren<Light>(true))
        {
            if (l.transform.name.Contains("Fire_Anchor"))
            {
                l.enabled = true;
                l.color = new Color(1f, .46f, .14f);
                l.intensity = 2.8f;
                l.range = 3.0f;
            }
        }
        AddPointLight(homeRoot.transform, "HearthWarmFill",
            potFocus + new Vector3(.75f, .42f, .45f),
            new Color(1f, .76f, .44f), 1.45f, 3.8f);

        // Focus Shot 2 on the sunlit wooden window looking out toward the village rice fields (no 3D Mother character)
        windowFocusPos = new Vector3(-3.45f, 1.48f, -4.95f);
        AddPointLight(homeRoot.transform, "WindowWarmKey",
            new Vector3(-2.50f, 2.05f, -3.70f),
            new Color(1f, .88f, .68f), .75f, 4.5f);

        // Place Mother's letter clearly in front-left of the vintage Radio on top of the Bedroom StorageChest
        Transform chest = FindDescendant(homeRoot.transform, "StorageChest");
        Transform radio = FindDescendant(homeRoot.transform, "Radio");
        letterFocusPos = new Vector3(6.6f, 1.05f, 10.1f);
        letterOutward = new Vector3(0f, 0f, -1f);

        if (chest != null && BoundsOf(chest.gameObject, out Bounds cb))
        {
            if (radio != null)
            {
                // Shift the radio slightly right on the chest so the letter has generous unobstructed space on the left
                radio.position += new Vector3(.18f, 0f, .03f);
            }
            Vector3 letterPos = new Vector3(cb.center.x - .22f, cb.max.y + .006f, cb.center.z - .08f);
            PlaceLetter(homeRoot.transform, "HomeMothersLetter", letterPos, 188f, .29f);
            letterFocusPos = letterPos + new Vector3(.14f, .06f, .02f);
            AddPointLight(homeRoot.transform, "LetterWarmSunbeam",
                letterFocusPos + new Vector3(.25f, .85f, -.65f),
                new Color(1f, .86f, .62f), .65f, 3.0f);
        }

        return homeRoot;
    }

    private static Animator BuildMother(Transform parent, Vector3 position, float yaw)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Characters/Mother/Mother.prefab");
        if (prefab == null) return null;
        var holder = new GameObject("MotherHolder").transform;
        holder.SetParent(parent, false);
        holder.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));

        var mother = (GameObject)PrefabUtility.InstantiatePrefab(prefab, holder);
        mother.name = "Mother";
        mother.transform.localPosition = Vector3.zero;
        mother.transform.localRotation = prefab.transform.localRotation;

        // Give Mother's eyes a warm dark-brown iris material so they don't appear pale in close-up light
        var eyeMat = GetOrCreateEyeMaterial();
        foreach (var smr in mother.GetComponentsInChildren<SkinnedMeshRenderer>(true))
        {
            smr.updateWhenOffscreen = true;
            if (smr.name.Contains("Eye") && eyeMat != null)
                smr.sharedMaterials = Enumerable.Repeat(eyeMat, smr.sharedMaterials.Length).ToArray();
        }

        var animator = mother.GetComponentInChildren<Animator>();
        if (animator != null)
        {
            animator.updateMode = AnimatorUpdateMode.UnscaledTime;
            var idle = AssetDatabase.LoadAllAssetsAtPath("Assets/_Project/Characters/Mother/Animations/Mother_Idle_Standing.fbx");
            foreach (var asset in idle)
                if (asset is AnimationClip clip && !clip.name.StartsWith("__"))
                {
                    clip.SampleAnimation(mother, 0f);
                    break;
                }
        }
        return animator;
    }

    private static Material GetOrCreateEyeMaterial()
    {
        var mat = AssetDatabase.LoadAssetAtPath<Material>(EyeMatEndingPath);
        if (mat == null)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) return null;
            mat = new Material(shader) { name = "MAT_EndingMotherEyes" };
            AssetDatabase.CreateAsset(mat, EyeMatEndingPath);
        }
        mat.SetColor("_BaseColor", new Color(.14f, .09f, .06f, 1f));
        mat.SetFloat("_Smoothness", .35f);
        mat.SetFloat("_Metallic", 0f);
        EditorUtility.SetDirty(mat);
        return mat;
    }

    // ---------------- Set 2: OfficeDawnSet ----------------
    private static GameObject BuildOfficeDawnSet(Transform world, Scene targetScene,
        out Vector3 officeLetterPos, out Vector3 officeMonitorPos, out float deskTopY)
    {
        var officeRoot = new GameObject("OfficeDawnSet");
        officeRoot.transform.SetParent(world, false);

        string[] skipOfficeRoots =
        {
            "Player", "OfficeGameplaySystems", "OfficeGameplayUI", "OfficeGameplayEventSystem",
            "OfficeIntroSystems", "OfficeIntroUI", "OfficeIntroAnchors",
            "UI", "EventSystem", "Canvas", "Directional Light", "Global Volume"
        };

        Scene officeScene = EditorSceneManager.OpenScene(OfficeScenePath, OpenSceneMode.Additive);
        try
        {
            foreach (var root in officeScene.GetRootGameObjects())
            {
                if (skipOfficeRoots.Contains(root.name)) continue;
                if (root.GetComponentsInChildren<Renderer>(true).Length == 0 &&
                    root.GetComponentsInChildren<Light>(true).Length == 0)
                    continue;

                var clone = Object.Instantiate(root);
                clone.name = root.name;
                SceneManager.MoveGameObjectToScene(clone, targetScene);
                clone.transform.SetParent(officeRoot.transform, true);
                StripGameplayComponents(clone, false);
            }
        }
        finally
        {
            EditorSceneManager.CloseScene(officeScene, true);
        }

        // Soften the fluorescent ceiling lights so the warm golden dawn through the East window leads the mood
        foreach (var light in officeRoot.GetComponentsInChildren<Light>(true))
        {
            if (light.type == LightType.Point)
            {
                light.color = new Color(1f, .90f, .76f);
                light.intensity = Mathf.Min(light.intensity, .45f);
            }
        }

        // Locate TyDesk_SLOT and determine its exact tabletop height
        Transform tyDesk = FindDescendant(officeRoot.transform, "TyDesk_SLOT");
        deskTopY = .80f;
        Vector3 deskCenter = new Vector3(5.5f, .80f, -2.4f);
        if (tyDesk != null)
        {
            Transform desktop = FindDescendant(tyDesk, "ImportedDesk") ?? FindDescendant(tyDesk, "Desktop");
            if (desktop != null && BoundsOf(desktop.gameObject, out Bounds db))
            {
                deskTopY = db.max.y;
                deskCenter = new Vector3(db.center.x, deskTopY, db.center.z);
            }
        }

        // Ensure Quân's desk has its CRT monitor position for camera focus, and place Mother's letter on the open tabletop to the left of the keyboard
        officeMonitorPos = new Vector3(deskCenter.x + .18f, deskTopY + .25f, deskCenter.z + .08f);
        officeLetterPos = new Vector3(deskCenter.x - .22f, deskTopY + .005f, deskCenter.z + .24f);
        PlaceLetter(officeRoot.transform, "OfficeMothersLetter", officeLetterPos, -98f, .24f);

        // Add warm golden dawn lighting streaming through the East Panoramic Window (x = 7.0) onto TyDesk_SLOT
        var spotGo = new GameObject("DawnWindowSunbeam");
        spotGo.transform.SetParent(officeRoot.transform, false);
        spotGo.transform.SetPositionAndRotation(
            new Vector3(7.65f, 2.05f, -2.15f),
            Quaternion.LookRotation(officeLetterPos - new Vector3(7.65f, 2.05f, -2.15f), Vector3.up));
        var spot = spotGo.AddComponent<Light>();
        spot.type = LightType.Spot;
        spot.spotAngle = 72f;
        spot.innerSpotAngle = 38f;
        spot.range = 7.5f;
        spot.color = new Color(1f, .82f, .52f);
        spot.intensity = 1.25f;
        spot.shadows = LightShadows.Soft;

        AddPointLight(officeRoot.transform, "DawnDeskWarmBounce",
            new Vector3(5.45f, deskTopY + .65f, -2.35f),
            new Color(1f, .85f, .58f), .75f, 4.0f);
        AddPointLight(officeRoot.transform, "DawnWindowWarmFill",
            new Vector3(6.15f, 1.75f, -1.2f),
            new Color(1f, .84f, .56f), 1.35f, 6.5f);
        AddPointLight(officeRoot.transform, "DawnCityHorizonGlow",
            new Vector3(13.5f, 3.2f, -1.0f),
            new Color(1f, .80f, .50f), 2.4f, 28f);

        return officeRoot;
    }

    private static void PlaceOfficePropIfMissing(Transform officeRoot, string namePrefix, string prefabPath, Vector3 deskPos, float yaw)
    {
        // Check if a prop with this prefix is already within 1.1m of deskPos
        foreach (var t in officeRoot.GetComponentsInChildren<Transform>(true))
        {
            if (t.name.StartsWith(namePrefix) && Vector3.Distance(t.position, deskPos) < 1.1f)
                return;
        }
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null) return;
        var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, officeRoot);
        instance.name = namePrefix + "_EndingDesk";
        instance.transform.SetPositionAndRotation(deskPos, Quaternion.Euler(0f, yaw, 0f));
    }

    // ---------------- camera anchors ----------------
    private static void BuildAnchors(Transform world,
        Vector3 potFocus, Vector3 motherFocusPos,
        Vector3 letterFocusPos, Vector3 letterOutward,
        Vector3 officeLetterPos, Vector3 officeMonitorPos, float deskTopY)
    {
        var anchors = new GameObject("Anchors").transform;
        anchors.SetParent(world, false);

        // Shot 1 (0.0s - 3.8s): Close-up slider from the glowing stove hearth up to the steaming rice pot
        Vector3 s1TargetStart = potFocus + new Vector3(.08f, -.16f, .18f);
        Vector3 s1TargetMid = potFocus + new Vector3(.04f, -.07f, .08f);
        Vector3 s1TargetEnd = potFocus + new Vector3(0f, .02f, 0f);
        Look(anchors, "S1_Start", potFocus + new Vector3(1.52f, .24f, .78f), s1TargetStart);
        Look(anchors, "S1_Mid", potFocus + new Vector3(1.34f, .28f, .58f), s1TargetMid);
        Look(anchors, "S1_End", potFocus + new Vector3(1.18f, .32f, .42f), s1TargetEnd);
        Mark(anchors, "HomeFocus1", potFocus);

        // Shot 2 (5.2s - 10.2s): Sunlit wooden window looking out at the village rice fields with floating memory frames
        Vector3 s2StartPos = motherFocusPos + new Vector3(1.04f, -.04f, 2.68f);
        Vector3 s2MidPos = motherFocusPos + new Vector3(.84f, -.03f, 2.40f);
        Vector3 s2EndPos = motherFocusPos + new Vector3(.66f, -.02f, 2.14f);
        Look(anchors, "S2_Start", s2StartPos, motherFocusPos + new Vector3(-.08f, .02f, -.10f));
        Look(anchors, "S2_Mid", s2MidPos, motherFocusPos + new Vector3(-.05f, .03f, -.08f));
        Look(anchors, "S2_End", s2EndPos, motherFocusPos + new Vector3(-.02f, .04f, -.05f));
        Mark(anchors, "MotherFocus", motherFocusPos);
        Mark(anchors, "ArchCenter", motherFocusPos + new Vector3(-.04f, -.04f, .18f));

        // Shot 3 (7.8s - 11.4s): Intimate close-up on Mother's letter & vintage radio on the sunlit chest
        Vector3 s3StartPos = letterFocusPos + letterOutward * .96f + new Vector3(-.12f, .50f, 0f);
        Vector3 s3MidPos = letterFocusPos + letterOutward * .84f + new Vector3(-.06f, .44f, 0f);
        Vector3 s3EndPos = letterFocusPos + letterOutward * .74f + new Vector3(-.02f, .38f, 0f);
        Look(anchors, "S3_Start", s3StartPos, letterFocusPos + new Vector3(.02f, .02f, 0f));
        Look(anchors, "S3_Mid", s3MidPos, letterFocusPos + new Vector3(.01f, .01f, 0f));
        Look(anchors, "S3_End", s3EndPos, letterFocusPos);
        Mark(anchors, "LetterFocus", letterFocusPos);

        // Shot 4 (11.4s - 18.2s): First-person awakening on Quân's office desk (TyDesk_SLOT)
        Vector3 wakeStartPos = new Vector3(4.84f, deskTopY + .20f, -2.52f);
        Vector3 wakeLookTarget = Vector3.Lerp(officeLetterPos, officeMonitorPos, .28f) + new Vector3(.15f, .06f, 0f);
        Quaternion wakeStartRot = Quaternion.LookRotation(wakeLookTarget - wakeStartPos, Vector3.up)
                                  * Quaternion.Euler(0f, 0f, 8f);
        var s4Start = new GameObject("S4_Start").transform;
        s4Start.SetParent(anchors, false);
        s4Start.SetPositionAndRotation(wakeStartPos, wakeStartRot);

        Vector3 seatEyePos = new Vector3(4.52f, 1.25f, -2.42f);
        Look(anchors, "S4_Mid", seatEyePos, officeLetterPos + new Vector3(.18f, .08f, .08f));
        Look(anchors, "S4_End", new Vector3(4.56f, 1.28f, -2.40f), new Vector3(11.5f, 1.62f, -1.85f));
        Mark(anchors, "OfficeLetterFocus", officeLetterPos);
        Mark(anchors, "OfficeWindowFocus", new Vector3(6.95f, 1.55f, -1.85f));

        // Shot 5 (18.2s - 24.0s): Rising from the office chair & stepping toward the East Panoramic Window
        Look(anchors, "S5_Start", new Vector3(4.56f, 1.28f, -2.40f), new Vector3(11.5f, 1.62f, -1.85f));
        Look(anchors, "S5_Mid", new Vector3(5.02f, 1.56f, -1.98f), new Vector3(15.0f, 1.95f, -1.25f));
        Look(anchors, "S5_End", new Vector3(5.42f, 1.62f, -1.55f), new Vector3(18.5f, 2.18f, -.75f));
        Mark(anchors, "OfficeHorizonFocus", new Vector3(18.5f, 2.18f, -.75f));

        // Final Drift (24.0s+): Living dawn window backdrop while EndingUI displays the Epilogue card
        Look(anchors, "Final_Start", new Vector3(5.42f, 1.62f, -1.55f), new Vector3(18.5f, 2.18f, -.75f));
        Look(anchors, "Final_Mid", new Vector3(5.52f, 1.63f, -1.05f), new Vector3(18.5f, 2.26f, -.30f));
        Look(anchors, "Final_End", new Vector3(5.60f, 1.64f, -.55f), new Vector3(18.5f, 2.34f, .15f));
    }

    private static void Look(Transform parent, string title, Vector3 position, Vector3 target)
    {
        var t = new GameObject(title).transform;
        t.SetParent(parent, false);
        t.position = position;
        t.rotation = Quaternion.LookRotation(target - position, Vector3.up);
    }

    private static void Mark(Transform parent, string title, Vector3 position)
    {
        var t = new GameObject(title).transform;
        t.SetParent(parent, false);
        t.position = position;
    }

    // ---------------- props & helpers ----------------
    private static Material GetOrCreateLetterMaterial()
    {
        var mat = AssetDatabase.LoadAssetAtPath<Material>(LetterMatEndingPath);
        if (mat == null)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) return null;
            mat = new Material(shader) { name = "MAT_EndingLetter" };
            AssetDatabase.CreateAsset(mat, LetterMatEndingPath);
        }
        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(LetterTexPath);
        if (tex != null) mat.SetTexture("_BaseMap", tex);
        // Warm parchment tint & very low smoothness so direct sunlight never blows out the handwritten text
        mat.SetColor("_BaseColor", new Color(.84f, .78f, .68f, 1f));
        mat.SetFloat("_Smoothness", .04f);
        mat.SetFloat("_Metallic", 0f);
        EditorUtility.SetDirty(mat);
        return mat;
    }

    private static GameObject PlaceLetter(Transform parent, string name, Vector3 bottomCenter, float yaw, float targetSize)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(LetterModelPath);
        var mat = GetOrCreateLetterMaterial();
        if (prefab == null) return null;

        var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        instance.name = name;
        instance.transform.rotation = Quaternion.Euler(-90f, yaw, 0f);
        instance.transform.localScale = Vector3.one;
        foreach (var c in instance.GetComponentsInChildren<Collider>(true))
            Object.DestroyImmediate(c);

        if (mat != null)
        {
            foreach (var r in instance.GetComponentsInChildren<Renderer>(true))
                r.sharedMaterials = Enumerable.Repeat(mat, r.sharedMaterials.Length).ToArray();
        }

        if (BoundsOf(instance, out Bounds b))
        {
            float longest = Mathf.Max(b.size.x, b.size.y, b.size.z, .001f);
            instance.transform.localScale *= targetSize / longest;
            if (BoundsOf(instance, out Bounds scaled))
            {
                instance.transform.position += bottomCenter - new Vector3(scaled.center.x, scaled.min.y, scaled.center.z);
            }
        }
        return instance;
    }

    private static void AddPointLight(Transform parent, string name, Vector3 position, Color color, float intensity, float range)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = position;
        var light = go.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = color;
        light.intensity = intensity;
        light.range = range;
        light.shadows = LightShadows.None;
    }

    private static Material GetOrCreateSteamMaterial()
    {
        var shader = Resources.Load<Shader>("Effects/MemoryParticle");
        if (shader == null) return null;

        var mat = AssetDatabase.LoadAssetAtPath<Material>(SteamMatEndingPath);
        if (mat == null)
        {
            mat = new Material(shader) { name = "MAT_CookingSteam" };
            AssetDatabase.CreateAsset(mat, SteamMatEndingPath);
        }

        Texture2D tex = null;
        foreach (var sub in AssetDatabase.LoadAllAssetsAtPath(SteamMatEndingPath))
        {
            if (sub is Texture2D t) { tex = t; break; }
        }
        if (tex == null)
        {
            const int size = 64;
            tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "TEX_SoftSteamRadial",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };
            var pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                float ny = (y + .5f) / size * 2f - 1f;
                for (int x = 0; x < size; x++)
                {
                    float nx = (x + .5f) / size * 2f - 1f;
                    float r = Mathf.Sqrt(nx * nx + ny * ny);
                    float a = Mathf.Clamp01(1f - r);
                    a = a * a * (3f - 2f * a);
                    pixels[y * size + x] = new Color(1f, 1f, 1f, a * a);
                }
            }
            tex.SetPixels(pixels);
            tex.Apply(false, false);
            AssetDatabase.AddObjectToAsset(tex, mat);
        }

        mat.shader = shader;
        mat.SetTexture("_BaseMap", tex);
        mat.SetFloat("_Brightness", 1.15f);
        mat.SetFloat("_SoftFade", 0f);
        EditorUtility.SetDirty(mat);
        AssetDatabase.SaveAssets();
        return mat;
    }

    private static void AddCookingSteam(Transform parent, Vector3 position)
    {
        var mat = GetOrCreateSteamMaterial();
        if (mat == null) return;
        var go = new GameObject("CookingSteam");
        go.transform.SetParent(parent, false);
        go.transform.position = position;
        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = ps.main;
        main.loop = true;
        main.playOnAwake = true;
        main.prewarm = true;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 45;
        main.startLifetime = new ParticleSystem.MinMaxCurve(1.8f, 3.0f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(.12f, .24f);
        main.startSize = new ParticleSystem.MinMaxCurve(.09f, .22f);
        main.startColor = new Color(1f, .96f, .90f, .16f);
        var emission = ps.emission;
        emission.rateOverTime = 14f;
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 14f;
        shape.radius = .09f;
        shape.rotation = new Vector3(-90f, 0f, 0f);
        var col = ps.colorOverLifetime;
        col.enabled = true;
        var grad = new Gradient();
        grad.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(.75f, .25f), new GradientAlphaKey(0f, 1f) });
        col.color = grad;
        var pr = ps.GetComponent<ParticleSystemRenderer>();
        pr.sharedMaterial = mat;
        ps.Play();
    }

    private static void StripGameplayComponents(GameObject root, bool isHome)
    {
        foreach (var mb in root.GetComponentsInChildren<MonoBehaviour>(true))
            if (mb != null) Object.DestroyImmediate(mb);
        foreach (var col in root.GetComponentsInChildren<Collider>(true))
            if (col != null) Object.DestroyImmediate(col);
        foreach (var rb in root.GetComponentsInChildren<Rigidbody>(true))
            if (rb != null) Object.DestroyImmediate(rb);
        foreach (var cc in root.GetComponentsInChildren<CharacterController>(true))
            if (cc != null) Object.DestroyImmediate(cc);
        foreach (var audio in root.GetComponentsInChildren<AudioSource>(true))
            if (audio != null) Object.DestroyImmediate(audio);
        foreach (var listener in root.GetComponentsInChildren<AudioListener>(true))
            if (listener != null) Object.DestroyImmediate(listener);
        foreach (var cam in root.GetComponentsInChildren<Camera>(true))
            if (cam != null) Object.DestroyImmediate(cam.gameObject);
        foreach (var canvas in root.GetComponentsInChildren<Canvas>(true))
            if (canvas != null) Object.DestroyImmediate(canvas.gameObject);
        foreach (var light in root.GetComponentsInChildren<Light>(true))
            if (light != null && light.type == LightType.Directional)
                Object.DestroyImmediate(light.gameObject);

        // Hide blockout/placeholder helpers so only final art renders
        var toDestroy = new List<GameObject>();
        foreach (var t in root.GetComponentsInChildren<Transform>(true))
        {
            if (t == null) continue;
            string n = t.name;
            if (n.StartsWith("Placeholder_") || n.StartsWith("ChoreTarget_") ||
                n == "InteractTrigger" || n == "Collision" || n == "WindowCollision" ||
                (isHome && (n == "Wooden rice paddle" || n == "Grip to tilt the pot")))
            {
                toDestroy.Add(t.gameObject);
            }
        }
        foreach (var go in toDestroy)
            if (go != null) Object.DestroyImmediate(go);
    }

    private static Transform FindDescendant(Transform root, string name)
    {
        if (root == null) return null;
        foreach (var t in root.GetComponentsInChildren<Transform>(true))
            if (t.name == name) return t;
        return null;
    }

    private static bool BoundsOf(GameObject go, out Bounds bounds)
    {
        bounds = default;
        if (go == null) return false;
        var renderers = go.GetComponentsInChildren<Renderer>(true)
            .Where(r => r != null && r.enabled && !(r is ParticleSystemRenderer)).ToArray();
        if (renderers.Length == 0) return false;
        bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);
        return true;
    }

    // ---------------- post processing ----------------
    private static Volume BuildVolume()
    {
        Directory.CreateDirectory(ProfileFolder);
        var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(ProfilePath);
        if (profile == null)
        {
            profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, ProfilePath);
        }
        profile.components.Clear();

        var bloom = profile.Add<Bloom>(true);
        bloom.threshold.Override(.92f);
        bloom.intensity.Override(1.1f);
        bloom.scatter.Override(.68f);
        bloom.tint.Override(new Color(1f, .90f, .74f));
        bloom.highQualityFiltering.Override(true);

        var vignette = profile.Add<Vignette>(true);
        vignette.color.Override(new Color(.08f, .04f, .02f));
        vignette.intensity.Override(.28f);
        vignette.smoothness.Override(.52f);

        var color = profile.Add<ColorAdjustments>(true);
        color.postExposure.Override(.12f);
        color.contrast.Override(12f);
        color.saturation.Override(8f);
        color.colorFilter.Override(new Color(1f, .97f, .93f));

        var balance = profile.Add<WhiteBalance>(true);
        balance.temperature.Override(12f);
        balance.tint.Override(2f);

        var tone = profile.Add<Tonemapping>(true);
        tone.mode.Override(TonemappingMode.ACES);

        var grain = profile.Add<FilmGrain>(true);
        grain.type.Override(FilmGrainLookup.Thin1);
        grain.intensity.Override(.18f);
        grain.response.Override(.8f);

        var dof = profile.Add<DepthOfField>(true);
        dof.mode.Override(DepthOfFieldMode.Bokeh);
        dof.focusDistance.Override(2.0f);
        dof.focalLength.Override(50f);
        dof.aperture.Override(4.2f);
        dof.bladeCount.Override(6);

        EditorUtility.SetDirty(profile);

        var go = new GameObject("EndingVolume");
        var volume = go.AddComponent<Volume>();
        volume.isGlobal = true;
        volume.priority = 10f;
        volume.sharedProfile = profile;
        return volume;
    }

    private static void SetupCamera(GameObject cameraObject, Volume volume)
    {
        var cam = cameraObject.GetComponent<Camera>();
        cam.clearFlags = CameraClearFlags.Skybox;
        cam.nearClipPlane = .06f;
        cam.farClipPlane = 500f;
        cam.fieldOfView = 30f;
        cam.allowHDR = true;

        var data = cam.GetUniversalAdditionalCameraData();
        data.renderPostProcessing = true;
        data.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
        data.antialiasingQuality = AntialiasingQuality.High;
        data.renderShadows = true;

        if (cameraObject.GetComponent<AudioListener>() == null && Object.FindObjectOfType<AudioListener>() == null)
            cameraObject.AddComponent<AudioListener>();

        var rig = cameraObject.GetComponent<EndingCameraRig>();
        if (rig == null) rig = cameraObject.AddComponent<EndingCameraRig>();
        var so = new SerializedObject(rig);
        so.FindProperty("cam").objectReferenceValue = cam;
        so.FindProperty("volume").objectReferenceValue = volume;
        so.ApplyModifiedPropertiesWithoutUndo();

        var start = GameObject.Find("EndingWorld/Anchors/S1_Start");
        if (start != null) cameraObject.transform.SetPositionAndRotation(start.transform.position, start.transform.rotation);
    }

    private static void SetupDirector(Transform world, GameObject cameraObject, Light sun, Animator mother)
    {
        var go = new GameObject("EndingDirector");
        var director = go.AddComponent<EndingDirector>();
        var so = new SerializedObject(director);
        so.FindProperty("worldRoot").objectReferenceValue = world;
        so.FindProperty("rig").objectReferenceValue = cameraObject.GetComponent<EndingCameraRig>();
        so.FindProperty("sun").objectReferenceValue = sun;
        so.FindProperty("mother").objectReferenceValue = mother;
        so.FindProperty("finalCard").objectReferenceValue = Object.FindObjectOfType<EndingUI>(true);
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // ---------------- review helper ----------------
    [MenuItem("KuTy/Ending/Capture review frames (edit mode)")]
    public static void CaptureReview()
    {
        if (Application.isPlaying) throw new System.InvalidOperationException("Stop Play Mode first.");
        var cam = Camera.main;
        if (cam == null) throw new System.InvalidOperationException("No main camera.");
        var world = GameObject.Find("EndingWorld")?.transform;
        var homeSet = world?.Find("HomeMemorySet")?.gameObject;
        var officeSet = world?.Find("OfficeDawnSet")?.gameObject;
        var sun = RenderSettings.sun;

        Directory.CreateDirectory("Tools/Ending");
        string[] anchors =
        {
            "S1_Start", "S1_End", "S2_Start", "S2_End",
            "S3_Start", "S3_End", "S4_Start", "S4_Mid",
            "S4_End", "S5_End", "Final_Start", "Final_End"
        };
        var saved = (cam.transform.position, cam.transform.rotation, cam.fieldOfView);
        foreach (string name in anchors)
        {
            var anchor = GameObject.Find("EndingWorld/Anchors/" + name);
            if (anchor == null) continue;
            bool isHome = name.StartsWith("S1_") || name.StartsWith("S2_") || name.StartsWith("S3_");
            if (homeSet != null) homeSet.SetActive(isHome);
            if (officeSet != null) officeSet.SetActive(!isHome);
            if (sun != null) sun.transform.rotation = isHome ? HomeSunRotation : OfficeSunRotation;
            cam.fieldOfView = name.StartsWith("S4_") ? 46f : (name.StartsWith("S5_") || name.StartsWith("Final_") ? 36f : 30f);

            cam.transform.SetPositionAndRotation(anchor.transform.position, anchor.transform.rotation);
            var target = new RenderTexture(1600, 900, 24, RenderTextureFormat.DefaultHDR);
            var previous = cam.targetTexture;
            cam.targetTexture = target;
            cam.Render();
            cam.targetTexture = previous;
            RenderTexture.active = target;
            var tex = new Texture2D(1600, 900, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, 1600, 900), 0, 0);
            tex.Apply();
            RenderTexture.active = null;
            File.WriteAllBytes("Tools/Ending/review_" + name + ".png", tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            target.Release();
            Object.DestroyImmediate(target);
        }
        if (homeSet != null) homeSet.SetActive(true);
        if (officeSet != null) officeSet.SetActive(false);
        if (sun != null) sun.transform.rotation = HomeSunRotation;
        cam.transform.SetPositionAndRotation(saved.Item1, saved.Item2);
        cam.fieldOfView = saved.Item3;
        Debug.Log("[Ending builder] Review frames written to Tools/Ending.");
    }
}
#endif
