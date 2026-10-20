using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ExcelDataAuditorGame : MonoBehaviour
{
    [System.Serializable]
    public class ChatMessageData
    {
        public string senderName;
        public string messageContent;
        public string responseButtonText;
    }

    [Header("Main References")]
    [SerializeField] private GameObject mainPanel;
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private float timeLimit = 60f;
    
    [Header("Excel Grid Setup")]
    [SerializeField] private RectTransform gridParent;
    [SerializeField] private GameObject cellPrefab;
    
    [Header("Boss Chat System")]
    [SerializeField] private GameObject chatPopupPanel;
    [SerializeField] private TextMeshProUGUI chatSenderText;
    [SerializeField] private TextMeshProUGUI chatContentText;
    [SerializeField] private TextMeshProUGUI chatResponseButtonText;
    [SerializeField] private Button chatResponseButton;
    [SerializeField] private List<ChatMessageData> randomMessages;
    
    [Header("Game State")]
    [SerializeField] private Button submitButton;
    [SerializeField] private TextMeshProUGUI warningFeedbackText;

    private readonly Color errorOptionColor = new Color(0.48f, 0.13f, 0.16f, 1f);
    private readonly Color successTextColor = new Color(0.07f, 0.48f, 0.23f, 1f);
    private readonly Color errorTextColor = new Color(0.72f, 0.12f, 0.1f, 1f);
    private readonly Color bgPanelColor = new Color(0.94f, 0.955f, 0.95f, 1f);
    private readonly Color excelGreen = new Color(0.09f, 0.42f, 0.24f, 1f);
    private readonly Color editableCellColor = new Color(0.9f, 0.98f, 0.92f, 1f);
    private readonly Color editableCellHoverColor = new Color(0.82f, 0.95f, 0.86f, 1f);
    private readonly Color invalidCellColor = new Color(1f, 0.82f, 0.82f, 1f);

    private float timeRemaining;
    private bool isGameActive = false;
    private OfficeSceneController owner;
    private TMP_FontAsset globalFont;
    
    private RectTransform timerProgressBar;
    private Image timerProgressImage;
    private TextMeshProUGUI sheetProgressText;
    private CanvasGroup mainPanelCanvasGroup;
    private Coroutine revealRoutine;
    private bool attemptStarted;
    private bool attemptRunning;
    private bool tutorialCompleted;
    private bool tutorialActive;
    private bool relaxedMode;
    private bool retryPending;
    private ExcelTutorialController tutorial;
    private Image chatBackdrop;
    private TextMeshProUGUI formulaHint;
    private Vector3 feedbackBaseScale = Vector3.one;
    private float feedbackPulseRemaining;
    private float AttemptDuration => timeLimit > 0f ? Mathf.Max(1f, timeLimit) : 60f;

    public bool IsTutorialActive => tutorialActive;
    public bool IsAttemptRunning => attemptRunning;
    public bool IsRelaxedMode => relaxedMode;

    public class SheetData
    {
        public string Name;
        public string[,] Grid;
        public bool[,] Editable;
        public string[,] TargetValues;
    }

    private List<SheetData> sheets;
    private int currentSheetIndex = 0;
    private Dictionary<int, Dictionary<Vector2Int, string>> userInputs = new Dictionary<int, Dictionary<Vector2Int, string>>();
    private List<Image> tabBgImages = new List<Image>();
    private readonly List<TextMeshProUGUI> tabLabels = new List<TextMeshProUGUI>();
    private readonly Dictionary<Vector2Int, TMP_InputField> visibleInputs = new Dictionary<Vector2Int, TMP_InputField>();
    private readonly Dictionary<int, HashSet<Vector2Int>> validationErrors = new Dictionary<int, HashSet<Vector2Int>>();

    private void InitializeSheets()
    {
        sheets = new List<SheetData>()
        {
            new SheetData
            {
                Name = "Q3_Report",
                Grid = new string[,] {
                    {"Doanh thu", "Chi phí", "Lợi nhuận"},
                    {"1500", "800", "700"},
                    {"2100", "1500", "600"},
                    {"3600", "2300", "9999"} // Target 1300
                },
                Editable = new bool[,] {
                    {false, false, false},
                    {false, false, false},
                    {false, false, false},
                    {false, false, true}
                },
                TargetValues = new string[,] {
                    {null, null, null},
                    {null, null, null},
                    {null, null, null},
                    {null, null, "1300"}
                }
            },
            new SheetData
            {
                Name = "Q4_Forecast",
                Grid = new string[,] {
                    {"Hạng mục", "Ngân sách", "Thực tế"},
                    {"Marketing", "5000", "4500"},
                    {"Phát triển", "8000", "7000"},
                    {"Tổng", "13000", "12000"} // Actual target 11500
                },
                Editable = new bool[,] {
                    {false, false, false},
                    {false, false, false},
                    {false, false, false},
                    {false, false, true}
                },
                TargetValues = new string[,] {
                    {null, null, null},
                    {null, null, null},
                    {null, null, null},
                    {null, null, "11500"}
                }
            }
        };

        for (int i = 0; i < sheets.Count; i++)
        {
            userInputs[i] = new Dictionary<Vector2Int, string>();
        }
    }

    private void ResetRect(RectTransform rt)
    {
        if (rt == null) return;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        rt.localScale = Vector3.one;
    }

    private void StretchRect(RectTransform rt)
    {
        if (rt == null) return;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        rt.localScale = Vector3.one;
    }

    private void CreateHeaders() 
    {
        if (mainPanel == null) return;

        var ribbonObj = new GameObject("ExcelRibbon");
        ribbonObj.transform.SetParent(mainPanel.transform, false);
        var ribbonRect = ribbonObj.AddComponent<RectTransform>();
        StretchRect(ribbonRect);
        ribbonRect.anchorMin = new Vector2(0f, 0.85f);
        ribbonRect.anchorMax = new Vector2(1f, 1f);
        var ribbonImg = ribbonObj.AddComponent<Image>();
        ribbonImg.sprite = null;
        ribbonImg.color = excelGreen;

        var ribbonShadow = ribbonObj.AddComponent<Shadow>();
        ribbonShadow.effectColor = new Color(0f, 0f, 0f, 0.24f);
        ribbonShadow.effectDistance = new Vector2(0f, -4f);

        var tObj = new GameObject("TaskTitleText");
        tObj.transform.SetParent(ribbonObj.transform, false);
        var tRect = tObj.AddComponent<RectTransform>();
        StretchRect(tRect);
        tRect.offsetMin = new Vector2(32f, 0f);
        tRect.offsetMax = new Vector2(-220f, 0f);
        var tText = tObj.AddComponent<TextMeshProUGUI>();
        tText.text = "<color=#E9FFF1><b>REPORT_Q3_FINAL.xlsx</b></color>  <color=#91DDB3>•  KIỂM TRA DỮ LIỆU</color>";
        tText.color = Color.white;
        tText.fontStyle = FontStyles.Bold;
        tText.alignment = TextAlignmentOptions.MidlineLeft;
        tText.enableAutoSizing = true;
        tText.fontSizeMin = 18f;
        tText.fontSizeMax = 30f;
        tText.raycastTarget = false;
        if (globalFont != null) tText.font = globalFont;

        if (timerText != null)
        {
            timerText.transform.SetParent(ribbonObj.transform, false);
            RectTransform timerRect = timerText.rectTransform;
            timerRect.anchorMin = new Vector2(0.82f, 0.2f);
            timerRect.anchorMax = new Vector2(0.98f, 0.8f);
            timerRect.offsetMin = Vector2.zero;
            timerRect.offsetMax = Vector2.zero;
            timerText.gameObject.SetActive(true);
            timerText.text = "01:00";
            timerText.color = Color.white;
            timerText.fontStyle = FontStyles.Bold;
            timerText.alignment = TextAlignmentOptions.Center;
            timerText.enableAutoSizing = true;
            timerText.fontSizeMin = 16f;
            timerText.fontSizeMax = 26f;
            timerText.raycastTarget = false;
            if (globalFont != null) timerText.font = globalFont;
        }

        var instructionCard = new GameObject("InstructionCard");
        instructionCard.transform.SetParent(mainPanel.transform, false);
        var cardRect = instructionCard.AddComponent<RectTransform>();
        ResetRect(cardRect);
        cardRect.anchorMin = new Vector2(0.1f, 0.74f);
        cardRect.anchorMax = new Vector2(0.9f, 0.82f);
        var cardImage = instructionCard.AddComponent<Image>();
        cardImage.color = new Color(0.86f, 0.93f, 0.88f, 1f);
        var cardOutline = instructionCard.AddComponent<Outline>();
        cardOutline.effectColor = new Color(0.48f, 0.66f, 0.53f, 0.55f);
        cardOutline.effectDistance = new Vector2(1f, -1f);

        var iObj = new GameObject("InstructionText");
        iObj.transform.SetParent(instructionCard.transform, false);
        var iRect = iObj.AddComponent<RectTransform>();
        StretchRect(iRect);
        iRect.offsetMin = new Vector2(22f, 10f);
        iRect.offsetMax = new Vector2(-260f, -10f);
        var iText = iObj.AddComponent<TextMeshProUGUI>();
        iText.text = "<color=#176B3A><b>HOÀN THIỆN BẢNG TÍNH</b></color>\n" +
            "Tìm ô xanh nhạt, tính lại <b>Lợi nhuận</b> và <b>Tổng thực tế</b>, rồi nhập giá trị đúng.";
        iText.color = new Color(0.08f, 0.2f, 0.12f, 1f);
        iText.alignment = TextAlignmentOptions.MidlineLeft;
        iText.enableAutoSizing = true;
        iText.fontSizeMin = 13f;
        iText.fontSizeMax = 22f;
        iText.raycastTarget = false;
        if (globalFont != null) iText.font = globalFont;

        var progressObj = new GameObject("SheetProgressText");
        progressObj.transform.SetParent(instructionCard.transform, false);
        var progressRect = progressObj.AddComponent<RectTransform>();
        progressRect.anchorMin = new Vector2(0.68f, 0f);
        progressRect.anchorMax = new Vector2(0.98f, 1f);
        progressRect.offsetMin = Vector2.zero;
        progressRect.offsetMax = Vector2.zero;
        sheetProgressText = progressObj.AddComponent<TextMeshProUGUI>();
        sheetProgressText.color = excelGreen;
        sheetProgressText.fontStyle = FontStyles.Bold;
        sheetProgressText.alignment = TextAlignmentOptions.Center;
        sheetProgressText.enableAutoSizing = true;
        sheetProgressText.fontSizeMin = 13f;
        sheetProgressText.fontSizeMax = 20f;
        sheetProgressText.raycastTarget = false;
        if (globalFont != null) sheetProgressText.font = globalFont;

        var hintObject = new GameObject("FormulaHint", typeof(RectTransform));
        hintObject.transform.SetParent(mainPanel.transform, false);
        formulaHint = hintObject.AddComponent<TextMeshProUGUI>();
        formulaHint.font = globalFont;
        formulaHint.enableAutoSizing = true;
        formulaHint.fontSizeMin = 13f;
        formulaHint.fontSizeMax = 18f;
        formulaHint.color = excelGreen;
        formulaHint.alignment = TextAlignmentOptions.Center;
        formulaHint.raycastTarget = false;
        formulaHint.rectTransform.anchorMin = new Vector2(.12f, .685f);
        formulaHint.rectTransform.anchorMax = new Vector2(.88f, .725f);
        formulaHint.rectTransform.offsetMin = formulaHint.rectTransform.offsetMax = Vector2.zero;

        var closeObject = new GameObject("CloseWorkbook", typeof(RectTransform), typeof(Image), typeof(Button));
        closeObject.transform.SetParent(mainPanel.transform, false);
        var closeRect = (RectTransform)closeObject.transform;
        closeRect.anchorMin = new Vector2(.74f, .055f);
        closeRect.anchorMax = new Vector2(.96f, .145f);
        closeRect.offsetMin = closeRect.offsetMax = Vector2.zero;
        closeObject.GetComponent<Image>().color = new Color(.23f, .3f, .27f);
        var closeButton = closeObject.GetComponent<Button>();
        ConfigureButton(closeButton);
        closeButton.onClick.AddListener(() => owner?.CancelWork());
        var closeLabel = new GameObject("Label", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
        closeLabel.transform.SetParent(closeObject.transform, false);
        StretchRect(closeLabel.rectTransform);
        closeLabel.font = globalFont;
        closeLabel.text = "ĐÓNG  [ESC]\n<size=70%>Giữ dữ liệu đang làm</size>";
        closeLabel.enableAutoSizing = true;
        closeLabel.fontSizeMin = 13f;
        closeLabel.fontSizeMax = 19f;
        closeLabel.alignment = TextAlignmentOptions.Center;
        closeLabel.raycastTarget = false;
    }

    private void CreateTabs()
    {
        if (mainPanel == null) return;
        
        var tabsParent = new GameObject("SheetTabs");
        tabsParent.transform.SetParent(mainPanel.transform, false);
        var tabsRect = tabsParent.AddComponent<RectTransform>();
        StretchRect(tabsRect);
        tabsRect.anchorMin = new Vector2(0f, 0.17f);
        tabsRect.anchorMax = new Vector2(1f, 0.25f);
        var tabsImg = tabsParent.AddComponent<Image>();
        tabsImg.sprite = null;
        tabsImg.color = new Color(0.78f, 0.82f, 0.8f, 1f);

        var horizLayout = tabsParent.AddComponent<HorizontalLayoutGroup>();
        horizLayout.spacing = 5;
        horizLayout.childAlignment = TextAnchor.MiddleLeft;
        horizLayout.childControlWidth = true;
        horizLayout.childControlHeight = true;
        horizLayout.childForceExpandWidth = false;
        horizLayout.childForceExpandHeight = true;
        horizLayout.padding = new RectOffset(10, 10, 5, 5);

        for (int i = 0; i < sheets.Count; i++)
        {
            int loopIndex = i;
            var tabObj = new GameObject("Tab_" + sheets[i].Name);
            tabObj.transform.SetParent(tabsParent.transform, false);
            var tabImg = tabObj.AddComponent<Image>();
            tabImg.sprite = null;
            tabBgImages.Add(tabImg);

            var tabBtn = tabObj.AddComponent<Button>();
            tabBtn.onClick.AddListener(() => SwitchSheet(loopIndex));
            ConfigureButton(tabBtn);

            var tabLe = tabObj.AddComponent<LayoutElement>();
            tabLe.minWidth = 165f;
            
            var txtObj = new GameObject("Text");
            txtObj.transform.SetParent(tabObj.transform, false);
            var txtRect = txtObj.AddComponent<RectTransform>();
            StretchRect(txtRect);
            var txt = txtObj.AddComponent<TextMeshProUGUI>();
            txt.text = sheets[i].Name;
            txt.color = new Color(0.2f, 0.2f, 0.2f, 1f);
            txt.alignment = TextAlignmentOptions.Center;
            txt.enableWordWrapping = false;
            txt.enableAutoSizing = true;
            txt.fontSizeMin = 12;
            txt.fontSizeMax = 22;
            txt.raycastTarget = false;
            if (globalFont != null) txt.font = globalFont;
            tabLabels.Add(txt);
        }

        UpdateTabUI();
    }

    private void SwitchSheet(int index)
    {
        if (!isGameActive || !attemptRunning || tutorialActive || GameManager.Instance?.IsPaused == true ||
            (chatPopupPanel != null && chatPopupPanel.activeSelf)) return;
        if (index == currentSheetIndex) return;
        currentSheetIndex = index;
        AudioManager.Instance?.PlaySFX(SfxId.UIConfirm);
        UpdateTabUI();
        GenerateGrid();
        SetFeedback($"Đang xem sheet {sheets[currentSheetIndex].Name}.", new Color(0.25f, 0.32f, 0.28f, 1f), "•");
        FocusFirstEditableCell();
    }

    private void UpdateTabUI()
    {
        for (int i = 0; i < tabBgImages.Count; i++)
        {
            if (tabBgImages[i] != null)
            {
                bool active = i == currentSheetIndex;
                tabBgImages[i].color = active ? excelGreen : new Color(0.72f, 0.76f, 0.74f, 1f);
                if (i < tabLabels.Count && tabLabels[i] != null)
                {
                    tabLabels[i].color = active ? Color.white : new Color(0.15f, 0.2f, 0.17f, 1f);
                    tabLabels[i].fontStyle = active ? FontStyles.Bold : FontStyles.Normal;
                }
            }
        }
        UpdateCompletionSummary();
    }

    private void Awake()
    {
        if (warningFeedbackText != null) feedbackBaseScale = warningFeedbackText.rectTransform.localScale;
        if (submitButton != null) 
        {
            var tmp = submitButton.GetComponentInChildren<TextMeshProUGUI>();
            if (tmp != null) globalFont = tmp.font;
        }

        InitializeSheets();
        if (mainPanel == null)
        {
            Debug.LogError("Excel mini-game main panel is not configured.", this);
            return;
        }

        tutorial = GetComponent<ExcelTutorialController>();
        if (tutorial == null) tutorial = gameObject.AddComponent<ExcelTutorialController>();

        mainPanelCanvasGroup = mainPanel.GetComponent<CanvasGroup>();
        if (mainPanelCanvasGroup == null) mainPanelCanvasGroup = mainPanel.AddComponent<CanvasGroup>();
        mainPanelCanvasGroup.interactable = true;
        mainPanelCanvasGroup.blocksRaycasts = true;

        if (mainPanel.TryGetComponent<Image>(out var mainBackground))
        {
            mainBackground.sprite = null;
            mainBackground.color = bgPanelColor;
        }

        CreateHeaders();
        CreateTabs();

        if (chatPopupPanel != null)
        {
            var backdropObject = new GameObject("ChatModalBackdrop", typeof(RectTransform), typeof(Image));
            backdropObject.transform.SetParent(chatPopupPanel.transform.parent, false);
            chatBackdrop = backdropObject.GetComponent<Image>();
            StretchRect(chatBackdrop.rectTransform);
            chatBackdrop.color = new Color(.015f, .025f, .035f, .65f);
            backdropObject.SetActive(false);
            RectTransform popupRect = chatPopupPanel.GetComponent<RectTransform>();
            ResetRect(popupRect);
            popupRect.anchorMin = new Vector2(0.31f, 0.29f);
            popupRect.anchorMax = new Vector2(0.69f, 0.71f);
            if (chatPopupPanel.TryGetComponent<Image>(out var popBg)) 
            {
                popBg.sprite = null;
                popBg.color = new Color(0.98f, 0.99f, 1f, 1f);
            }

            var popOutline = chatPopupPanel.gameObject.GetComponent<Outline>() ?? chatPopupPanel.gameObject.AddComponent<Outline>();
            popOutline.effectColor = new Color(0.08f, 0.18f, 0.3f, 0.48f);
            popOutline.effectDistance = new Vector2(3f, -3f);

            var titleBar = new GameObject("TitleBar");
            titleBar.transform.SetParent(chatPopupPanel.transform, false);
            var titleBarRect = titleBar.AddComponent<RectTransform>();
            StretchRect(titleBarRect);
            titleBarRect.anchorMin = new Vector2(0f, 0.78f);
            titleBarRect.anchorMax = new Vector2(1f, 1f);
            var titleImg = titleBar.AddComponent<Image>();
            titleImg.sprite = null;
            titleImg.color = new Color(0.08f, 0.34f, 0.62f, 1f);

            ResetRect(chatSenderText?.GetComponent<RectTransform>());
            ResetRect(chatContentText?.GetComponent<RectTransform>());
            ResetRect(chatResponseButton?.GetComponent<RectTransform>());
            StretchRect(chatResponseButtonText?.GetComponent<RectTransform>());
            
            if (chatSenderText != null) 
            {
                chatSenderText.transform.SetParent(titleBar.transform, false);
                StretchRect(chatSenderText.GetComponent<RectTransform>());
                chatSenderText.GetComponent<RectTransform>().offsetMin = new Vector2(20f, 0f);
                chatSenderText.GetComponent<RectTransform>().offsetMax = new Vector2(-16f, 0f);
                chatSenderText.color = Color.white;
                chatSenderText.fontStyle = FontStyles.Bold;
                chatSenderText.alignment = TextAlignmentOptions.MidlineLeft;
                chatSenderText.enableAutoSizing = true;
                chatSenderText.fontSizeMin = 15f;
                chatSenderText.fontSizeMax = 24f;
                if (globalFont != null) chatSenderText.font = globalFont;
            }

            if (chatContentText != null) 
            {
                var cRect = chatContentText.GetComponent<RectTransform>();
                if (cRect != null)
                {
                    cRect.anchorMin = new Vector2(0.07f, 0.31f);
                    cRect.anchorMax = new Vector2(0.93f, 0.72f);
                    cRect.offsetMin = Vector2.zero;
                    cRect.offsetMax = Vector2.zero;
                }
                chatContentText.color = new Color(0.12f, 0.16f, 0.21f, 1f);
                chatContentText.alignment = TextAlignmentOptions.MidlineLeft;
                chatContentText.enableAutoSizing = true;
                chatContentText.fontSizeMin = 14f;
                chatContentText.fontSizeMax = 23f;
                if (globalFont != null) chatContentText.font = globalFont;
            }

            if (chatResponseButtonText != null) 
            {
                chatResponseButtonText.color = Color.white;
                chatResponseButtonText.alignment = TextAlignmentOptions.Center;
                chatResponseButtonText.enableAutoSizing = true;
                chatResponseButtonText.fontSizeMin = 14;
                chatResponseButtonText.fontSizeMax = 22;
                chatResponseButtonText.raycastTarget = false;
                if (globalFont != null) chatResponseButtonText.font = globalFont;
            }
            if (chatResponseButton != null && chatResponseButton.TryGetComponent<Image>(out var btnBg)) 
            {
                RectTransform responseRect = chatResponseButton.GetComponent<RectTransform>();
                responseRect.anchorMin = new Vector2(0.08f, 0.08f);
                responseRect.anchorMax = new Vector2(0.92f, 0.25f);
                responseRect.offsetMin = Vector2.zero;
                responseRect.offsetMax = Vector2.zero;
                btnBg.sprite = null;
                btnBg.color = new Color(0.08f, 0.38f, 0.72f, 1f);
                ConfigureButton(chatResponseButton);
            }
        }

        if (submitButton != null) 
        {
            var sRect = submitButton.GetComponent<RectTransform>();
            ResetRect(sRect);
            sRect.anchorMin = new Vector2(0.36f, 0.055f);
            sRect.anchorMax = new Vector2(0.64f, 0.145f);
            if (submitButton.TryGetComponent<Image>(out var subBg)) subBg.color = excelGreen;
            ConfigureButton(submitButton);
            submitButton.onClick.AddListener(CheckValidation);
            var btnText = submitButton.GetComponentInChildren<TextMeshProUGUI>();
            if (btnText != null) 
            {
                StretchRect(btnText.GetComponent<RectTransform>());
                btnText.text = "KIỂM TRA & GỬI\n<size=65%>CTRL + ENTER</size>";
                btnText.color = Color.white;
                btnText.alignment = TextAlignmentOptions.Center;
                btnText.enableAutoSizing = true;
                btnText.fontSizeMin = 13;
                btnText.fontSizeMax = 24;
                btnText.raycastTarget = false;
            }
        }
        
        if (warningFeedbackText != null) 
        {
            ResetRect(warningFeedbackText.GetComponent<RectTransform>());
            var wRect = warningFeedbackText.GetComponent<RectTransform>();
            wRect.anchorMin = new Vector2(0.12f, 0.265f);
            wRect.anchorMax = new Vector2(0.88f, 0.34f);
            warningFeedbackText.color = new Color(0.25f, 0.32f, 0.28f, 1f);
            warningFeedbackText.alignment = TextAlignmentOptions.Center;
            warningFeedbackText.enableAutoSizing = true;
            warningFeedbackText.fontSizeMin = 13f;
            warningFeedbackText.fontSizeMax = 22f;
            warningFeedbackText.raycastTarget = false;
            if (globalFont != null) warningFeedbackText.font = globalFont;
        }

        var pbBg = new GameObject("ProgressBarBg");
        pbBg.transform.SetParent(mainPanel.transform, false);
        var pbBgRect = pbBg.AddComponent<RectTransform>();
        StretchRect(pbBgRect);
        pbBgRect.anchorMin = new Vector2(0f, 0.837f);
        pbBgRect.anchorMax = new Vector2(1f, 0.85f);
        var pbBgImg = pbBg.AddComponent<Image>();
        pbBgImg.sprite = null;
        pbBgImg.color = new Color(0.64f, 0.7f, 0.66f, 1f);

        var pbFill = new GameObject("ProgressBarFill");
        pbFill.transform.SetParent(pbBg.transform, false);
        timerProgressBar = pbFill.AddComponent<RectTransform>();
        StretchRect(timerProgressBar);
        timerProgressImage = pbFill.AddComponent<Image>();
        timerProgressImage.sprite = null;
        timerProgressImage.color = new Color(0.16f, 0.72f, 0.34f, 1f);

        if (mainPanel != null) mainPanel.SetActive(false);
        if (chatPopupPanel != null) chatPopupPanel.SetActive(false);
        
        if (chatResponseButton != null)
        {
            chatResponseButton.onClick.AddListener(CloseChatPopup);
        }

        if (randomMessages == null || randomMessages.Count == 0)
        {
            randomMessages = new List<ChatMessageData> {
                new ChatMessageData { senderName = "Quản lý", messageContent = "Em kiểm tra kỹ số liệu rồi gửi lại báo cáo nhé.", responseButtonText = "Dạ, em đang kiểm tra ạ." },
                new ChatMessageData { senderName = "Quản lý", messageContent = "Nhớ giữ định dạng bảng thống nhất trước khi gửi.", responseButtonText = "Vâng, em đã rõ ạ." },
                new ChatMessageData { senderName = "Nhân sự", messageContent = "Ngày mai phòng mình họp lúc 9 giờ nhé.", responseButtonText = "Mình đã nhận thông tin." },
                new ChatMessageData { senderName = "Đồng nghiệp", messageContent = "Mọi người về rồi, Tý còn làm à?", responseButtonText = "Ừ, mình đang làm nốt báo cáo." }
            };
        }
    }

    private void Update()
    {
        if (feedbackPulseRemaining > 0f && GameManager.Instance?.IsPaused != true)
        {
            feedbackPulseRemaining = Mathf.Max(0f, feedbackPulseRemaining - Time.deltaTime);
            if (warningFeedbackText != null)
            {
                float pulse = Mathf.Sin((1f - feedbackPulseRemaining / .3f) * Mathf.PI);
                warningFeedbackText.rectTransform.localScale = feedbackBaseScale * (1f + .09f * pulse);
            }
        }
        if (!isGameActive || !attemptRunning || tutorialActive || GameManager.Instance?.IsPaused == true) return;
        bool submitShortcut = (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl) ||
                               Input.GetKey(KeyCode.LeftCommand) || Input.GetKey(KeyCode.RightCommand)) &&
                              Input.GetKeyDown(KeyCode.Return);
        if (submitShortcut && (chatPopupPanel == null || !chatPopupPanel.activeSelf)) CheckValidation();
        else if (Input.GetKeyDown(KeyCode.Return) && chatPopupPanel != null && chatPopupPanel.activeSelf)
            CloseChatPopup();
    }

    private void OnDestroy()
    {
        if (submitButton != null) submitButton.onClick.RemoveListener(CheckValidation);
        if (chatResponseButton != null) chatResponseButton.onClick.RemoveListener(CloseChatPopup);
    }

    public bool Open(OfficeSceneController controller)
    {
        // Repeated UI events must not start duplicate timer/chat coroutines.
        if (isGameActive || retryPending) return true;

        if (mainPanel == null || gridParent == null || cellPrefab == null)
        {
            Debug.LogError("Excel mini-game UI is not fully configured.", this);
            return false;
        }

        gameObject.SetActive(true);
        owner = controller;
        isGameActive = true;
        attemptRunning = false;
        
        mainPanel.SetActive(true);
        mainPanel.transform.SetAsLastSibling();
        if (chatPopupPanel != null) chatPopupPanel.SetActive(false);
        if (chatBackdrop != null) chatBackdrop.gameObject.SetActive(false);
        if (submitButton != null) submitButton.interactable = true;
        
        SetFeedback("Nhập giá trị đúng vào các ô được tô màu.", new Color(0.25f, 0.32f, 0.28f, 1f), "•");
        
        UpdateTabUI();
        GenerateGrid();
        tutorialActive = !tutorialCompleted;
        UpdateTimerUI();
        PlayRevealAnimation();
        if (!tutorialCompleted)
        {
            tutorialActive = true;
            tutorial.Show(mainPanel.transform as RectTransform, globalFont, BeginAttempt);
            SetFeedback("Chọn cách chơi sau khi đọc hướng dẫn. Đồng hồ chưa bắt đầu.", excelGreen, "?");
        }
        else BeginAttempt(relaxedMode ? ExcelAssistMode.Relaxed : ExcelAssistMode.Timed);
        return true;
    }

    public void BeginAttempt(ExcelAssistMode mode)
    {
        if (!isGameActive || mainPanel == null || !mainPanel.activeInHierarchy) return;
        if (attemptRunning) StopAllCoroutines();
        tutorialActive = false;
        tutorial?.HideWithoutSelection();
        tutorialCompleted = true;
        relaxedMode = mode == ExcelAssistMode.Relaxed;
        if (!attemptStarted || timeRemaining <= 0f) timeRemaining = AttemptDuration;
        attemptStarted = true;
        attemptRunning = true;
        retryPending = false;
        if (submitButton != null) submitButton.interactable = true;
        UpdateTimerUI();
        SetFeedback(relaxedMode
            ? "Chế độ Thư thả: kiểm tra hai ô được tô màu, không giới hạn thời gian."
            : "Nhập giá trị đúng vào các ô được tô màu.",
            new Color(0.25f, 0.32f, 0.28f, 1f), "•");
        FocusFirstEditableCell();
        if (!relaxedMode) StartCoroutine(TimerRoutine());
        StartCoroutine(BossDistractionRoutine());
    }

    public void ClosePanel()
    {
        feedbackPulseRemaining = 0f;
        if (warningFeedbackText != null) warningFeedbackText.rectTransform.localScale = feedbackBaseScale;
        isGameActive = false;
        attemptRunning = false;
        retryPending = false;
        StopAllCoroutines();
        tutorialActive = false;
        tutorial?.HideWithoutSelection();
        revealRoutine = null;
        if (mainPanelCanvasGroup != null) mainPanelCanvasGroup.alpha = 1f;
        if (chatPopupPanel != null) chatPopupPanel.SetActive(false);
        if (chatBackdrop != null) chatBackdrop.gameObject.SetActive(false);
        if (mainPanel != null) mainPanel.SetActive(false);
    }

    private void GenerateGrid()
    {
        if (gridParent == null || cellPrefab == null || sheets == null || sheets.Count == 0) return;
        if (formulaHint != null) formulaHint.text = currentSheetIndex == 0
            ? "Q3 · Ô C4: Lợi nhuận = Doanh thu − Chi phí (A4 − B4)"
            : "Q4 · Ô C4: Tổng thực tế = Marketing + Phát triển (C2 + C3)";

        foreach (Transform child in gridParent)
        {
            Destroy(child.gameObject);
        }
        visibleInputs.Clear();

        if (mainPanel != null && mainPanel.TryGetComponent<Image>(out var img))
        {
            img.color = bgPanelColor;
            img.sprite = null;
        }

        RectTransform gridRect = gridParent;
        gridRect.anchorMin = new Vector2(0.12f, 0.36f);
        gridRect.anchorMax = new Vector2(0.88f, 0.68f);
        gridRect.offsetMin = Vector2.zero;
        gridRect.offsetMax = Vector2.zero;
        Canvas.ForceUpdateCanvases();

        var gridImg = gridParent.GetComponent<Image>();
        if (gridImg != null) gridImg.enabled = false;

        var grid = gridParent.GetComponent<GridLayoutGroup>();
        if (grid != null)
        {
            float width = Mathf.Max(520f, gridRect.rect.width);
            float height = Mathf.Max(210f, gridRect.rect.height);
            grid.cellSize = new Vector2((width - 3f) / 4f, (height - 4f) / 5f);
            grid.spacing = Vector2.one;
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 4;
            grid.childAlignment = TextAnchor.MiddleCenter;
        }

        for (int r = 0; r < 5; r++)
        {
            for (int c = 0; c < 4; c++)
            {
                var cellObj = Instantiate(cellPrefab, gridParent);
                cellObj.name = $"Cell_{r}_{c}";
                cellObj.SetActive(true);

                var inputField = cellObj.GetComponentInChildren<TMP_InputField>();
                var textComponent = cellObj.GetComponentInChildren<TextMeshProUGUI>();
                var rootBg = cellObj.GetComponent<Image>();
                var outline = cellObj.GetComponent<Outline>() ?? cellObj.AddComponent<Outline>();
                outline.effectColor = new Color(0.46f, 0.52f, 0.49f, 0.9f);
                outline.effectDistance = new Vector2(1f, -1f);
                if (rootBg != null)
                {
                    rootBg.sprite = null;
                    rootBg.type = Image.Type.Simple;
                }

                bool isHeader = (r == 0 || c == 0);
                string val = " ";
                bool canEdit = false;

                if (isHeader)
                {
                    if (r == 0 && c > 0) val = ((char)('A' + (c - 1))).ToString(); // A, B, C
                    if (c == 0 && r > 0) val = r.ToString(); // 1, 2, 3, 4
                }
                else
                {
                    val = sheets[currentSheetIndex].Grid[r - 1, c - 1];
                    canEdit = sheets[currentSheetIndex].Editable[r - 1, c - 1];
                    
                    if (userInputs[currentSheetIndex].TryGetValue(new Vector2Int(r, c), out var savedVal))
                    {
                        val = savedVal; 
                    }
                }

                Vector2Int coordinate = new Vector2Int(r, c);
                bool hasError = validationErrors.TryGetValue(currentSheetIndex, out var errors) && errors.Contains(coordinate);
                if (rootBg != null)
                {
                    rootBg.color = isHeader
                        ? new Color(0.82f, 0.87f, 0.84f, 1f)
                        : hasError ? invalidCellColor : canEdit ? editableCellColor : Color.white;
                }

                if (inputField != null)
                {
                    var colors = inputField.colors;
                    colors.disabledColor = Color.white;
                    colors.normalColor = Color.white;
                    colors.highlightedColor = new Color(0.96f, 1f, 0.97f, 1f);
                    colors.selectedColor = new Color(0.92f, 1f, 0.94f, 1f);
                    colors.pressedColor = new Color(0.88f, 0.95f, 0.9f, 1f);
                    inputField.colors = colors;

                    if (inputField.placeholder != null) inputField.placeholder.gameObject.SetActive(false);
                    if (inputField.targetGraphic is Image fieldBg)
                    {
                        fieldBg.sprite = null;
                        fieldBg.color = Color.clear;
                    }

                    inputField.text = val;
                    inputField.interactable = canEdit;
                    inputField.contentType = TMP_InputField.ContentType.IntegerNumber;
                    inputField.characterValidation = TMP_InputField.CharacterValidation.Integer;
                    inputField.characterLimit = 8;
                    
                    var txt = inputField.textComponent;
                    if (txt != null) 
                    {
                        if (globalFont != null) txt.font = globalFont;
                        txt.color = isHeader ? new Color(0.2f, 0.2f, 0.2f, 1f) : Color.black; 
                        txt.fontStyle = isHeader ? FontStyles.Bold : FontStyles.Normal;
                        txt.alignment = TextAlignmentOptions.Center;
                        txt.enableAutoSizing = true;
                        txt.fontSizeMin = 12f;
                        txt.fontSizeMax = 22f;
                        txt.raycastTarget = false;
                    }
                    
                    if (canEdit)
                    {
                        int sheetIndex = currentSheetIndex;
                        int captureR = r;
                        int captureC = c;
                        Vector2Int capturedCoordinate = coordinate;
                        visibleInputs[capturedCoordinate] = inputField;
                        inputField.onValueChanged.RemoveAllListeners();
                        inputField.onValueChanged.AddListener((changedVal) => 
                        {
                            userInputs[sheetIndex][new Vector2Int(captureR, captureC)] = changedVal;
                            if (validationErrors.TryGetValue(sheetIndex, out var sheetErrors))
                                sheetErrors.Remove(capturedCoordinate);
                            if (rootBg != null) rootBg.color = editableCellColor;
                            outline.effectColor = excelGreen;
                            UpdateCompletionSummary();
                        });
                        inputField.onSelect.RemoveAllListeners();
                        inputField.onSelect.AddListener(_ =>
                        {
                            if (rootBg != null && !hasError) rootBg.color = editableCellHoverColor;
                            outline.effectColor = excelGreen;
                            outline.effectDistance = new Vector2(2f, -2f);
                        });
                        inputField.onDeselect.RemoveAllListeners();
                        inputField.onDeselect.AddListener(_ =>
                        {
                            bool stillInvalid = validationErrors.TryGetValue(sheetIndex, out var sheetErrors) &&
                                                sheetErrors.Contains(capturedCoordinate);
                            if (rootBg != null) rootBg.color = stillInvalid ? invalidCellColor : editableCellColor;
                            outline.effectColor = stillInvalid ? errorOptionColor : new Color(0.46f, 0.52f, 0.49f, 0.9f);
                            outline.effectDistance = new Vector2(1f, -1f);
                        });
                    }
                }
                else if (textComponent != null)
                {
                    if (globalFont != null) textComponent.font = globalFont;
                    textComponent.text = val;
                    textComponent.color = isHeader ? new Color(0.2f, 0.2f, 0.2f, 1f) : Color.black; 
                    textComponent.alignment = TextAlignmentOptions.Center;
                    textComponent.fontStyle = isHeader ? FontStyles.Bold : FontStyles.Normal;
                    textComponent.enableAutoSizing = true;
                    textComponent.fontSizeMin = 12f;
                    textComponent.fontSizeMax = 22f;
                    textComponent.raycastTarget = false;
                }
            }
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(gridParent);
        UpdateCompletionSummary();
    }

    private IEnumerator TimerRoutine()
    {
        while (isGameActive && attemptRunning && !relaxedMode && timeRemaining > 0)
        {
            if (chatPopupPanel != null && chatPopupPanel.activeSelf)
            {
                yield return null;
                continue;
            }
            timeRemaining -= Time.deltaTime;
            UpdateTimerUI();
            yield return null;
        }

        if (isGameActive && attemptRunning && !relaxedMode && timeRemaining <= 0)
        {
            FailGame("HẾT THỜI GIAN\nBình tĩnh kiểm tra lại; dữ liệu bạn đã nhập vẫn được giữ nguyên.");
        }
    }

    private void UpdateTimerUI()
    {
        float fill = relaxedMode ? 1f : Mathf.Clamp01(timeRemaining / AttemptDuration);
        if (timerProgressBar != null)
        {
            timerProgressBar.anchorMax = new Vector2(fill, 1f);
            if (timerProgressImage != null)
            {
                timerProgressImage.color = Color.Lerp(new Color(0.88f, 0.16f, 0.12f, 1f),
                    new Color(0.16f, 0.72f, 0.34f, 1f), fill);
            }
        }

        if (timerText != null)
        {
            if (tutorialActive)
            {
                timerText.text = "CHƯA BẮT ĐẦU";
                timerText.color = new Color(.75f, 1f, .85f, 1f);
                return;
            }
            if (relaxedMode)
            {
                timerText.text = "THƯ THẢ · KHÔNG GIỚI HẠN";
                timerText.color = new Color(.75f, 1f, .85f, 1f);
                return;
            }
            int seconds = Mathf.Max(0, Mathf.CeilToInt(timeRemaining));
            timerText.text = $"CÒN  {seconds / 60:00}:{seconds % 60:00}";
            timerText.color = fill <= 0.25f
                ? new Color(1f, 0.68f, 0.55f, 1f)
                : fill <= 0.5f ? new Color(1f, 0.9f, 0.62f, 1f) : Color.white;
        }
    }

    private IEnumerator BossDistractionRoutine()
    {
        while (isGameActive && attemptRunning)
        {
            // Wait 10 to 18 seconds before popping up
            yield return new WaitForSeconds(Random.Range(10f, 18f));
            
            if (!isGameActive || !attemptRunning) yield break;

            ShowChatPopup();

            // Freeze the input/timer wait until they close it
            while (chatPopupPanel != null && chatPopupPanel.activeSelf)
            {
                yield return null;
            }
        }
    }

    private void ShowChatPopup()
    {
        if (!isGameActive || !attemptRunning || tutorialActive) return;
        if (chatPopupPanel == null || chatSenderText == null || chatContentText == null ||
            chatResponseButtonText == null || randomMessages == null || randomMessages.Count == 0) return;

        var randomMsg = randomMessages[Random.Range(0, randomMessages.Count)];
        
        chatSenderText.text = randomMsg.senderName;
        chatContentText.text = randomMsg.messageContent;
        chatResponseButtonText.text = randomMsg.responseButtonText;
        
        if (chatBackdrop != null)
        {
            chatBackdrop.gameObject.SetActive(true);
            chatBackdrop.transform.SetAsLastSibling();
        }
        chatPopupPanel.SetActive(true);
        chatPopupPanel.transform.SetAsLastSibling();
        if (EventSystem.current != null && chatResponseButton != null)
            EventSystem.current.SetSelectedGameObject(chatResponseButton.gameObject);
    }

    private void CloseChatPopup()
    {
        if (GameManager.Instance?.IsPaused == true) return;
        if (chatPopupPanel != null) chatPopupPanel.SetActive(false);
        if (chatBackdrop != null) chatBackdrop.gameObject.SetActive(false);
        if (attemptRunning && !tutorialActive) FocusFirstEditableCell();
    }

    private void CheckValidation()
    {
        if (!isGameActive || !attemptRunning || tutorialActive || GameManager.Instance?.IsPaused == true ||
            (chatPopupPanel != null && chatPopupPanel.activeSelf)) return;

        validationErrors.Clear();
        int errorCount = 0;
        int firstErrorSheet = -1;
        Vector2Int firstErrorCoordinate = default;

        for (int s = 0; s < sheets.Count; s++)
        {
            var sheet = sheets[s];
            for (int r = 0; r < 4; r++)
            {
                for (int c = 0; c < 3; c++)
                {
                    if (sheet.Editable[r, c] && !string.IsNullOrEmpty(sheet.TargetValues[r, c]))
                    {
                        userInputs[s].TryGetValue(new Vector2Int(r + 1, c + 1), out var uVal); // 1-indexed UI row/col
                        if (string.IsNullOrEmpty(uVal)) uVal = sheet.Grid[r, c]; 

                        if (uVal.Trim() != sheet.TargetValues[r, c])
                        {
                            Vector2Int coordinate = new Vector2Int(r + 1, c + 1);
                            if (!validationErrors.TryGetValue(s, out var errors))
                            {
                                errors = new HashSet<Vector2Int>();
                                validationErrors[s] = errors;
                            }
                            errors.Add(coordinate);
                            errorCount++;
                            if (firstErrorSheet < 0)
                            {
                                firstErrorSheet = s;
                                firstErrorCoordinate = coordinate;
                            }
                        }
                    }
                }
            }
        }

        if (errorCount == 0)
        {
            WinGame();
        }
        else
        {
            AudioManager.Instance?.PlaySFX(SfxId.UIInvalid);
            currentSheetIndex = firstErrorSheet;
            UpdateTabUI();
            GenerateGrid();
            SetFeedback($"Còn {errorCount} ô chưa chính xác · Đã chuyển tới vị trí cần sửa.", errorTextColor, "!");
            PulseFeedback();
            FocusEditableCell(firstErrorCoordinate);
        }
    }

    private void WinGame()
    {
        isGameActive = false;
        attemptRunning = false;
        StopAllCoroutines();
        if (chatPopupPanel != null) chatPopupPanel.SetActive(false);
        if (submitButton != null) submitButton.interactable = false;
        if (timerProgressBar != null) timerProgressBar.anchorMax = Vector2.one;
        if (timerProgressImage != null) timerProgressImage.color = new Color(0.12f, 0.68f, 0.3f, 1f);
        SetFeedback("Dữ liệu chính xác. Đang gửi lệnh in tới máy photo.", successTextColor, "OK");
        PulseFeedback();
        AudioManager.Instance?.PlaySFX(SfxId.TaskComplete);
        StartCoroutine(CompleteTaskAfterDelay(1.5f));
    }

    private void FailGame(string reason)
    {
        isGameActive = false;
        attemptRunning = false;
        retryPending = true;
        StopAllCoroutines();
        if (chatPopupPanel != null) chatPopupPanel.SetActive(false);
        if (chatBackdrop != null) chatBackdrop.gameObject.SetActive(false);
        if (submitButton != null) submitButton.interactable = false;
        SetFeedback(reason, errorTextColor, "!");
        PulseFeedback();
        AudioManager.Instance?.PlaySFX(SfxId.UIInvalid);
        StartCoroutine(RestartAfterFailure(2.0f));
    }

    private IEnumerator RestartAfterFailure(float delay)
    {
        yield return new WaitForSeconds(delay);
        timeRemaining = AttemptDuration;
        isGameActive = true;
        attemptRunning = true;
        retryPending = false;
        if (submitButton != null) submitButton.interactable = true;
        UpdateTimerUI();
        SetFeedback("Thử lại từ dữ liệu hiện tại. Đồng hồ đã được đặt lại.", excelGreen, ">");
        FocusFirstEditableCell();
        if (!relaxedMode) StartCoroutine(TimerRoutine());
        StartCoroutine(BossDistractionRoutine());
    }

    private void UpdateCompletionSummary()
    {
        if (sheetProgressText == null || sheets == null) return;

        int correct = 0;
        int total = 0;
        for (int s = 0; s < sheets.Count; s++)
        {
            SheetData sheet = sheets[s];
            for (int r = 0; r < 4; r++)
            {
                for (int c = 0; c < 3; c++)
                {
                    if (!sheet.Editable[r, c] || string.IsNullOrEmpty(sheet.TargetValues[r, c])) continue;
                    total++;
                    string value = GetEffectiveValue(s, r, c);
                    if (value.Trim() == sheet.TargetValues[r, c]) correct++;
                }
            }
        }

        sheetProgressText.text = $"TIẾN ĐỘ\n{correct} / {total} Ô ĐÚNG";
        owner?.UpdateReportMiniGameProgress(total > 0 ? (float)correct / total : 0f);
        sheetProgressText.color = correct == total && total > 0 ? new Color(0.07f, 0.48f, 0.23f, 1f) : excelGreen;
    }

    private string GetEffectiveValue(int sheetIndex, int row, int column)
    {
        Vector2Int coordinate = new Vector2Int(row + 1, column + 1);
        if (userInputs.TryGetValue(sheetIndex, out var inputs) &&
            inputs.TryGetValue(coordinate, out string value) && !string.IsNullOrWhiteSpace(value))
            return value;
        return sheets[sheetIndex].Grid[row, column] ?? string.Empty;
    }

    private void SetFeedback(string message, Color color, string icon)
    {
        if (warningFeedbackText == null) return;
        warningFeedbackText.text = string.IsNullOrEmpty(icon) ? message : $"{icon}  {message}";
        warningFeedbackText.color = color;
    }

    private void PulseFeedback()
    {
        feedbackPulseRemaining = .3f;
        if (warningFeedbackText != null) warningFeedbackText.rectTransform.localScale = feedbackBaseScale;
    }

    private static void ConfigureButton(Button button)
    {
        if (button == null) return;
        button.transition = Selectable.Transition.ColorTint;
        button.navigation = new Navigation { mode = Navigation.Mode.Automatic };
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1.1f, 1.1f, 1.1f, 1f);
        colors.pressedColor = new Color(0.78f, 0.84f, 0.8f, 1f);
        colors.selectedColor = new Color(1.06f, 1.06f, 1.06f, 1f);
        colors.disabledColor = new Color(0.5f, 0.52f, 0.5f, 0.55f);
        colors.colorMultiplier = 1f;
        colors.fadeDuration = 0.08f;
        button.colors = colors;
        if (button.GetComponent<UIButtonJuice>() == null) button.gameObject.AddComponent<UIButtonJuice>();
    }

    private void FocusFirstEditableCell()
    {
        foreach (var entry in visibleInputs)
        {
            FocusEditableCell(entry.Key);
            return;
        }
    }

    private void FocusEditableCell(Vector2Int coordinate)
    {
        if (!visibleInputs.TryGetValue(coordinate, out TMP_InputField input) || input == null) return;
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(input.gameObject);
        input.ActivateInputField();
        input.Select();
    }

    private void PlayRevealAnimation()
    {
        if (mainPanelCanvasGroup == null) return;
        if (revealRoutine != null) StopCoroutine(revealRoutine);
        revealRoutine = StartCoroutine(RevealPanel());
    }

    private IEnumerator RevealPanel()
    {
        const float duration = 0.2f;
        float elapsed = 0f;
        mainPanelCanvasGroup.alpha = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            mainPanelCanvasGroup.alpha = 1f - Mathf.Pow(1f - t, 3f);
            yield return null;
        }
        mainPanelCanvasGroup.alpha = 1f;
        revealRoutine = null;
        if (attemptRunning && !tutorialActive) FocusFirstEditableCell();
    }
    
    private IEnumerator CompleteTaskAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        OfficeSceneController controller = owner;
        if (controller != null) controller.CompleteExcelTask();
        else ClosePanel();
    }
}
