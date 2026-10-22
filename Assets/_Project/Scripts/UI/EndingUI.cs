using UnityEngine;
using TMPro;

/// <summary>
/// EndingUI — Giao diện màn hình kết thúc (Act3).
/// 
/// Hiển thị:
/// - Hình ảnh "Chìa khóa ước mơ"
/// - Thông điệp kết
/// - Nút Chơi lại / Thoát
/// </summary>
public class EndingUI : MonoBehaviour
{
    // ========== UI REFERENCES ==========
    [Header("References")]
    [SerializeField] private TextMeshProUGUI messageText;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Message")]
    [SerializeField]
    [TextArea(3, 6)]
    private string endingMessage = "Thế giới của người lớn luôn đầy những cơn bão.\nNhưng đứa trẻ bên trong bạn, cùng những ký ức tươi đẹp này,\nsẽ luôn là nơi trú ẩn an toàn nhất.\nNgày mai trời lại sáng.";

    // ========== LIFECYCLE ==========
    private void Start()
    {
        var canvas = GetComponentInParent<Canvas>() ?? GetComponentInChildren<Canvas>(true);
        GameUiPresentation.ConfigureCanvas(canvas);
        foreach (var label in GetComponentsInChildren<TMP_Text>(true))
        {
            if (string.IsNullOrEmpty(label.text)) continue;
            if (label.text.StartsWith("Bản prototype")) label.gameObject.SetActive(false);
            else if (!messageText && label.text.StartsWith("Trước hiên")) messageText = label as TextMeshProUGUI;
            else if (label.text.Contains("CHÌA KHÓA ƯỚC MƠ"))
            {
                GameUiPresentation.Text(label, 44, GameUiPresentation.Gold);
                OfficeUiStyle.Rect(label.rectTransform, Vector2.one * .5f, new Vector2(1200, 90), new Vector2(0, 290));
                label.alignment = TextAlignmentOptions.Center;
            }
        }
        if (messageText)
        {
            GameUiPresentation.Text(messageText, 34, GameUiPresentation.Paper);
            messageText.alignment = TextAlignmentOptions.Center;
            OfficeUiStyle.Rect(messageText.rectTransform, Vector2.one * .5f, new Vector2(1160, 360), new Vector2(0, 25));
            messageText.lineSpacing = 12;
        }
        var buttons = GetComponentsInChildren<UnityEngine.UI.Button>(true);
        for (int i = 0; i < buttons.Length; i++)
        {
            OfficeUiStyle.Rect((RectTransform)buttons[i].transform, Vector2.one * .5f,
                new Vector2(380, 84), new Vector2((i - (buttons.Length - 1) * .5f) * 420, -270));
            GameUiPresentation.Button(buttons[i]);
        }
        // Set message
        if (messageText != null)
            messageText.text = endingMessage;

        // Fade in
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            StartCoroutine(FadeIn());
        }
    }

    private System.Collections.IEnumerator FadeIn()
    {
        yield return new WaitForSeconds(1f); // Chờ 1 giây

        float elapsed = 0f;
        float duration = 2f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            if (canvasGroup != null)
                canvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / duration);
            yield return null;
        }

        if (canvasGroup != null)
            canvasGroup.alpha = 1f;
    }

    // ========== BUTTON HANDLERS ==========

    /// <summary>Chơi lại — quay về Main Menu.</summary>
    public void OnPlayAgainClicked()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.ReturnToMainMenu();
    }

    /// <summary>Thoát game.</summary>
    public void OnQuitClicked()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.QuitGame();
    }
}
