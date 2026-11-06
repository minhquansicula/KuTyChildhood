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
        if (!Check(office != null, "Open an office scene before running this test")) yield break;
        typeof(OfficeSceneController).GetMethod("SetSeated", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(office, new object[] { false });
        office.SitDown(true);
        office.MakeRestAvailableForTests();
        float started = Time.realtimeSinceStartup;
        office.HandleInteraction(OfficeInteractionKind.Chair);
        Check(loader.IsLoading, "Departure starts preloading the cutscene scene");
        Check(!loader.PrepareSleepTransition(SceneNames.Act2_Home), "Duplicate preload is rejected");
        Check(GameManager.Instance == null || GameManager.Instance.InputBlocked, "Input blocked during preload");
        yield return new WaitForSeconds(.35f);
        GameManager.Instance.SetPaused(true);
        yield return new WaitForSecondsRealtime(2f);
        Check(office != null && loader.IsLoading, "Pause cannot activate destination early");
        GameManager.Instance.SetPaused(false);
        float timeout = Time.realtimeSinceStartup + 40f;
        var cutscene = GameObject.Find("Office Departure Cutscene");
        if (!Check(cutscene != null, "Departure creates a fade overlay")) yield break;
        var fade = cutscene.transform.Find("Fade").GetComponent<UnityEngine.UI.Image>();
        while (fade != null && fade.color.a <= .1f && Time.realtimeSinceStartup < timeout)
            yield return null;
        if (!Check(fade != null && fade.color.a < .95f && cutscene.GetComponent<UnityEngine.Video.VideoPlayer>() == null,
            "Office fades visibly before video preparation")) yield break;
        while (SceneManager.GetActiveScene().name != SceneNames.Act1_CutScene_Act2 && Time.realtimeSinceStartup < timeout)
            yield return null;
        if (!Check(SceneManager.GetActiveScene().name == SceneNames.Act1_CutScene_Act2,
            "Office activates the dedicated cutscene scene")) yield break;
        UnityEngine.Video.VideoPlayer player = null;
        while (player == null && Time.realtimeSinceStartup < timeout)
        {
            player = FindObjectOfType<UnityEngine.Video.VideoPlayer>();
            yield return null;
        }
        if (!Check(player != null, "Cutscene scene finds the packaged video")) yield break;
        while (player != null && player.frame < 0 && Time.realtimeSinceStartup < timeout)
            yield return null;
        if (!Check(player != null && player.frame >= 0 && office == null,
            "Video plays in its own scene after office unloads")) yield break;
        Check(GameManager.Instance.InputBlocked, "Cutscene retains input lock");
        Capture("01-departure-video.png");
        while (SceneManager.GetActiveScene().name != SceneNames.Act2_Home && Time.realtimeSinceStartup < timeout)
            yield return null;
        while (loader.IsLoading && Time.realtimeSinceStartup < timeout) yield return null;
        if (!Check(!loader.IsLoading, "Load completes after the departure video")) yield break;
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

        // Entering the cutscene directly must autoplay and reach the same wake-up flow.
        loader.LoadScene(SceneNames.Act1_CutScene_Act2, 0f);
        timeout = Time.realtimeSinceStartup + 40f;
        while (SceneManager.GetActiveScene().name != SceneNames.Act1_CutScene_Act2 && Time.realtimeSinceStartup < timeout)
            yield return null;
        if (!Check(SceneManager.GetActiveScene().name == SceneNames.Act1_CutScene_Act2,
            "Direct cutscene load succeeds")) yield break;
        while (SceneManager.GetActiveScene().name != SceneNames.Act2_Home && Time.realtimeSinceStartup < timeout)
            yield return null;
        while (loader.IsLoading && Time.realtimeSinceStartup < timeout) yield return null;
        bed = FindObjectOfType<BedWakeUpController>();
        if (!Check(bed != null, "Direct cutscene test reaches home")) yield break;
        while (!bed.CanPauseWhileWaiting && Time.realtimeSinceStartup < timeout) yield return null;
        Check(bed.CanPauseWhileWaiting && !loader.IsSleepTransitionCovered,
            "Direct cutscene test finishes opening eyes");
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
