using System;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class KuTyHomeGameplayVoices
{
    private const string AudioRoot = "Assets/_Project/Audio/Voice/vi/Act2/";
    [MenuItem("KuTy/Audio/Import home gameplay voices")]
    public static void Import()
    {
        var lines = new[]
        {
            new[] { "act2_mom_wakeup_001", "MẸ", "Con dậy rồi đấy à. Vào dọn đồ, phụ mẹ một tay nhé." },
            new[] { "act2_mom_resume_001", "MẸ", "Mình tiếp tục việc đang làm nhé." },
            new[] { "act2_mom_tools_001", "MẸ", "Dùng xong nhớ cất, mai còn tìm được con nhé." },
            new[] { "act2_mom_letter_001", "MẸ", "Sau này đi đâu làm gì, con cũng nhớ phải ăn cơm đúng bữa nhé. Nhà mình luôn có một chỗ chờ con." },
            new[] { "act2_ty_keepsakes_001", "TÝ", "Chiếc rương giữ cả những mùa áo cũ." },
            new[] { "act2_ty_radio_001", "TÝ", "Tiếng radio mở đầu mỗi buổi sáng của nhà mình." },
            new[] { "act2_ty_grind_corn_001", "TÝ", "Tiếng cối quay đều như nhịp tay mẹ." },
            new[] { "act2_ty_firewood_001", "TÝ", "Bó củi nhỏ mang theo hơi ấm cho cả nhà." },
            new[] { "act2_ty_fetch_water_001", "TÝ", "Gáo nước mát nhờ mẹ để dành từ sáng." },
            new[] { "act2_ty_measure_rice_001", "TÝ", "Mẹ luôn đong thêm một chút cho con." },
            new[] { "act2_ty_wash_rice_001", "TÝ", "Nước đục đi, hạt gạo lại trắng." },
            new[] { "act2_ty_light_fire_001", "TÝ", "Bếp bắt lửa, cả nhà bắt đầu ấm." },
            new[] { "act2_ty_cook_rice_001", "TÝ", "Mùi cơm chín khiến con biết sắp được gọi về." },
            new[] { "act2_ty_set_table_001", "TÝ", "Mâm cơm đủ người là bữa ngon nhất." },
            new[] { "act2_ty_wash_dishes_001", "TÝ", "Mẹ rửa, con tráng, hai mẹ con chuyện trò." },
            new[] { "act2_ty_put_away_dishes_001", "TÝ", "Tiếng chén khẽ chạm nhau, việc nhà đã xong." },
            new[] { "act2_ty_mom_memory_001", "TÝ", "Mẹ xoa đầu khen mình ngoan. Cảm giác thật bình yên." },
        };
        const string assetPath = "Assets/_Project/Resources/HomeGameplayVoices.asset";
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
        Debug.Log("[Home voices] Imported " + entries.arraySize + " home recordings; missing recordings keep their text.");
    }
}
