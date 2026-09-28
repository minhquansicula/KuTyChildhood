using UnityEditor;
using UnityEngine;
using UnityEditor.SceneManagement;

public static class Act2BedroomFix
{
    [MenuItem("KuTy/Setup/2. Fix Bedroom Layout")]
    public static void FixLayout()
    {
        // 1. Xóa cái MainFloor đi vì nó bị đè lên sàn cũ
        GameObject mainFloor = GameObject.Find("MainFloor");
        if (mainFloor != null)
        {
            GameObject.DestroyImmediate(mainFloor);
        }

        // 2. Bật lại cái Ground (sàn nhà) cũ của người dùng
        foreach (var obj in Resources.FindObjectsOfTypeAll<GameObject>())
        {
            if (obj.name == "Ground" && obj.scene.name == "Act2_MemoryWorld_Home")
            {
                obj.SetActive(true);
            }
        }

        // 3. Gom tường, tủ, và giường vào 1 thư mục chung (BedroomArea) để dễ di chuyển
        GameObject bedroomArea = GameObject.Find("BedroomArea");
        if (bedroomArea == null)
        {
            bedroomArea = new GameObject("BedroomArea");
            
            // Tìm và đưa các object vào trong BedroomArea
            string[] wallNames = { "BedroomWall_North", "BedroomWall_South", "BedroomWall_West", "BedroomWall_East_1", "BedroomWall_East_2" };
            foreach(var n in wallNames) {
                GameObject w = GameObject.Find(n);
                if (w != null) w.transform.SetParent(bedroomArea.transform);
            }

            GameObject bed = GameObject.Find("BedBlock");
            if (bed != null) bed.transform.SetParent(bedroomArea.transform);

            GameObject wardrobe = GameObject.Find("WardrobeBlock");
            if (wardrobe != null) wardrobe.transform.SetParent(bedroomArea.transform);

            // Tạm thời dịch chuyển nguyên cụm phòng ngủ sang tọa độ (5, 0, 5) 
            // để người dùng dễ kéo thả vào góc nhà của họ.
            bedroomArea.transform.position = new Vector3(5, 0, 5);
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("Fixed Layout - Bedroom grouped into BedroomArea");
    }
}
