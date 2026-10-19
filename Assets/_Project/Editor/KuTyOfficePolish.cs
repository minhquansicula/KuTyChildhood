using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public static class KuTyOfficePolish
{
    [MenuItem("KuTy/Office/Apply shift overview")]
    public static void Apply()
    {
        var scene = EditorSceneManager.GetActiveScene();
        if (EditorApplication.isPlaying || scene.name != "Act1_Office_Blockout") return;
        var office = Object.FindObjectOfType<OfficeSceneController>();
        if (office == null) { Debug.LogError("Office gameplay must be installed first."); return; }
        if (office.GetComponent<OfficeShiftHUD>() == null) Undo.AddComponent<OfficeShiftHUD>(office.gameObject);
        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_Project/Generated/Vietnamese.asset");
        string glyphs = "ÁÀẢÃẠĂẮẰẲẴẶÂẤẦẨẪẬĐÉÈẺẼẸÊẾỀỂỄỆÍÌỈĨỊÓÒỎÕỌÔỐỒỔỖỘƠỚỜỞỠỢÚÙỦŨỤƯỨỪỬỮỰÝỲỶỸỴ" +
            "áàảãạăắằẳẵặâấầẩẫậđéèẻẽẹêếềểễệíìỉĩịóòỏõọôốồổỗộơớờởỡợúùủũụưứừửữựýỳỷỹỵ−›•";
        font.isMultiAtlasTexturesEnabled = true;
        font.TryAddCharacters(glyphs, out string missing);
        if (!string.IsNullOrEmpty(missing)) Debug.LogWarning("Office font unavailable glyphs: " + missing);
        EditorUtility.SetDirty(font);
        foreach (var root in scene.GetRootGameObjects())
        foreach (var label in root.GetComponentsInChildren<TMP_Text>(true))
        {
            Undo.RecordObject(label, "Office Vietnamese typography");
            label.font = font;
            EditorUtility.SetDirty(label);
        }
        var ui = GameObject.Find("OfficeGameplayUI");
        if (ui != null)
        {
            var objective = ui.transform.Find("OfficeObjectivePanel") as RectTransform;
            if (objective != null)
            {
                objective.anchorMin = new Vector2(.02f, .86f);
                objective.anchorMax = new Vector2(.70f, .97f);
                objective.offsetMin = objective.offsetMax = Vector2.zero;
            }
            var hud = ui.GetComponent<HUDController>();
            var serialized = new SerializedObject(hud);
            var prompt = serialized.FindProperty("promptPanel").objectReferenceValue as GameObject;
            if (prompt != null)
            {
                var background = prompt.GetComponent<Image>();
                if (background == null) background = Undo.AddComponent<Image>(prompt);
                background.color = new Color(.025f, .045f, .06f, .94f);
                background.enabled = true;
                var promptLabel = serialized.FindProperty("promptText").objectReferenceValue as TMP_Text;
                if (promptLabel != null)
                {
                    var shadow = promptLabel.GetComponent<Shadow>();
                    if (shadow == null) shadow = Undo.AddComponent<Shadow>(promptLabel.gameObject);
                    shadow.effectColor = Color.black;
                    shadow.effectDistance = new Vector2(2, -2);
                }
            }
        }
        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("Office shift overview installed; environment preserved.");
    }
}
