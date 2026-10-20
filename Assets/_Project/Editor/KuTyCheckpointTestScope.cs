using System;
using UnityEditor;
using UnityEngine;

/// <summary>Restore the player's chapter checkpoint even when a Play Mode smoke test is stopped early.</summary>
[InitializeOnLoad]
public static class KuTyCheckpointTestScope
{
    private const string BackupKey = "KuTy.Tests.CheckpointBackup";
    private const string Prefix = "KuTy.Scene2Checkpoint.";
    private static readonly string[] Keys = { "Version", "Stage", "Chores", "Step", "Money", "Candy", "Marbles", "Key", "Memories" };
    [Serializable] private sealed class Backup { public bool[] present; public int[] values; }

    static KuTyCheckpointTestScope()
    {
        EditorApplication.playModeStateChanged += state =>
        {
            if (state == PlayModeStateChange.EnteredEditMode) Restore();
        };
    }

    public static void Capture()
    {
        if (!string.IsNullOrEmpty(SessionState.GetString(BackupKey, "")))
            throw new InvalidOperationException("A checkpoint smoke test is already active.");
        var backup = new Backup { present = new bool[Keys.Length], values = new int[Keys.Length] };
        for (int i = 0; i < Keys.Length; i++)
        {
            backup.present[i] = PlayerPrefs.HasKey(Prefix + Keys[i]);
            backup.values[i] = PlayerPrefs.GetInt(Prefix + Keys[i]);
        }
        SessionState.SetString(BackupKey, JsonUtility.ToJson(backup));
        Scene2Checkpoint.Clear();
    }

    [MenuItem("KuTy/Tests/Restore checkpoint after interrupted test")]
    public static void Restore()
    {
        string saved = SessionState.GetString(BackupKey, "");
        if (string.IsNullOrEmpty(saved)) return;
        var backup = JsonUtility.FromJson<Backup>(saved);
        for (int i = 0; i < Keys.Length; i++)
        {
            if (backup.present[i]) PlayerPrefs.SetInt(Prefix + Keys[i], backup.values[i]);
            else PlayerPrefs.DeleteKey(Prefix + Keys[i]);
        }
        PlayerPrefs.Save();
        SessionState.EraseString(BackupKey);
        Debug.Log("[KuTy tests] Restored the player's chapter checkpoint.");
    }
}
