using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class KuTySfxSetup
{
    private const string CatalogFolder = "Assets/_Project/Data/Audio";
    private const string CatalogPath = CatalogFolder + "/SfxCatalog.asset";
    private const string GeneratedFolder = "Assets/_Project/Generated/SFX";
    private const string ManifestPath = "Assets/_Project/Audio/SFX/sfx_manifest.json";
    private const string AudioPrefabPath = "Assets/_Project/Resources/KuTyAudio.prefab";

    private sealed class CueSpec
    {
        public SfxId id;
        public SfxCategory category;
        public string path;
        public float frequency;
        public float seconds;
        public float volume;
        public float pitchMin;
        public float pitchMax;
        public float cooldown;
    }

    [Serializable]
    private sealed class SfxManifest
    {
        public string status = "placeholder-foundation";
        public string catalogAsset = CatalogPath;
        public string note = "Generated tones are safe fallbacks, not approved production SFX.";
        public List<SfxManifestCue> cues = new List<SfxManifestCue>();
    }

    [Serializable]
    private sealed class SfxManifestCue
    {
        public string id;
        public string category;
        public string[] clipPaths;
        public bool placeholder;
        public string source;
        public string license;
        public string status;
    }

    [MenuItem("KuTy/SFX/Install SFX-A foundation")]
    public static void Install()
    {
        if (EditorApplication.isPlaying)
        {
            Debug.LogError("[KuTy SFX] Stop Play Mode before installing SFX-A.");
            return;
        }

        EnsureFolder(CatalogFolder);
        EnsureFolder(GeneratedFolder);
        EnsureFolder("Assets/_Project/Audio/SFX");

        List<CueSpec> specs = BuildSpecs();
        for (int i = 0; i < specs.Count; i++)
        {
            CueSpec spec = specs[i];
            if (string.IsNullOrEmpty(spec.path)) spec.path = GeneratedFolder + "/" + spec.id + ".wav";
            if (!File.Exists(spec.path)) WritePlaceholderTone(spec.path, spec.frequency, spec.seconds);
            AssetDatabase.ImportAsset(spec.path, ImportAssetOptions.ForceUpdate);
        }

        SfxCatalog catalog = AssetDatabase.LoadAssetAtPath<SfxCatalog>(CatalogPath);
        if (catalog == null)
        {
            catalog = ScriptableObject.CreateInstance<SfxCatalog>();
            catalog.name = "SfxCatalog";
            AssetDatabase.CreateAsset(catalog, CatalogPath);
        }

        var existingVariants = new Dictionary<SfxId, AudioClip[]>();
        foreach (SfxCue cue in catalog.Cues)
            if (cue != null && cue.id != SfxId.None && cue.variants != null && cue.variants.Any(clip => clip != null))
                existingVariants[cue.id] = cue.variants;

        var cues = new List<SfxCue>();
        for (int i = 0; i < specs.Count; i++)
        {
            CueSpec spec = specs[i];
            AudioClip[] variants = existingVariants.TryGetValue(spec.id, out AudioClip[] saved)
                ? saved.Where(clip => clip != null).ToArray()
                : new[] { AssetDatabase.LoadAssetAtPath<AudioClip>(spec.path) };
            cues.Add(new SfxCue
            {
                id = spec.id,
                category = spec.category,
                variants = variants,
                volume = spec.volume,
                pitchMin = spec.pitchMin,
                pitchMax = spec.pitchMax,
                cooldownSeconds = spec.cooldown,
                avoidImmediateRepeat = true
            });
        }
        catalog.ReplaceCuesForEditor(cues);
        EditorUtility.SetDirty(catalog);

        ConfigureAudioPrefab(catalog);
        RefreshManifest(catalog);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[KuTy SFX] SFX-A foundation installed: {catalog.Count} typed cues, UI pause source and manifest. Production replacements remain pending listening review.");
    }

    [MenuItem("KuTy/Validation/Validate SFX-A foundation")]
    public static void Validate()
    {
        var errors = new List<string>();
        var warnings = new List<string>();
        SfxCatalog catalog = AssetDatabase.LoadAssetAtPath<SfxCatalog>(CatalogPath);
        if (catalog == null) errors.Add("SfxCatalog asset is missing.");
        else
        {
            errors.AddRange(catalog.CollectValidationIssues(true));
            int placeholders = 0;
            foreach (SfxCue cue in catalog.Cues)
                if (cue != null && cue.variants != null && cue.variants.Any(IsPlaceholder)) placeholders++;
            if (placeholders > 0) warnings.Add($"{placeholders} cues still use generated placeholder audio.");
        }

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AudioPrefabPath);
        AudioManager manager = prefab != null ? prefab.GetComponent<AudioManager>() : null;
        if (manager == null) errors.Add("KuTyAudio prefab or AudioManager is missing.");
        else
        {
            SerializedObject serialized = new SerializedObject(manager);
            if (serialized.FindProperty("sfxCatalog").objectReferenceValue != catalog)
                errors.Add("KuTyAudio does not reference the SFX catalog.");
            AudioSource uiSource = serialized.FindProperty("uiSource").objectReferenceValue as AudioSource;
            // AudioSource.ignoreListenerPause is runtime-only and is restored by
            // AudioManager.InitializeSfxSources, so the smoke test validates it.
            if (uiSource == null || uiSource.playOnAwake || uiSource.loop)
                errors.Add("UI AudioSource must be non-looping and not play on awake.");
        }

        string[] scripts = Directory.GetFiles("Assets/_Project/Scripts", "*.cs", SearchOption.AllDirectories);
        List<string> stringCalls = scripts.Where(path => File.ReadAllText(path).Contains("PlaySFX(\"")).ToList();
        if (stringCalls.Count > 0) errors.Add("Legacy string SFX calls remain: " + string.Join(", ", stringCalls));
        if (!File.Exists(ManifestPath)) errors.Add("SFX manifest is missing.");

        if (errors.Count > 0) Debug.LogError("[KuTy SFX] Validation failed:\n- " + string.Join("\n- ", errors));
        else if (warnings.Count > 0) Debug.LogWarning("[KuTy SFX] Runtime foundation is valid; content review remains:\n- " + string.Join("\n- ", warnings));
        else Debug.Log("[KuTy SFX] SFX-A validation passed with production clips.");
    }

    private static List<CueSpec> BuildSpecs()
    {
        return new List<CueSpec>
        {
            Spec(SfxId.UIConfirm, SfxCategory.UI, 720f, .09f, .62f, .98f, 1.02f, .05f),
            Spec(SfxId.UIBack, SfxCategory.UI, 420f, .11f, .55f, .98f, 1.02f, .08f),
            Spec(SfxId.UIInvalid, SfxCategory.UI, 145f, .18f, .68f, 1f, 1f, .18f),
            Spec(SfxId.UIHover, SfxCategory.UI, 920f, .055f, .2f, 1.12f, 1.16f, .055f),
            Spec(SfxId.TaskProgress, SfxCategory.UI, 880f, .12f, .55f, .98f, 1.03f, .08f),
            Spec(SfxId.TaskComplete, SfxCategory.UI, 1046f, .25f, .72f, 1f, 1f, .3f, "Assets/_Project/Generated/Complete.wav"),
            Spec(SfxId.WaterSplash, SfxCategory.Gameplay, 510f, .24f, .62f, .92f, 1.05f, .12f),
            Spec(SfxId.DishClean, SfxCategory.Gameplay, 660f, .25f, .68f, .96f, 1.04f, .15f, "Assets/_Project/Generated/DishClean.wav"),
            Spec(SfxId.MarbleShoot, SfxCategory.Gameplay, 220f, .25f, .72f, .94f, 1.06f, .06f, "Assets/_Project/Generated/MarbleShoot.wav"),
            Spec(SfxId.MarbleHit, SfxCategory.Gameplay, 290f, .12f, .68f, .88f, 1.12f, .04f),
            Spec(SfxId.MarbleRoll, SfxCategory.Gameplay, 105f, .3f, .48f, .92f, 1.08f, .12f),
            Spec(SfxId.Purchase, SfxCategory.UI, 880f, .25f, .7f, 1f, 1f, .2f, "Assets/_Project/Generated/Purchase.wav"),
            Spec(SfxId.PurchaseFail, SfxCategory.UI, 140f, .25f, .7f, 1f, 1f, .2f, "Assets/_Project/Generated/PurchaseFail.wav"),
            Spec(SfxId.MemoryCollected, SfxCategory.UI, 1046f, .25f, .68f, .98f, 1.02f, .2f, "Assets/_Project/Generated/Memory.wav"),
            Spec(SfxId.AllMemoriesComplete, SfxCategory.UI, 1320f, .25f, .74f, 1f, 1f, .5f, "Assets/_Project/Generated/Complete.wav")
        };
    }

    private static CueSpec Spec(SfxId id, SfxCategory category, float frequency, float seconds,
        float volume, float pitchMin, float pitchMax, float cooldown, string path = null)
    {
        return new CueSpec
        {
            id = id,
            category = category,
            path = path,
            frequency = frequency,
            seconds = seconds,
            volume = volume,
            pitchMin = pitchMin,
            pitchMax = pitchMax,
            cooldown = cooldown
        };
    }

    private static void ConfigureAudioPrefab(SfxCatalog catalog)
    {
        GameObject root = PrefabUtility.LoadPrefabContents(AudioPrefabPath);
        try
        {
            AudioManager manager = root.GetComponent<AudioManager>();
            if (manager == null) throw new MissingComponentException("KuTyAudio prefab has no AudioManager.");
            SerializedObject serialized = new SerializedObject(manager);
            serialized.FindProperty("sfxCatalog").objectReferenceValue = catalog;
            AudioSource uiSource = serialized.FindProperty("uiSource").objectReferenceValue as AudioSource;
            if (uiSource == null) uiSource = root.AddComponent<AudioSource>();
            uiSource.playOnAwake = false;
            uiSource.loop = false;
            uiSource.spatialBlend = 0f;
            uiSource.ignoreListenerPause = true;
            serialized.FindProperty("uiSource").objectReferenceValue = uiSource;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(manager);
            EditorUtility.SetDirty(uiSource);
            PrefabUtility.SaveAsPrefabAsset(root, AudioPrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    public static void RefreshManifest(SfxCatalog catalog)
    {
        var manifest = new SfxManifest();
        foreach (SfxCue cue in catalog.Cues)
        {
            if (cue == null) continue;
            string[] paths = cue.variants == null
                ? Array.Empty<string>()
                : cue.variants.Where(clip => clip != null).Select(AssetDatabase.GetAssetPath).ToArray();
            bool placeholder = paths.Any(path => path.StartsWith("Assets/_Project/Generated/", StringComparison.Ordinal));
            bool kenneyUi = paths.Length > 0 && paths.All(path =>
                path.StartsWith("Assets/_Project/Audio/SFX/UI/", StringComparison.Ordinal));
            manifest.cues.Add(new SfxManifestCue
            {
                id = cue.id.ToString(),
                category = cue.category.ToString(),
                clipPaths = paths,
                placeholder = placeholder,
                source = placeholder ? "KuTy generated fallback" : kenneyUi ? "Kenney Interface Sounds" : "unrecorded",
                license = placeholder ? "project-owned generated tone" : kenneyUi ? "CC0 1.0" : "unrecorded",
                status = placeholder ? "replace-before-release" : "assigned-awaiting-listening-review"
            });
        }
        int placeholderCount = manifest.cues.Count(cue => cue.placeholder);
        manifest.status = placeholderCount == 0 ? "production-awaiting-listening-review" :
            placeholderCount == manifest.cues.Count ? "placeholder-foundation" : "mixed-production-and-placeholders";
        File.WriteAllText(ManifestPath, JsonUtility.ToJson(manifest, true));
        AssetDatabase.ImportAsset(ManifestPath, ImportAssetOptions.ForceUpdate);
    }

    private static bool IsPlaceholder(AudioClip clip)
    {
        return clip != null && AssetDatabase.GetAssetPath(clip).StartsWith("Assets/_Project/Generated/", StringComparison.Ordinal);
    }

    private static void WritePlaceholderTone(string path, float frequency, float seconds)
    {
        const int sampleRate = 22050;
        int count = Mathf.Max(256, Mathf.RoundToInt(sampleRate * Mathf.Max(.05f, seconds)));
        using (var stream = new BinaryWriter(File.Create(path)))
        {
            stream.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
            stream.Write(36 + count * 2);
            stream.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));
            stream.Write(16);
            stream.Write((short)1);
            stream.Write((short)1);
            stream.Write(sampleRate);
            stream.Write(sampleRate * 2);
            stream.Write((short)2);
            stream.Write((short)16);
            stream.Write(System.Text.Encoding.ASCII.GetBytes("data"));
            stream.Write(count * 2);
            for (int i = 0; i < count; i++)
            {
                float t = (float)i / sampleRate;
                float envelope = Mathf.Pow(1f - (float)i / count, 2f);
                float sample = Mathf.Sin(2f * Mathf.PI * frequency * t) * envelope * .28f;
                stream.Write((short)(Mathf.Clamp(sample, -1f, 1f) * short.MaxValue));
            }
        }
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
