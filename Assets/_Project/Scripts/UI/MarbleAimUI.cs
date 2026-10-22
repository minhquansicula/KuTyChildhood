using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// MarbleAimUI — UI cho mini-game bắn bi.
/// 
/// Hiển thị:
/// - Số lượt bắn còn lại
/// - Số bi đã đẩy ra / mục tiêu
/// - Thanh lực bắn
/// </summary>
public class MarbleAimUI : MonoBehaviour
{
    // ========== SINGLETON ==========
    public static MarbleAimUI Instance { get; private set; }
    public bool IsVisible => panel != null && panel.activeInHierarchy;
    private void OnDestroy() { if (Instance == this) Instance = null; }
    private TextMeshProUGUI powerText;
    private TextMeshProUGUI heading;
    private Image scoreFill;
    private Button leaveButton;
    private System.Action leaveAction;

    public void SetExitAction(System.Action action)
    {
        leaveAction = action;
        if (leaveButton) leaveButton.interactable = action != null;
    }

    // ========== UI REFERENCES ==========
    [Header("References")]
    [SerializeField] private GameObject panel;
    [SerializeField] private TextMeshProUGUI shotsText;      // "Lượt: 2/5"
    [SerializeField] private TextMeshProUGUI knockedText;    // "Bi trúng: 1/3"
    [SerializeField] private Slider forceSlider;             // Thanh lực bắn
    [SerializeField] private Image forceSliderFill;
    [SerializeField] private TextMeshProUGUI statusText;

    [Header("Colors")]
    [SerializeField] private Color forceLow = new Color(0.3f, 0.8f, 0.3f);   // Xanh lá nhẹ
    [SerializeField] private Color forceHigh = new Color(1f, 0.3f, 0.2f);     // Đỏ mạnh

    // ========== LIFECYCLE ==========
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (panel != null)
            panel.SetActive(false);
        ConfigureLayout();
    }

    // Keep the entire HUD in a shallow top strip, including messages that used
    // to sit below the panel and cover the scoring circle.
    private void ConfigureLayout()
    {
        if (panel != null && statusText == null)
        {
            var labelObject = new GameObject("MarbleInstructions", typeof(RectTransform), typeof(TextMeshProUGUI));
            labelObject.transform.SetParent(panel.transform, false);
            var rect = labelObject.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, 0f);
            rect.pivot = new Vector2(.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -8f);
            rect.sizeDelta = new Vector2(940f, 70f);
            statusText = labelObject.GetComponent<TextMeshProUGUI>();
            statusText.alignment = TextAlignmentOptions.Center;
            statusText.fontSize = 21f;
            statusText.color = new Color(1f, .91f, .72f);
            statusText.raycastTarget = false;
        }
        if (panel == null) return;
        if (heading == null)
        {
            heading = OfficeUiStyle.Label("MarbleHeading", panel.transform, GameUiTheme.Current ? GameUiTheme.Current.font : null,
                "BẮN BI  /  MỘT CHIỀU TUỔI THƠ", 20, Vector2.zero, Vector2.zero, GameUiPresentation.Gold);
            powerText = OfficeUiStyle.Label("MarblePower", panel.transform, heading.font,
                "LỰC BẮN · 0%", 20, Vector2.zero, Vector2.zero, GameUiPresentation.Paper);
            var track = OfficeUiStyle.Panel("MarbleScoreTrack", panel.transform, Vector2.zero, Vector2.zero, new Color(.2f, .28f, .23f));
            track.rectTransform.anchorMin = new Vector2(.70f, .035f);
            track.rectTransform.anchorMax = new Vector2(.96f, .065f);
            track.rectTransform.offsetMin = track.rectTransform.offsetMax = Vector2.zero;
            scoreFill = OfficeUiStyle.Panel("ScoreFill", track.transform, Vector2.zero, Vector2.zero, GameUiPresentation.Gold);
            GameUiPresentation.Stretch(scoreFill.rectTransform, 0, 0);
            var exit = OfficeUiStyle.Panel("LeaveMarbles", panel.transform, Vector2.zero, Vector2.zero, new Color(.13f, .22f, .18f));
            exit.rectTransform.anchorMin = new Vector2(.80f, .78f);
            exit.rectTransform.anchorMax = new Vector2(.98f, .98f);
            exit.rectTransform.offsetMin = exit.rectTransform.offsetMax = Vector2.zero;
            leaveButton = exit.gameObject.AddComponent<Button>();
            leaveButton.targetGraphic = exit;
            leaveButton.onClick.AddListener(() => { if (GameManager.Instance?.IsPaused != true) leaveAction?.Invoke(); });
            var caption = OfficeUiStyle.Label("LeaveCaption", exit.transform, heading.font, "RỜI BÀN  [ESC]", 19,
                Vector2.zero, Vector2.zero, GameUiPresentation.Paper);
            GameUiPresentation.Stretch(caption.rectTransform, 6, 2);
            MenuButtonSfx.Attach(leaveButton);
        }
        var panelRect = panel.GetComponent<RectTransform>();
        if (panelRect == null) return;
        GameUiPresentation.ConfigureCanvas(panel.GetComponentInParent<Canvas>(true));
        panelRect.anchorMin = new Vector2(.08f, 1f);
        panelRect.anchorMax = new Vector2(.92f, 1f);
        panelRect.pivot = new Vector2(.5f, 1f);
        panelRect.anchoredPosition = new Vector2(0, -32);
        panelRect.sizeDelta = new Vector2(0, 184);
        GameUiPresentation.Card(panel.GetComponent<Image>());
        foreach (var label in panel.GetComponentsInChildren<TextMeshProUGUI>(true))
        {
            if (label != shotsText && label != knockedText && label != statusText && label != powerText && label != heading && label.name != "LeaveCaption")
                label.enabled = false; // Replaced by the live instruction/status row.
            label.raycastTarget = false;
        }
        PlaceLabel(heading, new Vector2(.02f, .78f), new Vector2(.76f, .98f));
        heading.fontSizeMin = 18; heading.fontSizeMax = 20;
        PlaceLabel(statusText, new Vector2(.02f, .43f), new Vector2(.98f, .76f));
        PlaceLabel(shotsText, new Vector2(.02f, .04f), new Vector2(.35f, .40f));
        PlaceLabel(knockedText, new Vector2(.67f, .09f), new Vector2(.98f, .40f));
        PlaceLabel(powerText, new Vector2(.36f, .17f), new Vector2(.64f, .40f));
        powerText.fontSizeMin = 18; powerText.fontSizeMax = 20;
        if (forceSlider != null)
        {
            var bar = forceSlider.GetComponent<RectTransform>();
            bar.anchorMin = new Vector2(.39f, .09f);
            bar.anchorMax = new Vector2(.61f, .15f);
            bar.offsetMin = bar.offsetMax = Vector2.zero;
            forceSlider.minValue = 0; forceSlider.maxValue = 1;
            forceSlider.interactable = false;
            if (forceSlider.handleRect) forceSlider.handleRect.gameObject.SetActive(false);
        }
        foreach (var graphic in panel.GetComponentsInChildren<Graphic>(true))
            graphic.raycastTarget = false;
        leaveButton.targetGraphic.raycastTarget = true;
        leaveButton.interactable = leaveAction != null;
    }

    private static void PlaceLabel(TextMeshProUGUI label, Vector2 min, Vector2 max)
    {
        if (label == null) return;
        var rect = label.rectTransform;
        rect.anchorMin = min; rect.anchorMax = max;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        label.alignment = TextAlignmentOptions.Center;
        label.enableAutoSizing = true;
        GameUiPresentation.Text(label, 25, label.name == "MarbleInstructions" ? GameUiPresentation.Gold : GameUiPresentation.Paper);
        label.enableAutoSizing = true;
        label.fontSizeMin = 21f;
        label.fontSizeMax = 25f;
        label.enableWordWrapping = true;
    }

    // ========== PUBLIC METHODS ==========

    /// <summary>Hiện UI bắn bi.</summary>
    public void Show(int currentShot, int maxShots, int knocked, int target)
    {
        ConfigureLayout();
        if (panel != null)
            panel.SetActive(true);

        UpdateShots(currentShot, maxShots);
        UpdateKnocked(knocked, target);
        UpdateForceIndicator(0f);
        if (statusText != null && HUDController.Instance != null && HUDController.Instance.ObjectiveFont != null)
            statusText.font = HUDController.Instance.ObjectiveFont;
        SetStatus("Kéo từ viên bi rồi thả để bắn · Chuột phải hủy ngắm · Esc rời bàn chơi");
    }

    /// <summary>Ẩn UI bắn bi.</summary>
    public void Hide()
    {
        if (panel != null)
            panel.SetActive(false);
        leaveAction = null;
    }

    public void SetStatus(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
            statusText.color = message.StartsWith("Chưa") ? new Color(1f, .79f, .52f) : GameUiPresentation.Paper;
        }
    }

    /// <summary>Cập nhật số lượt bắn.</summary>
    public void UpdateShots(int current, int max)
    {
        if (shotsText != null)
            shotsText.text = $"Đã bắn: {current}/{max} · Còn {Mathf.Max(0, max - current)} lượt";
    }

    /// <summary>Cập nhật số bi đã đẩy ra.</summary>
    public void UpdateKnocked(int knocked, int target)
    {
        if (knockedText != null)
            knockedText.text = $"Bi ra khỏi vòng: {knocked}/{target}";
        if (scoreFill) scoreFill.rectTransform.anchorMax = new Vector2(target > 0 ? Mathf.Clamp01((float)knocked / target) : 0f, 1f);
    }

    /// <summary>Cập nhật thanh lực bắn (0-1).</summary>
    public void UpdateForceIndicator(float forcePercent)
    {
        forcePercent = Mathf.Clamp01(forcePercent);
        if (powerText) powerText.text = $"LỰC BẮN · {Mathf.RoundToInt(forcePercent * 100f)}%";

        if (forceSlider != null)
            forceSlider.value = forcePercent;

        // Đổi màu theo lực (xanh → đỏ)
        if (forceSliderFill != null)
            forceSliderFill.color = Color.Lerp(forceLow, forceHigh, forcePercent);
    }
}
