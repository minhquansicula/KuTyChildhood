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
    public bool IsShowing { get; private set; }
    private Action onClosed;
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        if (panel != null) panel.SetActive(false);
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
        if (panel != null) panel.SetActive(false);
        IsShowing = false;
        GameManager.Instance?.ReleaseInput(this);
        Action callback = onClosed;
        onClosed = null;
        callback?.Invoke();
    }
}
