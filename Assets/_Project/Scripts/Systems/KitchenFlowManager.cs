using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class KitchenFlowManager : MonoBehaviour
{
    public static KitchenFlowManager Instance;
    
    [System.Serializable]
    public class FlowTask {
        public string objectName;
        public string displayName;
        public bool isCompleted;
    }

    public List<FlowTask> tasks = new List<FlowTask>();
    
    private GameObject uiCanvas;
    private Text checklistText;

    private void Awake()
    {
        Instance = this;
        
        // Danh sách công việc dựa theo tên GameObject
        tasks.Add(new FlowTask { objectName = "WaterJar", displayName = "Múc nước lu" });
        tasks.Add(new FlowTask { objectName = "RiceWashingBowl", displayName = "Vo gạo" });
        tasks.Add(new FlowTask { objectName = "FirewoodStove", displayName = "Nhóm lửa" });
        tasks.Add(new FlowTask { objectName = "RicePot", displayName = "Nấu cơm" });
        tasks.Add(new FlowTask { objectName = "DiningTable", displayName = "Dọn mâm cơm" });
        
        CreateUI();
    }

    private void CreateUI()
    {
        uiCanvas = new GameObject("ChecklistCanvas");
        uiCanvas.transform.SetParent(transform);
        var canvas = uiCanvas.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;
        uiCanvas.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        
        GameObject panel = new GameObject("Panel");
        panel.transform.SetParent(uiCanvas.transform, false);
        var rect = panel.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0, 1);
        rect.anchorMax = new Vector2(0, 1);
        rect.pivot = new Vector2(0, 1);
        rect.anchoredPosition = new Vector2(20, -20);
        rect.sizeDelta = new Vector2(250, 250);
        var img = panel.AddComponent<Image>();
        img.color = new Color(0, 0, 0, 0.6f);

        GameObject txtObj = new GameObject("Text");
        txtObj.transform.SetParent(panel.transform, false);
        var txtRect = txtObj.AddComponent<RectTransform>();
        txtRect.anchorMin = new Vector2(0, 0);
        txtRect.anchorMax = new Vector2(1, 1);
        txtRect.offsetMin = new Vector2(15, 15);
        txtRect.offsetMax = new Vector2(-15, -15);
        
        checklistText = txtObj.AddComponent<Text>();
        checklistText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        checklistText.fontSize = 20;
        checklistText.color = Color.white;
        checklistText.supportRichText = true;
        
        UpdateUIText();
        uiCanvas.SetActive(false); // Ẩn lúc đầu
    }

    public void ShowChecklist()
    {
        if (uiCanvas != null) uiCanvas.SetActive(true);
    }

    public void MarkTaskCompleted(string objName)
    {
        bool changed = false;
        foreach (var t in tasks)
        {
            if (t.objectName == objName && !t.isCompleted)
            {
                t.isCompleted = true;
                changed = true;
                AudioManager.Instance?.PlaySFX("ui_select"); // Phát âm thanh hoàn thành
            }
        }
        if (changed) UpdateUIText();
    }

    private void UpdateUIText()
    {
        if (checklistText == null) return;
        string content = "<color=yellow><b>VIỆC CẦN LÀM:</b></color>\n\n";
        foreach (var t in tasks)
        {
            if (t.isCompleted)
                content += $"<color=#00FF00><s>[x] {t.displayName}</s></color>\n";
            else
                content += $"[ ] {t.displayName}\n";
        }
        checklistText.text = content;
    }
}
