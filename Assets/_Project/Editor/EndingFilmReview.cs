#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Plays Act_Ending in Play Mode and writes frames at key moments of the film (full film or the skip path)
/// to Tools/Ending, failing on any runtime error. Used for visual review and as a regression check.
/// </summary>
[InitializeOnLoad]
public static class EndingFilmReview
{
    private const string Pending = "KuTy.Ending.Review.Mode";
    private const string ScenePath = "Assets/_Project/Scenes/Act_Ending.unity";

    static EndingFilmReview()
    {
        EditorApplication.playModeStateChanged += state =>
        {
            if (state != PlayModeStateChange.EnteredPlayMode) return;
            string mode = SessionState.GetString(Pending, "");
            if (string.IsNullOrEmpty(mode)) return;
            SessionState.SetString(Pending, "");
            new GameObject("Ending film review").AddComponent<EndingFilmReviewRunner>().Mode = mode;
        };
    }

    [MenuItem("KuTy/Ending/Review full film")]
    public static void ReviewFull() => Start("film");

    [MenuItem("KuTy/Ending/Review skip path")]
    public static void ReviewSkip() => Start("skip");

    private static void Start(string mode)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play Mode first.");
        EditorSceneManager_Open();
        SessionState.SetString(Pending, mode);
        EditorApplication.EnterPlaymode();
    }

    private static void EditorSceneManager_Open()
    {
        UnityEditor.SceneManagement.EditorSceneManager.OpenScene(ScenePath);
    }
}

public sealed class EndingFilmReviewRunner : MonoBehaviour
{
    public string Mode = "film";
    private const string Output = "Tools/Ending";
    private readonly StringBuilder report = new StringBuilder();
    private bool finished, oldBackground;
    private float started;

    private void Start()
    {
        DontDestroyOnLoad(gameObject);
        Directory.CreateDirectory(Output);
        foreach (var old in Directory.GetFiles(Output, "film_*.png")) File.Delete(old);
        File.WriteAllText(Output + "/film-results.txt", "RUNNING");
        oldBackground = Application.runInBackground;
        Application.runInBackground = true;
        started = Time.realtimeSinceStartup;
        Application.logMessageReceived += OnLog;
        StartCoroutine(Run());
    }

    private void OnDestroy()
    {
        Application.logMessageReceived -= OnLog;
        Application.runInBackground = oldBackground;
    }

    private void Update()
    {
        if (!finished && Time.realtimeSinceStartup - started > 90f) Finish(false, "Timed out.");
    }

    private void OnLog(string message, string stack, LogType type)
    {
        if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
            Finish(false, message + "\n" + stack);
    }

    private IEnumerator Run()
    {
        var times = Mode == "skip"
            ? new float[] { 1.8f, 4.5f, 5.6f, 8.0f, 11.5f }
            : new float[] { 1.2f, 3.6f, 6.2f, 8.6f, 11.4f, 14.5f, 17.6f, 20.2f, 22.8f, 25.2f, 28.0f, 32.0f, 36.0f };
        float t0 = Time.realtimeSinceStartup;
        bool skipped = false;
        foreach (float t in times)
        {
            while (Time.realtimeSinceStartup - t0 < t) yield return null;
            if (Mode == "skip" && !skipped && t >= 4.5f)
            {
                skipped = true;
                var director = FindObjectOfType<EndingDirector>();
                if (director != null) director.SkipToEnd();
                yield return null;
            }
            yield return null;
            Capture("film_" + t.ToString("00.0").Replace('.', '_'));
        }
        Finish(true, "Frames written to " + Output);
    }

    private void Capture(string name)
    {
        const int width = 1600, height = 900;
        var camera = Camera.main;
        if (camera == null) { report.AppendLine(name + " no camera"); return; }
        var canvases = FindObjectsOfType<Canvas>().Where(c => c.isRootCanvas && c.renderMode != RenderMode.WorldSpace).ToArray();
        var states = new List<(Canvas canvas, Camera camera, float distance, RenderMode mode)>();
        var target = new RenderTexture(width, height, 24, RenderTextureFormat.DefaultHDR);
        var oldTarget = camera.targetTexture;
        var oldActive = RenderTexture.active;
        var image = new Texture2D(width, height, TextureFormat.RGB24, false);
        try
        {
            camera.targetTexture = target;
            foreach (var canvas in canvases)
            {
                states.Add((canvas, canvas.worldCamera, canvas.planeDistance, canvas.renderMode));
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = camera;
                canvas.planeDistance = Mathf.Max(.5f, camera.nearClipPlane + .2f);
            }
            Canvas.ForceUpdateCanvases();
            camera.Render();
            RenderTexture.active = target;
            image.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            image.Apply();
            File.WriteAllBytes(Output + "/" + name + ".png", image.EncodeToPNG());
            report.AppendLine(name + " ok");
        }
        finally
        {
            camera.targetTexture = oldTarget;
            foreach (var s in states)
            {
                s.canvas.renderMode = s.mode;
                s.canvas.worldCamera = s.camera;
                s.canvas.planeDistance = s.distance;
            }
            Canvas.ForceUpdateCanvases();
            RenderTexture.active = oldActive;
            target.Release();
            Destroy(target);
            Destroy(image);
        }
    }

    private void Finish(bool passed, string message)
    {
        if (finished) return;
        finished = true;
        Application.logMessageReceived -= OnLog;
        File.WriteAllText(Output + "/film-results.txt", (passed ? "PASS" : "FAIL") + "\n" + message + "\n" + report);
        Debug.Log("[Ending review] " + (passed ? "PASS " : "FAIL ") + message);
        EditorApplication.delayCall += () => EditorApplication.ExitPlaymode();
    }
}
#endif
