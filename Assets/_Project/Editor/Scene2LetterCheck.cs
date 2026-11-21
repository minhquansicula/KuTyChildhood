#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class Scene2LetterCheck : MonoBehaviour
{
    const string Pending = "KuTy.TestLetter";
    [MenuItem("KuTy/Tests/Check final letter")]
    static void Begin()
    {
        if (EditorApplication.isPlaying || SceneManager.GetActiveScene().name != SceneNames.Act2_Home)
            throw new InvalidOperationException("Open Home in Edit Mode first.");
        KuTyCheckpointTestScope.Capture();
        Scene2Checkpoint.SaveHome(4);
        SessionState.SetBool(Pending, true);
        EditorApplication.EnterPlaymode();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Spawn()
    {
        if (!SessionState.GetBool(Pending, false)) return;
        SessionState.EraseBool(Pending);
        var runner = new GameObject("Letter test");
        DontDestroyOnLoad(runner);
        runner.AddComponent<Scene2LetterCheck>();
    }

    static void Expect(bool ok, string message)
    {
        if (!ok) throw new InvalidOperationException(message);
        Debug.Log("[LetterCheck] " + message);
    }

    IEnumerator Start()
    {
        var steps = Check();
        try { while (steps.MoveNext()) yield return steps.Current; }
        finally { EditorApplication.ExitPlaymode(); }
    }

    IEnumerator Check()
    {
        yield return null;
        var flow = HouseholdChoreFlow.Instance;
        var bed = FindObjectOfType<BedWakeUpController>();
        typeof(BedWakeUpController).GetMethod("GetOutOfBed", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(bed, null);
        yield return new WaitForSeconds(1.5f);
        flow.ShowChecklist();
        Expect(flow.IsCurrent(HouseholdChore.ReadLetter), "Letter unlocked at four chores");
        var letter = FindObjectOfType<Scene2TabletopLetter>();
        Expect(letter.Table && letter.Table.name == "bandai_khach", "Letter linked to dining table");
        var bounds = Scene2DishwashingLayout.VisualBounds(letter.transform);
        Expect(letter.Table.Raycast(new Ray(bounds.center + Vector3.up, Vector3.down), out var hit, 2f)
            && Mathf.Abs(hit.point.y - bounds.min.y) < .025f, "Letter rests on tabletop");
        var player = FindObjectOfType<FirstPersonController>();
        player.enabled = false;
        var camera = player.PlayerCamera;
        camera.position = bounds.center + Vector3.up * .85f + Vector3.back * .65f;
        camera.LookAt(bounds.center);
        Physics.SyncTransforms();
        var interaction = player.GetComponent<PlayerInteraction>();
        yield return null;
        Expect(interaction.CurrentTarget == (IInteractable)letter.GetComponent<HouseholdChoreInteractable>() && interaction.TryInteract(), "Aim and E target the letter");
        yield return new WaitForSeconds(.4f);
        Expect(flow.UI.SimpleActionVisible, "Reading UI opens");
        var done = FindObjectsOfType<Button>().First(b => b.GetComponentInChildren<TMPro.TMP_Text>()?.text == "ĐỌC XONG");
        yield return new WaitForSeconds(4.2f);
        Expect(done.interactable, "Read confirmation enabled");
        done.onClick.Invoke();
        Expect(flow.IsCompleted(HouseholdChore.ReadLetter) && flow.Pieces == 50, "Final reward recorded");
        string destination = QuestManager.Instance.HouseholdDestination;
        for (int i = 0; i < 2 && DialogueUI.Instance.IsShowing; i++) DialogueUI.Instance.HideDialogue();
        float until = Time.realtimeSinceStartup + 40f;
        while (SceneManager.GetActiveScene().name != destination && Time.realtimeSinceStartup < until) yield return null;
        Expect(SceneManager.GetActiveScene().name == destination, "Reached " + destination);
        Debug.Log("[LetterCheck] PASS");
    }
}
#endif
