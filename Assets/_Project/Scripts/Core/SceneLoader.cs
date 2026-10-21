using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SceneLoader : MonoBehaviour
{
    public static SceneLoader Instance { get; private set; }
    [SerializeField] private Image fadeImage;
    [SerializeField] private float defaultFadeDuration = 0.7f;
    public bool IsLoading { get; private set; }
    public bool IsSleepTransitionCovered { get; private set; }
    private bool activateSleepScene;

    // Begin loading during the collapse, but activate only after the eyes have closed.
    public bool PrepareSleepTransition(string sceneName)
    {
        if (IsLoading || !Application.CanStreamedLevelBeLoaded(sceneName)) return false;
        StartCoroutine(SleepTransition(sceneName));
        return true;
    }

    public void CompleteSleepTransition() => activateSleepScene = true;

    private IEnumerator SleepTransition(string sceneName)
    {
        IsLoading = true;
        IsSleepTransitionCovered = true;
        activateSleepScene = false;
        GameManager.Instance?.SetPaused(false);
        GameManager.Instance?.AcquireInput(this);
        try
        {
            var operation = SceneManager.LoadSceneAsync(sceneName);
            if (operation == null) yield break;
            operation.allowSceneActivation = false;
            while (!activateSleepScene) yield return null;
            fadeImage.color = Color.black;
            fadeImage.raycastTarget = true;
            AudioManager.Instance?.BeginHomeTransition();
            operation.allowSceneActivation = true;
            yield return operation;
            // Give destination Start() a frame to position the camera and create closed eyelids.
            yield return null;
        }
        finally
        {
            IsSleepTransitionCovered = false;
            IsLoading = false;
            fadeImage.color = Color.clear;
            fadeImage.raycastTarget = false;
            GameManager.Instance?.ReleaseInput(this);
        }
    }
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        if (fadeImage == null)
        {
            var root = new GameObject("TransitionCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            root.transform.SetParent(transform);
            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 999;
            var overlay = new GameObject("Fade", typeof(RectTransform), typeof(Image));
            overlay.transform.SetParent(root.transform, false);
            fadeImage = overlay.GetComponent<Image>();
            fadeImage.rectTransform.anchorMin = Vector2.zero;
            fadeImage.rectTransform.anchorMax = Vector2.one;
            fadeImage.rectTransform.offsetMin = fadeImage.rectTransform.offsetMax = Vector2.zero;
        }
        fadeImage.color = Color.clear;
        fadeImage.raycastTarget = false;
    }
    private void OnDestroy() { if (Instance == this) Instance = null; }
    public void LoadScene(string name, float duration = -1f)
        => LoadSceneWithColor(name, Color.black, duration < 0 ? defaultFadeDuration : duration);
    public void LoadSceneWithColor(string name, Color color, float duration = 1.5f)
    {
        if (IsLoading) return;
        if (!Application.CanStreamedLevelBeLoaded(name))
        {
            Debug.LogError("Scene not in Build Settings: " + name);
            return;
        }
        StartCoroutine(Transition(name, color, Mathf.Max(0f, duration)));
    }
    private IEnumerator Transition(string name, Color color, float duration)
    {
        IsLoading = true;
        GameManager.Instance?.SetPaused(false);
        fadeImage.raycastTarget = true;
        try
        {
            yield return Fade(color, 0f, 1f, duration);
            var operation = SceneManager.LoadSceneAsync(name);
            if (operation != null) yield return operation;
            yield return Fade(color, 1f, 0f, duration);
        }
        finally
        {
            IsLoading = false;
            if (fadeImage != null) { fadeImage.raycastTarget = false; fadeImage.color = Color.clear; }
            GameManager.Instance?.RefreshCursor();
        }
    }
    private IEnumerator Fade(Color color, float from, float to, float duration)
    {
        for (float t = 0; t < duration; t += Time.unscaledDeltaTime)
        {
            color.a = Mathf.Lerp(from, to, t / duration);
            fadeImage.color = color;
            yield return null;
        }
        color.a = to;
        fadeImage.color = color;
    }
}
