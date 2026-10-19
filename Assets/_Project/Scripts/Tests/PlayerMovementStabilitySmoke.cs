#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class PlayerMovementStabilitySmokeBootstrap
{
    private const string PendingKey = "KuTy.PlayerMovementStability.Pending";

    static PlayerMovementStabilitySmokeBootstrap()
    {
        EditorApplication.playModeStateChanged += state =>
        {
            if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(PendingKey, false)) return;
            SessionState.SetBool(PendingKey, false);
            new GameObject("Player Movement Stability Smoke").AddComponent<PlayerMovementStabilitySmokeRunner>();
        };
    }

    [MenuItem("KuTy/Tests/Run player movement stability")]
    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        EditorSceneManager.OpenScene("Assets/_Project/Scenes/Act2_MemoryWorld_Home.unity");
        SessionState.SetBool(PendingKey, true);
        EditorApplication.EnterPlaymode();
    }
}

public sealed class PlayerMovementStabilitySmokeRunner : MonoBehaviour
{
    private IEnumerator Start()
    {
        Application.runInBackground = true;
        yield return null;
        var wake = FindObjectOfType<BedWakeUpController>();
        // Tắt cả GameObject để mọi coroutine thức dậy đang chạy cũng dừng lại;
        // chỉ tắt component không dừng coroutine đã được StartCoroutine trước đó.
        if (wake != null) wake.gameObject.SetActive(false);
        var player = FindObjectOfType<FirstPersonController>();
        if (player == null)
        {
            Finish(false, "Missing FirstPersonController");
            yield break;
        }
        player.enabled = false;

        // Tạo một mặt sàn cô lập ở xa scene để phép đo chỉ phản ánh controller,
        // không bị đồ nội thất hoặc các khoảng trống trong mesh ngôi nhà ảnh hưởng.
        var testFloor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        testFloor.name = "Player Movement Stability Floor";
        testFloor.transform.SetPositionAndRotation(new Vector3(0f, 999.95f, 0f), Quaternion.identity);
        testFloor.transform.localScale = new Vector3(40f, .1f, 40f);
        int groundLayer = LayerMask.NameToLayer("Ground");
        if (groundLayer >= 0) testFloor.layer = groundLayer;
        var renderer = testFloor.GetComponent<Renderer>();
        if (renderer != null) renderer.enabled = false;

        player.TeleportTo(new Vector3(0f, 1000.02f, -5f), Quaternion.identity);
        Physics.SyncTransforms();

        float minY = float.PositiveInfinity;
        float maxY = float.NegativeInfinity;
        const float dt = 1f / 60f;
        for (int frame = 0; frame < 240; frame++)
        {
            player.SimulateMovementForTests(Vector2.up, false, dt);
            if (frame >= 30)
            {
                minY = Mathf.Min(minY, player.transform.position.y);
                maxY = Mathf.Max(maxY, player.transform.position.y);
            }
            yield return null;
        }

        float verticalJitter = maxY - minY;
        Finish(verticalJitter < .002f,
            $"Vertical jitter={verticalJitter:F6}, start-to-end movement stable over 240 frames");
    }

    private static void Finish(bool success, string message)
    {
        Directory.CreateDirectory("Assets/Screenshots");
        File.WriteAllText("Assets/Screenshots/PlayerMovementStability.txt",
            (success ? "PASS\n" : "FAIL\n") + message);
        if (success) Debug.Log("[KuTy] " + message);
        else Debug.LogError("[KuTy] " + message);
        EditorApplication.isPlaying = false;
        if (!success) throw new InvalidOperationException(message);
    }
}
#endif
