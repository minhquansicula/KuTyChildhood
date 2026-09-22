using System.Collections;
using UnityEngine;
using TMPro;

/// <summary>
/// DialogueUI — Hiển thị text hội thoại/narration.
/// 
/// Dùng để hiện:
/// - Tên ký ức khi thu thập ("Ký ức về Mẹ")
/// - Mô tả ký ức ("Những bữa cơm ấm áp...")
/// - Text kể chuyện tại các thời điểm quan trọng
/// 
/// Setup trong Unity:
/// 1. Tạo Panel ở dưới màn hình (style text box)
/// 2. Thêm TextMeshPro cho title (tên)
/// 3. Thêm TextMeshPro cho body (nội dung)
/// 4. Panel ẩn mặc định
/// </summary>
public class DialogueUI : MonoBehaviour
{
    // ========== SINGLETON ==========
    public static DialogueUI Instance { get; private set; }

    // ========== UI REFERENCES ==========
    [Header("References")]
    [SerializeField] private GameObject panel;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI bodyText;
    [SerializeField] private CanvasGroup canvasGroup; // Cho fade effect

    [Header("Settings")]
    [SerializeField] private float fadeInDuration = 0.5f;
    [SerializeField] private float fadeOutDuration = 0.5f;
    [SerializeField] private float typewriterSpeed = 0.03f; // Giây/ký tự (hiệu ứng đánh máy)
    [SerializeField] private bool useTypewriterEffect = true;

    // ========== STATE ==========
    private Coroutine currentDialogueCoroutine;
    private bool isShowing = false;

    // ========== LIFECYCLE ==========
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Ẩn mặc định
        if (panel != null) panel.SetActive(false);
        if (canvasGroup != null) canvasGroup.alpha = 0f;
    }

    // ========== PUBLIC METHODS ==========

    /// <summary>
    /// Hiện dialogue với title và body, tự ẩn sau duration giây.
    /// </summary>
    /// <param name="title">Tiêu đề (vd: "Ký ức về Mẹ")</param>
    /// <param name="body">Nội dung (vd: "Những bữa cơm ấm áp...")</param>
    /// <param name="displayDuration">Thời gian hiển thị (giây) trước khi tự ẩn. 0 = không tự ẩn.</param>
    public void ShowDialogue(string title, string body, float displayDuration = 0f)
    {
        // Cancel dialogue đang hiện (nếu có)
        if (currentDialogueCoroutine != null)
            StopCoroutine(currentDialogueCoroutine);

        currentDialogueCoroutine = StartCoroutine(ShowDialogueCoroutine(title, body, displayDuration));
    }

    /// <summary>
    /// Hiện text đơn giản (chỉ body, không title).
    /// </summary>
    public void ShowText(string text, float displayDuration = 3f)
    {
        ShowDialogue("", text, displayDuration);
    }

    /// <summary>
    /// Ẩn dialogue ngay lập tức.
    /// </summary>
    public void HideDialogue()
    {
        if (currentDialogueCoroutine != null)
            StopCoroutine(currentDialogueCoroutine);

        StartCoroutine(FadeOut());
    }

    // ========== COROUTINES ==========

    private IEnumerator ShowDialogueCoroutine(string title, string body, float displayDuration)
    {
        isShowing = true;

        // Setup text
        if (titleText != null)
        {
            titleText.text = title;
            titleText.gameObject.SetActive(!string.IsNullOrEmpty(title));
        }

        if (bodyText != null)
            bodyText.text = useTypewriterEffect ? "" : body;

        // Hiện panel
        if (panel != null) panel.SetActive(true);

        // Fade in
        yield return StartCoroutine(FadeIn());

        // Typewriter effect
        if (useTypewriterEffect && bodyText != null && !string.IsNullOrEmpty(body))
        {
            bodyText.text = "";
            foreach (char c in body)
            {
                bodyText.text += c;
                yield return new WaitForSeconds(typewriterSpeed);
            }
        }

        // Chờ display duration
        if (displayDuration > 0)
        {
            yield return new WaitForSeconds(displayDuration);

            // Fade out
            yield return StartCoroutine(FadeOut());
        }

        isShowing = false;
    }

    private IEnumerator FadeIn()
    {
        if (canvasGroup == null) yield break;

        float elapsed = 0f;
        while (elapsed < fadeInDuration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / fadeInDuration);
            yield return null;
        }
        canvasGroup.alpha = 1f;
    }

    private IEnumerator FadeOut()
    {
        if (canvasGroup == null)
        {
            if (panel != null) panel.SetActive(false);
            yield break;
        }

        float elapsed = 0f;
        float startAlpha = canvasGroup.alpha;
        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, elapsed / fadeOutDuration);
            yield return null;
        }
        canvasGroup.alpha = 0f;

        if (panel != null) panel.SetActive(false);
    }
}
