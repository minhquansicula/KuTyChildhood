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
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        if (panel != null) panel.SetActive(false);
    }
    private void Start()
    {
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
    private void Update() { if (IsOpen && Input.GetKeyDown(KeyCode.Escape)) Close(); }
    private void QuestChanged(QuestStep step) => Refresh();
    private void MemoryCollected(MemoryType type, int count) => Refresh();
    public void Open()
    {
        if (panel == null) return;
        IsOpen = true;
        panel.SetActive(true);
        GameManager.Instance?.AcquireInput(this);
        Refresh();
    }
    public void Close()
    {
        IsOpen = false;
        if (panel != null) panel.SetActive(false);
        GameManager.Instance?.ReleaseInput(this);
    }
    public void Refresh()
    {
        if (bodyText == null) return;
        var text = new StringBuilder();
        text.AppendLine("NHẬT KÝ CỦA QUÂN");
        text.AppendLine();
        text.Append("Hôm nay: ").AppendLine(quests != null ? quests.ObjectiveText : "...");
        text.AppendLine();
        for (int i = 0; i < MemoryCollectionManager.TOTAL_MEMORIES; i++)
        {
            var type = (MemoryType)i;
            bool found = memories != null && memories.HasCollected(type);
            text.Append(found ? "★ " : "☆ ").AppendLine(MemoryCollectionManager.GetMemoryName(type));
            if (found) text.Append("    ").AppendLine(MemoryCollectionManager.GetMemoryDescription(type));
        }
        text.AppendLine();
        if (quests != null && quests.HasDreamKey) text.AppendLine("Chìa Khóa Ước Mơ đang ở trong tay.");
        else if (quests != null && quests.CurrentStep >= QuestStep.TakeDreamKey) text.AppendLine("Ba mảnh ký ức đã hợp thành chìa khóa.");
        bodyText.text = text.ToString();
    }
}
