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

/// <summary>Render real interaction effects and exercise their pause, cancellation and scene lifetime.</summary>
[InitializeOnLoad]
public static class KuTyEffectsReview
{
    const string Pending = "KuTy.EffectsReview.Pending";
    static KuTyEffectsReview()
    {
        EditorApplication.playModeStateChanged += state =>
        {
            if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(Pending, false)) return;
            SessionState.SetBool(Pending, false);
            new GameObject("Interaction effects review").AddComponent<KuTyEffectsReviewRunner>();
        };
    }
    [MenuItem("KuTy/Tests/Review Scene 1 and 2 interaction effects")]
    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play Mode first.");
        if (SceneManager.GetActiveScene().isDirty) throw new InvalidOperationException("Save the current scene before testing.");
        KuTyCheckpointTestScope.Capture();
        EditorSceneManager.OpenScene("Assets/_Project/Scenes/Act1_Office_Blockout.unity");
        SessionState.SetBool(Pending, true);
        EditorApplication.EnterPlaymode();
    }
}

public sealed class KuTyEffectsReviewRunner : MonoBehaviour
{
    const string Output = "Library/EffectsReview";
    readonly StringBuilder report = new StringBuilder();
    bool finished, previousBackground;
    int checks;
    float started;
    static readonly BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static T Field<T>(object owner, string name) => (T)owner.GetType().GetField(name, Private).GetValue(owner);
    void Start()
    {
        DontDestroyOnLoad(gameObject);
        previousBackground = Application.runInBackground;
        Application.runInBackground = true;
        started = Time.realtimeSinceStartup;
        Directory.CreateDirectory(Output);
        File.WriteAllText(Output + "/results.txt", "RUNNING");
        Application.logMessageReceived += OnLog;
        StartCoroutine(Run());
    }
    void OnDestroy() { Application.logMessageReceived -= OnLog; Application.runInBackground = previousBackground; }
    void Update() { if (!finished && Time.realtimeSinceStartup - started > 100) Finish(false, "Timed out."); }
    void OnLog(string message, string stack, LogType type)
    { if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) Finish(false, message + "\n" + stack); }
    void Check(bool value, string message)
    { if (!value) throw new InvalidOperationException(message); checks++; report.AppendLine(message); }
    IEnumerator Run()
    {
        yield return new WaitForSecondsRealtime(2);
        FindObjectOfType<OfficeIntroController>().SkipToReadyAtDeskForTests();
        yield return null;
        var effects = FindObjectOfType<OfficeSceneEffects>();
        var water = Field<ParticleSystem>(effects, "waterParticles");
        var stream = Field<LineRenderer>(effects, "waterStream");
        Check(water && stream && !stream.enabled && water.particleCount == 0, "Water is quiet while idle.");
        var before = FindObjectsOfType<ParticleSystem>().Length;
        for (int i = 0; i < 20; i++) effects.PlayWater();
        yield return new WaitForSeconds(.2f);
        Check(stream.enabled && water.particleCount > 0, "Water flows from the actual cooler tap.");
        Check(FindObjectsOfType<ParticleSystem>().Length == before, "Repeated feedback reuses the particle system.");
        Check(Vector3.Distance(stream.GetPosition(0), water.transform.position) < .001f &&
            stream.GetPosition(3).y < stream.GetPosition(0).y, "Stream and droplets share a downward-facing source.");
        Capture("office-water-pour", new Vector3(5.45f, 1.40f, 2.80f), water.transform.position + Vector3.up * .20f);
        var pause = FindObjectOfType<PauseUI>();
        pause.Pause();
        yield return null;
        float time = water.time, width = stream.widthMultiplier;
        yield return new WaitForSecondsRealtime(.2f);
        Check(Mathf.Abs(water.time - time) < .005f && Mathf.Abs(stream.widthMultiplier - width) < .0001f,
            "Pausing freezes the pour and its particles.");
        pause.Resume();
        yield return new WaitForSeconds(1);
        Check(!stream.enabled && water.particleCount == 0, "Pour stops and droplets expire after the action.");
        effects.SetPhase(OfficePhase.PrintingReport);
        yield return new WaitForSeconds(2.8f);
        effects.SetPhase(OfficePhase.CollectPrint);
        var sheet = Field<Transform>(effects, "printerSheet");
        Check(sheet.gameObject.activeSelf, "Completed print remains in the output tray.");
        Capture("office-printed-report", sheet.position + new Vector3(.55f, .68f, -.85f), sheet.position);
        effects.PlayPaperPickup();
        yield return new WaitForSeconds(.4f);
        Check(!sheet.gameObject.activeSelf, "Collected paper leaves the tray.");
        effects.PlayReportDelivered();
        yield return new WaitForSeconds(.45f);
        var delivered = Field<Transform>(effects, "deliveredSheet");
        Check(delivered.gameObject.activeSelf, "Delivered report rests on the boss desk.");
        effects.PlayDeskImpact();
        Capture("office-delivered-report", delivered.position + new Vector3(.5f, .85f, -.75f), delivered.position);
        effects.PlayWater();
        effects.enabled = false;
        Check(!stream.enabled && water.particleCount == 0, "Disabling office feedback clears an unfinished pour.");
        effects.enabled = true;
        Check(!ShaderUtil.ShaderHasError(Resources.Load<Shader>("Effects/MemoryParticle")), "Particle shader has no compilation errors.");

        SceneLoader.Instance.LoadScene("Act2_MemoryWorld_Home", .05f);
        float loading = Time.realtimeSinceStartup;
        while (SceneManager.GetActiveScene().name != "Act2_MemoryWorld_Home" || SceneLoader.Instance.IsLoading)
        {
            if (Time.realtimeSinceStartup - loading > 40) throw new TimeoutException("Loading Scene 2.");
            yield return null;
        }
        yield return new WaitForSeconds(2);
        typeof(BedWakeUpController).GetMethod("GetOutOfBed", Private).Invoke(FindObjectOfType<BedWakeUpController>(), null);
        yield return new WaitForSeconds(1.3f);
        DialogueUI.Instance?.HideDialogue();
        int progress = HouseholdChoreFlow.Instance.ActiveIndex;
        foreach (HouseholdChore chore in Enum.GetValues(typeof(HouseholdChore)))
        {
            var item = FindObjectsOfType<HouseholdChoreInteractable>().FirstOrDefault(t => t.isActiveAndEnabled && t.Chore == chore);
            if (!item) continue;
            var anchor = Field<Transform>(item, "effectAnchor");
            var feedback = new HouseholdWorkFeedback(chore, anchor ? anchor : item.transform);
            feedback.SetIntensity(.65f);
            feedback.Pulse();
            for (float t = 0; t < 1.4f; t += Time.deltaTime) { feedback.Tick(); yield return null; }
            var root = GameObject.Find("HouseholdWorkEffect_" + chore);
            Check(root, chore + ": feedback opens at the work surface.");
            if (chore == HouseholdChore.CookRice && anchor)
                foreach (var visible in anchor.GetComponentsInChildren<MeshRenderer>())
                    Check(root.transform.position.y > visible.bounds.max.y, "Cooking steam starts above the pot rather than inside its lid.");
            foreach (var ps in root.GetComponentsInChildren<ParticleSystem>())
            {
                Check(ps.particleCount <= ps.main.maxParticles && ps.main.maxParticles <= 48, chore + ": particle count stays bounded.");
                Check(ps.GetComponent<ParticleSystemRenderer>().sharedMaterial.GetTexture("_BaseMap") != null, chore + ": particles use an alpha mask.");
            }
            if (chore == HouseholdChore.CookRice || chore == HouseholdChore.LightFire || chore == HouseholdChore.WashDishes)
                Capture("home-" + chore, root.transform.position + new Vector3(.7f, .55f, 1.1f), root.transform.position + Vector3.up * .08f);
            feedback.Dispose();
            yield return null;
            Check(!root, chore + ": closing releases all temporary objects and lights.");
        }
        Check(HouseholdChoreFlow.Instance.ActiveIndex == progress, "Reviewing and cancelling effects does not award chore progress.");
        Check(FindObjectsOfType<ParticleSystem>().All(p => !p.name.StartsWith("WaterCooler")), "Office effects do not survive the scene handoff.");
        Finish(true, "Both scenes rendered; pause, repeat, cancellation and scene cleanup passed.");
    }
    void Capture(string name, Vector3 position, Vector3 target)
    {
        var go = new GameObject("Effects review camera");
        var camera = go.AddComponent<Camera>();
        camera.CopyFrom(Camera.main);
        camera.enabled = false;
        camera.fieldOfView = 48;
        camera.transform.SetPositionAndRotation(position, Quaternion.LookRotation(target - position));
        var texture = new RenderTexture(1280, 720, 24);
        var image = new Texture2D(1280, 720, TextureFormat.RGB24, false);
        var active = RenderTexture.active;
        try
        {
            camera.targetTexture = texture;
            camera.Render();
            RenderTexture.active = texture;
            image.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); image.Apply();
            File.WriteAllBytes(Output + "/" + name + ".png", image.EncodeToPNG());
        }
        finally
        { RenderTexture.active = active; camera.targetTexture = null; texture.Release(); Destroy(texture); Destroy(image); Destroy(go); }
    }
    void Finish(bool passed, string message)
    {
        if (finished) return;
        finished = true;
        Application.logMessageReceived -= OnLog;
        File.WriteAllText(Output + "/results.txt", (passed ? "PASS" : "FAIL") + " (" + checks + " checks)\n" + message + "\n" + report);
        Debug.Log("[Effects review] " + (passed ? "PASS" : "FAIL") + " " + message);
        EditorApplication.delayCall += () => EditorApplication.ExitPlaymode();
    }
}
