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
    private readonly Color bgPanelColor = new Color(0.105f, 0.145f, 0.215f, 0.95f);

    private float timeRemaining;
    private bool isGameActive = false;
    private OfficeSceneController owner;

    // We will hardcode a 3x4 grid for this specific task
    // Columns: A (Item), B (Value 1), C (Value 2)
    // Rows: 1 (Q1), 2 (Q2), 3 (Q3), 4 (Total)
    private readonly string[,] initialGrid = {
        {"Revenue", "Expenses", "Profit"},
        {"1500", "800", "700"},
        {"2100", "1500", "600"},
        {"3600", "2300", "9999"} // 9999 is the intentional error. (700+600 = 1300)
    };
    
    private readonly bool[,] isEditable = {
        {false, false, false},
        {false, false, false},
        {false, false, false},
        {false, false, true} // Only the Profit Total can be clicked and edited
    };
    
    private readonly string targetCorrectValue = "1300";
    
    private TMP_InputField targetCellInput;

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
        var tObj = new GameObject("TaskTitleText");
        tObj.transform.SetParent(mainPanel.transform, false);
        var tRect = tObj.AddComponent<RectTransform>();
        ResetRect(tRect);
        tRect.anchorMin = new Vector2(0.1f, 0.85f);
        tRect.anchorMax = new Vector2(0.9f, 0.95f);
        var tText = tObj.AddComponent<TextMeshProUGUI>();
        tText.text = "REPORT_Q3_FINAL.xlsx";
        tText.color = hintTextColor;
        tText.fontStyle = FontStyles.Bold;
        tText.alignment = TextAlignmentOptions.Left;

        var iObj = new GameObject("InstructionText");
        iObj.transform.SetParent(mainPanel.transform, false);
        var iRect = iObj.AddComponent<RectTransform>();
        ResetRect(iRect);
        iRect.anchorMin = new Vector2(0.1f, 0.75f);
        iRect.anchorMax = new Vector2(0.8f, 0.85f);
        var iText = iObj.AddComponent<TextMeshProUGUI>();
        iText.text = "SỬA BÁO CÁO CHO SẾP\nKiểm tra lại dữ liệu và sửa lỗi sai trước khi nộp.";
        iText.color = Color.white;
        iText.alignment = TextAlignmentOptions.TopLeft;
    }

    private void Awake()
    {
        CreateHeaders();

        if (chatPopupPanel != null)
        {
            ResetRect(chatPopupPanel.GetComponent<RectTransform>());
            if (chatPopupPanel.TryGetComponent<Image>(out var popBg)) popBg.color = normalOptionColor;

            ResetRect(chatSenderText?.GetComponent<RectTransform>());
            ResetRect(chatContentText?.GetComponent<RectTransform>());
            ResetRect(chatResponseButton?.GetComponent<RectTransform>());
            StretchRect(chatResponseButtonText?.GetComponent<RectTransform>());
            
            if (chatSenderText != null) chatSenderText.color = successTextColor;
            if (chatContentText != null) chatContentText.color = hintTextColor;
            if (chatResponseButtonText != null) 
            {
                chatResponseButtonText.color = Color.white;
                chatResponseButtonText.enableAutoSizing = true;
                chatResponseButtonText.fontSizeMin = 14;
                chatResponseButtonText.fontSizeMax = 36;
            }
            if (chatResponseButton != null && chatResponseButton.TryGetComponent<Image>(out var btnBg)) btnBg.color = selectedOptionColor;
        }

        if (submitButton != null) 
        {
            ResetRect(submitButton.GetComponent<RectTransform>());
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
        
        if (warningFeedbackText != null) ResetRect(warningFeedbackText.GetComponent<RectTransform>());
        if (timerText != null) 
        {
            ResetRect(timerText.GetComponent<RectTransform>());
            timerText.color = hintTextColor;
        }

        if (mainPanel != null) mainPanel.SetActive(false);
        if (chatPopupPanel != null) chatPopupPanel.SetActive(false);
        
        if (chatResponseButton != null)
        {
            chatResponseButton.onClick.AddListener(CloseChatPopup);
        }
        
        if (gridParent != null)
        {
            var grid = gridParent.GetComponent<GridLayoutGroup>();
            if (grid != null)
            {
                grid.cellSize = new Vector2(160, 45);
                grid.spacing = new Vector2(4, 4);
                grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                grid.constraintCount = 3;
                grid.childAlignment = TextAnchor.MiddleCenter;
            }
        }
        
        // Initialize default messages if empty
        if (randomMessages == null || randomMessages.Count == 0)
        {
            randomMessages = new List<ChatMessageData> {
                new ChatMessageData { senderName = "Sếp", messageContent = "Quân, báo cáo làm xong chưa? Gửi gấp nhé!", responseButtonText = "Dạ vâng sếp!" },
                new ChatMessageData { senderName = "Sếp", messageContent = "Check kĩ phần màu mè formatting, tôi không thích đâu.", responseButtonText = "Vâng em đang sửa" },
                new ChatMessageData { senderName = "HR", messageContent = "4h chiều nay phòng mình họp nhé.", responseButtonText = "Đã nhận thông tin!" },
                new ChatMessageData { senderName = "Đồng Nghiệp", messageContent = "Trưa nay ăn bún chả không?", responseButtonText = "Sắp chạy deadline tụt quần" }
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
        // Clear existing siblings first
        foreach (Transform child in gridParent)
        {
            Destroy(child.gameObject);
        }

        // Darken background if it's an Image
        if (mainPanel != null && mainPanel.TryGetComponent<Image>(out var img))
        {
            img.color = bgPanelColor;
        }

        for (int r = 0; r < 4; r++)
        {
            for (int c = 0; c < 3; c++)
            {
                var cellObj = Instantiate(cellPrefab, gridParent);
                cellObj.SetActive(true); // WE MUST ENABLE IT BECAUSE THE PREFAB IS DISABLED!
                
                // Assuming Prefab has a TMP_InputField for editable cells, or just TextMeshProUGUI for statics
                var inputField = cellObj.GetComponentInChildren<TMP_InputField>();
                var textComponent = cellObj.GetComponentInChildren<TextMeshProUGUI>();
                var bgImage = cellObj.GetComponent<Image>();
                
                string val = initialGrid[r, c];
                bool canEdit = isEditable[r, c];
                
                if (bgImage != null)
                {
                    bgImage.color = canEdit ? errorOptionColor : new Color(0.15f, 0.2f, 0.28f, 1f);
                }

                if (inputField != null)
                {
                    inputField.text = val;
                    inputField.interactable = canEdit;
                    var txt = inputField.textComponent;
                    if (txt != null) txt.color = canEdit ? Color.black : hintTextColor;
                    
                    if (canEdit)
                    {
                        targetCellInput = inputField; // Only one cell is editable in this simple mode
                    }
                }
                else if (textComponent != null)
                {
                    textComponent.text = val;
                    textComponent.color = hintTextColor;
                    textComponent.alignment = TextAlignmentOptions.Center;
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
            FailGame("TIMEOUT! Sếp đã trừ lương...");
        }
    }

    private void UpdateTimerUI()
    {
        if (timerText == null) return;
        int seconds = (int)timeRemaining;
        timerText.text = $"Hệ thống tự động nộp sau: {seconds}s";
        timerText.color = seconds < 10 ? errorTextColor : hintTextColor;
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

        if (targetCellInput != null && targetCellInput.text.Trim() == targetCorrectValue)
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
