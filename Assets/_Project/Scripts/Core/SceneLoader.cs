using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// SceneLoader — Singleton quản lý việc chuyển scene với hiệu ứng fade.
/// 
/// Cách dùng:
///   SceneLoader.Instance.LoadScene("Act2_MemoryWorld");
///   SceneLoader.Instance.LoadScene("Act3_Ending", 2f); // fade chậm hơn
/// 
/// Setup trong Unity:
/// 1. Tạo Canvas (Screen Space - Overlay, Sort Order cao nhất, vd: 999)
/// 2. Thêm Image full screen, màu đen, alpha = 0
/// 3. Gán Image vào fadeImage field
/// 4. Đặt Canvas + SceneLoader lên GameObject có DontDestroyOnLoad
/// </summary>
public class SceneLoader : MonoBehaviour
{
    // ========== SINGLETON ==========
    public static SceneLoader Instance { get; private set; }

    // ========== SETTINGS ==========
    [Header("Fade Settings")]
    [SerializeField] private Image fadeImage;           // Image đen full screen
    [SerializeField] private float defaultFadeDuration = 1f;
    [SerializeField] private Color fadeColor = Color.black;

    // ========== STATE ==========
    private bool isLoading = false;

    // ========== LIFECYCLE ==========
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Đảm bảo fade image bắt đầu trong suốt
        if (fadeImage != null)
        {
            fadeImage.color = new Color(fadeColor.r, fadeColor.g, fadeColor.b, 0f);
            fadeImage.raycastTarget = false;
        }
    }

    // ========== PUBLIC METHODS ==========

    /// <summary>
    /// Load scene với hiệu ứng fade in/out.
    /// </summary>
    /// <param name="sceneName">Tên scene (phải được thêm vào Build Settings)</param>
    /// <param name="fadeDuration">Thời gian fade (giây). Mặc định 1 giây.</param>
    public void LoadScene(string sceneName, float fadeDuration = -1f)
    {
        if (isLoading)
        {
            Debug.LogWarning("[SceneLoader] Đang loading scene khác, bỏ qua yêu cầu.");
            return;
        }

        if (fadeDuration < 0) fadeDuration = defaultFadeDuration;

        StartCoroutine(LoadSceneCoroutine(sceneName, fadeDuration));
    }

    /// <summary>
    /// Load scene với hiệu ứng fade đặc biệt (vd: fade vàng ấm cho xuyên không).
    /// </summary>
    public void LoadSceneWithColor(string sceneName, Color color, float fadeDuration = 1.5f)
    {
        if (isLoading) return;

        fadeColor = color;
        if (fadeImage != null)
            fadeImage.color = new Color(color.r, color.g, color.b, 0f);

        StartCoroutine(LoadSceneCoroutine(sceneName, fadeDuration));
    }

    // ========== COROUTINES ==========

    private IEnumerator LoadSceneCoroutine(string sceneName, float fadeDuration)
    {
        isLoading = true;

        // Bật raycast để chặn click trong lúc fade
        if (fadeImage != null)
            fadeImage.raycastTarget = true;

        // === FADE OUT (trong suốt → đen) ===
        yield return StartCoroutine(Fade(0f, 1f, fadeDuration));

        // === LOAD SCENE ===
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
        asyncLoad.allowSceneActivation = false;

        // Chờ scene load xong (90% là gần xong)
        while (asyncLoad.progress < 0.9f)
        {
            yield return null;
        }

        // Kích hoạt scene mới
        asyncLoad.allowSceneActivation = true;

        // Chờ 1 frame để scene mới setup
        yield return new WaitForSeconds(0.1f);

        // === FADE IN (đen → trong suốt) ===
        yield return StartCoroutine(Fade(1f, 0f, fadeDuration));

        // Tắt raycast
        if (fadeImage != null)
            fadeImage.raycastTarget = false;

        // Reset fade color về đen
        fadeColor = Color.black;

        isLoading = false;

        Debug.Log($"[SceneLoader] Loaded scene: {sceneName}");
    }

    /// <summary>
    /// Fade alpha của fadeImage từ startAlpha → endAlpha trong duration giây.
    /// </summary>
    private IEnumerator Fade(float startAlpha, float endAlpha, float duration)
    {
        if (fadeImage == null)
        {
            Debug.LogWarning("[SceneLoader] fadeImage chưa được gán!");
            yield break;
        }

        float elapsed = 0f;
        Color color = fadeImage.color;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime; // Dùng unscaled để fade vẫn chạy khi game pause
            float t = Mathf.Clamp01(elapsed / duration);
            color.a = Mathf.Lerp(startAlpha, endAlpha, t);
            fadeImage.color = color;
            yield return null;
        }

        // Đảm bảo giá trị cuối chính xác
        color.a = endAlpha;
        fadeImage.color = color;
    }
}
