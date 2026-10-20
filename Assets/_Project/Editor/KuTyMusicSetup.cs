using System;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>Installs the selected soundtrack without opening scenes or touching player saves.</summary>
public static class KuTyMusicSetup
{
    private const string Folder = "Assets/_Project/Audio/Music/";
    private static readonly string[] Names = {
        "Childhood_ScottBuckley.mp3", "CountrysideSummer_YoshinoriTanaka.mp3", "Reverie_ScottBuckley.mp3"
    };

    [MenuItem("KuTy/Audio/Install selected soundtrack")]
    public static void Install()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Stop Play Mode before installing music.");
        var clips = new AudioClip[Names.Length];
        for (int i = 0; i < Names.Length; i++)
        {
            string path = Folder + Names[i];
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = AssetImporter.GetAtPath(path) as AudioImporter;
            if (importer == null) throw new InvalidOperationException("Missing audio: " + path);
            importer.forceToMono = false;
            importer.loadInBackground = true;
            var settings = importer.defaultSampleSettings;
            settings.preloadAudioData = false;
            settings.loadType = AudioClipLoadType.Streaming;
            settings.compressionFormat = AudioCompressionFormat.Vorbis;
            settings.quality = .7f;
            settings.sampleRateSetting = AudioSampleRateSetting.PreserveSampleRate;
            importer.defaultSampleSettings = settings;
            importer.SaveAndReimport();
            clips[i] = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            if (clips[i] == null || clips[i].length < 60f)
                throw new InvalidOperationException("Invalid music file: " + path);
        }
        var library = AssetDatabase.LoadAssetAtPath<SoundLibrary>("Assets/_Project/Generated/SoundLibrary.asset");
        if (library == null) throw new InvalidOperationException("SoundLibrary missing.");
        Undo.RecordObject(library, "Install selected soundtrack");
        library.bgmMainMenu = clips[0];
        library.bgmAct2 = clips[1];
        library.bgmVillage = clips[1];
        library.bgmLetter = clips[2];
        library.bgmAct3 = clips[2];
        EditorUtility.SetDirty(library);
        AssetDatabase.SaveAssets();
        Validate();
    }

    [MenuItem("KuTy/Audio/Validate selected soundtrack")]
    public static void Validate()
    {
        var library = AssetDatabase.LoadAssetAtPath<SoundLibrary>("Assets/_Project/Generated/SoundLibrary.asset");
        if (library == null || !library.bgmMainMenu || !library.bgmVillage || !library.bgmLetter ||
            library.bgmAct2 != library.bgmVillage || library.bgmAct3 != library.bgmLetter)
            throw new InvalidOperationException("Soundtrack routing is incomplete.");
        var report = new System.Text.StringBuilder("Selected soundtrack validation\n");
        foreach (string name in Names)
        {
            string path = Folder + name;
            var importer = AssetImporter.GetAtPath(path) as AudioImporter;
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            if (importer == null || clip == null || clip.length < 60f ||
                importer.defaultSampleSettings.loadType != AudioClipLoadType.Streaming ||
                importer.defaultSampleSettings.compressionFormat != AudioCompressionFormat.Vorbis ||
                importer.defaultSampleSettings.preloadAudioData || !importer.loadInBackground || importer.forceToMono)
                throw new InvalidOperationException("Music import validation failed: " + path);
            report.AppendLine($"PASS {name}: {clip.length:F2}s, {clip.channels} channels, {clip.frequency}Hz, Streaming/Vorbis q0.7, background load, no preload");
        }
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Resources/KuTyAudio.prefab");
        if (!prefab || !prefab.GetComponent<AudioManager>()) throw new InvalidOperationException("Audio bootstrap prefab missing.");
        var manager = new SerializedObject(prefab.GetComponent<AudioManager>());
        if (manager.FindProperty("soundLibrary").objectReferenceValue != library ||
            manager.FindProperty("bgmSource").objectReferenceValue == null)
            throw new InvalidOperationException("Bootstrap audio references missing.");
        report.AppendLine("PASS bootstrap prefab references and menu/home/village/letter/ending routing");
        Directory.CreateDirectory("Tools/Music");
        File.WriteAllText("Tools/Music/validation.txt", report.ToString());
        Debug.Log(report.ToString());
    }
}
