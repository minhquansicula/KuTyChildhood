using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>A scene-local overview of the office shift; never owns input or progression.</summary>
public sealed class OfficeShiftHUD : MonoBehaviour
{
    private OfficeSceneController office;
    private CanvasGroup group;
    private TextMeshProUGUI content;
    private string lastText;

    private void Start()
    {
        office = GetComponent<OfficeSceneController>();
        var root = new GameObject("OfficeShiftOverview", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(CanvasGroup));
        root.transform.SetParent(transform, false);
        root.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        root.GetComponent<Canvas>().sortingOrder = 2;
        var scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = .5f;
        group = root.GetComponent<CanvasGroup>();
        group.interactable = group.blocksRaycasts = false;
        var card = new GameObject("ShiftCard", typeof(RectTransform), typeof(Image));
        card.transform.SetParent(root.transform, false);
        var rect = (RectTransform)card.transform;
        rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one;
        rect.anchoredPosition = new Vector2(-28, -128);
        rect.sizeDelta = new Vector2(350, 226);
        card.GetComponent<Image>().color = new Color(.025f, .045f, .06f, .92f);
        card.GetComponent<Image>().raycastTarget = false;
        var label = new GameObject("ShiftSteps", typeof(RectTransform));
        label.transform.SetParent(card.transform, false);
        content = label.AddComponent<TextMeshProUGUI>();
        var intro = FindObjectOfType<OfficeIntroHUD>(true);
        if (intro != null && intro.objective != null) content.font = intro.objective.font;
        content.rectTransform.anchorMin = Vector2.zero;
        content.rectTransform.anchorMax = Vector2.one;
        content.rectTransform.offsetMin = new Vector2(22, 16);
        content.rectTransform.offsetMax = new Vector2(-22, -16);
        content.fontSize = 20;
        content.lineSpacing = 8;
        content.raycastTarget = false;
    }

    private void LateUpdate()
    {
        if (office == null || group == null) return;
        bool visible = office.Phase != OfficePhase.Opening && office.Phase != OfficePhase.Leaving &&
            !office.IsWorking && GameManager.Instance?.IsPaused != true;
        group.alpha = visible ? 1 : 0;
        if (!visible) return;
        bool sent = office.Phase == OfficePhase.AfterWork || office.Phase == OfficePhase.Rest;
        int step = sent ? 3 : office.Phase == OfficePhase.ExcelAuditing ? 2 : office.IsSeated ? 1 : 0;
        string text = "<size=16><color=#F4C66A>CHƯƠNG 01 · CA LÀM CUỐI NGÀY</color></size>\n" +
            "<size=27><b>" + (sent ? "18:17 · ĐÃ GỬI" : "17:42 · VĂN PHÒNG") + "</b></size>\n" +
            Line(0, step, "Về bàn làm việc") + Line(1, step, "Mở báo cáo Excel") +
            Line(2, step, "Sửa 2 sheet và gửi báo cáo") + Line(3, step, "Nghỉ một chút");
        if (text != lastText) content.text = lastText = text;
    }

    private static string Line(int index, int step, string label) =>
        "<size=18><color=" + (index < step ? "#75CDB3" : index == step ? "#F4C66A" : "#8FA4B8") + ">" +
        (index < step ? "+  " : index == step ? ">  " : "·  ") + label + "</color></size>\n";
}
