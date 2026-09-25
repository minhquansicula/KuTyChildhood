using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum OfficeWorkTask { Emails, Documents, Report }

/// <summary>
/// Three-part work sequence described by the Scene 1 design document:
/// email triage, document sorting, then report correction and submission.
/// Each part can be closed and resumed without losing progress.
/// </summary>
public sealed class OfficeReportMiniGame : MonoBehaviour
{
    private const int TotalTasks = 3;

    [Header("Panel")]
    [SerializeField] private GameObject panel;
    [SerializeField] private TextMeshProUGUI stepText;
    [SerializeField] private TextMeshProUGUI taskTitleText;
    [SerializeField] private TextMeshProUGUI instructionText;
    [SerializeField] private TextMeshProUGUI documentText;
    [SerializeField] private TextMeshProUGUI feedbackText;
    [SerializeField] private TextMeshProUGUI fileNameText;
    [SerializeField] private Image progressFill;
    [SerializeField] private Button closeButton;

    [Header("Choices")]
    [SerializeField] private Button[] optionButtons = new Button[4];
    [SerializeField] private TextMeshProUGUI[] optionLabels = new TextMeshProUGUI[4];

    [Header("Theme")]
    [SerializeField] private Color normalOptionColor = new Color(0.105f, 0.145f, 0.215f, 1f);
    [SerializeField] private Color selectedOptionColor = new Color(0.12f, 0.34f, 0.28f, 1f);
    [SerializeField] private Color errorOptionColor = new Color(0.48f, 0.13f, 0.16f, 1f);
    [SerializeField] private Color successTextColor = new Color(0.42f, 0.92f, 0.64f, 1f);
    [SerializeField] private Color errorTextColor = new Color(1f, 0.43f, 0.43f, 1f);
    [SerializeField] private Color hintTextColor = new Color(0.72f, 0.78f, 0.88f, 1f);

    public bool IsOpen { get; private set; }
    public OfficeWorkTask CurrentTask { get; private set; }
    public int ItemIndex => CurrentItemIndex();
    public int CurrentStep => (int)CurrentTask;
    public float Progress => ((int)CurrentTask + LocalProgress()) / TotalTasks;

    private readonly int[] itemIndices = new int[TotalTasks];
    private readonly bool[] finalChecklist = new bool[3];
    private OfficeSceneController owner;
    private bool transitioning;

    private void Awake()
    {
        if (panel != null) panel.SetActive(false);
    }

    private void Start()
    {
        if (closeButton != null) closeButton.onClick.AddListener(RequestClose);
        for (int i = 0; i < optionButtons.Length; i++)
        {
            int index = i;
            if (optionButtons[i] != null) optionButtons[i].onClick.AddListener(() => ChooseOption(index));
        }
    }

    private void OnDestroy()
    {
        if (closeButton != null) closeButton.onClick.RemoveListener(RequestClose);
        for (int i = 0; i < optionButtons.Length; i++)
            if (optionButtons[i] != null) optionButtons[i].onClick.RemoveAllListeners();
    }

    public bool Open(OfficeSceneController controller, OfficeWorkTask task)
    {
        if (panel == null || optionButtons.Length < 4 || optionLabels.Length < 4)
        {
            Debug.LogError("Office work mini-game UI is not fully configured.", this);
            return false;
        }

        owner = controller;
        CurrentTask = task;
        IsOpen = true;
        transitioning = false;
        panel.SetActive(true);
        panel.transform.SetAsLastSibling();
        RefreshTask();
        return true;
    }

    // Preview entry point used by the Editor menu.
    public bool Open(OfficeSceneController controller) => Open(controller, OfficeWorkTask.Emails);

    public void ClosePanel()
    {
        StopAllCoroutines();
        transitioning = false;
        IsOpen = false;
        if (panel != null) panel.SetActive(false);
    }

    private void RequestClose() => owner?.CancelWork();

    public void ChooseOption(int index)
    {
        if (!IsOpen || transitioning || index < 0 || index >= optionButtons.Length) return;

        switch (CurrentTask)
        {
            case OfficeWorkTask.Emails: HandleEmail(index); break;
            case OfficeWorkTask.Documents: HandleDocument(index); break;
            case OfficeWorkTask.Report: HandleReport(index); break;
        }
    }

    private void HandleEmail(int selected)
    {
        int[] correct = { 1, 2, 0 };
        string[] success =
        {
            "Đã đánh dấu ưu tiên và mở file dữ liệu tháng 8.",
            "Đã đính kèm lại file và phản hồi khách hàng rõ ràng.",
            "Đã xác nhận lịch họp mới với HR."
        };
        CheckAnswer(selected, correct[itemIndices[0]], success[itemIndices[0]], 3);
    }

    private void HandleDocument(int selected)
    {
        int[] correct = { 0, 1, 2, 2 };
        string[] success =
        {
            "Báo cáo đã được đặt vào khay REPORT.",
            "Hóa đơn đã được đặt vào khay INVOICE.",
            "Biên bản họp đã được lưu vào ARCHIVE.",
            "Hợp đồng cũ đã được lưu vào ARCHIVE."
        };
        CheckAnswer(selected, correct[itemIndices[1]], success[itemIndices[1]], 4);
    }

    private void HandleReport(int selected)
    {
        int item = itemIndices[2];
        if (item < 3)
        {
            int[] correct = { 2, 1, 3 };
            string[] success =
            {
                "Đúng. Doanh thu tháng 8 là 128.",
                "Đúng. Tổng số đơn hàng là 46.",
                "Đúng. Có 7 khiếu nại trong tháng 8."
            };
            CheckAnswer(selected, correct[item], success[item], 4);
            return;
        }

        if (selected < finalChecklist.Length)
        {
            finalChecklist[selected] = !finalChecklist[selected];
            owner?.PlayReportTypingFeedback();
            RefreshReportChecklist();
            return;
        }

        if (!ChecklistComplete)
        {
            SetFeedback("Cần xác nhận đủ ba mục trước khi gửi.", errorTextColor);
            return;
        }

        transitioning = true;
        SetFeedback("SENDING REPORT...", successTextColor);
        if (progressFill != null) progressFill.fillAmount = 1f;
        owner?.UpdateReportMiniGameProgress(1f);
        StartCoroutine(CompleteTaskAfterDelay(0.9f));
    }

    private void CheckAnswer(int selected, int correct, string success, int itemCount)
    {
        if (selected != correct)
        {
            SetFeedback("Chưa đúng — hãy đọc lại nội dung bên trái.", errorTextColor);
            SetOptionColor(selected, errorOptionColor);
            return;
        }

        SetOptionColor(selected, selectedOptionColor);
        SetFeedback(success, successTextColor);
        owner?.PlayReportTypingFeedback();
        transitioning = true;
        StartCoroutine(AdvanceAfterFeedback(itemCount));
    }

    private IEnumerator AdvanceAfterFeedback(int itemCount)
    {
        yield return new WaitForSecondsRealtime(0.55f);
        int taskIndex = (int)CurrentTask;
        itemIndices[taskIndex]++;
        owner?.UpdateReportMiniGameProgress(Progress);
        transitioning = false;

        if (CurrentTask != OfficeWorkTask.Report && itemIndices[taskIndex] >= itemCount)
        {
            yield return CompleteTaskAfterDelay(0.25f);
            yield break;
        }

        RefreshTask();
    }

    private IEnumerator CompleteTaskAfterDelay(float delay)
    {
        if (delay > 0f) yield return new WaitForSecondsRealtime(delay);
        OfficeWorkTask completed = CurrentTask;
        ClosePanel();
        owner?.CompleteWorkTask(completed);
    }

    private int CurrentItemIndex() => itemIndices[Mathf.Clamp((int)CurrentTask, 0, TotalTasks - 1)];

    private float LocalProgress()
    {
        switch (CurrentTask)
        {
            case OfficeWorkTask.Emails: return Mathf.Clamp01(itemIndices[0] / 3f);
            case OfficeWorkTask.Documents: return Mathf.Clamp01(itemIndices[1] / 4f);
            default: return Mathf.Clamp01(itemIndices[2] / 4f);
        }
    }

    private bool ChecklistComplete => finalChecklist[0] && finalChecklist[1] && finalChecklist[2];

    private void RefreshTask()
    {
        ResetOptions();
        if (progressFill != null) progressFill.fillAmount = Progress;
        switch (CurrentTask)
        {
            case OfficeWorkTask.Emails: ShowEmail(); break;
            case OfficeWorkTask.Documents: ShowDocumentSort(); break;
            case OfficeWorkTask.Report: ShowReport(); break;
        }
    }

    private void ShowEmail()
    {
        int item = Mathf.Clamp(itemIndices[0], 0, 2);
        string[] senders = { "MANAGER", "CLIENT – AN PHÚ", "HR" };
        string[] subjects = { "Báo cáo doanh thu", "Thiếu file đính kèm", "Đổi giờ họp" };
        string[] bodies =
        {
            "File hiện tại thiếu dữ liệu tháng 8.\nCập nhật và gửi lại trước 18:00.",
            "Chào Quân, email trước chưa có tài liệu đính kèm.\nVui lòng gửi lại giúp chúng tôi.",
            "Lịch họp ngày mai đổi từ 09:00 sang 08:30.\nVui lòng xác nhận đã nhận thông tin."
        };

        SetHeader("INBOX · 3 EMAIL KHẨN", "XỬ LÝ EMAIL",
            "Chọn hành động phù hợp và chuyên nghiệp nhất.");
        if (stepText != null) stepText.text = $"EMAIL {item + 1} / 3";
        if (documentText != null)
            documentText.text = $"<color=#8EA6C8>FROM:</color> {senders[item]}\n" +
                                $"<color=#8EA6C8>SUBJECT:</color> {subjects[item]}\n\n{bodies[item]}";

        if (item == 0)
        {
            SetOption(0, "Để lại xử lý sau", true);
            SetOption(1, "Đánh dấu ưu tiên và kiểm tra file dữ liệu", true);
            SetOption(2, "Trả lời rằng báo cáo không thuộc trách nhiệm", true);
            SetOption(3, "Chuyển tiếp cho toàn bộ công ty", true);
        }
        else if (item == 1)
        {
            SetOption(0, "Gửi lại email cũ không kèm file", true);
            SetOption(1, "Không phản hồi", true);
            SetOption(2, "Xin lỗi và gửi lại đúng file đính kèm", true);
            SetOption(3, "Yêu cầu khách hàng tự tìm file", true);
        }
        else
        {
            SetOption(0, "Xác nhận đã nhận lịch họp mới", true);
            SetOption(1, "Xóa email", true);
            SetOption(2, "Giữ lịch cũ", true);
            SetOption(3, "Chuyển lịch sang tuần sau", true);
        }
        SetFeedback("Công việc vừa xong thì email khác lại tới.", hintTextColor);
    }

    private void ShowDocumentSort()
    {
        int item = Mathf.Clamp(itemIndices[1], 0, 3);
        string[] names = { "REPORT_Q3_DRAFT", "INVOICE_0918", "MEETING_NOTES", "CLIENT_CONTRACT_OLD" };
        string[] descriptions =
        {
            "Bản nháp báo cáo kết quả quý III.",
            "Hóa đơn dịch vụ tháng 9 – chờ đối chiếu.",
            "Biên bản cuộc họp tuần trước – đã hoàn tất.",
            "Hợp đồng khách hàng phiên bản cũ – chỉ dùng lưu trữ."
        };
        SetHeader("KHAY TÀI LIỆU TRÊN BÀN", "SẮP XẾP GIẤY TỜ",
            "Đặt từng tài liệu vào đúng khay.");
        if (stepText != null) stepText.text = $"TÀI LIỆU {item + 1} / 4";
        if (documentText != null)
            documentText.text = $"<b>{names[item]}</b>\n\n{descriptions[item]}\n\n" +
                                "<color=#8EA6C8>Điện thoại rung. Máy in phía xa lại bắt đầu chạy...</color>";
        SetOption(0, "KHAY REPORT", true);
        SetOption(1, "KHAY INVOICE", true);
        SetOption(2, "KHAY ARCHIVE", true);
        SetOption(3, string.Empty, false);
        SetFeedback("Chọn đúng khay cho tài liệu đang cầm.", hintTextColor);
    }

    private void ShowReport()
    {
        int item = itemIndices[2];
        SetHeader("REPORT_Q3_FINAL.xlsx", "SỬA BÁO CÁO CHO SẾP",
            item < 3 ? "Đối chiếu tài liệu và chọn số liệu đúng." : "Kiểm tra lần cuối trước khi gửi.");

        if (item >= 3)
        {
            if (stepText != null) stepText.text = "KIỂM TRA CUỐI";
            if (documentText != null)
                documentText.text = "<b>BÁO CÁO QUÝ III</b>\n\n" +
                                    "June Revenue                 120\n" +
                                    "July Revenue                  135\n" +
                                    "August Revenue             <color=#62D89A>128</color>\n" +
                                    "September Revenue          142\n\n" +
                                    "Orders                            <color=#62D89A>46</color>\n" +
                                    "Complaints                       <color=#62D89A>7</color>";
            RefreshReportChecklist();
            return;
        }

        string[] fieldNames = { "AUGUST REVENUE", "ORDERS", "COMPLAINTS" };
        string[] sourceLines =
        {
            "TÀI LIỆU NGUỒN\nAugust Revenue: 128\nOrders: 46\nComplaints: 7",
            "TÀI LIỆU NGUỒN\nAugust Revenue: 128\nOrders: 46\nComplaints: 7",
            "TÀI LIỆU NGUỒN\nAugust Revenue: 128\nOrders: 46\nComplaints: 7"
        };
        if (stepText != null) stepText.text = $"TRƯỜNG {item + 1} / 3";
        if (taskTitleText != null) taskTitleText.text = "ĐIỀN " + fieldNames[item];
        if (documentText != null)
            documentText.text = "<b>BÁO CÁO ĐANG SỬA</b>\n\n" +
                                "June Revenue                 120\n" +
                                "July Revenue                  135\n" +
                                "August Revenue             <color=#FF8B78>???</color>\n" +
                                "September Revenue          142\n\n" +
                                $"<color=#8EA6C8>{sourceLines[item]}</color>";

        if (item == 0)
        {
            SetOption(0, "118", true); SetOption(1, "125", true);
            SetOption(2, "128", true); SetOption(3, "138", true);
        }
        else if (item == 1)
        {
            SetOption(0, "42", true); SetOption(1, "46", true);
            SetOption(2, "64", true); SetOption(3, "76", true);
        }
        else
        {
            SetOption(0, "3", true); SetOption(1, "4", true);
            SetOption(2, "6", true); SetOption(3, "7", true);
        }
        SetFeedback("Sếp đang chờ. Hãy đối chiếu cẩn thận.", hintTextColor);
    }

    private void RefreshReportChecklist()
    {
        string[] labels = { "Số liệu đã đối chiếu", "File đính kèm đúng", "Tên file đúng phiên bản" };
        for (int i = 0; i < finalChecklist.Length; i++)
        {
            SetOption(i, (finalChecklist[i] ? "✓  " : "□  ") + labels[i], true);
            SetOptionColor(i, finalChecklist[i] ? selectedOptionColor : normalOptionColor);
        }
        SetOption(3, ChecklistComplete ? "SUBMIT REPORT" : "HOÀN TẤT CHECKLIST ĐỂ GỬI", ChecklistComplete);
        SetFeedback(ChecklistComplete ? "Report đã sẵn sàng để gửi." : "Xác nhận đủ ba mục kiểm tra.",
            ChecklistComplete ? successTextColor : hintTextColor);
    }

    private void SetHeader(string fileName, string title, string instruction)
    {
        if (fileNameText != null) fileNameText.text = fileName;
        if (taskTitleText != null) taskTitleText.text = title;
        if (instructionText != null) instructionText.text = instruction;
    }

    private void ResetOptions()
    {
        for (int i = 0; i < optionButtons.Length; i++)
        {
            SetOption(i, string.Empty, false);
            SetOptionColor(i, normalOptionColor);
        }
    }

    private void SetOption(int index, string label, bool interactable)
    {
        if (index < 0 || index >= optionButtons.Length) return;
        if (optionButtons[index] != null)
        {
            optionButtons[index].gameObject.SetActive(!string.IsNullOrEmpty(label));
            optionButtons[index].interactable = interactable;
        }
        if (index < optionLabels.Length && optionLabels[index] != null) optionLabels[index].text = label;
    }

    private void SetOptionColor(int index, Color color)
    {
        if (index >= 0 && index < optionButtons.Length && optionButtons[index] != null &&
            optionButtons[index].TryGetComponent(out Image image)) image.color = color;
    }

    private void SetFeedback(string message, Color color)
    {
        if (feedbackText == null) return;
        feedbackText.text = message;
        feedbackText.color = color;
    }
}
