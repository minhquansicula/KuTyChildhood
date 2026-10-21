#if UNITY_EDITOR
using System.Collections;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Integration check for the office blackout, scene handoff and bed input lifecycle.</summary>
public sealed class SleepTransitionSmokeRuntime : MonoBehaviour
{
    private const string PendingKey = "KuTy.SleepTransitionSmoke.Pending";
    private bool failed;
    private bool previousRunInBackground;

    [MenuItem("Tools/KuTy/Tests/Sleep Transition Smoke")]
    private static void RunFromMenu()
    {
        if (EditorApplication.isPlaying) return;
        EditorApplication.ExecuteMenuItem("Window/General/Game");
        SessionState.SetBool(PendingKey, true);
        EditorApplication.isPlaying = true;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (!SessionState.GetBool(PendingKey, false)) return;
        SessionState.SetBool(PendingKey, false);
        new GameObject("Sleep Transition Smoke").AddComponent<SleepTransitionSmokeRuntime>();
    }

    private void Start()
    {
        DontDestroyOnLoad(gameObject);
        previousRunInBackground = Application.runInBackground;
        Application.runInBackground = true;
        StartCoroutine(Run());
    }

    private void OnDestroy() => Application.runInBackground = previousRunInBackground;

    private IEnumerator Run()
    {
        yield return null;
        var loader = SceneLoader.Instance;
        if (!Check(loader != null, "SceneLoader exists")) yield break;
        // Run from the open office scene. Disable its intro so it cannot fight the test camera.
        var intro = FindObjectOfType<OfficeIntroController>();
        if (intro != null) { intro.StopAllCoroutines(); intro.enabled = false; }
        FindObjectOfType<OfficeIntroHUD>()?.SetOpeningFade(0f);
        var office = FindObjectOfType<OfficeSceneController>();
        if (office != null) office.StopAllCoroutines();
        var collapse = FindObjectOfType<OfficeExhaustionSequence>();
        if (!Check(collapse != null, "Open an office scene before running this test")) yield break;

        float started = Time.realtimeSinceStartup;
        if (!Check(office != null, "Office controller exists for real dialogue path")) yield break;
        typeof(OfficeSceneController).GetMethod("SetSeated", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(office, new object[] { false });
        office.SitDown(true);
        typeof(OfficeSceneController).GetMethod("BeginExhaustion", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(office, null);
        float expectedSpeech = office.CurrentSubtitleSeconds;
        Check(expectedSpeech >= 2.35f, "Voice duration includes the complete rest line");
        Check(loader.IsLoading, "Real exhaustion path starts preload");
        Check(!loader.PrepareSleepTransition(SceneNames.Act2_Home), "Duplicate preload is rejected");
        Check(GameManager.Instance == null || GameManager.Instance.InputBlocked, "Input blocked during preload");
        var voice = office.GameplayVoicePlayer;
        if (!Check(voice != null && voice.IsPlaying, "Actual rest voice is playing")) yield break;
        int voiceToken = voice.PlaybackToken;
        office.ShowGuidanceHint("This hint must not interrupt the final line");
        Check(voice.PlaybackToken == voiceToken, "Guidance cannot replace the final voice line");
        yield return new WaitForSeconds(.35f);
        Capture("01-final-line.png");
        yield return new WaitForSecondsRealtime(.1f);
        GameManager.Instance.SetPaused(true);
        float pausedAt = voice.Source.time;
        yield return new WaitForSecondsRealtime(.4f);
        Check(voice.IsPaused && Mathf.Abs(voice.Source.time - pausedAt) < .05f, "Pause freezes voice playback");
        Check(office != null && loader.IsLoading, "Pause cannot activate destination early");
        GameManager.Instance.SetPaused(false);
        bool officeAliveDuringVoice = true;
        while (voice != null && voice.IsPlaying)
        {
            officeAliveDuringVoice &= office != null;
            yield return null;
        }
        Check(officeAliveDuringVoice, "Office remains alive while voice plays");
        float timeout = Time.realtimeSinceStartup + 30f;
        while (loader.IsLoading && Time.realtimeSinceStartup < timeout) yield return null;
        if (!Check(!loader.IsLoading && Time.realtimeSinceStartup - started >= expectedSpeech + .4f,
            "Load completes only after full dialogue and paused time")) yield break;
        if (!Check(SceneManager.GetActiveScene().name == SceneNames.Act2_Home, "Destination is Act2 Home")) yield break;
        var bed = FindObjectOfType<BedWakeUpController>();
        if (!Check(bed != null, "Destination bed controller exists")) yield break;
        Check(GameManager.Instance == null || GameManager.Instance.InputBlocked, "Bed retains input lock after loader releases it");
        if (GameManager.Instance != null) GameManager.Instance.RefreshCursor();
        Check(!Cursor.visible, "Wake-up retains Dat's hidden-cursor input lock");
        yield return new WaitForSeconds(.65f);
        Capture("02-opening-eyes.png");
        while (!bed.CanPauseWhileWaiting && Time.realtimeSinceStartup < timeout) yield return null;
        if (!Check(bed.CanPauseWhileWaiting, "Eye opening finishes and bed prompt becomes available")) yield break;
        Check(!loader.IsSleepTransitionCovered, "Persistent black overlay is released");
        Check(bed.promptText != null && bed.promptText.gameObject.activeInHierarchy, "Exit prompt is visible");
        yield return new WaitForSeconds(.3f);
        Capture("03-wake-prompt.png");
        yield return new WaitForSecondsRealtime(.1f);
        Debug.Log("[SleepTransitionSmoke] Seconds to wake prompt: " + (Time.realtimeSinceStartup - started).ToString("F2"));
        typeof(BedWakeUpController).GetMethod("GetOutOfBed", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(bed, null);
        yield return new WaitForSecondsRealtime(1.3f);
        Check(GameManager.Instance == null || !GameManager.Instance.InputBlocked, "Getting out of bed releases input");
        Check(GameObject.Find("WakeVision") == null, "Eye overlay is cleaned up");

        // A normal load (without the special handoff) must also reveal the bed correctly.
        loader.LoadScene(SceneNames.Act2_Home, .05f);
        timeout = Time.realtimeSinceStartup + 30f;
        while (loader.IsLoading && Time.realtimeSinceStartup < timeout) yield return null;
        bed = FindObjectOfType<BedWakeUpController>();
        if (!Check(bed != null, "Direct home load creates bed controller")) yield break;
        while (!bed.CanPauseWhileWaiting && Time.realtimeSinceStartup < timeout) yield return null;
        Check(bed.CanPauseWhileWaiting && !loader.IsSleepTransitionCovered, "Direct home load finishes opening eyes");
        Debug.Log(failed ? "[SleepTransitionSmoke] FAILED" : "[SleepTransitionSmoke] PASS");
        EditorApplication.isPlaying = false;
    }

    private bool Check(bool condition, string message)
    {
        if (condition) { Debug.Log("[SleepTransitionSmoke] OK: " + message); return true; }
        failed = true;
        Debug.LogError("[SleepTransitionSmoke] FAIL: " + message);
        EditorApplication.isPlaying = false;
        return false;
    }

    private static void Capture(string name)
    {
        string folder = System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData),
            "Temp", "opencode", "sleep-transition");
        System.IO.Directory.CreateDirectory(folder);
        ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(folder, name));
    }
}
#endif
