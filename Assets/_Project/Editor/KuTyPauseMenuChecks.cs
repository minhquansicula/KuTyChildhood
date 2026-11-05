using System;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class KuTyPauseMenuChecks
{
    [MenuItem("KuTy/Pause menu/Sync main menu artwork and check layout")]
    public static void Run()
    {
        var theme = GameUiTheme.Current;
        if (!theme) throw new InvalidOperationException("GameUiTheme is missing.");
        var woodenButton = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/UI/button.png");
        var logo = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/UI/newtitle.png");
        if (!woodenButton || !logo) throw new InvalidOperationException("Main menu artwork is missing.");
        Undo.RecordObject(theme, "Sync pause menu artwork");
        theme.woodenButton = woodenButton;
        theme.pauseLogo = logo;
        EditorUtility.SetDirty(theme);
        AssetDatabase.SaveAssetIfDirty(theme);

        // An isolated preview scene leaves the user's open scene and Play Mode untouched.
        var preview = EditorSceneManager.NewPreviewScene();
        try
        {
            var root = new GameObject("Pause layout checks", typeof(RectTransform), typeof(Canvas));
            SceneManager.MoveGameObjectToScene(root, preview);
            root.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
            var rootRect = (RectTransform)root.transform;
            rootRect.sizeDelta = new Vector2(1920, 1080);
            var panel = new GameObject("PausePanel", typeof(RectTransform));
            panel.transform.SetParent(root.transform, false);
            var resume = new GameObject("Resume", typeof(RectTransform), typeof(Image), typeof(Button)).GetComponent<Button>();
            resume.transform.SetParent(panel.transform, false);
            bool clicked = false;
            resume.onClick.AddListener(() => clicked = true);
            var menu = OfficeUiStyle.BuildPause(panel, resume, null, theme.font);
            var card = (RectTransform)panel.transform.Find("OfficePauseCard");
            Check(menu && card, "Pause card and return button exist");
            Check(card.Find("PauseLogo").GetComponent<Image>().sprite == logo, "Uses the main menu logo");
            Check(resume.GetComponent<Image>().sprite == woodenButton, "Resume uses wooden artwork");
            Check(menu.GetComponent<Image>().sprite == woodenButton, "Return uses wooden artwork");
            Check(resume.GetComponentInChildren<TMP_Text>().text == "Tiếp tục", "Resume caption");
            Check(menu.GetComponentInChildren<TMP_Text>().text == "Về menu chính", "Return caption");
            resume.onClick.Invoke();
            Check(clicked, "Existing button listeners survive styling");
            foreach (var size in new[] { new Vector2(1920, 1080), new Vector2(1280, 720),
                         new Vector2(733, 559), new Vector2(800, 600), new Vector2(2560, 1080), new Vector2(559, 733) })
            {
                rootRect.sizeDelta = size;
                OfficeUiStyle.FitPauseCard(panel);
                var fitted = Vector2.Scale(card.sizeDelta, (Vector2)card.localScale);
                Check(fitted.x <= size.x - 63 && fitted.y <= size.y - 63, "Fits " + size);
            }
            Debug.Log("[PauseMenu] PASS: main menu logo, wooden buttons, Vietnamese captions, preserved listener and six screen sizes.");
        }
        finally { EditorSceneManager.ClosePreviewScene(preview); }
    }

    private static void Check(bool value, string message)
    {
        if (!value) throw new InvalidOperationException("[PauseMenu] " + message);
    }
}
