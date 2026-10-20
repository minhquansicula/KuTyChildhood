using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class KuTyAct1TtsSetup
{
    private const string ScenePath = "Assets/_Project/Scenes/Act1_Office_Blockout.unity";
    private const string DataFolder = "Assets/_Project/Data/Dialogue";
    private const string SequencePath = DataFolder + "/Act1OfficeIntro.asset";
    private const string VoiceFolder = "Assets/_Project/Audio/Voice/vi/Act1";
    private const string ManifestPath = VoiceFolder + "/act1_tts_manifest.json";

    [Serializable]
    private sealed class VoiceManifest
    {
        public string language = "vi-VN";
        public string sequenceAsset = SequencePath;
        public string status = "awaiting-voice-assets";
        public string provider = "unselected";
        public string generatedAt = "";
        public List<VoiceManifestLine> lines = new List<VoiceManifestLine>();
    }

    [Serializable]
    private sealed class VoiceManifestLine
    {
        public string id;
        public string speakerId;
        public string text;
        public string spokenText;
        public string textHash;
        public string emotionDirection;
        public string expectedAssetPath;
        public string assignedClipPath;
        public string status;
    }

    [MenuItem("KuTy/TTS/Install Act1 pilot")]
    public static void Install()
    {
        if (EditorApplication.isPlaying || SceneManager.GetActiveScene().path != ScenePath)
        {
            Debug.LogError("[KuTy TTS] Open Act1_Office_Blockout outside Play Mode before installing the pilot.");
            return;
        }

        OfficeIntroController intro = UnityEngine.Object.FindObjectOfType<OfficeIntroController>();
        if (intro == null)
        {
            Debug.LogError("[KuTy TTS] OfficeIntroController is missing from Act1.");
            return;
        }

        EnsureFolder(DataFolder);
        EnsureFolder(VoiceFolder);
        DialogueSequence sequence = AssetDatabase.LoadAssetAtPath<DialogueSequence>(SequencePath);
        if (sequence == null)
        {
            sequence = ScriptableObject.CreateInstance<DialogueSequence>();
            sequence.name = "Act1OfficeIntro";
            AssetDatabase.CreateAsset(sequence, SequencePath);
        }

        Dictionary<string, AudioClip> existingClips = new Dictionary<string, AudioClip>(StringComparer.Ordinal);
        Dictionary<string, string> existingHashes = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (DialogueSequence.DialogueLine line in sequence.Lines)
        {
            if (line == null || string.IsNullOrWhiteSpace(line.id)) continue;
            existingHashes[line.id] = line.TextHash;
            if (line.voiceClip != null) existingClips[line.id] = line.voiceClip;
        }

        List<DialogueSequence.DialogueLine> lines = CreatePilotLines();
        var staleTextIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (DialogueSequence.DialogueLine line in lines)
        {
            bool textChanged = existingHashes.TryGetValue(line.id, out string previousHash) && previousHash != line.TextHash;
            if (textChanged)
            {
                staleTextIds.Add(line.id);
                continue;
            }
            if (existingClips.TryGetValue(line.id, out AudioClip clip)) line.voiceClip = clip;
            else line.voiceClip = AssetDatabase.LoadAssetAtPath<AudioClip>(VoiceFolder + "/" + line.id + ".wav");
        }
        sequence.ReplaceLinesForEditor(lines);
        EditorUtility.SetDirty(sequence);

        AudioSource source = intro.voice;
        if (source == null)
        {
            Transform existing = intro.transform.Find("DialogueVoice");
            GameObject voiceObject = existing != null ? existing.gameObject : new GameObject("DialogueVoice");
            if (existing == null) voiceObject.transform.SetParent(intro.transform, false);
            source = voiceObject.GetComponent<AudioSource>() ?? voiceObject.AddComponent<AudioSource>();
            intro.voice = source;
        }

        DialogueVoicePlayer player = source.GetComponent<DialogueVoicePlayer>();
        if (player == null) player = source.gameObject.AddComponent<DialogueVoicePlayer>();
        player.ConfigureForEditor(source);
        EditorUtility.SetDirty(player);
        EditorUtility.SetDirty(source);

        SerializedObject serializedIntro = new SerializedObject(intro);
        serializedIntro.FindProperty("dialogueSequence").objectReferenceValue = sequence;
        serializedIntro.FindProperty("voicePlayer").objectReferenceValue = player;
        serializedIntro.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(intro);

        WriteManifest(sequence, staleTextIds);
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        int missingVoiceCount = sequence.Lines.Count(line => line != null && !line.silentBeat && line.voiceClip == null);
        Debug.Log($"[KuTy TTS] Act1 pilot installed: {sequence.Count} beats, {missingVoiceCount} voice clips awaiting Vietnamese TTS approval. Existing clip assignments were preserved by stable ID.");
    }

    [MenuItem("KuTy/Validation/Validate Act1 TTS pilot")]
    public static void Validate()
    {
        DialogueSequence sequence = AssetDatabase.LoadAssetAtPath<DialogueSequence>(SequencePath);
        var errors = new List<string>();
        var warnings = new List<string>();
        if (sequence == null) errors.Add("DialogueSequence asset is missing.");
        else
        {
            errors.AddRange(sequence.CollectValidationIssues(false));
            warnings.AddRange(sequence.CollectValidationIssues(true)
                .Where(issue => issue.Contains("missing its voice clip")));
        }

        OfficeIntroController intro = UnityEngine.Object.FindObjectOfType<OfficeIntroController>();
        if (SceneManager.GetActiveScene().path == ScenePath)
        {
            if (intro == null) errors.Add("OfficeIntroController is missing from Act1.");
            else
            {
                if (intro.Sequence != sequence) errors.Add("OfficeIntroController is not assigned to the Act1 sequence.");
                if (intro.VoicePlayer == null || intro.VoicePlayer.Source == null) errors.Add("DialogueVoicePlayer or its AudioSource is missing.");
                else if (intro.VoicePlayer.Source.playOnAwake || intro.VoicePlayer.Source.loop || intro.VoicePlayer.Source.spatialBlend != 0f)
                    errors.Add("Voice AudioSource must be 2D, non-looping, and not play on awake.");
            }
        }
        else warnings.Add("Act1 scene is not open, so scene wiring was not checked.");

        if (!File.Exists(ManifestPath)) errors.Add("TTS manifest is missing.");
        if (errors.Count > 0) Debug.LogError("[KuTy TTS] Validation failed:\n- " + string.Join("\n- ", errors));
        else if (warnings.Count > 0) Debug.LogWarning("[KuTy TTS] Runtime integration is valid; pilot audio is not yet complete:\n- " + string.Join("\n- ", warnings));
        else Debug.Log("[KuTy TTS] Act1 TTS pilot validation passed with all voice clips assigned.");
    }

    private static List<DialogueSequence.DialogueLine> CreatePilotLines()
    {
        return new List<DialogueSequence.DialogueLine>
        {
            Line("act1_intro_boss_001", "SẾP", "Số liệu trong file này là sao?\nNhiều ô còn sai mà cậu đã gửi rồi à?", 7f, .2f),
            Line("act1_intro_boss_002", "SẾP", "Khách hàng đang chờ bản chốt.\nTôi đã nhắc cậu từ sáng rồi.", 5f, .2f),
            Beat("act1_intro_slam_001", .8f, true),
            Line("act1_intro_boss_003", "SẾP", "Quay về bàn, kiểm tra lại file Excel.\nXong thì in một bản, mang lại đây cho tôi ký.", 5f, .25f),
            Line("act1_intro_ty_001", "TÝ", "Dạ… em kiểm tra lại ngay ạ.", 3f, .25f),
            Beat("act1_intro_pause_001", 1f, false)
        };
    }

    private static DialogueSequence.DialogueLine Line(string id, string speaker, string text, float fallback, float postDelay)
    {
        return new DialogueSequence.DialogueLine
        {
            id = id,
            speakerId = speaker,
            text = text,
            spokenText = text,
            fallbackDuration = fallback,
            postDelay = postDelay
        };
    }

    private static DialogueSequence.DialogueLine Beat(string id, float duration, bool slam)
    {
        return new DialogueSequence.DialogueLine
        {
            id = id,
            fallbackDuration = duration,
            silentBeat = true,
            slam = slam
        };
    }

    private static void WriteManifest(DialogueSequence sequence, HashSet<string> staleTextIds)
    {
        var manifest = new VoiceManifest();
        foreach (DialogueSequence.DialogueLine line in sequence.Lines)
        {
            if (line == null || line.silentBeat) continue;
            string clipPath = line.voiceClip != null ? AssetDatabase.GetAssetPath(line.voiceClip) : string.Empty;
            manifest.lines.Add(new VoiceManifestLine
            {
                id = line.id,
                speakerId = line.speakerId,
                text = line.text,
                spokenText = line.EffectiveSpokenText,
                textHash = line.TextHash,
                emotionDirection = line.speakerId == "SẾP" ? "Điềm, khô, dứt câu; không quát." : "Mệt, dè dặt, ngắt tự nhiên.",
                expectedAssetPath = VoiceFolder + "/" + line.id + ".wav",
                assignedClipPath = clipPath,
                status = staleTextIds.Contains(line.id) ? "stale-text" : string.IsNullOrEmpty(clipPath) ? "missing" : "assigned"
            });
        }
        manifest.status = manifest.lines.All(line => line.status == "assigned") ? "clips-assigned-awaiting-listening-review" : "awaiting-voice-assets";
        File.WriteAllText(ManifestPath, JsonUtility.ToJson(manifest, true));
        AssetDatabase.ImportAsset(ManifestPath, ImportAssetOptions.ForceUpdate);
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
