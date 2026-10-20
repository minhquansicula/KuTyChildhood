using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Converts the credited CC0 source clips into short, mono game cues and binds
/// them to the existing Act 1 atmosphere and global UI catalog.
/// </summary>
public static class KuTySfxProductionSetup
{
    private const string ScenePath = "Assets/_Project/Scenes/Act1_Office_Blockout.unity";
    private const string CatalogPath = "Assets/_Project/Data/Audio/SfxCatalog.asset";
    private const string SourceRoot = "Assets/_Project/Audio/SFX/Source/CC0";
    private const string OfficeRoot = "Assets/_Project/Audio/SFX/Office";
    private const string UiRoot = "Assets/_Project/Audio/SFX/UI";
    private const string SourceManifestPath = "Assets/_Project/Audio/SFX/sfx_sources.json";

    private sealed class ClipRecipe
    {
        public string source;
        public string output;
        public string sceneField;
        public SfxId uiId;
        public float start;
        public float duration;
        public bool firstActivity;
    }

    private sealed class PreparedClip
    {
        public ClipRecipe recipe;
        public byte[] wav;
    }

    private static readonly ClipRecipe[] Recipes =
    {
        new ClipRecipe { source = SourceRoot + "/fs186142_photocopier_hq.mp3", output = OfficeRoot + "/sfx_office_printer_01.wav", sceneField = "printerSound", start = 0f, duration = 3.2f },
        new ClipRecipe { source = SourceRoot + "/fs353125_paper_rustle_hq.mp3", output = OfficeRoot + "/sfx_office_paper_01.wav", sceneField = "paperRustle", start = 0f, duration = 1.1f },
        new ClipRecipe { source = SourceRoot + "/fs519478_water_cup_hq.mp3", output = OfficeRoot + "/sfx_office_water_01.wav", sceneField = "waterPour", start = 0f, duration = 5.45f },
        new ClipRecipe { source = SourceRoot + "/fs519478_water_cup_hq.mp3", output = OfficeRoot + "/sfx_office_cup_01.wav", sceneField = "cupSetDown", start = 5.45f, duration = 1.1f },
        new ClipRecipe { source = SourceRoot + "/fs338143_office_chair_hq.mp3", output = OfficeRoot + "/sfx_office_chair_01.wav", sceneField = "chairShift", duration = .9f, firstActivity = true },
        new ClipRecipe { source = SourceRoot + "/kenney_ui_confirm.ogg", output = UiRoot + "/sfx_ui_confirm_01.wav", uiId = SfxId.UIConfirm, duration = .45f },
        new ClipRecipe { source = SourceRoot + "/kenney_ui_back.ogg", output = UiRoot + "/sfx_ui_back_01.wav", uiId = SfxId.UIBack, duration = .45f },
        new ClipRecipe { source = SourceRoot + "/kenney_ui_invalid.ogg", output = UiRoot + "/sfx_ui_invalid_01.wav", uiId = SfxId.UIInvalid, duration = .65f }
    };

    [MenuItem("KuTy/SFX/Install selected CC0 clips")]
    public static void Install()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (EditorApplication.isPlaying || scene.path != ScenePath || scene.isDirty)
        {
            Debug.LogError("[KuTy SFX] Open a saved Act1_Office_Blockout scene outside Play Mode before installing selected clips.");
            return;
        }

        OfficeAtmosphere atmosphere = UnityEngine.Object.FindObjectOfType<OfficeAtmosphere>();
        SfxCatalog catalog = AssetDatabase.LoadAssetAtPath<SfxCatalog>(CatalogPath);
        if (atmosphere == null || catalog == null || !File.Exists(SourceManifestPath))
        {
            Debug.LogError("[KuTy SFX] OfficeAtmosphere, SfxCatalog or sfx_sources.json is missing.");
            return;
        }

        SerializedObject serializedAtmosphere = new SerializedObject(atmosphere);
        foreach (ClipRecipe recipe in Recipes)
        {
            if (recipe.sceneField != null && serializedAtmosphere.FindProperty(recipe.sceneField) == null)
            {
                Debug.LogError("[KuTy SFX] Office audio field is missing: " + recipe.sceneField);
                return;
            }
            if (recipe.uiId != SfxId.None && !catalog.TryGetCue(recipe.uiId, out SfxCue _))
            {
                Debug.LogError("[KuTy SFX] UI catalog cue is missing: " + recipe.uiId);
                return;
            }
        }

        string[] missing = Recipes.Select(item => item.source).Distinct().Where(path => !File.Exists(path)).ToArray();
        if (missing.Length > 0)
        {
            Debug.LogError("[KuTy SFX] Missing CC0 source clips; no scene or catalog changes were made:\n- " + string.Join("\n- ", missing));
            return;
        }

        var prepared = new List<PreparedClip>();
        try
        {
            foreach (ClipRecipe recipe in Recipes)
            {
                AudioClip source = LoadReadableSource(recipe.source);
                prepared.Add(new PreparedClip { recipe = recipe, wav = MakeWav(source, recipe) });
            }
        }
        catch (Exception exception)
        {
            Debug.LogError("[KuTy SFX] Source processing failed; no scene or catalog changes were made: " + exception);
            return;
        }

        EnsureFolder(OfficeRoot);
        EnsureFolder(UiRoot);
        foreach (PreparedClip item in prepared)
        {
            File.WriteAllBytes(item.recipe.output, item.wav);
            AssetDatabase.ImportAsset(item.recipe.output, ImportAssetOptions.ForceUpdate);
            AudioImporter importer = AssetImporter.GetAtPath(item.recipe.output) as AudioImporter;
            if (importer == null) throw new InvalidOperationException("Audio importer missing for " + item.recipe.output);
            importer.forceToMono = true;
            importer.loadInBackground = false;
            AudioImporterSampleSettings settings = importer.defaultSampleSettings;
            settings.loadType = AudioClipLoadType.DecompressOnLoad;
            settings.compressionFormat = AudioCompressionFormat.PCM;
            importer.defaultSampleSettings = settings;
            importer.SaveAndReimport();
        }

        foreach (ClipRecipe recipe in Recipes)
        {
            AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(recipe.output);
            if (clip == null) throw new InvalidOperationException("Imported clip missing: " + recipe.output);
            if (recipe.sceneField != null)
            {
                SerializedProperty field = serializedAtmosphere.FindProperty(recipe.sceneField);
                if (field == null) throw new MissingFieldException(typeof(OfficeAtmosphere).Name, recipe.sceneField);
                field.objectReferenceValue = clip;
            }
            else
            {
                SfxCue cue = catalog.Cues.FirstOrDefault(item => item != null && item.id == recipe.uiId);
                if (cue == null) throw new InvalidOperationException("Catalog cue missing: " + recipe.uiId);
                cue.variants = new[] { clip };
            }
        }

        serializedAtmosphere.ApplyModifiedProperties();
        EditorUtility.SetDirty(catalog);
        EditorUtility.SetDirty(atmosphere);
        KuTySfxSetup.RefreshManifest(catalog);
        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[KuTy SFX] Installed 5 Office and 3 UI CC0 clips. Run production validation and listen to the Act 1 scene before release.");
    }

    [MenuItem("KuTy/Validation/Validate selected CC0 SFX")]
    public static void Validate()
    {
        var errors = new List<string>();
        SfxCatalog catalog = AssetDatabase.LoadAssetAtPath<SfxCatalog>(CatalogPath);
        if (!File.Exists(SourceManifestPath)) errors.Add("Source/license manifest is missing.");
        if (catalog == null) errors.Add("SfxCatalog is missing.");
        OfficeAtmosphere atmosphere = SceneManager.GetActiveScene().path == ScenePath
            ? UnityEngine.Object.FindObjectOfType<OfficeAtmosphere>() : null;
        if (atmosphere == null) errors.Add("Open Act1_Office_Blockout to validate its OfficeAtmosphere.");
        SerializedObject serialized = atmosphere != null ? new SerializedObject(atmosphere) : null;
        foreach (ClipRecipe recipe in Recipes)
        {
            if (!File.Exists(recipe.source)) errors.Add("Credited source clip is missing: " + recipe.source);
            AudioClip expected = AssetDatabase.LoadAssetAtPath<AudioClip>(recipe.output);
            if (expected == null || expected.length < .08f)
            {
                errors.Add("Missing or empty production clip: " + recipe.output);
                continue;
            }
            AudioImporter importer = AssetImporter.GetAtPath(recipe.output) as AudioImporter;
            if (importer == null || !importer.forceToMono ||
                importer.defaultSampleSettings.loadType != AudioClipLoadType.DecompressOnLoad)
                errors.Add("Short SFX import settings are incorrect: " + recipe.output);
            if (recipe.sceneField != null && serialized != null)
            {
                SerializedProperty field = serialized.FindProperty(recipe.sceneField);
                if (field == null || field.objectReferenceValue != expected)
                    errors.Add("Office field is not bound to production audio: " + recipe.sceneField);
            }
            if (recipe.uiId != SfxId.None && catalog != null &&
                (!catalog.TryGetCue(recipe.uiId, out SfxCue cue) || cue.variants == null ||
                 cue.variants.Length != 1 || cue.variants[0] != expected))
                errors.Add("UI cue is not bound to production audio: " + recipe.uiId);
        }
        if (errors.Count > 0) Debug.LogError("[KuTy SFX] Production validation failed:\n- " + string.Join("\n- ", errors));
        else Debug.Log("[KuTy SFX] Selected CC0 SFX validation passed (5 Office, 3 UI clips and source manifest). Listening review remains.");
    }

    private static AudioClip LoadReadableSource(string path)
    {
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        AudioImporter importer = AssetImporter.GetAtPath(path) as AudioImporter;
        if (importer == null) throw new InvalidOperationException("No AudioImporter for " + path);
        importer.forceToMono = true;
        importer.loadInBackground = false;
        AudioImporterSampleSettings settings = importer.defaultSampleSettings;
        settings.loadType = AudioClipLoadType.DecompressOnLoad;
        settings.compressionFormat = AudioCompressionFormat.PCM;
        importer.defaultSampleSettings = settings;
        importer.SaveAndReimport();
        AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        if (clip == null || clip.samples <= 0 || clip.channels <= 0 || !clip.LoadAudioData())
            throw new InvalidOperationException("Cannot decode " + path);
        return clip;
    }

    private static byte[] MakeWav(AudioClip source, ClipRecipe recipe)
    {
        float[] input = new float[source.samples * source.channels];
        if (!source.GetData(input, 0)) throw new InvalidOperationException("Cannot read PCM samples: " + recipe.source);
        int start = Mathf.Clamp(Mathf.RoundToInt(recipe.start * source.frequency), 0, source.samples - 1);
        int length = Mathf.Min(Mathf.RoundToInt(recipe.duration * source.frequency), source.samples - start);
        if (recipe.firstActivity) start = FindFirstActivity(input, source.channels, source.frequency, length);
        length = Mathf.Min(length, source.samples - start);
        if (length < source.frequency / 12) throw new InvalidOperationException("Clip too short: " + recipe.source);

        float[] mono = new float[length];
        float peak = 0f;
        for (int i = 0; i < length; i++)
        {
            float sample = 0f;
            for (int channel = 0; channel < source.channels; channel++)
                sample += input[(start + i) * source.channels + channel];
            mono[i] = sample / source.channels;
            peak = Mathf.Max(peak, Mathf.Abs(mono[i]));
        }
        if (peak < .002f) throw new InvalidOperationException("Source selection is silent: " + recipe.source);

        float gain = Mathf.Min(1.5f, .58f / peak);
        int fadeSamples = Mathf.Min(length / 2, Mathf.RoundToInt(source.frequency * .015f));
        using (var stream = new MemoryStream(44 + length * 2))
        using (var writer = new BinaryWriter(stream))
        {
            writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
            writer.Write(36 + length * 2);
            writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));
            writer.Write(16);
            writer.Write((short)1);
            writer.Write((short)1);
            writer.Write(source.frequency);
            writer.Write(source.frequency * 2);
            writer.Write((short)2);
            writer.Write((short)16);
            writer.Write(System.Text.Encoding.ASCII.GetBytes("data"));
            writer.Write(length * 2);
            for (int i = 0; i < length; i++)
            {
                float fade = Mathf.Min(1f, Mathf.Min(i, length - 1 - i) / (float)Mathf.Max(1, fadeSamples));
                float sample = Mathf.Clamp(mono[i] * gain * fade, -.98f, .98f);
                writer.Write((short)Mathf.RoundToInt(sample * short.MaxValue));
            }
            return stream.ToArray();
        }
    }

    private static int FindFirstActivity(float[] input, int channels, int frequency, int clipLength)
    {
        int frames = input.Length / channels;
        int window = Mathf.Max(1, frequency / 20);
        float maximum = 0f;
        var levels = new List<float>();
        for (int start = 0; start < frames; start += window)
        {
            float sum = 0f;
            int end = Mathf.Min(start + window, frames);
            for (int i = start; i < end; i++)
            {
                float sample = input[i * channels];
                sum += sample * sample;
            }
            float rms = Mathf.Sqrt(sum / (end - start));
            levels.Add(rms);
            maximum = Mathf.Max(maximum, rms);
        }
        float threshold = Mathf.Max(.015f, maximum * .22f);
        for (int i = 0; i < levels.Count; i++)
            if (levels[i] >= threshold && i * window + clipLength <= frames)
                return Mathf.Max(0, i * window - window);
        throw new InvalidOperationException("No usable chair event found in source clip.");
    }

    private static void EnsureFolder(string path)
    {
        string[] parts = path.Split('/');
        string current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }
}
