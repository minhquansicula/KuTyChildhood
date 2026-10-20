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
    private void OnDestroy() { if (Instance == this) Instance = null; }

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
    }

    // ========== PUBLIC METHODS ==========

    /// <summary>Hiện UI bắn bi.</summary>
    public void Show(int currentShot, int maxShots, int knocked, int target)
    {
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
    }

    public void SetStatus(string message)
    {
        if (statusText != null) statusText.text = message;
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
    }

    /// <summary>Cập nhật thanh lực bắn (0-1).</summary>
    public void UpdateForceIndicator(float forcePercent)
    {
        forcePercent = Mathf.Clamp01(forcePercent);

        if (forceSlider != null)
            forceSlider.value = forcePercent;

        // Đổi màu theo lực (xanh → đỏ)
        if (forceSliderFill != null)
            forceSliderFill.color = Color.Lerp(forceLow, forceHigh, forcePercent);
    }
}
