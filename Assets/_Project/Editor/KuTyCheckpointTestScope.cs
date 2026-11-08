using System;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>Restore the player's chapter checkpoint even when a Play Mode smoke test is stopped early.</summary>
[InitializeOnLoad]
public static class KuTyCheckpointTestScope
{
    private const string BackupKey = "KuTy.Tests.CheckpointBackup";
    private const string Prefix = "KuTy.Scene2Checkpoint.";
    private const string BackupPath = "Library/KuTyCheckpointTestBackup.json";
    private static readonly string[] Keys = { "Version", "Stage", "Chores", "Step", "Money", "Candy", "Marbles", "Key", "Memories", "Optional", "WorkChore" };
    [Serializable] private sealed class Backup { public bool[] present; public int[] values; public bool hasWork; public string work; }

    static KuTyCheckpointTestScope()
    {
        // SessionState survives domain reloads, but a disk copy is needed after an Editor crash.
        EditorApplication.delayCall += () =>
        {
            if (!EditorApplication.isPlayingOrWillChangePlaymode && File.Exists(BackupPath) &&
                string.IsNullOrEmpty(SessionState.GetString(BackupKey, ""))) Restore();
        };
        EditorApplication.playModeStateChanged += state =>
        {
            if (state == PlayModeStateChange.EnteredEditMode) Restore();
        };
    }

    public static void Capture()
    {
        if (!string.IsNullOrEmpty(SessionState.GetString(BackupKey, "")) || File.Exists(BackupPath))
            throw new InvalidOperationException("A checkpoint smoke test is already active.");
        var backup = new Backup { present = new bool[Keys.Length], values = new int[Keys.Length] };
        backup.hasWork = PlayerPrefs.HasKey(Prefix + "Work");
        backup.work = PlayerPrefs.GetString(Prefix + "Work", "");
        for (int i = 0; i < Keys.Length; i++)
        {
            backup.present[i] = PlayerPrefs.HasKey(Prefix + Keys[i]);
            backup.values[i] = PlayerPrefs.GetInt(Prefix + Keys[i]);
        }
        string saved = JsonUtility.ToJson(backup);
        File.WriteAllText(BackupPath, saved);
        SessionState.SetString(BackupKey, saved);
        Scene2Checkpoint.Clear();
    }

    [MenuItem("KuTy/Tests/Restore checkpoint after interrupted test")]
    public static void Restore()
    {
        string saved = SessionState.GetString(BackupKey, "");
        if (string.IsNullOrEmpty(saved) && File.Exists(BackupPath)) saved = File.ReadAllText(BackupPath);
        if (string.IsNullOrEmpty(saved)) return;
        var backup = JsonUtility.FromJson<Backup>(saved);
        for (int i = 0; i < Keys.Length; i++)
        {
            if (i >= backup.present.Length || i >= backup.values.Length) { PlayerPrefs.DeleteKey(Prefix + Keys[i]); continue; }
            if (backup.present[i]) PlayerPrefs.SetInt(Prefix + Keys[i], backup.values[i]);
            else PlayerPrefs.DeleteKey(Prefix + Keys[i]);
        }
        if (backup.hasWork) PlayerPrefs.SetString(Prefix + "Work", backup.work ?? "");
        else PlayerPrefs.DeleteKey(Prefix + "Work");
        PlayerPrefs.Save();
        SessionState.EraseString(BackupKey);
        if (File.Exists(BackupPath)) File.Delete(BackupPath);
        Debug.Log("[KuTy tests] Restored the player's chapter checkpoint.");
    }
}
