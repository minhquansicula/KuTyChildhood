using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public class TableMoverEditor
{
    [MenuItem("Tools/Chuyển Bàn sang Office Scene")]
    public static void MoveTable()
    {
        // 1. Tìm GameObject tên là "Table" trong scene hiện tại
        GameObject table = GameObject.Find("Table");
        if (table == null)
        {
            Debug.LogError("Không tìm thấy GameObject nào tên là 'Table' trong Scene hiện tại. Đảm bảo bạn đã đặt tên nó là 'Table' nhé!");
            return;
        }
        
        string officeScenePath = "Assets/_Project/Scenes/Act1_RealWorld.unity";
        
        // 2. Mở scene office ở chế độ Additive (không tắt scene hiện tại)
        Scene officeScene = EditorSceneManager.OpenScene(officeScenePath, OpenSceneMode.Additive);
        
        // 3. Di chuyển chiếc bàn sang scene office
        SceneManager.MoveGameObjectToScene(table, officeScene);
        
        // 4. Lưu lại scene office
        EditorSceneManager.SaveScene(officeScene);
        
        // 5. Đóng scene office (hoặc bạn có thể comment dòng này nếu muốn giữ cả 2 scene mở)
        EditorSceneManager.CloseScene(officeScene, true);
        
        Debug.Log("🎉 Đã chuyển 'Table' sang scene 1 office (Act1_RealWorld) thành công!");
    }
}
