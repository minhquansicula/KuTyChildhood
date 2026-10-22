using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// ProgressBarUI — Thanh progress cho mini-game rửa chén.
/// 
/// Setup trong Unity:
/// 1. Tạo Panel (ẩn mặc định)
/// 2. Thêm Slider (0-1, không interactable)
/// 3. Thêm TextMeshPro cho label ("Chén 1/5")
/// 4. Gán references
/// </summary>
public class ProgressBarUI : MonoBehaviour
{
    // ========== SINGLETON ==========
    public static ProgressBarUI Instance { get; private set; }
    private void OnDestroy() { if (Instance == this) Instance = null; }
    private TMP_Text percentText;

    // ========== UI REFERENCES ==========
    [Header("References")]
    [SerializeField] private GameObject panel;
    [SerializeField] private Slider progressSlider;
    [SerializeField] private TextMeshProUGUI labelText;
    [SerializeField] private Image fillImage;

    [Header("Colors")]
    [SerializeField] private Color fillColorStart = new Color(0.4f, 0.7f, 1f);  // Xanh nước
    [SerializeField] private Color fillColorEnd = new Color(0.2f, 1f, 0.4f);    // Xanh lá

    // ========== LIFECYCLE ==========
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        if (panel)
        {
            GameUiPresentation.ConfigureCanvas(panel.GetComponentInParent<Canvas>(true));
            GameUiPresentation.Card(panel.GetComponent<Image>());
            OfficeUiStyle.Rect((RectTransform)panel.transform, new Vector2(.5f, 0), new Vector2(820, 154), new Vector2(0, 112));
            percentText = OfficeUiStyle.Label("TaskPercent", panel.transform, GameUiTheme.Current ? GameUiTheme.Current.font : null,
                "0%", 23, new Vector2(90, 34), new Vector2(335, -43), GameUiPresentation.Gold);
        }
        if (labelText)
        {
            GameUiPresentation.Text(labelText, 26, GameUiPresentation.Paper);
            OfficeUiStyle.Rect(labelText.rectTransform, new Vector2(.5f, 1), new Vector2(750, 68), new Vector2(0, -47));
            labelText.enableAutoSizing = true;
            labelText.fontSizeMin = 22; labelText.fontSizeMax = 26;
            labelText.alignment = TextAlignmentOptions.Center;
        }
        if (progressSlider)
        {
            progressSlider.interactable = false;
            OfficeUiStyle.Rect((RectTransform)progressSlider.transform, new Vector2(.5f, 0), new Vector2(650, 12), new Vector2(-35, 34));
            progressSlider.minValue = 0; progressSlider.maxValue = 1;
            if (progressSlider.handleRect) progressSlider.handleRect.gameObject.SetActive(false);
        }
        fillColorStart = new Color(.46f, .66f, .58f);
        fillColorEnd = GameUiPresentation.Gold;

        // Ẩn mặc định
        if (panel != null)
            panel.SetActive(false);
    }

    // ========== PUBLIC METHODS ==========

    /// <summary>Hiện progress bar với label.</summary>
    public void Show(string label = "")
    {
        if (panel != null)
            panel.SetActive(true);

        if (labelText != null)
            labelText.text = label;

        SetProgress(0f);
    }

    /// <summary>Ẩn progress bar.</summary>
    public void Hide()
    {
        if (panel != null)
            panel.SetActive(false);
    }

    /// <summary>Cập nhật progress (0-1).</summary>
    public void SetProgress(float value)
    {
        value = Mathf.Clamp01(value);
        if (percentText) percentText.text = Mathf.RoundToInt(value * 100) + "%";

        if (progressSlider != null)
            progressSlider.value = value;

        // Đổi màu theo progress (xanh nước → xanh lá)
        if (fillImage != null)
            fillImage.color = Color.Lerp(fillColorStart, fillColorEnd, value);
    }

    /// <summary>Cập nhật label text.</summary>
    public void UpdateLabel(string label)
    {
        if (labelText != null)
            labelText.text = label;
    }
}
