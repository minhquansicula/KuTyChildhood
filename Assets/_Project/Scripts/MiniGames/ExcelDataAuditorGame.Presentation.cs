using TMPro;
using UnityEngine;
using UnityEngine.UI;

public partial class ExcelDataAuditorGame
{
    private GameObject workbookModal;
    private MiniGameResultCard resultCard;

    private void CompletePresentation()
    {
        workbookModal = GameUiPresentation.ModalRoot(mainPanel, "SpreadsheetModal", 130);
        if (!workbookModal) return;
        var rect = (RectTransform)mainPanel.transform;
        OfficeUiStyle.Rect(rect, Vector2.one * .5f, new Vector2(1560f, 940f), Vector2.zero);
        mainPanel.AddComponent<MiniGamePanelFit>();
        if (mainPanel.TryGetComponent<Image>(out var background))
        {
            background.sprite = OfficeUiStyle.Rounded;
            background.type = Image.Type.Sliced;
        }
        var title = mainPanel.transform.Find("ExcelRibbon/TaskTitleText")?.GetComponent<TMP_Text>();
        if (title)
        {
            title.text = "<b>BÁO CÁO CUỐI NGÀY</b>\n<size=65%>REPORT_Q3_FINAL.xlsx  /  KIỂM TRA SỐ LIỆU</size>";
            title.rectTransform.anchorMin = new Vector2(.025f, .12f);
            title.rectTransform.anchorMax = new Vector2(.72f, .90f);
            title.rectTransform.offsetMin = title.rectTransform.offsetMax = Vector2.zero;
            title.fontSizeMax = 31f;
        }
        if (timerText)
        {
            timerText.rectTransform.anchorMin = new Vector2(.75f, .14f);
            timerText.rectTransform.anchorMax = new Vector2(.98f, .86f);
            timerText.fontSizeMin = 21f;
            timerText.fontSizeMax = 29f;
        }
        if (chatPopupPanel)
        {
            chatPopupPanel.transform.SetParent(workbookModal.transform, false);
            OfficeUiStyle.Rect((RectTransform)chatPopupPanel.transform, Vector2.one * .5f,
                new Vector2(730, 460), Vector2.zero);
            chatPopupPanel.AddComponent<MiniGamePanelFit>();
            if (chatBackdrop)
            {
                chatBackdrop.transform.SetParent(workbookModal.transform, false);
                GameUiPresentation.Stretch(chatBackdrop.rectTransform, 0, 0);
            }
        }
        if (sheetProgressText) sheetProgressText.fontSizeMax = 23f;
        if (formulaHint) formulaHint.fontSizeMax = 23f;
        if (warningFeedbackText) warningFeedbackText.fontSizeMax = 25f;
        foreach (var button in mainPanel.GetComponentsInChildren<Button>(true)) MenuButtonSfx.Attach(button);
        if (chatResponseButton) MenuButtonSfx.Attach(chatResponseButton);
        resultCard = new MiniGameResultCard(mainPanel.transform, globalFont);
        workbookModal.SetActive(false);
    }

    private void RetryFromResult(bool relaxed)
    {
        if (!retryPending || GameManager.Instance?.IsPaused == true) return;
        retryPending = false;
        relaxedMode = relaxed;
        resultCard?.Hide();
        StartCoroutine(RestartAfterFailure(0f));
    }

    private void LeaveWorkbook()
    {
        if (owner != null) owner.CancelWork();
        else ClosePanel();
    }
}
