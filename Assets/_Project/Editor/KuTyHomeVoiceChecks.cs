using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class KuTyHomeVoiceChecks
{
    private const string Pending = "KuTy.HomeVoiceChecks.Pending";
    private const string Previous = "KuTy.HomeVoiceChecks.PreviousStart";
    static KuTyHomeVoiceChecks()
    {
        EditorApplication.playModeStateChanged += state =>
        {
            if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Pending, false))
                new GameObject("Home voice checks").AddComponent<KuTyHomeVoiceRunner>().Begin();
            if (state == PlayModeStateChange.EnteredEditMode && SessionState.GetBool(Pending, false))
            {
                EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(SessionState.GetString(Previous, ""));
                SessionState.EraseBool(Pending);
            }
        };
    }
    [MenuItem("KuTy/Tests/Check home voices in isolation")]
    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Use Edit Mode.");
        KuTyCheckpointTestScope.Capture();
        SessionState.SetString(Previous, AssetDatabase.GetAssetPath(EditorSceneManager.playModeStartScene));
        SessionState.SetBool(Pending, true);
        EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/_Project/Generated/MusicSmoke.unity");
        EditorApplication.EnterPlaymode();
    }
}

public sealed class KuTyHomeVoiceRunner : MonoBehaviour
{
    private readonly StringBuilder report = new StringBuilder();
    private bool finished, previousBackground;
    private float started;
    public void Begin()
    {
        DontDestroyOnLoad(gameObject);
        previousBackground = Application.runInBackground;
        Application.runInBackground = true;
        started = Time.realtimeSinceStartup;
        StartCoroutine(Run());
    }
    private void Update() { if (!finished && Time.realtimeSinceStartup-started > 100f) Finish(false); }
    private void Check(bool condition, string text)
    {
        report.AppendLine((condition ? "PASS " : "FAIL ") + text);
        if (!condition) { Finish(false); throw new InvalidOperationException(text); }
    }
    private void CheckVoice(HouseholdVoicePlayer player, DialogueSequence sequence, string id)
    {
        var line = sequence.Lines.Single(l => l.id == id);
        Check(player.CurrentId == id && player.Voice.IsPlaying && player.Voice.Source.clip == line.voiceClip,
            "Voice plays at its home interaction: " + id);
        Check(line.text == line.spokenText && line.voiceClip.channels == 1 && player.Voice.Source.spatialBlend == 0f,
            "Matching text and clear mono narration: " + id);
    }
    private IEnumerator Run()
    {
        yield return SceneManager.LoadSceneAsync(SceneNames.Act2_Home);
        yield return null;
        var flow = FindObjectOfType<HouseholdChoreFlow>();
        var sequence = Resources.Load<DialogueSequence>("HomeGameplayVoices");
        Check(flow && sequence && sequence.Count == 15, "Fifteen supplied recordings are available in Scene 2");
        var bed = FindObjectOfType<BedWakeUpController>();
        yield return new WaitForSeconds(1.1f);
        typeof(BedWakeUpController).GetMethod("GetOutOfBed", BindingFlags.Instance|BindingFlags.NonPublic).Invoke(bed, null);
        CheckVoice(flow.Voice, sequence, "act2_mom_wakeup_001");
        Check(sequence.Lines.Single(l => l.id == "act2_mom_wakeup_001").text == HouseholdChoreFlow.WakeupText,
            "Mother's updated wakeup script matches the recording entry");
        yield return new WaitForSeconds(.2f);
        GameManager.Instance.SetPaused(true);
        float position = flow.Voice.Voice.Source.time;
        yield return new WaitForSecondsRealtime(.2f);
        Check(flow.Voice.Voice.IsPaused && Mathf.Abs(flow.Voice.Voice.Source.time-position) < .05f,
            "Pause freezes home narration at its playback position");
        GameManager.Instance.SetPaused(false);
        Check(flow.Voice.Voice.IsPlaying, "Resume continues the same home recording");
        int token = flow.Voice.Voice.PlaybackToken;
        flow.ShowChecklist();
        Check(flow.Voice.Voice.PlaybackToken == token, "Repeated checklist display does not replay wakeup voice");
        foreach (var chore in HouseholdChoreFlow.Order.Take(HouseholdChoreFlow.ChoreCount-1))
        {
            Check(flow.Complete(chore), "Chore completion succeeds: " + chore);
            string id = HouseholdVoicePlayer.ChoreId(chore);
            if (chore == HouseholdChore.SortTools)
                Check(flow.Voice.Voice.Source.clip == null, "Missing tools recording keeps text and stops previous narration");
            else CheckVoice(flow.Voice, sequence, id);
            if (chore == HouseholdChore.FetchWater)
                Check(sequence.Lines.Single(l => l.id == id).text == "Gáo nước mát nhờ mẹ để dành từ sáng." &&
                    HouseholdChoreFlow.Memories[(int)chore] == "Gáo nước mát nhờ mẹ để dành từ sáng.",
                    "Updated fetch-water text matches in both UI and voice data");
            token = flow.Voice.Voice.PlaybackToken;
            Check(!flow.Complete(chore) && flow.Voice.Voice.PlaybackToken == token,
                "Completed chore cannot replay its voice or reward: " + chore);
            yield return new WaitForSeconds(.1f);
        }
        var letter = FindObjectsOfType<HouseholdChoreInteractable>().Single(c => c.Chore == HouseholdChore.ReadLetter);
        typeof(HouseholdChoreInteractable).GetMethod("OnInteract", BindingFlags.Instance|BindingFlags.NonPublic).Invoke(letter, null);
        var advance = typeof(HouseholdActionBoard).GetMethod("AdvanceLetter", BindingFlags.Instance|BindingFlags.NonPublic);
        for (int i = 0; i < 3; i++) advance.Invoke(flow.UI.Actions, null);
        CheckVoice(flow.Voice, sequence, "act2_ty_mom_memory_001");
        Check(DialogueUI.Instance.IsShowing, "Final memory is displayed before the mother's letter");
        while (flow.Voice.CurrentId != "act2_mom_letter_001") yield return null;
        CheckVoice(flow.Voice, sequence, "act2_mom_letter_001");
        Check(sequence.Lines.Single(l => l.id == "act2_mom_letter_001").text == HouseholdChoreFlow.Memories[(int)HouseholdChore.ReadLetter],
            "Mother's updated letter text matches its supplied recording");
        Check(flow.Pieces == 140 && MemoryCollectionManager.Instance.HasCollected(MemoryType.FilialLove),
            "Voice sequence preserves all chore rewards and the mother's memory");
        yield return new WaitForSeconds(sequence.Lines.Single(l => l.id == "act2_mom_letter_001").ResolveDuration()+.3f);
        Check(DialogueUI.Instance.IsShowing && SceneManager.GetActiveScene().name == SceneNames.Act2_Home,
            "Letter has no deadline and stays open after the voice finishes");
        Directory.CreateDirectory("Tools/HomeVoices");
        ScreenCapture.CaptureScreenshot("Tools/HomeVoices/mother-letter.png");
        yield return new WaitForEndOfFrame();
        DialogueUI.Instance.HideDialogue();
        Check(flow.Voice.Voice.Source.clip == null, "Closing the letter stops narration before leaving home");
        while (SceneManager.GetActiveScene().name != SceneNames.Act3_OutSide) yield return null;
        Check(true, "Closing the letter continues to Scene 3");
        Finish(true);
    }
    private void Finish(bool success)
    {
        if (finished) return;
        finished = true;
        Directory.CreateDirectory("Tools/HomeVoices");
        File.WriteAllText("Tools/HomeVoices/checks.txt", (success ? "PASS\n" : "FAIL\n") + report);
        Application.runInBackground = previousBackground;
        EditorApplication.delayCall += () => EditorApplication.ExitPlaymode();
    }
}
