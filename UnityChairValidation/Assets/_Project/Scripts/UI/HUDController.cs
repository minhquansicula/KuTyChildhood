using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// HUDController — Quản lý giao diện HUD trong game.
/// 
/// Hiển thị:
/// - Crosshair giữa màn hình
/// - Prompt tương tác "[E] Tương tác"
/// - Số tiền hiện có
/// - Icon 3 mảnh ký ức (sáng lên khi thu thập)
/// 
/// Setup trong Unity:
/// 1. Tạo Canvas (Screen Space - Overlay)
/// 2. Thêm Image cho crosshair (giữa màn hình, nhỏ, ~20x20px)
/// 3. Thêm TextMeshPro cho interact prompt (dưới crosshair)
/// 4. Thêm TextMeshPro cho tiền (góc trên phải)
/// 5. Thêm 3 Image cho mảnh ký ức (góc trên trái)
/// 6. Gán references
/// </summary>
public class HUDController : MonoBehaviour
{
    // ========== SINGLETON ==========
    public static HUDController Instance { get; private set; }

    // ========== UI REFERENCES ==========
    [Header("Crosshair")]
    [SerializeField] private Image crosshairImage;
    [SerializeField] private Color crosshairNormal = Color.white;
    [SerializeField] private Color crosshairHighlight = new Color(1f, 0.85f, 0.2f); // Vàng ấm

    [Header("Interact Prompt")]
    [SerializeField] private GameObject promptPanel;
    [SerializeField] private TextMeshProUGUI promptText;

    [Header("Currency")]
    [SerializeField] private TextMeshProUGUI moneyText;

    [Header("Memory Pieces")]
    [SerializeField] private Image[] memoryIcons;   // Hiếu thảo, niềm vui, tự do
    [SerializeField] private TextMeshProUGUI objectiveText;
    [SerializeField] private Color memoryCollectedColor = new Color(1f, 0.9f, 0.3f);  // Vàng sáng
    [SerializeField] private Color memoryUncollectedColor = new Color(0.3f, 0.3f, 0.3f, 0.5f); // Xám mờ

    // ========== LIFECYCLE ==========
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        // Ẩn prompt ban đầu
        HideInteractPrompt();

        // Subscribe events
        if (CurrencyManager.Instance != null)
            CurrencyManager.Instance.OnMoneyChanged += UpdateMoneyDisplay;

        if (MemoryCollectionManager.Instance != null)
        {
            MemoryCollectionManager.Instance.OnMemoryCollected += OnMemoryCollected;
            MemoryCollectionManager.Instance.OnMemoriesReset += ResetMemoryIcons;
        }
        if (QuestManager.Instance != null) QuestManager.Instance.OnQuestChanged += QuestChanged;

        // Khởi tạo hiển thị
        UpdateMoneyDisplay(CurrencyManager.Instance?.CurrentMoney ?? 0);
        ResetMemoryIcons();
        QuestChanged(QuestManager.Instance != null ? QuestManager.Instance.CurrentStep : QuestStep.Ending);
    }

    private void OnDestroy()
    {
        // Unsubscribe events
        if (CurrencyManager.Instance != null)
            CurrencyManager.Instance.OnMoneyChanged -= UpdateMoneyDisplay;

        if (MemoryCollectionManager.Instance != null)
        {
            MemoryCollectionManager.Instance.OnMemoryCollected -= OnMemoryCollected;
            MemoryCollectionManager.Instance.OnMemoriesReset -= ResetMemoryIcons;
        }
        if (QuestManager.Instance != null) QuestManager.Instance.OnQuestChanged -= QuestChanged;
        if (Instance == this) Instance = null;
    }

    // ========== CROSSHAIR ==========

    /// <summary>
    /// Bật/tắt highlight crosshair (khi nhìn vào vật tương tác).
    /// </summary>
    public void SetCrosshairHighlight(bool highlighted)
    {
        if (crosshairImage != null)
        {
            crosshairImage.color = highlighted ? crosshairHighlight : crosshairNormal;

            // Phóng to nhẹ khi highlight
            crosshairImage.transform.localScale = highlighted
                ? Vector3.one * 1.3f
                : Vector3.one;
        }
    }

    // ========== INTERACT PROMPT ==========

    /// <summary>
    /// Hiện prompt tương tác (vd: "[E] Rửa chén").
    /// </summary>
    public void ShowInteractPrompt(string text)
    {
        if (promptPanel != null)
            promptPanel.SetActive(true);

        if (promptText != null)
            promptText.text = text;
    }

    /// <summary>
    /// Ẩn prompt tương tác.
    /// </summary>
    public void HideInteractPrompt()
    {
        if (promptPanel != null)
            promptPanel.SetActive(false);
    }

    // ========== CURRENCY ==========

    private void UpdateMoneyDisplay(int amount)
    {
        if (moneyText != null)
        {
            moneyText.text = $"{amount} đồng";
        }
    }
    private void QuestChanged(QuestStep step)
    {
        if (objectiveText != null) objectiveText.text = QuestManager.Instance != null
            ? "Nhiệm vụ: " + QuestManager.Instance.ObjectiveText : "";
    }

    // ========== MEMORY PIECES ==========

    private void ResetMemoryIcons()
    {
        if (memoryIcons == null) return;

        for (int i = 0; i < memoryIcons.Length; i++)
        {
            if (memoryIcons[i] != null)
            {
                memoryIcons[i].color = MemoryCollectionManager.Instance != null &&
                    MemoryCollectionManager.Instance.HasCollected((MemoryType)i)
                    ? memoryCollectedColor : memoryUncollectedColor;
            }
        }
    }

    private void OnMemoryCollected(MemoryType type, int totalCollected)
    {
        int index = (int)type;

        if (memoryIcons != null && index < memoryIcons.Length && memoryIcons[index] != null)
        {
            // Animation: icon sáng lên
            memoryIcons[index].color = memoryCollectedColor;

            // TODO: Thêm animation tween (scale up rồi scale lại) nếu muốn đẹp hơn
            StartCoroutine(AnimateMemoryIcon(memoryIcons[index]));
        }
    }

    private System.Collections.IEnumerator AnimateMemoryIcon(Image icon)
    {
        // Scale up
        float duration = 0.3f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float scale = 1f + Mathf.Sin(t * Mathf.PI) * 0.5f; // Bounce effect
            icon.transform.localScale = Vector3.one * scale;
            yield return null;
        }

        icon.transform.localScale = Vector3.one;
    }
}
