using UnityEditor;
using UnityEngine;
using UnityEditor.SceneManagement;

public static class Act2KitchenSetup
{
    [MenuItem("KuTy/Setup/3. Build Kitchen Layout")]
    public static void BuildKitchen()
    {
        GameObject houseRoot = GameObject.Find("HouseLayout");
        if (houseRoot == null)
        {
            houseRoot = new GameObject("HouseLayout");
        }

        GameObject kitchenArea = GameObject.Find("KitchenArea");
        if (kitchenArea == null)
        {
            kitchenArea = new GameObject("KitchenArea");
            kitchenArea.transform.SetParent(houseRoot.transform);
            
            // Đặt khu bếp ở một góc khác của nhà (ví dụ góc phải trên: X=8, Z=8)
            kitchenArea.transform.position = new Vector3(8, 0, 8);
        }

        // 1. Bếp củi (FirewoodStove)
        CreateKitchenProp("FirewoodStove", kitchenArea.transform, new Vector3(2, 0.5f, 2), new Vector3(1.5f, 1f, 1.5f), "[E] Nhóm lửa nấu cơm");

        // 2. Nồi cơm gang (RicePot) - đặt trên bếp củi
        CreateKitchenProp("RicePot", kitchenArea.transform, new Vector3(2, 1.25f, 2), new Vector3(0.6f, 0.5f, 0.6f), "[E] Kiểm tra nồi cơm");

        // 3. Lu nước (WaterJar) - đặt cạnh bếp
        CreateKitchenProp("WaterJar", kitchenArea.transform, new Vector3(4, 0.6f, 2), new Vector3(1f, 1.2f, 1f), "[E] Múc nước");

        // 4. Chạn bát / Gác-măng-rê (DishCabinet) - chỗ cất chén dĩa
        CreateKitchenProp("DishCabinet", kitchenArea.transform, new Vector3(1, 1.5f, -1), new Vector3(1.2f, 3f, 0.8f), "[E] Mở chạn bát");

        // 5. Bàn ăn (DiningTable)
        CreateKitchenProp("DiningTable", kitchenArea.transform, new Vector3(-2, 0.5f, 0), new Vector3(3f, 1f, 2f), "[E] Dọn cơm");

        // 6. Tìm Sink cũ (nếu có) và đưa vào bếp
        GameObject sink = GameObject.Find("Sink");
        if (sink != null)
        {
            sink.transform.SetParent(kitchenArea.transform);
            sink.transform.localPosition = new Vector3(-2, 0.5f, 3);
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("Kitchen Layout Built Successfully!");
    }

    private static GameObject CreateKitchenProp(string name, Transform parent, Vector3 localPosition, Vector3 scale, string promptText)
    {
        GameObject existing = GameObject.Find(name);
        if (existing != null) return existing;

        GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        obj.name = name;
        obj.transform.SetParent(parent);
        obj.transform.localPosition = localPosition;
        obj.transform.localScale = scale;
        
        // Đảm bảo là sàn nhà vật lý bình thường
        obj.layer = LayerMask.NameToLayer("Ground");
        
        // Tạo Trigger tương tác
        GameObject tObj = new GameObject("InteractTrigger");
        tObj.transform.SetParent(obj.transform, false);
        tObj.transform.localPosition = Vector3.zero;
        tObj.transform.localScale = new Vector3(1.1f, 1.1f, 1.1f);
        
        var col = tObj.AddComponent<BoxCollider>();
        col.isTrigger = true;
        
        var interactable = tObj.AddComponent<SimpleInteractable>();
        interactable.SetPrompt(promptText);
        
        tObj.layer = LayerMask.NameToLayer("Interactable");

        return obj;
    }
}
