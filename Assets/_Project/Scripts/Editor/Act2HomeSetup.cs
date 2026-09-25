using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public static class Act2HomeSetup
{
    [MenuItem("KuTy/Setup/Setup Bed Sequence")]
    public static void SetupBedSequence()
    {
        // 1. Create Bed Block
        GameObject bed = GameObject.CreatePrimitive(PrimitiveType.Cube);
        bed.name = "Bed";
        bed.transform.position = new Vector3(0, 0.25f, -5); // Under player
        bed.transform.localScale = new Vector3(2f, 0.5f, 2.5f);
        bed.layer = LayerMask.NameToLayer("Ground");
        
        // 2. Teleport Player to bed
        GameObject player = GameObject.Find("Player");
        if (player != null)
        {
            player.transform.position = new Vector3(0, 0.5f, -5);
        }

        // 3. Create WakeUp UI Canvas
        GameObject uiObj = new GameObject("WakeUpUI");
        Canvas canvas = uiObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        uiObj.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        uiObj.AddComponent<GraphicRaycaster>();

        // Blink Panel
        GameObject panelObj = new GameObject("BlinkPanel");
        panelObj.transform.SetParent(uiObj.transform, false);
        Image panelImage = panelObj.AddComponent<Image>();
        panelImage.color = Color.black;
        RectTransform rect = panelObj.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.sizeDelta = Vector2.zero;
        CanvasGroup group = panelObj.AddComponent<CanvasGroup>();
        group.alpha = 1f;

        // Prompt Text
        GameObject textObj = new GameObject("PromptText");
        textObj.transform.SetParent(uiObj.transform, false);
        TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = "[E] Xuống giường";
        tmp.fontSize = 36;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0.5f, 0.2f);
        textRect.anchorMax = new Vector2(0.5f, 0.2f);
        textRect.sizeDelta = new Vector2(400, 100);

        // 4. Add Controller
        GameObject controllerObj = new GameObject("BedWakeUpManager");
        var controller = controllerObj.AddComponent<BedWakeUpController>();
        controller.promptText = tmp;

        // Save Scene
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
        Debug.Log("Bed Sequence Setup Complete!");
    }
}
