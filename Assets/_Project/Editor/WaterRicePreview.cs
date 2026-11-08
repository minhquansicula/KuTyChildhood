using System;
using System.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class WaterRicePreview
{
    private const string Pending = "KuTy.WaterRicePreview.Pending";
    static WaterRicePreview()
    {
        EditorApplication.playModeStateChanged += state =>
        {
            int value = SessionState.GetInt(Pending, 0);
            if (state != PlayModeStateChange.EnteredPlayMode || value == 0) return;
            SessionState.EraseInt(Pending);
            EditorApplication.delayCall += () =>
            {
                var runner = new GameObject("Water/rice temporary preview").AddComponent<WaterRicePreviewRunner>();
                runner.chore = (HouseholdChore)(value - 1);
            };
        };
    }
    [MenuItem("KuTy/Scene 2/Preview Blender water scooping (temporary checkpoint)")]
    public static void Water() => Run(HouseholdChore.FetchWater);
    [MenuItem("KuTy/Scene 2/Preview Blender rice measuring (temporary checkpoint)")]
    public static void Rice() => Run(HouseholdChore.MeasureRice);
    private static void Run(HouseholdChore chore)
    {
        var scene = SceneManager.GetActiveScene();
        if (EditorApplication.isPlayingOrWillChangePlaymode || scene.path != "Assets/_Project/Scenes/Act2_MemoryWorld_Home.unity" || scene.isDirty)
            throw new InvalidOperationException("Open the saved Home scene in Edit Mode first.");
        var sim = UnityEngine.Object.FindObjectOfType<Scene2HouseholdSimulator>();
        if (!sim || !sim.WaterRiceArt || !sim.WaterRiceArt.IsReady) throw new InvalidOperationException("Import WaterRice_Set first.");
        KuTyCheckpointTestScope.Capture(); SessionState.SetInt(Pending, (int)chore + 1);
        EditorApplication.ExecuteMenuItem("Window/General/Game"); EditorApplication.EnterPlaymode();
    }
}

public sealed class WaterRicePreviewRunner : MonoBehaviour
{
    public HouseholdChore chore;
    private IEnumerator Start()
    {
        yield return null;
        var bed = FindObjectOfType<BedWakeUpController>();
        if (bed) typeof(BedWakeUpController).GetMethod("GetOutOfBed", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Invoke(bed, null);
        float deadline = Time.realtimeSinceStartup + 12;
        while (GameManager.Instance && GameManager.Instance.InputBlocked && Time.realtimeSinceStartup < deadline) yield return null;
        var flow = HouseholdChoreFlow.Instance;
        if (!flow || GameManager.Instance && GameManager.Instance.InputBlocked) { Debug.LogError("Water/rice preview: wake-up did not release input."); EditorApplication.isPlaying = false; yield break; }
        foreach (var item in HouseholdChoreFlow.Order)
        {
            if (item == chore) break;
            flow.Complete(item, false);
        }
        flow.Voice?.Stop(); DialogueUI.Instance?.HideDialogue();
        if (!flow.GetComponent<Scene2HouseholdSimulator>().Begin(chore, () => flow.Complete(chore, false), () => { }))
        { Debug.LogError("Water/rice preview could not begin."); EditorApplication.isPlaying = false; yield break; }
        Debug.Log("[WaterRice preview] " + chore + " active. Stop Play Mode to restore the original checkpoint.");
    }
}
