using UnityEditor;
using UnityEngine;
using UnityEditor.SceneManagement;

public static class Act2KitchenAssignTasks
{
    [MenuItem("KuTy/Setup/4. Assign Kitchen Tasks")]
    public static void AssignTasks()
    {
        // Sử dụng phím F (bàn phím) cho các tác vụ giữ (Hold) 
        // Sử dụng phím Space (bàn phím) cho các tác vụ nháy (Mash)
        AssignMashTask("FirewoodStove", "Nhóm lửa nấu cơm", "Đã nhóm lửa xong", KeyCode.Space, 15);
        AssignHoldTask("RicePot", "Đang chắt nước xôi", "Cơm đang chín", KeyCode.F, 4f);
        AssignMashTask("WaterJar", "Đang múc nước", "Đã múc đầy", KeyCode.Space, 5);
        AssignHoldTask("DishCabinet", "Đang xếp chén dĩa", "Đã xếp gọn", KeyCode.F, 2f);
        AssignHoldTask("DiningTable", "Đang dọn mâm cơm", "Mâm cơm đã sẵn sàng", KeyCode.F, 3f);
        
        // Thêm thau vo gạo
        GameObject kitchenArea = GameObject.Find("KitchenArea");
        if (kitchenArea != null) {
            AssignRiceWashingBowl(kitchenArea.transform);
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("Kitchen Tasks Assigned with Keyboard keys and Rice Washing!");
    }

    private static void AssignRiceWashingBowl(Transform parent)
    {
        GameObject obj = GameObject.Find("RiceWashingBowl");
        if (obj == null)
        {
            obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obj.name = "RiceWashingBowl";
            obj.transform.SetParent(parent);
            obj.transform.localPosition = new Vector3(3, 0.5f, 2);
            obj.transform.localScale = new Vector3(0.8f, 0.3f, 0.8f);
            obj.layer = LayerMask.NameToLayer("Ground");

            GameObject tObj = new GameObject("InteractTrigger");
            tObj.transform.SetParent(obj.transform, false);
            tObj.transform.localPosition = Vector3.zero;
            tObj.transform.localScale = new Vector3(1.1f, 1.1f, 1.1f);
            
            var col = tObj.AddComponent<BoxCollider>();
            col.isTrigger = true;
            tObj.layer = LayerMask.NameToLayer("Interactable");
        }

        Transform trigger = obj.transform.Find("InteractTrigger");
        if (trigger != null)
        {
            var task = trigger.GetComponent<RiceWashingTask>();
            if (task == null) trigger.gameObject.AddComponent<RiceWashingTask>();
        }
    }

    private static void AssignHoldTask(string objName, string taskName, string completedPrompt, KeyCode key, float duration)
    {
        GameObject obj = GameObject.Find(objName);
        if (obj == null) return;
        Transform trigger = obj.transform.Find("InteractTrigger");
        if (trigger == null) return;

        var old = trigger.GetComponent<SimpleInteractable>();
        if (old != null) Object.DestroyImmediate(old);

        var task = trigger.GetComponent<HoldToCompleteTask>();
        if (task == null) task = trigger.gameObject.AddComponent<HoldToCompleteTask>();

        // Gán trực tiếp qua code, không dùng SerializedObject để tránh lỗi lệch Enum của Unity
        task.taskName = taskName;
        task.completedPrompt = completedPrompt;
        task.holdKey = key;
        task.holdDuration = duration;
        
        // set promptText in InteractableBase
        SerializedObject so = new SerializedObject(task);
        so.FindProperty("promptText").stringValue = "[E] " + taskName;
        so.ApplyModifiedProperties();
    }

    private static void AssignMashTask(string objName, string taskName, string completedPrompt, KeyCode key, int presses)
    {
        GameObject obj = GameObject.Find(objName);
        if (obj == null) return;
        Transform trigger = obj.transform.Find("InteractTrigger");
        if (trigger == null) return;

        var old = trigger.GetComponent<SimpleInteractable>();
        if (old != null) Object.DestroyImmediate(old);

        var task = trigger.GetComponent<MashToCompleteTask>();
        if (task == null) task = trigger.gameObject.AddComponent<MashToCompleteTask>();

        // Gán trực tiếp
        task.taskName = taskName;
        task.completedPrompt = completedPrompt;
        task.mashKey = key;
        task.requiredPresses = presses;
        
        // set promptText in InteractableBase
        SerializedObject so = new SerializedObject(task);
        so.FindProperty("promptText").stringValue = "[E] " + taskName;
        so.ApplyModifiedProperties();
    }
}
