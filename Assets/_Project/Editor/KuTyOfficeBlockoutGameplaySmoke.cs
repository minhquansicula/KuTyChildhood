using UnityEditor;
using UnityEditor.SceneManagement;

public static class KuTyOfficeBlockoutGameplaySmoke
{
    internal const string PendingKey = "KuTy.OfficeBlockoutGameplaySmoke.Pending";

    [MenuItem("KuTy/Tests/Run Act1 Excel gameplay + edge cases")]
    public static void Run()
    {
        Prepare();
        EditorApplication.EnterPlaymode();
    }

    [MenuItem("KuTy/Tests/Prepare Act1 Office Blockout gameplay smoke")]
    public static void Prepare()
    {
        if (EditorSceneManager.GetActiveScene().path != "Assets/_Project/Scenes/Act1_Office_Blockout.unity")
            EditorSceneManager.OpenScene("Assets/_Project/Scenes/Act1_Office_Blockout.unity");
        SessionState.SetBool(PendingKey, true);
    }
}
