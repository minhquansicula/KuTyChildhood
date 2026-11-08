using System;
using System.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>One-click, non-persistent human mouse-feel review of the real activity.</summary>
[InitializeOnLoad]
public static class DishwashingPreview
{
    private const string Pending = "KuTy.DishwashingPreview.Pending";
    static DishwashingPreview()
    {
        EditorApplication.playModeStateChanged += state =>
        {
            if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(Pending, false)) return;
            SessionState.SetBool(Pending, false);
            EditorApplication.delayCall += () => new GameObject("Blender dishwashing preview").AddComponent<DishwashingPreviewRunner>();
        };
    }
    [MenuItem("KuTy/Scene 2/Preview Blender dishwashing (temporary checkpoint)")]
    public static void Run()
    {
        var scene = SceneManager.GetActiveScene();
        if (EditorApplication.isPlayingOrWillChangePlaymode || scene.path != "Assets/_Project/Scenes/Act2_MemoryWorld_Home.unity" || scene.isDirty)
            throw new InvalidOperationException("Open the saved Home scene in Edit Mode first.");
        var sim = UnityEngine.Object.FindObjectOfType<Scene2HouseholdSimulator>();
        if (!sim || !sim.DishwashingArt || !sim.DishwashingArt.IsReady) throw new InvalidOperationException("Import the Blender dishwashing set first.");
        KuTyCheckpointTestScope.Capture();
        SessionState.SetBool(Pending, true);
        EditorApplication.ExecuteMenuItem("Window/General/Game");
        EditorApplication.EnterPlaymode();
    }
}

public sealed class DishwashingPreviewRunner : MonoBehaviour
{
    private IEnumerator Start()
    {
        yield return null;
        var bed = FindObjectOfType<BedWakeUpController>();
        if (bed) typeof(BedWakeUpController).GetMethod("GetOutOfBed", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).Invoke(bed, null);
        float deadline = Time.realtimeSinceStartup + 12;
        while (GameManager.Instance && GameManager.Instance.InputBlocked && Time.realtimeSinceStartup < deadline) yield return null;
        var flow = HouseholdChoreFlow.Instance;
        if (!flow || GameManager.Instance && GameManager.Instance.InputBlocked) { Debug.LogError("Dishwashing preview: wake-up did not release input."); EditorApplication.isPlaying = false; yield break; }
        foreach (var chore in HouseholdChoreFlow.Order)
        {
            if (chore == HouseholdChore.WashDishes) break;
            flow.Complete(chore, false);
        }
        flow.Voice?.Stop(); DialogueUI.Instance?.HideDialogue();
        if (!flow.GetComponent<Scene2HouseholdSimulator>().Begin(HouseholdChore.WashDishes, () => flow.Complete(HouseholdChore.WashDishes, false), () => { }))
        { Debug.LogError("Dishwashing preview could not begin."); EditorApplication.isPlaying = false; yield break; }
        Debug.Log("[Dishwashing preview] Test with the mouse. Stop Play Mode to restore your original checkpoint. This preview never saves the scene.");
    }
}
