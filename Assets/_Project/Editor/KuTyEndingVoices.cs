#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class KuTyEndingVoices
{
    private const string Act2AudioRoot = "Assets/_Project/Audio/Voice/vi/Act2/";
    private const string EndingAudioRoot = "Assets/_Project/Audio/Voice/vi/Ending/";
    private const string AssetPath = "Assets/_Project/Resources/EndingVoices.asset";

    [MenuItem("KuTy/Audio/Import ending voices")]
    public static void Import()
    {
        var lines = new[]
        {
            new[] { "act2_ty_light_fire_001", "TÝ", "Bếp bắt lửa, cả nhà bắt đầu ấm.", Act2AudioRoot + "act2_ty_light_fire_001.wav" },
            new[] { "act2_ty_cook_rice_001", "TÝ", "Mùi cơm chín khiến con biết sắp được gọi về.", Act2AudioRoot + "act2_ty_cook_rice_001.wav" },
            new[] { "act2_ty_set_table_001", "TÝ", "Mâm cơm đủ người là bữa ngon nhất.", Act2AudioRoot + "act2_ty_set_table_001.wav" },
            new[] { "act2_ty_put_away_dishes_001", "TÝ", "Tiếng chén khẽ chạm nhau, việc nhà đã xong.", Act2AudioRoot + "act2_ty_put_away_dishes_001.wav" },
            new[] { "act2_mom_letter_001", "MẸ", "Sau này đi đâu làm gì, con cũng nhớ phải ăn cơm đúng bữa nhé. Nhà mình luôn có một chỗ chờ con.", Act2AudioRoot + "act2_mom_letter_001.wav" },
            new[] { "ending_ty_wake_001", "QUÂN", "Ngoài khung cửa sổ, cơn mưa đêm đã tạnh... Nắng sớm đã lên rồi.", EndingAudioRoot + "ending_ty_wake_001.wav" },
            new[] { "ending_ty_horizon_001", "QUÂN", "Dù đi đâu làm gì... trong lòng vẫn luôn có một bếp lửa ấm để trở về.", EndingAudioRoot + "ending_ty_horizon_001.wav" },
        };

        var sequence = AssetDatabase.LoadAssetAtPath<DialogueSequence>(AssetPath);
        if (!sequence)
        {
            sequence = ScriptableObject.CreateInstance<DialogueSequence>();
            AssetDatabase.CreateAsset(sequence, AssetPath);
        }

        var serialized = new SerializedObject(sequence);
        var entries = serialized.FindProperty("lines");
        entries.arraySize = 0;

        foreach (var row in lines)
        {
            string path = row[3];
            if (!File.Exists(path)) continue;

            var importer = (AudioImporter)AssetImporter.GetAtPath(path);
            if (!importer)
            {
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
                importer = (AudioImporter)AssetImporter.GetAtPath(path);
            }
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

            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            int index = entries.arraySize++;
            var line = entries.GetArrayElementAtIndex(index);
            line.FindPropertyRelative("id").stringValue = row[0];
            line.FindPropertyRelative("speakerId").stringValue = row[1];
            line.FindPropertyRelative("text").stringValue = row[2];
            line.FindPropertyRelative("spokenText").stringValue = row[2];
            line.FindPropertyRelative("fallbackDuration").floatValue = clip != null ? clip.length : 3f;
            line.FindPropertyRelative("postDelay").floatValue = .2f;
            line.FindPropertyRelative("voiceClip").objectReferenceValue = clip;
            line.FindPropertyRelative("slam").boolValue = false;
            line.FindPropertyRelative("silentBeat").boolValue = false;
        }

        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(sequence);
        AssetDatabase.SaveAssets();
        Debug.Log("[Ending voices] Imported " + entries.arraySize + " ending voice recordings into EndingVoices.asset.");
    }
}
#endif
