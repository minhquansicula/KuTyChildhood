using System;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class KuTyOfficeGameplayVoices
{
    private const string AudioRoot = "Assets/_Project/Audio/Voice/vi/Act1/";
    [MenuItem("KuTy/Audio/Import office gameplay voices")]
    public static void Import()
    {
        var lines = new[]
        {
            new[] { "act1_ty_print_ready_001", "TÝ", "Máy photo ở cạnh bình nước. Lấy bản in rồi mang cho sếp là xong." },
            new[] { "act1_ty_print_pickup_001", "TÝ", "Bản in còn ấm. Mang đến phòng sếp thôi." },
            new[] { "act1_ty_water_after_report_001", "TÝ", "Sao vẫn choáng thế này... về ghế ngồi một lát đã." },
            new[] { "act1_ty_rest_001", "TÝ", "Mệt quá... chỉ nghỉ một chút thôi..." },
            new[] { "act1_intro_boss_004", "SẾP", "Để trên bàn đi. Được rồi, cậu về đi." }
        };
        const string assetPath = "Assets/_Project/Resources/OfficeGameplayVoices.asset";
        var sequence = AssetDatabase.LoadAssetAtPath<DialogueSequence>(assetPath);
        if (!sequence)
        {
            sequence = ScriptableObject.CreateInstance<DialogueSequence>();
            AssetDatabase.CreateAsset(sequence, assetPath);
        }
        var serialized = new SerializedObject(sequence);
        var entries = serialized.FindProperty("lines");
        entries.arraySize = 0;
        foreach (var row in lines)
        {
            string path = AudioRoot + row[0] + ".wav";
            if (!File.Exists(path)) continue;
            var importer = (AudioImporter)AssetImporter.GetAtPath(path);
            if (!importer) throw new InvalidOperationException("Missing audio importer: " + path);
            importer.forceToMono = true;
            importer.loadInBackground = false;
            var settings = importer.defaultSampleSettings;
            settings.preloadAudioData = true;
            settings.loadType = AudioClipLoadType.DecompressOnLoad;
            settings.compressionFormat = AudioCompressionFormat.PCM;
            settings.sampleRateSetting = AudioSampleRateSetting.PreserveSampleRate;
            importer.defaultSampleSettings = settings;
            importer.SaveAndReimport();
            int index = entries.arraySize++;
            var line = entries.GetArrayElementAtIndex(index);
            line.FindPropertyRelative("id").stringValue = row[0];
            line.FindPropertyRelative("speakerId").stringValue = row[1];
            line.FindPropertyRelative("text").stringValue = row[2];
            line.FindPropertyRelative("spokenText").stringValue = row[2];
            line.FindPropertyRelative("fallbackDuration").floatValue = 3f;
            line.FindPropertyRelative("postDelay").floatValue = .25f;
            line.FindPropertyRelative("voiceClip").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            line.FindPropertyRelative("slam").boolValue = false;
            line.FindPropertyRelative("silentBeat").boolValue = false;
        }
        serialized.ApplyModifiedPropertiesWithoutUndo();
        AssetDatabase.SaveAssets();
        Debug.Log("[Office voices] Imported " + entries.arraySize + " gameplay recordings; opening voices unchanged.");
    }
}
