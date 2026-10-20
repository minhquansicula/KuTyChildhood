using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Runs before mini-game Escape handling, preventing Escape from opening two panels.
[DefaultExecutionOrder(-100)]
public class PauseUI : MonoBehaviour
{
    public static int LastResumeFrame { get; private set; } = -1;
    [SerializeField] private GameObject panel;
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button menuButton;
    private GameObject modalRoot;
    private void Start()
    {
        if (panel != null) panel.SetActive(false);
        StyleModal();
        if (resumeButton != null) resumeButton.onClick.AddListener(Resume);
        if (menuButton != null) menuButton.onClick.AddListener(() => GameManager.Instance?.ReturnToMainMenu());
    }
    private void Update()
    {
        var game = GameManager.Instance;
        if (game == null || !Input.GetKeyDown(KeyCode.Escape)) return;
        if (game.IsPaused) Resume();
        else if (!game.InputBlocked || CanPauseFromBed())
        {
            Pause();
        }
    }
    private static bool CanPauseFromBed()
    {
        var bed = FindObjectOfType<BedWakeUpController>();
        return bed != null && bed.CanPauseWhileWaiting;
    }
    public void Pause()
    {
        if (modalRoot != null) modalRoot.SetActive(true);
        if (panel != null) panel.SetActive(true);
        GameManager.Instance?.SetPaused(true);
    }
    public void Resume()
    {
        if (panel != null) panel.SetActive(false);
        if (modalRoot != null) modalRoot.SetActive(false);
        LastResumeFrame = Time.frameCount;
        GameManager.Instance?.SetPaused(false);
    }

    private void StyleModal()
    {
        if (panel == null) return;
        var parentCanvas = panel.GetComponentInParent<Canvas>(true);
        if (parentCanvas == null) return;
        modalRoot = new GameObject("PauseModalCanvas", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster));
        modalRoot.transform.SetParent(parentCanvas.transform, false);
        var rootRect = (RectTransform)modalRoot.transform;
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.offsetMin = rootRect.offsetMax = Vector2.zero;
        var canvas = modalRoot.GetComponent<Canvas>();
        canvas.overrideSorting = true;
        canvas.sortingOrder = 120;

        var backdrop = new GameObject("PauseDimmer", typeof(RectTransform), typeof(Image));
        backdrop.transform.SetParent(modalRoot.transform, false);
        var backdropRect = (RectTransform)backdrop.transform;
        backdropRect.anchorMin = Vector2.zero;
        backdropRect.anchorMax = Vector2.one;
        backdropRect.offsetMin = backdropRect.offsetMax = Vector2.zero;
        backdrop.GetComponent<Image>().color = new Color(.015f, .025f, .035f, .72f);

        panel.transform.SetParent(modalRoot.transform, false);
        // Some scenes already have a full-screen dimmer with a nested PauseCard.
        // Keep that layout; only turn the older compact panel into a card.
        var authoredCard = panel.transform.Find("PauseCard");
        var card = authoredCard != null ? authoredCard.gameObject : panel;
        if (authoredCard != null)
        {
            var oldDimmer = panel.GetComponent<Image>();
            if (oldDimmer != null) oldDimmer.color = Color.clear;
        }
        var rect = card.transform as RectTransform;
        if (authoredCard == null && rect != null) rect.sizeDelta = new Vector2(460f, 280f);
        var image = card.GetComponent<Image>();
        if (image != null) image.color = new Color(.055f, .10f, .13f, .98f);
        foreach (Transform child in card.transform)
        {
            var title = child.GetComponent<TextMeshProUGUI>();
            if (title == null) continue;
            title.text = "TẠM DỪNG";
            title.fontSize = 32f;
            title.fontStyle = FontStyles.Bold;
            title.color = new Color(.97f, .79f, .42f);
        }
        StyleButton(resumeButton, "Tiếp tục", new Color(.12f, .38f, .30f));
        StyleButton(menuButton, "Về menu chính", new Color(.16f, .25f, .32f));
        modalRoot.SetActive(false);
    }

    private static void StyleButton(Button button, string label, Color color)
    {
        if (button == null) return;
        var image = button.GetComponent<Image>();
        if (image != null) image.color = color;
        var text = button.GetComponentInChildren<TextMeshProUGUI>(true);
        if (text == null) return;
        text.text = label;
        text.fontSize = 21f;
        text.fontStyle = FontStyles.Bold;
        text.color = new Color(.96f, .97f, .92f);
    }
}
