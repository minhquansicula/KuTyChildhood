using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class JournalUI : MonoBehaviour
{
    public static JournalUI Instance { get; private set; }
    [SerializeField] private GameObject panel;
    [SerializeField] private TextMeshProUGUI bodyText;
    [SerializeField] private Button closeButton;
    public bool IsOpen { get; private set; }
    private QuestManager quests;
    private MemoryCollectionManager memories;
    private GameObject modalRoot;
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        if (panel != null) panel.SetActive(false);
    }
    private void Start()
    {
        StylePresentation();
        quests = QuestManager.Instance;
        memories = MemoryCollectionManager.Instance;
        if (closeButton != null) closeButton.onClick.AddListener(Close);
        if (quests != null) quests.OnQuestChanged += QuestChanged;
        if (memories != null) memories.OnMemoryCollected += MemoryCollected;
        Refresh();
    }
    private void OnDestroy()
    {
        if (closeButton != null) closeButton.onClick.RemoveListener(Close);
        if (quests != null) quests.OnQuestChanged -= QuestChanged;
        if (memories != null) memories.OnMemoryCollected -= MemoryCollected;
        GameManager.Instance?.ReleaseInput(this);
        if (Instance == this) Instance = null;
    }
    private void Update()
    {
        if (IsOpen && PauseUI.LastResumeFrame != Time.frameCount && Input.GetKeyDown(KeyCode.Escape)) Close();
    }
    private void QuestChanged(QuestStep step) => Refresh();
    private void MemoryCollected(MemoryType type, int count) => Refresh();
    public void Open()
    {
        if (panel == null) return;
        IsOpen = true;
        if (modalRoot) modalRoot.SetActive(true);
        panel.SetActive(true);
        GameManager.Instance?.AcquireInput(this);
        Refresh();
    }
    public void Close()
    {
        IsOpen = false;
        if (panel != null) panel.SetActive(false);
        if (modalRoot) modalRoot.SetActive(false);
        GameManager.Instance?.ReleaseInput(this);
    }
    public void Refresh()
    {
        if (bodyText == null) return;
        var text = new StringBuilder();
        text.AppendLine("<size=34><b>Nhật ký của Tý</b></size>");
        text.AppendLine();
        text.Append("<b>Hôm nay</b>\n").AppendLine(quests != null ? quests.ObjectiveText : "Một ngày để nhớ.");
        text.AppendLine();
        for (int i = 0; i < MemoryCollectionManager.TOTAL_MEMORIES; i++)
        {
            var type = (MemoryType)i;
            bool found = memories != null && memories.HasCollected(type);
            text.Append(found ? "<b>Đã tìm thấy · " : "<b>Chưa tìm thấy · ").Append(MemoryCollectionManager.GetMemoryName(type)).AppendLine("</b>");
            if (found) text.Append("    ").AppendLine(MemoryCollectionManager.GetMemoryDescription(type));
        }
        text.AppendLine();
        if (quests != null && quests.HasDreamKey) text.AppendLine("Chìa Khóa Ước Mơ đang ở trong tay.");
        else if (quests != null && quests.CurrentStep >= QuestStep.TakeDreamKey) text.AppendLine("Ba mảnh ký ức đã hợp thành chìa khóa.");
        bodyText.text = text.ToString();
    }

    private void StylePresentation()
    {
        if (!panel) return;
        modalRoot = GameUiPresentation.ModalRoot(panel, "JournalModalCanvas", 95);
        if (panel.transform is RectTransform rect)
            OfficeUiStyle.Rect(rect, Vector2.one * .5f, new Vector2(980, 760), Vector2.zero);
        GameUiPresentation.Card(panel.GetComponent<Image>(), true);
        if (bodyText)
        {
            bodyText.rectTransform.anchorMin = Vector2.zero; bodyText.rectTransform.anchorMax = Vector2.one;
            bodyText.rectTransform.offsetMin = new Vector2(70, 125);
            bodyText.rectTransform.offsetMax = new Vector2(-70, -55);
            GameUiPresentation.Text(bodyText, 26, GameUiTheme.Current ? GameUiTheme.Current.text : Color.black);
            bodyText.alignment = TextAlignmentOptions.TopLeft;
            bodyText.enableAutoSizing = true; bodyText.fontSizeMin = 23;
        }
        if (closeButton)
        {
            OfficeUiStyle.Rect((RectTransform)closeButton.transform, new Vector2(.5f, 0), new Vector2(380, 78), new Vector2(0, 66));
            var caption = closeButton.GetComponentInChildren<TMP_Text>(true);
            if (caption) caption.text = "Đóng nhật ký · Esc";
            GameUiPresentation.Button(closeButton);
        }
        if (modalRoot) modalRoot.SetActive(false);
    }
}
