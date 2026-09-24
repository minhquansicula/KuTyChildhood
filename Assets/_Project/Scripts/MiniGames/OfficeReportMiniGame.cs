using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Three short office tasks that replace the old hold-to-fill report interaction.
/// The component lives on the scene Canvas while its panel can be hidden safely.
/// </summary>
public sealed class OfficeReportMiniGame : MonoBehaviour
{
    private const int TotalSteps = 3;

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
    public int CurrentStep { get; private set; }
    public float Progress => Mathf.Clamp01(CurrentStep / (float)TotalSteps);

    private readonly bool[] checklist = new bool[3];
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

    public bool Open(OfficeSceneController controller)
    {
        if (panel == null || optionButtons.Length < 4 || optionLabels.Length < 4)
        {
            Debug.LogError("Office report mini-game UI is not fully configured.", this);
            return false;
        }

        owner = controller;
        IsOpen = true;
        transitioning = false;
        panel.SetActive(true);
        panel.transform.SetAsLastSibling();
        RefreshStep();
        return true;
    }

    public void ClosePanel()
    {
        StopAllCoroutines();
        transitioning = false;
        IsOpen = false;
        if (panel != null) panel.SetActive(false);
    }

    private void RequestClose()
    {
        owner?.CancelWork();
    }

    public void ChooseOption(int index)
    {
        if (!IsOpen || transitioning || index < 0 || index >= optionButtons.Length) return;

        switch (CurrentStep)
        {
            case 0: CheckSingleAnswer(index, 2, "Đúng. Tỷ lệ 87% không khớp số liệu gốc 78%."); break;
            case 1: CheckSingleAnswer(index, 1, "Đúng. Câu mới nêu rõ người nhận, hành động và thời hạn."); break;
            case 2: HandleChecklist(index); break;
        }
    }

    private void CheckSingleAnswer(int selectedIndex, int correctIndex, string successMessage)
    {
        if (selectedIndex != correctIndex)
        {
            SetFeedback("Chưa đúng — hãy đối chiếu lại tài liệu bên trái.", errorTextColor);
            SetOptionColor(selectedIndex, errorOptionColor);
            return;
        }

        SetOptionColor(selectedIndex, selectedOptionColor);
        SetFeedback(successMessage, successTextColor);
        owner?.PlayReportTypingFeedback();
        transitioning = true;
        StartCoroutine(AdvanceAfterFeedback());
    }

    private IEnumerator AdvanceAfterFeedback()
    {
        yield return new WaitForSecondsRealtime(0.65f);
        CurrentStep = Mathf.Min(CurrentStep + 1, TotalSteps - 1);
        owner?.UpdateReportMiniGameProgress(CurrentStep / (float)TotalSteps);
        transitioning = false;
        RefreshStep();
    }

    private void HandleChecklist(int index)
    {
        if (index < checklist.Length)
        {
            checklist[index] = !checklist[index];
            owner?.PlayReportTypingFeedback();
            RefreshChecklist();
            return;
        }

        if (!ChecklistComplete)
        {
            SetFeedback("Cần xác nhận đủ ba mục trước khi gửi.", errorTextColor);
            return;
        }

        transitioning = true;
        SetFeedback("Đã kiểm tra xong. Đang gửi báo cáo...", successTextColor);
        if (progressFill != null) progressFill.fillAmount = 1f;
        owner?.UpdateReportMiniGameProgress(1f);
        StartCoroutine(CompleteAfterFeedback());
    }

    private IEnumerator CompleteAfterFeedback()
    {
        yield return new WaitForSecondsRealtime(0.75f);
        owner?.CompleteReportMiniGame();
    }

    private bool ChecklistComplete => checklist[0] && checklist[1] && checklist[2];

    private void RefreshStep()
    {
        if (stepText != null) stepText.text = $"BƯỚC {CurrentStep + 1} / {TotalSteps}";
        if (progressFill != null) progressFill.fillAmount = CurrentStep / (float)TotalSteps;
        ResetOptions();

        switch (CurrentStep)
        {
            case 0: ShowDataCheck(); break;
            case 1: ShowEmailRewrite(); break;
            default: ShowFinalChecklist(); break;
        }
    }

    private void ShowDataCheck()
    {
        SetHeader("REPORT_Q3_FINAL_v7.xlsx", "ĐỐI CHIẾU SỐ LIỆU",
            "Bảng tổng hợp gốc ghi tỷ lệ hoàn tất là 78%. Chọn dòng đang bị nhập sai.");
        if (documentText != null)
            documentText.text =
                "<b>BÁO CÁO KẾT QUẢ QUÝ III</b>\n\n" +
                "Doanh thu thuần                42,8 triệu\n" +
                "Đơn hàng hoàn tất             17 đơn\n" +
                "Tỷ lệ hoàn tất                <color=#FF8B78>87%</color>\n" +
                "Chi phí vận hành              12,4 triệu\n\n" +
                "<color=#8EA6C8>NGUỒN ĐỐI CHIẾU\nTỷ lệ hoàn tất gốc             78%</color>";

        SetOption(0, "Doanh thu — 42,8 triệu", true);
        SetOption(1, "Đơn hoàn tất — 17 đơn", true);
        SetOption(2, "Tỷ lệ hoàn tất — 87%", true);
        SetOption(3, "Chi phí — 12,4 triệu", true);
        SetFeedback("Chọn một dòng để đánh dấu lỗi.", hintTextColor);
    }

    private void ShowEmailRewrite()
    {
        SetHeader("EMAIL_GIAI_TRINH.txt", "SỬA NỘI DUNG EMAIL",
            "Chọn cách viết rõ ràng và chuyên nghiệp nhất trước khi gửi kèm báo cáo.");
        if (documentText != null)
            documentText.text =
                "<b>KÍNH GỬI PHÒNG KẾ TOÁN,</b>\n\n" +
                "Tôi gửi báo cáo quý III.\n" +
                "<color=#FF8B78>Mọi người xem rồi phản hồi sớm.</color>\n\n" +
                "Quân";

        SetOption(0, "Mọi người xem giúp nhé.", true);
        SetOption(1, "Nhờ phòng Kế toán kiểm tra và phản hồi trước 17:00 hôm nay.", true);
        SetOption(2, "Có gì sai thì báo lại.", true);
        SetOption(3, "Gửi gấp. Đừng để trễ.", true);
        SetFeedback("Một câu tốt cần có hành động và thời hạn cụ thể.", hintTextColor);
    }

    private void ShowFinalChecklist()
    {
        SetHeader("REPORT_Q3_FINAL_v8.xlsx", "KIỂM TRA TRƯỚC KHI GỬI",
            "Xác nhận từng mục. Nút gửi chỉ mở khi báo cáo đã sẵn sàng.");
        if (documentText != null)
            documentText.text =
                "<b>THAY ĐỔI ĐÃ THỰC HIỆN</b>\n\n" +
                "<color=#62D89A>✓</color> Tỷ lệ hoàn tất: 87% → 78%\n" +
                "<color=#62D89A>✓</color> Email đã bổ sung hạn phản hồi\n" +
                "<color=#62D89A>✓</color> Tên tệp nâng lên phiên bản v8\n\n" +
                "<color=#8EA6C8>Trạng thái: Chờ xác nhận cuối</color>";
        RefreshChecklist();
    }

    private void RefreshChecklist()
    {
        string[] labels =
        {
            "Số liệu đã đối chiếu",
            "Nội dung đã sửa",
            "Tệp đính kèm đúng phiên bản"
        };

        for (int i = 0; i < checklist.Length; i++)
        {
            SetOption(i, (checklist[i] ? "✓  " : "□  ") + labels[i], true);
            SetOptionColor(i, checklist[i] ? selectedOptionColor : normalOptionColor);
        }

        SetOption(3, ChecklistComplete ? "GỬI BÁO CÁO" : "HOÀN TẤT CHECKLIST ĐỂ GỬI", ChecklistComplete);
        SetFeedback(ChecklistComplete
            ? "Tất cả đã sẵn sàng. Có thể gửi báo cáo."
            : "Đánh dấu đủ ba mục kiểm tra.", ChecklistComplete ? successTextColor : hintTextColor);
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
            optionButtons[index].gameObject.SetActive(true);
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
