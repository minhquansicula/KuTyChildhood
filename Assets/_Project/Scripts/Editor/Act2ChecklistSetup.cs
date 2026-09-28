using UnityEditor;
using UnityEngine;
using UnityEditor.SceneManagement;

public static class Act2ChecklistSetup
{
    [MenuItem("KuTy/Setup/5. Setup Checklist")]
    public static void SetupChecklist()
    {
        GameObject mgr = GameObject.Find("KitchenFlowManager");
        if (mgr == null)
        {
            mgr = new GameObject("KitchenFlowManager");
            mgr.AddComponent<KitchenFlowManager>();
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("Checklist System Setup Complete!");
    }
}
