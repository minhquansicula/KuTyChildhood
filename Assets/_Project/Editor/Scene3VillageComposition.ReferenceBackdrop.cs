using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

public static partial class Scene3VillageComposition
{
    private const string BackdropOutput = OutputFolder + "/ReferenceBackdrop";
    private const string BackdropTexture = "Assets/_Project/Art/Textures/Scene3/HDRI/VillageGoldenAfternoon.png";
    private const string BackdropMaterial = "Assets/_Project/Art/Materials/Scene3/VillageGoldenAfternoon.mat";

    [MenuItem("KuTy/Scene 3/Art direction/5 Apply reference afternoon backdrop")]
    public static void ApplyReferenceBackdrop()
    {
        RequireArtScene();
        Directory.CreateDirectory(BackdropOutput);
        var scene = SceneManager.GetActiveScene();
        string backup = BackdropOutput + "/before.unity";
        if (!File.Exists(backup) && !EditorSceneManager.SaveScene(scene,backup,true))
            throw new IOException("Cannot back up the scene before replacing the backdrop.");
        if (!Directory.Exists(BackdropOutput + "/Before")) CaptureReferenceBackdropViews("Before");
        var before = GameplaySnapshot();
        string checkpoint = ReadArtCheckpoint();
        var originalSky = RenderSettings.skybox;
        var sun = Find("Sun").GetComponent<Light>();
        var sunRotation = sun.transform.rotation;
        Color sunColor = sun.color;
        float sunIntensity = sun.intensity;
        var shader = Shader.Find("KuTy/Village/Golden Afternoon Backdrop");
        if (!shader || ShaderUtil.GetShaderMessages(shader).Any(m => m.severity == UnityEditor.Rendering.ShaderCompilerMessageSeverity.Error))
            throw new InvalidOperationException("Backdrop shader missing or failed to compile.");
        AssetDatabase.ImportAsset(BackdropTexture,ImportAssetOptions.ForceSynchronousImport);
        var importer = (TextureImporter)AssetImporter.GetAtPath(BackdropTexture);
        importer.textureType = TextureImporterType.Default;
        importer.textureShape = TextureImporterShape.Texture2D;
        importer.sRGBTexture = true;
        importer.mipmapEnabled = true;
        importer.isReadable = false;
        importer.maxTextureSize = 4096;
        importer.npotScale = TextureImporterNPOTScale.None;
        importer.wrapModeU = TextureWrapMode.Repeat;
        importer.wrapModeV = TextureWrapMode.Clamp;
        importer.filterMode = FilterMode.Trilinear;
        importer.textureCompression = TextureImporterCompression.CompressedHQ;
        importer.alphaSource = TextureImporterAlphaSource.None;
        importer.SaveAndReimport();
        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(BackdropTexture);
        if (!texture || Mathf.Abs(texture.width / (float)texture.height - 2) > .02f)
            throw new InvalidOperationException("Expected a 2:1 spherical panorama.");
        var material = AssetDatabase.LoadAssetAtPath<Material>(BackdropMaterial);
        if (!material)
        {
            material = new Material(shader) { name = "VillageGoldenAfternoon" };
            AssetDatabase.CreateAsset(material,BackdropMaterial);
        }
        else Undo.RecordObject(material,"Reference backdrop settings");
        Vector3 direction = -sun.transform.forward;
        // The warmest cloud band is at source U=.72. Rotate it towards the existing sun.
        float rotation = (.5f - .72f) * 360 - Mathf.Atan2(direction.z,direction.x) * Mathf.Rad2Deg;
        material.SetTexture("_MainTex",texture);
        material.SetFloat("_Exposure",1);
        material.SetFloat("_Rotation",rotation);
        material.SetFloat("_HorizonOffset",-.045f);
        material.SetFloat("_SunRadius",.8f);
        material.SetVector("_SunDirection",direction);
        material.SetColor("_SunColor",new Color(4,3,1.5f));
        material.SetColor("_HorizonColor",RenderSettings.fogColor);
        EditorUtility.SetDirty(material);
        try
        {
            RenderSettings.skybox = material;
            VerifyGameplay(before);
            if (ReadArtCheckpoint() != checkpoint) throw new Exception("Checkpoint changed while applying the backdrop.");
            if (sun.transform.rotation != sunRotation || sun.color != sunColor || sun.intensity != sunIntensity)
                throw new Exception("Backdrop replacement must preserve the scene's directional light.");
            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Cannot save the new backdrop.");
        }
        catch { RenderSettings.skybox = originalSky; throw; }
        DynamicGI.UpdateEnvironment();
        SceneView.RepaintAll();
        File.WriteAllText(BackdropOutput + "/report.txt",
            "PASS: reference backdrop installed; original sky material and HDRI retained.\n" +
            "Texture: " + texture.width + " x " + texture.height + ", sRGB, mipmaps, trilinear, U repeat / V clamp, no CPU read/write.\n" +
            "One full 360 panorama; no landscape repetition or photographed solar discs.\n" +
            "One shader sun aligned to the unchanged scene directional light.\n" +
            "Gameplay serialization unchanged: " + before.Count + " components. Checkpoint unchanged.\n" +
            "Geometry, colliders, fog, light, pipeline, water and vegetation untouched.\n");
        Debug.Log("[VillageBackdrop] Reference afternoon panorama saved. Capture comparison after the ambient probe updates.");
    }

    [MenuItem("KuTy/Scene 3/Art direction/6 Capture reference backdrop comparison")]
    public static void CaptureReferenceBackdropComparison()
    {
        RequireArtScene();
        if (RenderSettings.skybox != AssetDatabase.LoadAssetAtPath<Material>(BackdropMaterial))
            throw new InvalidOperationException("Install the reference backdrop first.");
        var shader = RenderSettings.skybox.shader;
        if (ShaderUtil.GetShaderMessages(shader).Any(m => m.severity == UnityEditor.Rendering.ShaderCompilerMessageSeverity.Error))
            throw new Exception("Backdrop shader compile error.");
        CaptureReferenceBackdropViews("After");
    }

    private static void CaptureReferenceBackdropViews(string phase)
    {
        string folder = BackdropOutput + "/" + phase;
        Directory.CreateDirectory(folder);
        Vector3 sun = -Find("Sun").transform.forward;
        Vector3 flat = new Vector3(sun.x,0,sun.z).normalized;
        float rotation = RenderSettings.skybox.HasProperty("_Rotation") ? RenderSettings.skybox.GetFloat("_Rotation") : 0;
        float seamAngle = (180 - rotation) * Mathf.Deg2Rad;
        Vector3 seam = new Vector3(Mathf.Cos(seamAngle),.2f,Mathf.Sin(seamAngle));
        Vector3 yard = ArtRingCenter();
        Vector3[] positions = { new Vector3(-5,1.8f,-7), yard + new Vector3(3.6f,1.65f,-4.8f),
            new Vector3(-6,1.8f,55), Vector3.up*2, Vector3.up*2, Vector3.up*2, Vector3.up*2 };
        Vector3[] directions = { new Vector3(0,.08f,1), (yard+Vector3.up*.7f)-(yard+new Vector3(3.6f,1.65f,-4.8f)),
            new Vector3(-1,0,11), flat+Vector3.up*.22f, -flat+Vector3.up*.22f, Vector3.up, seam };
        string[] names = { "01_Entrance", "02_MarbleYard", "03_VillageEnd", "04_SkySun", "05_SkyOpposite", "06_Zenith", "07_LongitudeJoin" };
        for (int i = 0; i < names.Length; i++)
        {
            var obj = new GameObject("BackdropCapture") { hideFlags = HideFlags.HideAndDontSave };
            var camera = obj.AddComponent<Camera>();
            camera.enabled = false; camera.fieldOfView = i >= 3 ? 95 : 65;
            camera.clearFlags = CameraClearFlags.Skybox; camera.nearClipPlane = .03f; camera.farClipPlane = 300;
            camera.allowHDR = true;
            if (i >= 3) camera.cullingMask = 0;
            camera.transform.SetPositionAndRotation(positions[i],Quaternion.LookRotation(directions[i],i == 5 ? Vector3.forward : Vector3.up));
            camera.GetUniversalAdditionalCameraData().renderPostProcessing = i < 3;
            var rt = new RenderTexture(1600,900,24); var image = new Texture2D(1600,900,TextureFormat.RGB24,false);
            var previous = RenderTexture.active;
            try
            {
                rt.Create();
                RenderPipeline.SubmitRenderRequest(camera,new UniversalRenderPipeline.SingleCameraRequest { destination = rt });
                RenderTexture.active = rt; image.ReadPixels(new Rect(0,0,1600,900),0,0); image.Apply();
                File.WriteAllBytes(folder + "/" + names[i] + ".png",image.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active = previous; rt.Release();
                UnityEngine.Object.DestroyImmediate(image); UnityEngine.Object.DestroyImmediate(rt); UnityEngine.Object.DestroyImmediate(obj);
            }
        }
    }
}
