#if UNITY_EDITOR
using System.Collections;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public sealed class ChildWakeHandsSmoke : MonoBehaviour
{
    private const string Pending = "KuTy.ChildWakeHandsSmoke.Pending";
    private bool previousBackground;

    [MenuItem("Tools/KuTy/Tests/Child Wake Hands Smoke")]
    private static void RunFromMenu()
    {
        if (EditorApplication.isPlaying) return;
        // Checkpoint scope lives in the Editor assembly, so invoke its public capture method by reflection.
        var scope = System.Type.GetType("KuTyCheckpointTestScope, Assembly-CSharp-Editor");
        scope.GetMethod("Capture").Invoke(null, null);
        EditorSceneManager.OpenScene("Assets/_Project/Scenes/Act2_MemoryWorld_Home.unity");
        EditorApplication.ExecuteMenuItem("Window/General/Game");
        SessionState.SetBool(Pending, true);
        EditorApplication.isPlaying = true;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (!SessionState.GetBool(Pending, false)) return;
        SessionState.SetBool(Pending, false);
        new GameObject("Child Wake Hands Smoke").AddComponent<ChildWakeHandsSmoke>();
    }

    private IEnumerator Start()
    {
        previousBackground = Application.runInBackground;
        Application.runInBackground = true;
        yield return null;
        var bed = FindObjectOfType<BedWakeUpController>();
        var hands = FindObjectOfType<ChildWakeHands>();
        if (!Check(bed != null && hands != null, "Bed creates rigged child hands")) yield break;
        var renderer = hands.GetComponentInChildren<SkinnedMeshRenderer>();
        if (!Check(renderer != null && renderer.bones.Length >= 20, "Imported hands have a skinned skeleton")) yield break;
        if (!Check(renderer.sharedMesh.uv2.Length == renderer.sharedMesh.vertexCount,
            "Palm detail coordinates import with the mesh")) yield break;
        var colors = renderer.sharedMesh.colors;
        if (!Check(colors.Length == renderer.sharedMesh.vertexCount && System.Array.Exists(colors, c => c.r > .4f),
            "Anatomical skin colour masks import with the mesh")) yield break;
        if (!Check(GameManager.Instance.InputBlocked, "Input remains locked during waking")) yield break;
        var camera = hands.GetComponentInParent<Camera>();
        string folder = "Tools/Cutscene/ChildHands";
        Directory.CreateDirectory(folder);
        float elapsed = 0f;
        int shot = 0;
        float nextFrame = 0f;
        var frameTimes = new System.Collections.Generic.List<float>();
        Directory.CreateDirectory(Path.Combine(folder, "Frames"));
        float[] moments = { .85f, 1.2f, 3.85f, 5.2f };
        while (elapsed < 6.4f)
        {
            elapsed += Time.deltaTime;
            if (elapsed >= nextFrame)
            {
                ScreenCapture.CaptureScreenshot(Path.Combine(folder, "Frames", "frame-" + frameTimes.Count.ToString("D4") + ".png"));
                frameTimes.Add(elapsed);
                nextFrame = elapsed + 1f / 12f;
            }
            if (shot < moments.Length && elapsed >= moments[shot])
            {
                ScreenCapture.CaptureScreenshot(Path.Combine(folder, "unity-wake-" + shot + ".png"));
                if (shot == 2)
                {
                    var local = camera.transform.InverseTransformPoint(renderer.bounds.center);
                    Debug.Log("[ChildWakeHandsSmoke] Inspect-pose bounds in camera space: " + local);
                    if (!Check(local.z > 0f, "Hands animate in front of the camera")) yield break;
                }
                shot++;
            }
            yield return null;
        }
        File.WriteAllLines(Path.Combine(folder, "Frames", "times.txt"),
            frameTimes.ConvertAll(t => t.ToString("F6", System.Globalization.CultureInfo.InvariantCulture)));
        if (!Check(FindObjectOfType<ChildWakeHands>() == null, "Hands cleaned up after waking")) yield break;
        if (!Check(bed.CanPauseWhileWaiting, "Exit-bed prompt becomes available")) yield break;
        if (!Check(GameManager.Instance.InputBlocked, "Bed retains input until E")) yield break;
        typeof(BedWakeUpController).GetMethod("GetOutOfBed", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(bed, null);
        yield return new WaitForSeconds(1.5f);
        if (!Check(!GameManager.Instance.InputBlocked, "Getting out of bed releases input")) yield break;
        Debug.Log("[ChildWakeHandsSmoke] PASS");
        EditorApplication.isPlaying = false;
    }

    private static bool Check(bool valid, string message)
    {
        if (valid) { Debug.Log("[ChildWakeHandsSmoke] OK: " + message); return true; }
        Debug.LogError("[ChildWakeHandsSmoke] FAIL: " + message);
        EditorApplication.isPlaying = false;
        return false;
    }

    private void OnDestroy() => Application.runInBackground = previousBackground;
}
#endif
