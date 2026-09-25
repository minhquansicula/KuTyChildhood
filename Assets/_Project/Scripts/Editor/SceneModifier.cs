using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public class SceneModifier
{
    [MenuItem("KuTy/Setup/1. Delete Objects in Home Scene")]
    public static void ModifyHomeScene()
    {
        string scenePath = "Assets/_Project/Scenes/Act2_MemoryWorld_Home.unity";
        Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        
        string[] objectsToDelete = new string[] { "Ground", "MarbleTable", "ShopCounter", "JournalTable" };
        
        foreach (string objName in objectsToDelete)
        {
            GameObject obj = GameObject.Find(objName);
            if (obj != null)
            {
                Object.DestroyImmediate(obj);
                Debug.Log("Deleted " + objName);
            }
            else
            {
                Debug.LogWarning("Could not find " + objName);
            }
        }
        
        EditorSceneManager.SaveScene(scene);
        Debug.Log("Scene saved successfully.");
    }

    [MenuItem("KuTy/Setup/2. Fix Build Settings")]
    public static void FixBuildSettings()
    {
        EditorBuildSettingsScene[] original = EditorBuildSettings.scenes;
        var newScenes = new System.Collections.Generic.List<EditorBuildSettingsScene>();
        
        newScenes.Add(new EditorBuildSettingsScene("Assets/_Project/Scenes/MainMenu.unity", true));
        newScenes.Add(new EditorBuildSettingsScene("Assets/_Project/Scenes/Act1_RealWorld.unity", true));
        newScenes.Add(new EditorBuildSettingsScene("Assets/_Project/Scenes/Act2_MemoryWorld_Home.unity", true));
        newScenes.Add(new EditorBuildSettingsScene("Assets/_Project/Scenes/Act3_MemoryWorld_OutSide.unity", true));
        newScenes.Add(new EditorBuildSettingsScene("Assets/_Project/Scenes/Act_Ending.unity", true));

        EditorBuildSettings.scenes = newScenes.ToArray();
        Debug.Log("Build settings updated.");
    }
}
