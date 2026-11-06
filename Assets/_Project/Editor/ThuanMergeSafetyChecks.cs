using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Checkpoint discovery must work in Main Menu, before a scene-specific quest manager exists.</summary>
public static class ThuanMergeSafetyChecks
{
    [MenuItem("KuTy/Tests/Check Thuan merge routing and checkpoint safety")]
    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Run this check in Edit Mode.");
        KuTyCheckpointTestScope.Capture();
        var preview = EditorSceneManager.NewPreviewScene();
        int assertions = 0;
        try
        {
            Action<bool, string> check = (ok, message) =>
            {
                if (!ok) throw new InvalidOperationException("[ThuanMerge] " + message);
                assertions++;
            };
            check(!Scene2Checkpoint.HasCheckpoint, "Empty checkpoint must not enable Continue");
            const string prefix = "KuTy.Scene2Checkpoint.";
            PlayerPrefs.SetInt(prefix + "Version", 2);
            PlayerPrefs.SetInt(prefix + "Stage", 2);
            PlayerPrefs.SetInt(prefix + "Step", (int)QuestStep.PlayMarbles);
            PlayerPrefs.SetInt(prefix + "Money", 1500);
            PlayerPrefs.SetInt(prefix + "Candy", 1);
            PlayerPrefs.SetInt(prefix + "Marbles", 1);
            check(Scene2Checkpoint.HasOutsideProgress && Scene2Checkpoint.HasCheckpoint,
                "Outside checkpoint must be discoverable without loading its scene");
            check(Scene2Checkpoint.ResumeScene == SceneNames.Act3_OutSide &&
                Scene2Checkpoint.OutsideStep == QuestStep.PlayMarbles, "Outside destination and step must survive");
            check(Scene2Checkpoint.OutsideMoney == 1500 && Scene2Checkpoint.HasCandy && Scene2Checkpoint.HasMarbles,
                "Outside inventory and currency must survive");

            var host = new GameObject("Routing validation (preview only)");
            SceneManager.MoveGameObjectToScene(host, preview);
            var quests = host.AddComponent<QuestManager>();
            check(!quests.SkipScene3 && quests.HouseholdDestination == SceneNames.Act3_OutSide,
                "Existing scenes must default to the complete story");
            var settings = new SerializedObject(quests);
            settings.FindProperty("skipScene3").boolValue = true;
            settings.ApplyModifiedPropertiesWithoutUndo();
            check(quests.SkipScene3 && quests.HouseholdDestination == SceneNames.Act_Ending,
                "Skip Scene 3 remains an explicit, working opt-in");
            check(Scene2Checkpoint.HasOutsideProgress && Scene2Checkpoint.ResumeScene == SceneNames.Act3_OutSide,
                "A demo route must never hide an existing outdoor save");
            Scene2Checkpoint.SaveHome(5, 3);
            check(Scene2Checkpoint.HomeChores == 5 && Scene2Checkpoint.HomeOptional == 3 &&
                Scene2Checkpoint.ResumeScene == SceneNames.Act2_Home, "Home and optional progress remain independent of the demo route");
            Debug.Log("[ThuanMerge] PASS " + assertions + " routing/checkpoint checks; player's checkpoint is restored.");
        }
        finally
        {
            EditorSceneManager.ClosePreviewScene(preview);
            KuTyCheckpointTestScope.Restore();
        }
    }
}
