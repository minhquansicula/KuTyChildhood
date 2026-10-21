using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class OfficeIntroHUD : MonoBehaviour
{
    public TMP_Text subtitle, objective, prompt, crosshair;
    public GameObject subtitlePanel, pausePanel;
    public Button resumeButton;
    private Image openingFade;
    private TMP_Text speakerLabel;
    private Image promptPlate;
    private Button menuButton;
    private bool paused, restoreSubtitle, restoreObjective, restorePrompt, restoreCrosshair;
    private bool restoreSharedCrosshair;
    private bool sharedObjective;
    public float OpeningFadeAlpha => openingFade != null ? openingFade.color.a : 0f;

    private void Awake()
    {
        sharedObjective = FindObjectOfType<OfficeShiftHUD>(true) != null;
        ConfigurePresentation();
        SetPaused(false);
        SetOpeningFade(1f);
    }

    public void SetOpeningFade(float alpha)
    {
        if (openingFade == null)
        {
            var obj = new GameObject("OfficeOpeningFade", typeof(RectTransform), typeof(Canvas), typeof(Image));
            obj.transform.SetParent(transform, false);
            var rect = (RectTransform)obj.transform;
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            var canvas = obj.GetComponent<Canvas>();
            canvas.overrideSorting = true; canvas.sortingOrder = 1000;
            openingFade = obj.GetComponent<Image>();
            openingFade.raycastTarget = false;
        }
        openingFade.color = new Color(0f, 0f, 0f, Mathf.Clamp01(alpha));
        openingFade.gameObject.SetActive(alpha > 0f);
    }

    public void SetSubtitle(string speaker, string text)
    {
        if(subtitlePanel) subtitlePanel.SetActive(!paused && !string.IsNullOrEmpty(text));
        if(!subtitle) return;
        if (speakerLabel) speakerLabel.text = speaker;
        subtitle.text = text ?? string.Empty;
    }

    public void SetObjective(string text)
    {
        if(!objective) return;
        objective.text = string.IsNullOrEmpty(text) ? "" :
            "<color=#F4C66A><b>NHIỆM VỤ</b></color>  <color=#EDF3F8>" + text + "</color>";
        objective.gameObject.SetActive(!paused && !sharedObjective && !string.IsNullOrEmpty(text));
    }

    public void SetPrompt(string text)
    {
        if (promptPlate) promptPlate.gameObject.SetActive(false);
        if(prompt)
        {
            prompt.text = "";
        }
        if(crosshair) crosshair.color = string.IsNullOrEmpty(text) ? Color.white : new Color(1,.8f,.45f);
    }
    public void SetPaused(bool value)
    {
        if (pausePanel) pausePanel.SetActive(value);
        if (value) OfficeUiStyle.FitPauseCard(pausePanel);
        if (paused == value) return;
        paused = value;
        if (value)
        {
            restoreSubtitle = subtitlePanel && subtitlePanel.activeSelf;
            restoreObjective = objective && objective.gameObject.activeSelf;
            restorePrompt = promptPlate && promptPlate.gameObject.activeSelf;
            restoreCrosshair = crosshair && crosshair.gameObject.activeSelf;
            restoreSharedCrosshair = HUDController.Instance != null && HUDController.Instance.CrosshairVisible;
        }
        if (subtitlePanel) subtitlePanel.SetActive(!value && restoreSubtitle);
        if (objective) objective.gameObject.SetActive(!value && restoreObjective);
        if (promptPlate) promptPlate.gameObject.SetActive(!value && restorePrompt);
        if (crosshair) crosshair.gameObject.SetActive(!value && restoreCrosshair);
        HUDController.Instance?.SetCrosshairVisible(!value && restoreSharedCrosshair);
        if (value && resumeButton) resumeButton.Select();
    }

    private void ConfigurePresentation()
    {
        if(subtitle)
        {
            subtitle.richText = false;
            subtitle.enableAutoSizing = true;
            subtitle.fontSizeMin = 26f;
            subtitle.fontSizeMax = 34f;
            subtitle.color = OfficeUiStyle.Paper;
            subtitle.alignment = TextAlignmentOptions.MidlineLeft;
            subtitle.lineSpacing = 5f;
            subtitle.raycastTarget = false;
        }
        if(objective)
        {
            objective.richText = true;
            objective.enableAutoSizing = true;
            objective.fontSizeMin = 18f;
            objective.fontSizeMax = 26f;
        }
        if(prompt)
        {
            prompt.richText = true;
            prompt.enableAutoSizing = true;
            prompt.fontSizeMin = 18f;
            prompt.fontSizeMax = 28f;
        }
        TMP_FontAsset font = subtitle ? subtitle.font : objective ? objective.font : TMP_Settings.defaultFontAsset;
        if (pausePanel && resumeButton)
        {
            menuButton = OfficeUiStyle.BuildPause(pausePanel, resumeButton, null, font);
            menuButton.onClick.AddListener(() => {
                FindObjectOfType<OfficeIntroController>()?.Resume();
                GameManager.Instance?.ReturnToMainMenu();
            });
            MenuButtonSfx.Attach(resumeButton); MenuButtonSfx.Attach(menuButton);
        }
        if (crosshair) { crosshair.text = "·"; crosshair.fontSize = 32f; crosshair.color = new Color(1f, 1f, 1f, .65f); }
        if (prompt)
        {
            promptPlate = OfficeUiStyle.Panel("OfficeInteractionPlate", transform, new Vector2(700, 62), Vector2.zero, OfficeUiStyle.Ink);
            OfficeUiStyle.Rect(promptPlate.rectTransform, new Vector2(.5f, 0), new Vector2(700, 62), new Vector2(0, 236));
            prompt.transform.SetParent(promptPlate.transform, false);
            OfficeUiStyle.Rect(prompt.rectTransform, Vector2.one * .5f, new Vector2(660, 54), Vector2.zero);
            prompt.fontSize = 25f; prompt.color = OfficeUiStyle.Paper; prompt.alignment = TextAlignmentOptions.Center;
            promptPlate.gameObject.SetActive(false);
        }
        if(!subtitlePanel) return;
        RectTransform panelRect = subtitlePanel.GetComponent<RectTransform>();
        if(panelRect)
        {
            panelRect.anchorMin = new Vector2(.12f, 0f);
            panelRect.anchorMax = new Vector2(.88f, 0f);
            panelRect.pivot = new Vector2(.5f, 0f);
            panelRect.anchoredPosition = new Vector2(0f, 42f);
            panelRect.sizeDelta = new Vector2(0f, 160f);
            panelRect.localScale = Vector3.one;
        }
        Image panelImage = subtitlePanel.GetComponent<Image>();
        if(panelImage)
        {
            panelImage.sprite = OfficeUiStyle.Rounded; panelImage.type = Image.Type.Sliced;
            panelImage.color = OfficeUiStyle.Ink; panelImage.raycastTarget = false;
        }
        speakerLabel = OfficeUiStyle.Label("Speaker", subtitlePanel.transform, font, "", 19f,
            new Vector2(400, 30), Vector2.zero, OfficeUiStyle.Gold);
        speakerLabel.characterSpacing = 2f;
        speakerLabel.alignment = TextAlignmentOptions.Left;
        OfficeUiStyle.Rect(speakerLabel.rectTransform, new Vector2(0f, 1f), new Vector2(400, 30), new Vector2(234, -29));
        if (subtitle)
        {
            subtitle.rectTransform.anchorMin = Vector2.zero; subtitle.rectTransform.anchorMax = Vector2.one;
            subtitle.rectTransform.offsetMin = new Vector2(34, 19); subtitle.rectTransform.offsetMax = new Vector2(-34, -49);
        }
    }
}
