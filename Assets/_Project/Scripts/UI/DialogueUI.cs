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

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
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
    private IEnumerator Present(float duration)
    {
        int count = bodyText != null ? bodyText.textInfo.characterCount : 0;
        for (int i = 0; i <= count; i++)
        {
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
            float wave = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 5f);
            cg.alpha = Mathf.Lerp(0.35f, 1f, wave);
            yield return null;
        }
    }
}
