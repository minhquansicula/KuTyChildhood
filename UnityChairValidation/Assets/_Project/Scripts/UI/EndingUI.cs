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
