using System;
using System.Collections;
using TMPro;
using UnityEngine;

public class DialogueUI : MonoBehaviour
{
    public static DialogueUI Instance { get; private set; }
    [SerializeField] private GameObject panel;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI bodyText;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private float typewriterSpeed = 0.02f;

    [Header("Gamified Visuals")]
    [SerializeField] private GameObject speakerBadge;
    [SerializeField] private GameObject continuePrompt;
    [SerializeField] private RectTransform containerRect;

    public bool IsShowing { get; private set; }
    private Action onClosed;
    private Coroutine popInRoutine;
    private Coroutine pulseRoutine;
    private bool styled;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        ConfigurePresentation();
        if (panel != null) panel.SetActive(false);
        if (continuePrompt != null) continuePrompt.SetActive(false);
    }
    private void OnDestroy()
    {
        GameManager.Instance?.ReleaseInput(this);
        if (Instance == this) Instance = null;
    }
    public void ShowDialogue(string title, string body, float displayDuration = 3f, Action onCompleted = null)
    {
        HideDialogue();
        onClosed = onCompleted;
        IsShowing = true;
        GameManager.Instance?.AcquireInput(this);
        if (panel != null) panel.SetActive(true);
        if (panel != null) panel.transform.SetAsLastSibling();
        if (canvasGroup != null) canvasGroup.alpha = 1;
        if (titleText != null) titleText.text = title;
        if (bodyText != null) { bodyText.text = body; bodyText.maxVisibleCharacters = 0; bodyText.ForceMeshUpdate(); }
        if (panel != null && bodyText != null && panel.transform is RectTransform rect)
        {
            Canvas.ForceUpdateCanvases();
            float height = bodyText.GetPreferredValues(body, Mathf.Max(400f, rect.rect.width - 64f), 0).y;
            rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, Mathf.Clamp(height + 132f, 220f, 380f));
        }

        // Hiển thị khung tên người nói nếu có tên, ẩn nếu thoại trống tên
        if (speakerBadge != null) speakerBadge.SetActive(!string.IsNullOrEmpty(title));
        if (continuePrompt != null) continuePrompt.SetActive(false);

        // Hiệu ứng Pop-in mềm mại
        if (containerRect != null)
        {
            if (popInRoutine != null) StopCoroutine(popInRoutine);
            popInRoutine = StartCoroutine(AnimatePopIn());
        }

        StartCoroutine(Present(displayDuration));
    }
    public void ShowText(string text, float duration = 3f) => ShowDialogue("", text, duration);
    public void AddOnClosed(Action callback) => onClosed += callback;
    private IEnumerator Present(float duration)
    {
        int count = bodyText != null ? bodyText.textInfo.characterCount : 0;
        for (int i = 0; i <= count; i++)
        {
            while (GameManager.Instance != null && GameManager.Instance.IsPaused) yield return null;
            if (bodyText != null) bodyText.maxVisibleCharacters = i;
            if (Input.GetKeyDown(KeyCode.Space))
            {
                if (bodyText != null) bodyText.maxVisibleCharacters = int.MaxValue;
                break;
            }
            yield return new WaitForSecondsRealtime(Mathf.Max(0.001f, typewriterSpeed));
        }

        // Hiện nút/chỉ báo [Space Tiếp tục] nhấp nháy khi chạy xong chữ
        if (continuePrompt != null)
        {
            continuePrompt.SetActive(true);
            if (pulseRoutine != null) StopCoroutine(pulseRoutine);
            pulseRoutine = StartCoroutine(PulseContinuePrompt());
        }

        yield return null;
        float elapsed = 0;
        while (duration <= 0 || elapsed < duration)
        {
            if (GameManager.Instance != null && GameManager.Instance.IsPaused) { yield return null; continue; }
            if (Input.GetKeyDown(KeyCode.Space)) break;
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
        HideDialogue();
    }
    public void HideDialogue()
    {
        StopAllCoroutines();
        if (continuePrompt != null) continuePrompt.SetActive(false);
        if (panel != null) panel.SetActive(false);
        IsShowing = false;
        GameManager.Instance?.ReleaseInput(this);
        Action callback = onClosed;
        onClosed = null;
        callback?.Invoke();
    }

    private IEnumerator AnimatePopIn()
    {
        float elapsed = 0f;
        float duration = 0.16f;
        Vector3 startScale = new Vector3(0.96f, 0.96f, 1f);
        Vector3 endScale = Vector3.one;
        containerRect.localScale = startScale;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float ease = Mathf.Sin(t * Mathf.PI * 0.5f);
            containerRect.localScale = Vector3.Lerp(startScale, endScale, ease);
            yield return null;
        }
        containerRect.localScale = endScale;
    }

    private IEnumerator PulseContinuePrompt()
    {
        var cg = continuePrompt.GetComponent<CanvasGroup>();
        if (cg == null) cg = continuePrompt.AddComponent<CanvasGroup>();
        while (true)
        {
            float wave = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 3f);
            cg.alpha = Mathf.Lerp(0.65f, 1f, wave);
            yield return null;
        }
    }

    private void ConfigurePresentation()
    {
        if (styled || !panel) return;
        styled = true;
        GameUiPresentation.ConfigureCanvas(panel.GetComponentInParent<Canvas>(true));
        var rect = panel.transform as RectTransform;
        if (rect)
        {
            rect.anchorMin = new Vector2(.12f, 0); rect.anchorMax = new Vector2(.88f, 0);
            rect.pivot = new Vector2(.5f, 0);
            rect.anchoredPosition = new Vector2(0, 48);
            rect.sizeDelta = new Vector2(0, 240);
            rect.localScale = Vector3.one;
        }
        GameUiPresentation.Card(panel.GetComponent<UnityEngine.UI.Image>());
        if (speakerBadge && speakerBadge.transform is RectTransform badge)
        {
            badge.anchorMin = badge.anchorMax = badge.pivot = new Vector2(0, 1);
            badge.anchoredPosition = new Vector2(26, -15);
            badge.sizeDelta = new Vector2(300, 42);
            GameUiPresentation.Card(speakerBadge.GetComponent<UnityEngine.UI.Image>());
        }
        if (titleText)
        {
            GameUiPresentation.Text(titleText, 24, GameUiPresentation.Gold, false);
            titleText.alignment = TextAlignmentOptions.Left;
            if (speakerBadge) GameUiPresentation.Stretch(titleText.rectTransform, 12, 3);
            else
            {
                OfficeUiStyle.Rect(titleText.rectTransform, new Vector2(0, 1), new Vector2(500, 40), new Vector2(280, -34));
            }
        }
        if (bodyText)
        {
            GameUiPresentation.Text(bodyText, 30, GameUiPresentation.Paper);
            bodyText.alignment = TextAlignmentOptions.TopLeft;
            bodyText.rectTransform.anchorMin = Vector2.zero; bodyText.rectTransform.anchorMax = Vector2.one;
            bodyText.rectTransform.offsetMin = new Vector2(32, 49);
            bodyText.rectTransform.offsetMax = new Vector2(-32, -66);
        }
        foreach (var legacy in panel.GetComponentsInChildren<TMP_Text>(true))
        {
            if (legacy == titleText || legacy == bodyText || string.IsNullOrEmpty(legacy.text) || !legacy.text.TrimStart().StartsWith("Space")) continue;
            if (!continuePrompt) continuePrompt = legacy.gameObject;
            else if (legacy.gameObject != continuePrompt && !legacy.transform.IsChildOf(continuePrompt.transform)) legacy.gameObject.SetActive(false);
        }
        if (!continuePrompt)
        {
            var label = OfficeUiStyle.Label("DialogueContinue", panel.transform, GameUiTheme.Current ? GameUiTheme.Current.font : null,
                "SPACE  Tiếp tục", 20, new Vector2(300, 30), Vector2.zero, GameUiPresentation.Muted);
            continuePrompt = label.gameObject;
        }
        if (continuePrompt.transform is RectTransform promptRect)
        {
            promptRect.anchorMin = promptRect.anchorMax = promptRect.pivot = new Vector2(1, 0);
            promptRect.anchoredPosition = new Vector2(-30, 15);
            promptRect.sizeDelta = new Vector2(340, 30);
        }
        foreach (var label in continuePrompt.GetComponentsInChildren<TMP_Text>(true))
        {
            GameUiPresentation.Text(label, 20, GameUiPresentation.Muted, false);
            label.text = "SPACE  Tiếp tục";
            label.alignment = TextAlignmentOptions.Right;
        }
    }
}
