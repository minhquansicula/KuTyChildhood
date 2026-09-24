using System.Collections;
using TMPro;
using UnityEngine;

public enum OfficePhase { ReadScreen, InspectDocuments, RewriteReport, Rest, Leaving }
public enum OfficeInteractionKind { Laptop, Documents, Chair, Window, Boss }

// Act 1 only. Keeps exploration free while the boss speaks; input is locked only while typing/collapsing.
public class OfficeSceneController : MonoBehaviour
{
    public static OfficeSceneController Instance { get; private set; }
    [SerializeField] private TextMeshProUGUI objectiveText;
    [SerializeField] private TextMeshProUGUI subtitleText;
    [SerializeField] private GameObject subtitlePanel;
    [SerializeField] private TextMeshPro screenWarning;
    [SerializeField] private OfficeAtmosphere atmosphere;
    [SerializeField] private Transform playerTransform;
    [SerializeField] private Transform chairAnchor;
    [SerializeField] private float chairUseDistance = 1.5f;
    [SerializeField] private float workSeconds = 4f;
    [SerializeField] private float bossReminderSeconds = 19f;
    [SerializeField] private Color dreamFade = new Color(1f, .84f, .47f);
    public OfficePhase Phase { get; private set; } = OfficePhase.ReadScreen;
    public bool IsWorking { get; private set; }
    public float WorkProgress { get; private set; }
    public string ObjectiveText { get; private set; }
    private Coroutine subtitleRoutine;
    private float reminderTimer;
    private float typingTimer;
    private bool nearChair;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        if (subtitlePanel != null) subtitlePanel.SetActive(false);
    }
    private void Start()
    {
        SetPhase(OfficePhase.ReadScreen);
        StartCoroutine(Opening());
    }
    private IEnumerator Opening()
    {
        yield return new WaitForSecondsRealtime(1.2f);
        if (Phase == OfficePhase.ReadScreen)
        {
            atmosphere?.PlayBoss(false);
            ShowSubtitle("SẾP", "Báo cáo làm thế này à? Cậu có biết dùng não không? Làm lại ngay!", 5f);
        }
    }
    private void Update()
    {
        if (IsWorking)
        {
            if (Input.GetKeyDown(KeyCode.Escape)) { CancelWork(); return; }
            if (Input.GetMouseButton(0)) WorkFor(Time.deltaTime);
            return;
        }
        if (Phase == OfficePhase.Rest)
        {
            UpdateChairInteraction();
            return;
        }
        if (Phase == OfficePhase.Leaving || GameManager.Instance?.IsPaused == true) return;
        reminderTimer += Time.deltaTime;
        if (reminderTimer >= bossReminderSeconds)
        {
            reminderTimer = 0;
            atmosphere?.PlayBoss(true);
            ShowSubtitle("SẾP", "Tôi cần bản sửa trước khi mọi người về hết!", 3.5f);
        }
    }
    private void UpdateChairInteraction()
    {
        if (GameManager.Instance?.InputBlocked == true || playerTransform == null || chairAnchor == null) return;
        Vector3 offset = playerTransform.position - chairAnchor.position;
        offset.y = 0;
        bool inRange = offset.sqrMagnitude <= chairUseDistance * chairUseDistance;
        if (inRange != nearChair)
        {
            nearChair = inRange;
            if (objectiveText != null)
                objectiveText.text = inRange
                    ? "CÔNG VIỆC: [E] Ngồi xuống nghỉ một lát"
                    : "CÔNG VIỆC: " + ObjectiveText;
        }
        // The chair has no demo mesh; its action remains usable by proximity until art is added.
        if (inRange && Input.GetKeyDown(KeyCode.E)) HandleInteraction(OfficeInteractionKind.Chair);
    }
    public string PromptFor(OfficeInteractionKind kind)
    {
        if (IsWorking || Phase == OfficePhase.Leaving) return "";
        switch (kind)
        {
            case OfficeInteractionKind.Laptop:
                return Phase == OfficePhase.ReadScreen ? "[E] Đọc thông báo trên màn hình" :
                    Phase == OfficePhase.RewriteReport ? "[E] Làm lại báo cáo" : "";
            case OfficeInteractionKind.Documents: return Phase == OfficePhase.InspectDocuments ? "[E] Kiểm tra chồng hồ sơ" : "";
            case OfficeInteractionKind.Chair: return Phase == OfficePhase.Rest ? "[E] Ngồi xuống nghỉ một lát" : "";
            case OfficeInteractionKind.Window: return "[E] Nhìn ra phố";
            case OfficeInteractionKind.Boss: return "[E] Nhìn về phía sếp";
            default: return "";
        }
    }
    public void HandleInteraction(OfficeInteractionKind kind)
    {
        if (string.IsNullOrEmpty(PromptFor(kind))) return;
        reminderTimer = 0;
        switch (kind)
        {
            case OfficeInteractionKind.Laptop:
                if (Phase == OfficePhase.ReadScreen)
                {
                    ShowSubtitle("MÀN HÌNH", "BÁO CÁO BỊ TRẢ VỀ — LÀM LẠI NGAY.", 3.8f);
                    SetPhase(OfficePhase.InspectDocuments);
                }
                else if (Phase == OfficePhase.RewriteReport) StartWork();
                break;
            case OfficeInteractionKind.Documents:
                ShowSubtitle("HỒ SƠ", "Những trang giấy chồng lên nhau. Mình phải sửa lại từng con số.", 4f);
                SetPhase(OfficePhase.RewriteReport);
                break;
            case OfficeInteractionKind.Chair:
                SetPhase(OfficePhase.Leaving);
                GameManager.Instance?.AcquireInput(this);
                ShowSubtitle("QUÂN", "Tiếng còi xe xa dần. Mình chỉ muốn được nghỉ một lát...", 3f);
                StartCoroutine(LeaveOffice());
                break;
            case OfficeInteractionKind.Window:
                ShowSubtitle("CỬA SỔ", "Ngoài kia, dòng người vẫn vội vã. Tiếng còi xe chẳng lúc nào ngớt.", 4f);
                break;
            case OfficeInteractionKind.Boss:
                ShowSubtitle("SẾP", "Ánh mắt ông ấy vẫn đang chờ bản báo cáo mới.", 3f);
                break;
        }
    }
    private void SetPhase(OfficePhase next)
    {
        Phase = next;
        switch (next)
        {
            case OfficePhase.ReadScreen: ObjectiveText = "Nhìn vào bàn làm việc của mình và nhấn E."; break;
            case OfficePhase.InspectDocuments: ObjectiveText = "Kiểm tra hồ sơ ngay trên bàn của mình."; break;
            case OfficePhase.RewriteReport: ObjectiveText = "Tương tác với bàn để sửa báo cáo."; break;
            case OfficePhase.Rest: ObjectiveText = "Đến vị trí ghế bên phải, nhấn E để nghỉ."; nearChair = false; break;
            default: ObjectiveText = "..."; break;
        }
        if (objectiveText != null) objectiveText.text = "CÔNG VIỆC: " + ObjectiveText;
        if (screenWarning != null)
            screenWarning.text = next >= OfficePhase.Rest ? "ĐÃ GỬI\n23:47" : "BÁO CÁO BỊ TRẢ VỀ\nLÀM LẠI NGAY";
    }
    private void StartWork()
    {
        if (IsWorking) return;
        IsWorking = true;
        GameManager.Instance?.AcquireInput(this);
        ProgressBarUI.Instance?.Show("Giữ chuột trái để sửa báo cáo · Esc: dừng");
        ProgressBarUI.Instance?.SetProgress(WorkProgress);
    }
    public void WorkFor(float seconds)
    {
        if (!IsWorking || Phase != OfficePhase.RewriteReport || seconds <= 0 || GameManager.Instance?.IsPaused == true) return;
        WorkProgress = Mathf.Clamp01(WorkProgress + seconds / Mathf.Max(.1f, workSeconds));
        ProgressBarUI.Instance?.SetProgress(WorkProgress);
        typingTimer += seconds;
        if (typingTimer >= .13f) { atmosphere?.PlayTyping(); typingTimer = 0; }
        if (WorkProgress < 1f) return;
        CancelWork();
        SetPhase(OfficePhase.Rest);
        atmosphere?.PlayBoss(true);
        ShowSubtitle("SẾP", "Cuối cùng cũng xong. Lần sau tập trung hơn đi!", 4f);
    }
    public void CancelWork()
    {
        IsWorking = false;
        ProgressBarUI.Instance?.Hide();
        GameManager.Instance?.ReleaseInput(this);
    }
    private IEnumerator LeaveOffice()
    {
        yield return new WaitForSecondsRealtime(3f);
        if (SceneLoader.Instance != null) SceneLoader.Instance.LoadSceneWithColor(SceneNames.Act2, dreamFade, 1.8f);
        GameManager.Instance?.ReleaseInput(this);
    }
    private void ShowSubtitle(string speaker, string line, float seconds)
    {
        if (subtitleRoutine != null) StopCoroutine(subtitleRoutine);
        subtitleRoutine = StartCoroutine(Subtitle(speaker, line, seconds));
    }
    private IEnumerator Subtitle(string speaker, string line, float seconds)
    {
        if (subtitleText != null) subtitleText.text = "<b>" + speaker + ":</b> " + line;
        if (subtitlePanel != null) subtitlePanel.SetActive(true);
        yield return new WaitForSecondsRealtime(seconds);
        if (subtitlePanel != null) subtitlePanel.SetActive(false);
        subtitleRoutine = null;
    }
    private void OnDestroy()
    {
        GameManager.Instance?.ReleaseInput(this);
        if (Instance == this) Instance = null;
    }
}
