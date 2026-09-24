using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class KuTyOfficeReportMiniGameSetup
{
    private const string ScenePath = "Assets/_Project/Scenes/Act1_RealWorld.unity";

    [MenuItem("KuTy/Preview/Show office report mini-game")]
    public static void ShowPreview()
    {
        OfficeReportMiniGame miniGame = Object.FindObjectOfType<OfficeReportMiniGame>();
        OfficeSceneController office = Object.FindObjectOfType<OfficeSceneController>();
        if (miniGame == null || office == null)
        {
            Debug.LogError("[KuTy] Build the office report mini-game UI first.");
            return;
        }
        miniGame.Open(office);
    }

    [MenuItem("KuTy/Preview/Hide office report mini-game")]
    public static void HidePreview()
    {
        Object.FindObjectOfType<OfficeReportMiniGame>()?.ClosePanel();
    }

    [MenuItem("KuTy/Setup/Build office report mini-game UI")]
    public static void Build()
    {
        var scene = EditorSceneManager.GetActiveScene();
        if (scene.path != ScenePath)
        {
            if (scene.isDirty)
            {
                Debug.LogWarning("[KuTy] Save the current scene before building the report mini-game UI.");
                return;
            }
            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        GameObject canvas = GameObject.Find("UI");
        OfficeSceneController office = Object.FindObjectOfType<OfficeSceneController>();
        TextMeshProUGUI fontSource = GameObject.Find("OfficeObjective")?.GetComponent<TextMeshProUGUI>();
        if (canvas == null || office == null)
        {
            Debug.LogError("[KuTy] Act1 UI Canvas or OfficeSceneController is missing.");
            return;
        }

        Transform old = canvas.transform.Find("ReportMiniGameOverlay");
        if (old != null) Object.DestroyImmediate(old.gameObject);

        TMP_FontAsset font = fontSource != null ? fontSource.font : TMP_Settings.defaultFontAsset;
        GameObject overlay = CreateImage("ReportMiniGameOverlay", canvas.transform,
            new Color(0.018f, 0.027f, 0.047f, 0.94f));
        Stretch(overlay.GetComponent<RectTransform>());

        GameObject workspace = CreateImage("ReportWorkspace", overlay.transform,
            new Color(0.045f, 0.066f, 0.105f, 0.99f));
        Center(workspace.GetComponent<RectTransform>(), 1120f, 690f);
        var workspaceOutline = workspace.AddComponent<Outline>();
        workspaceOutline.effectColor = new Color(0.23f, 0.34f, 0.5f, 0.85f);
        workspaceOutline.effectDistance = new Vector2(2f, -2f);

        GameObject accent = CreateImage("UrgentAccent", workspace.transform,
            new Color(0.92f, 0.26f, 0.25f, 1f));
        TopStretch(accent.GetComponent<RectTransform>(), 0f, 0f, 7f);

        TextMeshProUGUI eyebrow = CreateText("UrgentLabel", workspace.transform,
            "YÊU CẦU KHẨN · 23:32", 17f, new Color(1f, 0.45f, 0.4f), font, FontStyles.Bold);
        TopLeft(eyebrow.rectTransform, 36f, 25f, 420f, 24f);

        TextMeshProUGUI header = CreateText("ReportHeader", workspace.transform,
            "BÁO CÁO BỊ TRẢ VỀ", 32f, new Color(0.96f, 0.97f, 1f), font, FontStyles.Bold);
        TopLeft(header.rectTransform, 36f, 50f, 630f, 45f);

        TextMeshProUGUI fileName = CreateText("ReportFileName", workspace.transform,
            "REPORT_Q3_FINAL_v7.xlsx", 16f, new Color(0.56f, 0.66f, 0.8f), font);
        TopLeft(fileName.rectTransform, 695f, 58f, 330f, 28f);
        fileName.alignment = TextAlignmentOptions.Right;

        Button close = CreateButton("ReportCloseButton", workspace.transform, "×", font,
            new Color(0.22f, 0.11f, 0.14f, 1f), 32f);
        TopRight(close.GetComponent<RectTransform>(), 24f, 24f, 48f, 48f);

        GameObject progressTrack = CreateImage("ReportProgressTrack", workspace.transform,
            new Color(0.12f, 0.16f, 0.23f, 1f));
        TopStretch(progressTrack.GetComponent<RectTransform>(), 36f, 102f, 7f);
        GameObject progressFillObject = CreateImage("ReportProgressFill", progressTrack.transform,
            new Color(0.96f, 0.47f, 0.24f, 1f));
        Stretch(progressFillObject.GetComponent<RectTransform>());
        Image progressFill = progressFillObject.GetComponent<Image>();
        progressFill.type = Image.Type.Filled;
        progressFill.fillMethod = Image.FillMethod.Horizontal;
        progressFill.fillOrigin = 0;
        progressFill.fillAmount = 0f;

        GameObject documentPanel = CreateImage("DocumentPreview", workspace.transform,
            new Color(0.072f, 0.092f, 0.135f, 1f));
        TopLeft(documentPanel.GetComponent<RectTransform>(), 36f, 132f, 594f, 490f);
        AddOutline(documentPanel, new Color(0.15f, 0.22f, 0.32f, 1f));

        TextMeshProUGUI documentLabel = CreateText("DocumentLabel", documentPanel.transform,
            "TÀI LIỆU ĐANG SỬA", 15f, new Color(0.53f, 0.64f, 0.8f), font, FontStyles.Bold);
        TopLeft(documentLabel.rectTransform, 24f, 20f, 400f, 24f);

        GameObject paper = CreateImage("Paper", documentPanel.transform,
            new Color(0.055f, 0.071f, 0.102f, 1f));
        TopLeft(paper.GetComponent<RectTransform>(), 22f, 58f, 550f, 405f);
        TextMeshProUGUI document = CreateText("DocumentBody", paper.transform, string.Empty,
            22f, new Color(0.88f, 0.91f, 0.96f), font);
        Stretch(document.rectTransform, 25f, 22f, 25f, 22f);
        document.alignment = TextAlignmentOptions.TopLeft;
        document.enableWordWrapping = true;
        document.lineSpacing = 14f;

        GameObject taskPanel = CreateImage("TaskPanel", workspace.transform,
            new Color(0.058f, 0.082f, 0.128f, 1f));
        TopLeft(taskPanel.GetComponent<RectTransform>(), 652f, 132f, 432f, 490f);
        AddOutline(taskPanel, new Color(0.19f, 0.28f, 0.41f, 1f));

        TextMeshProUGUI step = CreateText("ReportStep", taskPanel.transform, "BƯỚC 1 / 3",
            15f, new Color(1f, 0.53f, 0.3f), font, FontStyles.Bold);
        TopLeft(step.rectTransform, 24f, 20f, 190f, 24f);

        TextMeshProUGUI taskTitle = CreateText("ReportTaskTitle", taskPanel.transform,
            "ĐỐI CHIẾU SỐ LIỆU", 25f, new Color(0.96f, 0.97f, 1f), font, FontStyles.Bold);
        TopLeft(taskTitle.rectTransform, 24f, 50f, 384f, 38f);

        TextMeshProUGUI instruction = CreateText("ReportInstruction", taskPanel.transform, string.Empty,
            17f, new Color(0.68f, 0.74f, 0.84f), font);
        TopLeft(instruction.rectTransform, 24f, 91f, 384f, 60f);
        instruction.enableWordWrapping = true;

        Button[] buttons = new Button[4];
        TextMeshProUGUI[] labels = new TextMeshProUGUI[4];
        for (int i = 0; i < buttons.Length; i++)
        {
            buttons[i] = CreateButton("ReportOption" + (i + 1), taskPanel.transform, string.Empty, font,
                new Color(0.105f, 0.145f, 0.215f, 1f), 17f);
            TopLeft(buttons[i].GetComponent<RectTransform>(), 24f, 160f + i * 66f, 384f, 54f);
            labels[i] = buttons[i].GetComponentInChildren<TextMeshProUGUI>();
            labels[i].alignment = TextAlignmentOptions.MidlineLeft;
            labels[i].margin = new Vector4(17f, 3f, 14f, 3f);
        }

        TextMeshProUGUI feedback = CreateText("ReportFeedback", taskPanel.transform, string.Empty,
            16f, new Color(0.72f, 0.78f, 0.88f), font, FontStyles.Italic);
        TopLeft(feedback.rectTransform, 24f, 432f, 384f, 42f);
        feedback.enableWordWrapping = true;

        TextMeshProUGUI footer = CreateText("ReportFooter", workspace.transform,
            "ESC  Tạm dừng và quay lại văn phòng   ·   Tiến độ được tự động lưu", 15f,
            new Color(0.48f, 0.57f, 0.7f), font);
        BottomCenter(footer.rectTransform, 0f, 18f, 950f, 28f);
        footer.alignment = TextAlignmentOptions.Center;

        OfficeReportMiniGame miniGame = canvas.GetComponent<OfficeReportMiniGame>();
        if (miniGame == null) miniGame = Undo.AddComponent<OfficeReportMiniGame>(canvas);
        var miniSerialized = new SerializedObject(miniGame);
        SetReference(miniSerialized, "panel", overlay);
        SetReference(miniSerialized, "stepText", step);
        SetReference(miniSerialized, "taskTitleText", taskTitle);
        SetReference(miniSerialized, "instructionText", instruction);
        SetReference(miniSerialized, "documentText", document);
        SetReference(miniSerialized, "feedbackText", feedback);
        SetReference(miniSerialized, "fileNameText", fileName);
        SetReference(miniSerialized, "progressFill", progressFill);
        SetReference(miniSerialized, "closeButton", close);
        SetArray(miniSerialized, "optionButtons", buttons);
        SetArray(miniSerialized, "optionLabels", labels);
        miniSerialized.ApplyModifiedPropertiesWithoutUndo();

        var officeSerialized = new SerializedObject(office);
        SetReference(officeSerialized, "reportMiniGame", miniGame);
        officeSerialized.ApplyModifiedPropertiesWithoutUndo();

        overlay.SetActive(false);
        EditorUtility.SetDirty(miniGame);
        EditorUtility.SetDirty(office);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Selection.activeGameObject = workspace;
        Debug.Log("[KuTy] Office report mini-game UI built and connected.");
    }

    private static GameObject CreateImage(string name, Transform parent, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        Undo.RegisterCreatedObjectUndo(go, "Create " + name);
        go.transform.SetParent(parent, false);
        go.GetComponent<Image>().color = color;
        return go;
    }

    private static TextMeshProUGUI CreateText(string name, Transform parent, string value, float size,
        Color color, TMP_FontAsset font, FontStyles style = FontStyles.Normal)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        Undo.RegisterCreatedObjectUndo(go, "Create " + name);
        go.transform.SetParent(parent, false);
        var text = go.GetComponent<TextMeshProUGUI>();
        text.text = value;
        text.font = font;
        text.fontSize = size;
        text.fontStyle = style;
        text.color = color;
        text.raycastTarget = false;
        text.overflowMode = TextOverflowModes.Ellipsis;
        return text;
    }

    private static Button CreateButton(string name, Transform parent, string label, TMP_FontAsset font,
        Color background, float fontSize)
    {
        GameObject go = CreateImage(name, parent, background);
        Button button = go.AddComponent<Button>();
        button.navigation = new Navigation { mode = Navigation.Mode.None };
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1.12f, 1.12f, 1.12f, 1f);
        colors.pressedColor = new Color(0.82f, 0.82f, 0.82f, 1f);
        colors.disabledColor = new Color(0.45f, 0.48f, 0.54f, 0.72f);
        colors.colorMultiplier = 1f;
        colors.fadeDuration = 0.08f;
        button.colors = colors;

        TextMeshProUGUI text = CreateText("Label", go.transform, label, fontSize,
            new Color(0.92f, 0.94f, 0.98f), font, FontStyles.Bold);
        Stretch(text.rectTransform);
        text.alignment = TextAlignmentOptions.Center;
        text.enableWordWrapping = true;
        return button;
    }

    private static void AddOutline(GameObject go, Color color)
    {
        var outline = go.AddComponent<Outline>();
        outline.effectColor = color;
        outline.effectDistance = new Vector2(1f, -1f);
    }

    private static void SetReference(SerializedObject serialized, string property, Object value)
    {
        serialized.FindProperty(property).objectReferenceValue = value;
    }

    private static void SetArray<T>(SerializedObject serialized, string property, T[] values) where T : Object
    {
        SerializedProperty array = serialized.FindProperty(property);
        array.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++) array.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
    }

    private static void Stretch(RectTransform rect, float left = 0f, float bottom = 0f, float right = 0f, float top = 0f)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(left, bottom);
        rect.offsetMax = new Vector2(-right, -top);
    }

    private static void Center(RectTransform rect, float width, float height)
    {
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(width, height);
    }

    private static void TopLeft(RectTransform rect, float x, float y, float width, float height)
    {
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(x, -y);
        rect.sizeDelta = new Vector2(width, height);
    }

    private static void TopRight(RectTransform rect, float x, float y, float width, float height)
    {
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(1f, 1f);
        rect.anchoredPosition = new Vector2(-x, -y);
        rect.sizeDelta = new Vector2(width, height);
    }

    private static void TopStretch(RectTransform rect, float horizontalInset, float y, float height)
    {
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, -y);
        rect.sizeDelta = new Vector2(-horizontalInset * 2f, height);
    }

    private static void BottomCenter(RectTransform rect, float x, float y, float width, float height)
    {
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0f);
        rect.anchoredPosition = new Vector2(x, y);
        rect.sizeDelta = new Vector2(width, height);
    }
}
