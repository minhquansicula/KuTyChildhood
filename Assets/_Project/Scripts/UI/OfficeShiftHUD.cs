using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Shows one actionable objective; progression remains owned by OfficeSceneController.</summary>
public sealed class OfficeShiftHUD : MonoBehaviour
{
    private OfficeSceneController office;
    private CanvasGroup group;
    private TextMeshProUGUI content;
    private string lastText;
    private RectTransform objectiveCard;

    private void Start()
    {
        office = GetComponent<OfficeSceneController>();
        if (office == null) office = OfficeSceneController.Instance;
        if (GetComponent<OfficeGuidanceController>() == null)
            gameObject.AddComponent<OfficeGuidanceController>();
        office?.SetBuiltInObjectiveVisible(false);
        CreateView();
        Subscribe();
        Refresh();
    }

    private void OnDestroy()
    {
        if (office == null) return;
        office.PhaseChanged -= HandlePhaseChanged;
        office.WorkingChanged -= HandleWorkingChanged;
        office.SeatedChanged -= HandleSeatedChanged;
        office.ObjectiveChanged -= Refresh;
    }

    private void LateUpdate()
    {
        if (office == null || group == null) return;
        bool visible = office.Phase != OfficePhase.Opening && office.Phase != OfficePhase.Leaving &&
            !office.IsWorking && GameManager.Instance?.IsPaused != true;
        group.alpha = visible ? 1f : 0f;
    }

    private void Subscribe()
    {
        if (office == null) return;
        office.PhaseChanged += HandlePhaseChanged;
        office.WorkingChanged += HandleWorkingChanged;
        office.SeatedChanged += HandleSeatedChanged;
        office.ObjectiveChanged += Refresh;
    }

    private void CreateView()
    {
        var root = new GameObject("OfficeCurrentObjective", typeof(RectTransform), typeof(Canvas),
            typeof(CanvasScaler), typeof(CanvasGroup));
        root.transform.SetParent(transform, false);
        root.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        root.GetComponent<Canvas>().sortingOrder = 2;
        CanvasScaler scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = .5f;
        group = root.GetComponent<CanvasGroup>();
        group.interactable = group.blocksRaycasts = false;
        var safe = GameUiPresentation.SafeRoot(root.transform, "OfficeObjectiveSafeArea");

        var card = new GameObject("ObjectiveCard", typeof(RectTransform), typeof(Image));
        card.transform.SetParent(safe, false);
        RectTransform rect = (RectTransform)card.transform;
        objectiveCard = rect;
        rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one;
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(492f, 250f);
        Image background = card.GetComponent<Image>();
        background.sprite = OfficeUiStyle.Rounded;
        background.type = Image.Type.Sliced;
        background.color = OfficeUiStyle.Ink;
        background.raycastTarget = false;
        OfficeUiStyle.Panel("ObjectiveAccent", card.transform, new Vector2(3, 164), new Vector2(-233, 0), OfficeUiStyle.Gold);

        content = new GameObject("ObjectiveText", typeof(RectTransform), typeof(TextMeshProUGUI))
            .GetComponent<TextMeshProUGUI>();
        content.transform.SetParent(card.transform, false);
        OfficeIntroHUD intro = FindObjectOfType<OfficeIntroHUD>(true);
        if (intro != null && intro.objective != null) content.font = intro.objective.font;
        content.rectTransform.anchorMin = Vector2.zero;
        content.rectTransform.anchorMax = Vector2.one;
        content.rectTransform.offsetMin = new Vector2(27f, 22f);
        content.rectTransform.offsetMax = new Vector2(-27f, -22f);
        content.fontSize = 20f;
        GameUiPresentation.Text(content, 24, GameUiPresentation.Paper);
        content.lineSpacing = 3f;
        content.enableAutoSizing = false;
        content.alignment = TextAlignmentOptions.TopLeft;
        content.raycastTarget = false;
    }

    private void Refresh()
    {
        if (office == null || content == null) return;
        string status = office.ReportDelivered || office.Phase == OfficePhase.Rest
            ? "18:17 · ĐÃ NỘP"
            : office.HasPrintedReport ? "18:14 · ĐANG GIAO" : "17:42 · VĂN PHÒNG";
        string action = string.IsNullOrEmpty(office.ObjectiveText) ? "Đợi chỉ dẫn tiếp theo." : office.ObjectiveText;
        string text = "<size=19><color=#" + ColorUtility.ToHtmlStringRGB(OfficeUiStyle.Gold) + ">CHƯƠNG 01 · CA LÀM CUỐI NGÀY</color></size>\n" +
            "<size=21><color=#" + ColorUtility.ToHtmlStringRGB(OfficeUiStyle.Muted) + ">" + status + "</color></size>\n\n" +
            "<size=28><color=#" + ColorUtility.ToHtmlStringRGB(OfficeUiStyle.Paper) + ">" + action + "</color></size>\n\n" +
            "<size=20><color=#" + ColorUtility.ToHtmlStringRGB(OfficeUiStyle.Muted) + ">" + ControlHint() + "</color></size>";
        if (text != lastText)
        {
            content.text = lastText = text;
            float preferredHeight = content.GetPreferredValues(text, 438f, 0f).y;
            objectiveCard.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, Mathf.Clamp(preferredHeight + 44f, 220f, 360f));
        }
    }

    private string ControlHint()
    {
        if (office.Phase == OfficePhase.Rest && office.IsSeated) return "E Nghỉ  ·  H Xem gợi ý";
        if (office.IsSeated) return "E  Tương tác  ·  H  Gợi ý\nSPACE  Đứng dậy";
        return "E Tương tác  ·  H Xem gợi ý";
    }

    private void HandlePhaseChanged(OfficePhase previous, OfficePhase current) => Refresh();
    private void HandleWorkingChanged(bool working) => Refresh();
    private void HandleSeatedChanged(bool seated) => Refresh();
}
