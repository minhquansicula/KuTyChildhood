using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
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

    private readonly Color normalOptionColor = new Color(0.105f, 0.145f, 0.215f, 1f);
    private readonly Color selectedOptionColor = new Color(0.12f, 0.34f, 0.28f, 1f);
    private readonly Color errorOptionColor = new Color(0.48f, 0.13f, 0.16f, 1f);
    private readonly Color successTextColor = new Color(0.42f, 0.92f, 0.64f, 1f);
    private readonly Color errorTextColor = new Color(1f, 0.43f, 0.43f, 1f);
    private readonly Color hintTextColor = new Color(0.72f, 0.78f, 0.88f, 1f);
    private readonly Color bgPanelColor = new Color(0.96f, 0.96f, 0.98f, 1f); 

    private float timeRemaining;
    private bool isGameActive = false;
    private OfficeSceneController owner;
    private TMP_FontAsset globalFont;
    
    private RectTransform timerProgressBar;
    private Image timerProgressImage;

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

    private void InitializeSheets()
    {
        sheets = new List<SheetData>()
        {
            new SheetData
            {
                Name = "Q3_Report",
                Grid = new string[,] {
                    {"Revenue", "Expenses", "Profit"},
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
                    {"Project", "Budget", "Actual"},
                    {"Mktg", "5000", "4500"},
                    {"Dev", "8000", "7000"},
                    {"Total", "13000", "12000"} // Actual target 11500
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
        
        // 1. Ribbon Menu
        var ribbonObj = new GameObject("ExcelRibbon");
        ribbonObj.transform.SetParent(mainPanel.transform, false);
        var ribbonRect = ribbonObj.AddComponent<RectTransform>();
        StretchRect(ribbonRect);
        ribbonRect.anchorMin = new Vector2(0f, 0.85f);
        ribbonRect.anchorMax = new Vector2(1f, 1f);
        var ribbonImg = ribbonObj.AddComponent<Image>();
        ribbonImg.sprite = null;
        ribbonImg.color = new Color(0.12f, 0.45f, 0.25f, 1f); // Excel Green

        var tObj = new GameObject("TaskTitleText");
        tObj.transform.SetParent(ribbonObj.transform, false);
        var tRect = tObj.AddComponent<RectTransform>();
        StretchRect(tRect);
        tRect.offsetMin = new Vector2(25, 0); // Padding left
        var tText = tObj.AddComponent<TextMeshProUGUI>();
        tText.text = "REPORT_Q3_FINAL.xlsx - Excel";
        tText.color = Color.white;
        tText.fontStyle = FontStyles.Bold;
        tText.alignment = TextAlignmentOptions.MidlineLeft;
        if (globalFont != null) tText.font = globalFont;

        var iObj = new GameObject("InstructionText");
        iObj.transform.SetParent(mainPanel.transform, false);
        var iRect = iObj.AddComponent<RectTransform>();
        ResetRect(iRect);
        iRect.anchorMin = new Vector2(0.1f, 0.70f);
        iRect.anchorMax = new Vector2(0.9f, 0.82f);
        var iText = iObj.AddComponent<TextMeshProUGUI>();
        iText.text = "SỬA BÁO CÁO CHO SẾP\nKiểm tra lại dữ liệu và sửa lỗi sai trước khi nộp.";
        iText.color = new Color(0.15f, 0.15f, 0.15f, 1f); // Black text
        iText.alignment = TextAlignmentOptions.TopLeft;
        if (globalFont != null) iText.font = globalFont;
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
        tabsImg.color = new Color(0.85f, 0.85f, 0.85f, 1f);

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

            var tabLe = tabObj.AddComponent<LayoutElement>();
            tabLe.minWidth = 140; 
            
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
            if (globalFont != null) txt.font = globalFont;
        }

        UpdateTabUI();
    }

    private void SwitchSheet(int index)
    {
        if (index == currentSheetIndex) return;
        currentSheetIndex = index;
        UpdateTabUI();
        GenerateGrid();
    }

    private void UpdateTabUI()
    {
        for (int i = 0; i < tabBgImages.Count; i++)
        {
            if (tabBgImages[i] != null)
            {
                tabBgImages[i].color = (i == currentSheetIndex) ? Color.white : new Color(0.75f, 0.75f, 0.75f, 1f);
            }
        }
    }

    private void Awake()
    {
        if (submitButton != null) 
        {
            var tmp = submitButton.GetComponentInChildren<TextMeshProUGUI>();
            if (tmp != null) globalFont = tmp.font;
        }

        InitializeSheets();
        CreateHeaders();
        CreateTabs();

        if (chatPopupPanel != null)
        {
            ResetRect(chatPopupPanel.GetComponent<RectTransform>());
            if (chatPopupPanel.TryGetComponent<Image>(out var popBg)) 
            {
                popBg.sprite = null;
                popBg.color = Color.white;
            }
            
            // Add slight padding/border to Popup
            var popOutline = chatPopupPanel.gameObject.GetComponent<Outline>() ?? chatPopupPanel.gameObject.AddComponent<Outline>();
            popOutline.effectColor = new Color(0.8f, 0.8f, 0.8f, 1f);
            popOutline.effectDistance = new Vector2(2, -2);

            var titleBar = new GameObject("TitleBar");
            titleBar.transform.SetParent(chatPopupPanel.transform, false);
            var titleBarRect = titleBar.AddComponent<RectTransform>();
            StretchRect(titleBarRect);
            titleBarRect.anchorMin = new Vector2(0f, 0.8f);
            titleBarRect.anchorMax = new Vector2(1f, 1f);
            var titleImg = titleBar.AddComponent<Image>();
            titleImg.sprite = null;
            titleImg.color = new Color(0.12f, 0.45f, 0.85f, 1f); // Zalo Blue

            ResetRect(chatSenderText?.GetComponent<RectTransform>());
            ResetRect(chatContentText?.GetComponent<RectTransform>());
            ResetRect(chatResponseButton?.GetComponent<RectTransform>());
            StretchRect(chatResponseButtonText?.GetComponent<RectTransform>());
            
            if (chatSenderText != null) 
            {
                chatSenderText.transform.SetParent(titleBar.transform, false);
                StretchRect(chatSenderText.GetComponent<RectTransform>());
                chatSenderText.GetComponent<RectTransform>().offsetMin = new Vector2(10, 0); // padding left
                chatSenderText.color = Color.white;
                chatSenderText.alignment = TextAlignmentOptions.MidlineLeft;
                if (globalFont != null) chatSenderText.font = globalFont;
            }

            if (chatContentText != null) 
            {
                // Push content down below title bar
                var cRect = chatContentText.GetComponent<RectTransform>();
                if (cRect != null) cRect.anchorMax = new Vector2(cRect.anchorMax.x, 0.75f);
                chatContentText.color = Color.black;
                if (globalFont != null) chatContentText.font = globalFont;
            }

            if (chatResponseButtonText != null) 
            {
                chatResponseButtonText.color = Color.white;
                chatResponseButtonText.enableAutoSizing = true;
                chatResponseButtonText.fontSizeMin = 14;
                chatResponseButtonText.fontSizeMax = 36;
                if (globalFont != null) chatResponseButtonText.font = globalFont;
            }
            if (chatResponseButton != null && chatResponseButton.TryGetComponent<Image>(out var btnBg)) 
            {
                btnBg.sprite = null;
                btnBg.color = new Color(0.1f, 0.45f, 0.85f, 1f); // Blue Zalo
            }
        }

        if (submitButton != null) 
        {
            var sRect = submitButton.GetComponent<RectTransform>();
            ResetRect(sRect);
            sRect.anchorMin = new Vector2(0.35f, 0.05f);
            sRect.anchorMax = new Vector2(0.65f, 0.15f);
            if (submitButton.TryGetComponent<Image>(out var subBg)) subBg.color = selectedOptionColor;
            submitButton.onClick.AddListener(CheckValidation);
            var btnText = submitButton.GetComponentInChildren<TextMeshProUGUI>();
            if (btnText != null) 
            {
                StretchRect(btnText.GetComponent<RectTransform>());
                btnText.text = "GỬI BÁO CÁO";
                btnText.color = Color.white;
                btnText.alignment = TextAlignmentOptions.Center;
                btnText.enableAutoSizing = true;
                btnText.fontSizeMin = 14;
                btnText.fontSizeMax = 36;
            }
        }
        
        if (warningFeedbackText != null) 
        {
            ResetRect(warningFeedbackText.GetComponent<RectTransform>());
            var wRect = warningFeedbackText.GetComponent<RectTransform>();
            wRect.anchorMin = new Vector2(0.1f, 0.26f);
            wRect.anchorMax = new Vector2(0.9f, 0.35f);
            warningFeedbackText.color = Color.red;
            warningFeedbackText.alignment = TextAlignmentOptions.Center;
            if (globalFont != null) warningFeedbackText.font = globalFont;
        }

        if (timerText != null) 
        {
            timerText.gameObject.SetActive(false); // Hide text timer, use Progress Bar instead
        }

        // Create Progress Bar background right below Ribbon
        var pbBg = new GameObject("ProgressBarBg");
        pbBg.transform.SetParent(mainPanel.transform, false);
        var pbBgRect = pbBg.AddComponent<RectTransform>();
        StretchRect(pbBgRect);
        pbBgRect.anchorMin = new Vector2(0f, 0.84f); 
        pbBgRect.anchorMax = new Vector2(1f, 0.85f);
        var pbBgImg = pbBg.AddComponent<Image>();
        pbBgImg.sprite = null;
        pbBgImg.color = new Color(0.85f, 0.85f, 0.85f, 1f);

        // Create Progress Bar Fill
        var pbFill = new GameObject("ProgressBarFill");
        pbFill.transform.SetParent(pbBg.transform, false);
        timerProgressBar = pbFill.AddComponent<RectTransform>();
        StretchRect(timerProgressBar); // Anchor 0..1 initially
        timerProgressImage = pbFill.AddComponent<Image>();
        timerProgressImage.sprite = null;
        timerProgressImage.color = new Color(0.13f, 0.65f, 0.27f, 1f);

        if (mainPanel != null) mainPanel.SetActive(false);
        if (chatPopupPanel != null) chatPopupPanel.SetActive(false);
        
        if (chatResponseButton != null)
        {
            chatResponseButton.onClick.AddListener(CloseChatPopup);
        }
        // Clean up Awake layout remnants
        // ...
        
        // Initialize default messages if empty
        if (randomMessages == null || randomMessages.Count == 0)
        {
            randomMessages = new List<ChatMessageData> {
                new ChatMessageData { senderName = "Quản lý", messageContent = "Báo cáo nộp đi em! Kiểm tra số liệu nha.", responseButtonText = "Dạ vâng, nộp đây!" },
                new ChatMessageData { senderName = "Quản lý", messageContent = "Sửa format cho đúng, tôi không thích bảng xấu.", responseButtonText = "Vâng tôi đang làm" },
                new ChatMessageData { senderName = "Nhân Sự", messageContent = "4h nay phòng mình có ca họp.", responseButtonText = "Đã nhận thông tin!" },
                new ChatMessageData { senderName = "Đồng Nghiệp", messageContent = "Trưa nay ăn bún không bạn ơi?", responseButtonText = "Xin lỗi, tôi đang làm" }
            };
        }
    }

    public void Open(OfficeSceneController controller)
    {
        gameObject.SetActive(true);
        owner = controller;
        isGameActive = true;
        timeRemaining = timeLimit;
        
        mainPanel.SetActive(true);
        mainPanel.transform.SetAsLastSibling();
        chatPopupPanel.SetActive(false);
        
        if (warningFeedbackText != null) warningFeedbackText.text = "";
        
        GenerateGrid();
        StartCoroutine(TimerRoutine());
        StartCoroutine(BossDistractionRoutine());
    }

    public void ClosePanel()
    {
        isGameActive = false;
        StopAllCoroutines();
        if (mainPanel != null) mainPanel.SetActive(false);
    }

    private void GenerateGrid()
    {
        foreach (Transform child in gridParent)
        {
            Destroy(child.gameObject);
        }

        if (mainPanel != null && mainPanel.TryGetComponent<Image>(out var img))
        {
            img.color = bgPanelColor;
            img.sprite = null;
        }

        if (gridParent != null)
        {
            var gridImg = gridParent.GetComponent<Image>();
            if (gridImg != null) gridImg.enabled = false; // Disable huge gray background

            var grid = gridParent.GetComponent<GridLayoutGroup>();
            if (grid != null)
            {
                grid.cellSize = new Vector2(170, 35);
                grid.spacing = new Vector2(0, 0); // Outlines will act as borders
                grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                grid.constraintCount = 4; // 1 header + 3 col
                grid.childAlignment = TextAnchor.MiddleCenter;
            }
        }

        for (int r = 0; r < 5; r++)
        {
            for (int c = 0; c < 4; c++)
            {
                var cellObj = Instantiate(cellPrefab, gridParent);
                cellObj.SetActive(true); 
                
                var inputField = cellObj.GetComponentInChildren<TMP_InputField>();
                var textComponent = cellObj.GetComponentInChildren<TextMeshProUGUI>();
                
                // Kill all rounded corners and add exact 1px border
                foreach (var cImg in cellObj.GetComponentsInChildren<Image>())
                {
                    cImg.sprite = null; 
                    cImg.type = Image.Type.Simple;
                    var outline = cImg.gameObject.GetComponent<Outline>();
                    if (outline == null) outline = cImg.gameObject.AddComponent<Outline>();
                    outline.effectColor = new Color(0.7f, 0.7f, 0.7f, 1f); // Border color
                    outline.effectDistance = new Vector2(1, -1);
                }
                
                var rootBg = cellObj.GetComponent<Image>();

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

                if (rootBg != null)
                {
                    rootBg.color = isHeader ? new Color(0.9f, 0.92f, 0.94f, 1f) : Color.white;
                }

                if (inputField != null)
                {
                    var colors = inputField.colors;
                    colors.disabledColor = isHeader ? new Color(0.9f, 0.92f, 0.94f, 1f) : Color.white;
                    colors.normalColor = Color.white;
                    inputField.colors = colors;

                    // Hide placeholder completely
                    if (inputField.placeholder != null) inputField.placeholder.gameObject.SetActive(false);

                    inputField.text = val;
                    inputField.interactable = canEdit;
                    
                    var txt = inputField.textComponent;
                    if (txt != null) 
                    {
                        if (globalFont != null) txt.font = globalFont;
                        txt.color = isHeader ? new Color(0.2f, 0.2f, 0.2f, 1f) : Color.black; 
                        txt.fontStyle = isHeader ? FontStyles.Bold : FontStyles.Normal;
                        txt.alignment = TextAlignmentOptions.Center;
                    }
                    
                    if (canEdit)
                    {
                        int captureR = r;
                        int captureC = c;
                        inputField.onValueChanged.RemoveAllListeners();
                        inputField.onValueChanged.AddListener((changedVal) => 
                        {
                            userInputs[currentSheetIndex][new Vector2Int(captureR, captureC)] = changedVal;
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
                }
            }
        }
    }

    private IEnumerator TimerRoutine()
    {
        while (isGameActive && timeRemaining > 0)
        {
            timeRemaining -= Time.deltaTime;
            UpdateTimerUI();
            yield return null;
        }

        if (timeRemaining <= 0)
        {
            FailGame("TIMEOUT!\nQuản lý đã nổi giận vì quên nộp báo cáo...");
        }
    }

    private void UpdateTimerUI()
    {
        if (timerProgressBar != null)
        {
            float fill = Mathf.Clamp01(timeRemaining / timeLimit);
            timerProgressBar.anchorMax = new Vector2(fill, 1f);
            if (timerProgressImage != null)
            {
                timerProgressImage.color = Color.Lerp(Color.red, new Color(0.13f, 0.65f, 0.27f, 1f), fill);
            }
        }
    }

    private IEnumerator BossDistractionRoutine()
    {
        while (isGameActive)
        {
            // Wait 10 to 18 seconds before popping up
            yield return new WaitForSeconds(Random.Range(10f, 18f));
            
            if (!isGameActive) yield break;

            ShowChatPopup();

            // Freeze the input/timer wait until they close it
            while (chatPopupPanel.activeSelf)
            {
                yield return null;
            }
        }
    }

    private void ShowChatPopup()
    {
        var randomMsg = randomMessages[Random.Range(0, randomMessages.Count)];
        
        chatSenderText.text = randomMsg.senderName;
        chatContentText.text = randomMsg.messageContent;
        chatResponseButtonText.text = randomMsg.responseButtonText;
        
        chatPopupPanel.SetActive(true);
        chatPopupPanel.transform.SetAsLastSibling();
        
        // Maybe play a "Ting" sound using owner
        // owner?.PlayChatSound();
    }

    private void CloseChatPopup()
    {
        chatPopupPanel.SetActive(false);
    }

    private void CheckValidation()
    {
        if (!isGameActive) return;

        bool allCorrect = true;

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
                            allCorrect = false;
                        }
                    }
                }
            }
        }

        if (allCorrect)
        {
            WinGame();
        }
        else
        {
            if (warningFeedbackText != null)
            {
                warningFeedbackText.text = "SAI SỐ LIỆU! TÍNH LẠI ĐI!!";
                warningFeedbackText.color = Color.red;
            }
        }
    }

    private void WinGame()
    {
        isGameActive = false;
        StopAllCoroutines();
        if (warningFeedbackText != null)
        {
            warningFeedbackText.text = "CHÍNH XÁC! REPORT ĐÃ ĐƯỢC GỬI ĐI.";
            warningFeedbackText.color = Color.green;
        }
        StartCoroutine(CompleteTaskAfterDelay(1.5f));
    }

    private void FailGame(string reason)
    {
        isGameActive = false;
        StopAllCoroutines();
        if (warningFeedbackText != null)
        {
            warningFeedbackText.text = reason;
            warningFeedbackText.color = Color.red;
        }
        StartCoroutine(CompleteTaskAfterDelay(2.0f));
    }
    
    private IEnumerator CompleteTaskAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        ClosePanel();
        
        // Pass a dummy or newly added OfficeWorkTask.ExcelData if it was added to the enum.
        // For now, it notifies the scene controller directly.
        owner?.CompleteExcelTask();
    }
}
