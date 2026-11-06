using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;

public static class KuTyCutsceneSceneSetup
{
    private const string ScenePath = "Assets/_Project/Scenes/Act1_CutScene_Act2.unity";

    [MenuItem("Tools/KuTy/Cutscene/Create Bridge Scene")]
    public static void CreateScene()
    {
        if (EditorApplication.isPlaying)
            throw new System.InvalidOperationException("Stop Play Mode before creating the bridge scene.");
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null)
            throw new System.InvalidOperationException("Bridge scene already exists; open it to edit.");

        var previous = SceneManager.GetActiveScene();
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        try
        {
            SceneManager.SetActiveScene(scene);
            var cameraObject = new GameObject("Cutscene Camera", typeof(Camera), typeof(AudioListener));
            cameraObject.tag = "MainCamera";
            var camera = cameraObject.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
            camera.cullingMask = 0;
            var controller = new GameObject("Act1 to Act2 Cutscene").AddComponent<Act1CutsceneAct2Controller>();
            var serialized = new SerializedObject(controller);
            serialized.FindProperty("clip").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<VideoClip>("Assets/_Project/Resources/Cutscenes/OfficeToHome_Final.mp4");
            serialized.ApplyModifiedPropertiesWithoutUndo();
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
                throw new System.IO.IOException("Could not save cutscene scene.");
            var scenes = EditorBuildSettings.scenes.ToList();
            scenes.RemoveAll(entry => entry.path == ScenePath);
            int office = scenes.FindIndex(entry => entry.path.EndsWith("/" + SceneNames.Act1 + ".unity"));
            scenes.Insert(office < 0 ? scenes.Count : office + 1, new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
            AssetDatabase.SaveAssets();
            Debug.Log("[CutsceneSceneSetup] Created " + ScenePath + " and added it to Build Settings.");
        }
        finally
        {
            if (previous.IsValid()) SceneManager.SetActiveScene(previous);
            EditorSceneManager.CloseScene(scene, true);
        }
    }
}
