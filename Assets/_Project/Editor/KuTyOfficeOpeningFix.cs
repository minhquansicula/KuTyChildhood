using System;
using System.Collections;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class KuTyOfficeOpeningFix
{
    private const string Pending = "KuTy.OpeningFix.Test";
    private const string Previous = "KuTy.OpeningFix.PreviousStart";
    static KuTyOfficeOpeningFix()
    {
        EditorApplication.playModeStateChanged += state => {
            if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Pending, false))
                new GameObject("Opening regression check").AddComponent<KuTyOpeningCheckRunner>().Begin();
            if (state == PlayModeStateChange.EnteredEditMode && SessionState.GetBool(Pending, false))
            {
                EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(SessionState.GetString(Previous, ""));
                SessionState.EraseBool(Pending);
            }
        };
    }

    [MenuItem("KuTy/Office/Fix opening framing")]
    public static void FixFraming()
    {
        var scene = SceneManager.GetActiveScene();
        if (EditorApplication.isPlayingOrWillChangePlaymode || scene.isDirty)
            throw new InvalidOperationException("Open a saved Act1 scene in Edit Mode.");
        if (scene.name != SceneNames.Act1)
            scene = EditorSceneManager.OpenScene("Assets/_Project/Scenes/Act1_Office_Blockout.unity");
        var intro = UnityEngine.Object.FindObjectOfType<OfficeIntroController>();
        var boss = GameObject.Find("BossPlaceholder_SLOT");
        var head = boss != null ? boss.transform.Find("Visual_REPLACE/Head") : null;
        if (!intro || !head) throw new InvalidOperationException("Intro or existing boss head missing.");
        Directory.CreateDirectory("Tools/OfficeOpening");
        if (!File.Exists("Tools/OfficeOpening/before-framing.unity"))
            File.Copy(scene.path, "Tools/OfficeOpening/before-framing.unity");
        Undo.RecordObject(intro.spawn, "Frame boss beside monitor");
        Vector3 position = new Vector3(-5.9f, intro.spawn.position.y, 1.6f);
        Vector3 direction = head.position - position; direction.y = 0f;
        intro.spawn.SetPositionAndRotation(position, Quaternion.LookRotation(direction));
        var serialized = new SerializedObject(intro);
        serialized.FindProperty("openingLookTarget").objectReferenceValue = head;
        serialized.FindProperty("openingFadeDuration").floatValue = 2.1f;
        serialized.ApplyModifiedProperties();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[Office opening] Opening camera now looks at the existing boss from beside the monitor.");
    }

    [MenuItem("KuTy/Tests/Check office opening transition")]
    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play Mode first.");
        SessionState.SetString(Previous, AssetDatabase.GetAssetPath(EditorSceneManager.playModeStartScene));
        EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/_Project/Generated/MusicSmoke.unity");
        SessionState.SetBool(Pending, true);
        EditorApplication.EnterPlaymode();
    }
}

public sealed class KuTyOpeningCheckRunner : MonoBehaviour
{
    private readonly System.Text.StringBuilder report = new System.Text.StringBuilder();
    private bool previousBackground, finished;
    private float started;
    public void Begin()
    {
        DontDestroyOnLoad(gameObject);
        previousBackground = Application.runInBackground;
        Application.runInBackground = true;
        started = Time.realtimeSinceStartup;
        StartCoroutine(Run());
    }
    private void Check(bool condition, string message)
    {
        report.AppendLine((condition ? "PASS " : "FAIL ") + message);
        if (!condition) { Finish(false); throw new InvalidOperationException(message); }
    }
    private void Update() { if (!finished && Time.realtimeSinceStartup-started > 65f) Finish(false); }
    private IEnumerator Run()
    {
        yield return null;
        SceneLoader.Instance.LoadScene(SceneNames.Act1, .7f);
        while (SceneManager.GetActiveScene().name != SceneNames.Act1) yield return null;
        yield return null;
        var intro = FindObjectOfType<OfficeIntroController>();
        Check(intro != null && !intro.IsPaused && !intro.hud.pausePanel.activeSelf, "Scene transition does not activate intro pause");
        Check(intro.hud.OpeningFadeAlpha > .9f, "Office starts behind a black fade");
        while (SceneLoader.Instance.IsLoading) yield return null;
        yield return new WaitForSeconds(.65f);
        Check(!intro.IsPaused && intro.hud.OpeningFadeAlpha > 0f && intro.hud.OpeningFadeAlpha < 1f, "Black overlay fades progressively");
        Check(intro.LineElapsed == 0f && !intro.hud.subtitlePanel.activeSelf, "Dialogue waits until the opening image is visible");
        while (intro.IsFadingIn) yield return null;
        yield return new WaitForSeconds(.3f);
        Check(intro.hud.OpeningFadeAlpha == 0f && intro.hud.subtitlePanel.activeSelf && intro.LineElapsed > 0f, "Dialogue begins after fade without a pause panel");
        var head = GameObject.Find("BossPlaceholder_SLOT").transform.Find("Visual_REPLACE/Head");
        Check(head != null, "Existing boss visual is present");
        Vector3 screen = intro.view.WorldToViewportPoint(head.position);
        Check(screen.z > 0f && screen.x > .2f && screen.x < .8f && screen.y > .2f && screen.y < .8f, "Boss head is framed in front of the player");
        bool blocked = Physics.Linecast(intro.view.transform.position, head.position, out RaycastHit hit, ~0, QueryTriggerInteraction.Ignore);
        Check(!blocked || hit.transform == head || hit.transform.IsChildOf(head.parent), "Boss head is not hidden behind a solid desk or monitor");
        Directory.CreateDirectory("Tools/OfficeOpening");
        ScreenCapture.CaptureScreenshot("Tools/OfficeOpening/after-fade.png");
        yield return new WaitForEndOfFrame();
        intro.SetPaused(true);
        Check(intro.IsPaused && intro.hud.pausePanel.activeSelf, "Explicit pause still opens the pause panel");
        intro.Resume();
        Check(!intro.IsPaused && !intro.hud.pausePanel.activeSelf, "Resume closes the pause panel");
        if (intro.Sequence != null && intro.Sequence.CollectValidationIssues(true).Count == 0)
        {
            int spoken = 0, lastLine = -1;
            while (intro.State == OfficeIntroController.IntroState.OpeningDialogue)
            {
                int index = intro.DialogueIndex;
                var line = intro.Sequence.GetLine(index);
                if (index != lastLine)
                {
                    lastLine = index;
                    if (!line.silentBeat)
                    {
                        Check(intro.VoicePlayer.Source.clip == line.voiceClip && intro.VoicePlayer.IsPlaying,
                            "Voice playback matches " + line.id);
                        Check(Mathf.Abs(line.ResolveDuration() - line.voiceClip.length - line.postDelay) < .01f,
                            "Subtitle duration follows clip length for " + line.id);
                        if (line.id == "act1_intro_ty_001")
                            Check(line.text == "Dạ dạ sếp, em kiểm tra lại ngay ạ" && line.spokenText == line.text,
                                "Ty subtitle and spoken script match the supplied recording");
                        spoken++;
                    }
                }
                yield return null;
            }
            Check(spoken == 4 && intro.DialogueIndex == intro.Sequence.Count,
                "All four voice clips and silent beats finish before returning control");
        }
        Finish(true);
    }
    private void Finish(bool success)
    {
        if (finished) return;
        finished = true;
        Directory.CreateDirectory("Tools/OfficeOpening");
        File.WriteAllText("Tools/OfficeOpening/checks.txt", (success ? "PASS\n" : "FAIL\n") + report);
        Application.runInBackground = previousBackground;
        EditorApplication.delayCall += () => EditorApplication.ExitPlaymode();
    }
}
