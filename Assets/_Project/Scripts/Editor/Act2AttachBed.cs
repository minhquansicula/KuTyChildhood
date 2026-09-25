using UnityEditor;
using UnityEngine;

public static class Act2AttachBed
{
    [MenuItem("KuTy/Setup/Attach Bed Interactable")]
    public static void Attach()
    {
        GameObject bed = GameObject.Find("BedBlock");
        if (bed != null)
        {
            // Ensure bed is solid
            if (bed.GetComponent<BoxCollider>() == null) bed.AddComponent<BoxCollider>();
            bed.layer = LayerMask.NameToLayer("Ground"); // Ensure player can walk on it

            // Add interaction trigger as child
            Transform trigger = bed.transform.Find("InteractTrigger");
            if (trigger == null)
            {
                GameObject tObj = new GameObject("InteractTrigger");
                tObj.transform.SetParent(bed.transform, false);
                tObj.transform.localPosition = Vector3.zero;
                // Slightly larger than bed to catch raycasts easily
                tObj.transform.localScale = new Vector3(1.1f, 1.1f, 1.1f);
                
                var col = tObj.AddComponent<BoxCollider>();
                col.isTrigger = true;
                
                tObj.AddComponent<BedInteractable>();
                tObj.layer = LayerMask.NameToLayer("Interactable");
            }
            
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
            Debug.Log("Attached BedInteractable to BedBlock via child trigger");
        }
    }
}
