using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class OfficeIntroHUD : MonoBehaviour
{
    public TMP_Text subtitle, objective, prompt, crosshair;
    public GameObject subtitlePanel, pausePanel;
    public Button resumeButton;

    private void Awake()
    {
        ConfigurePresentation();
    }

    public void SetSubtitle(string speaker, string text)
    {
        if(subtitlePanel) subtitlePanel.SetActive(!string.IsNullOrEmpty(text));
        if(!subtitle) return;
        if(string.IsNullOrEmpty(text)) { subtitle.text = ""; return; }
        string accent = speaker == "SẾP" ? "#FF786B" : speaker == "TÝ" ? "#F4C66A" : "#71CFE8";
        subtitle.text = "<align=center><size=76%><color=" + accent + "><b>" + speaker +
            "</b></color></size>\n<color=#F2F5F8>" + text + "</color></align>";
    }

    public void SetObjective(string text)
    {
        if(!objective) return;
        objective.text = string.IsNullOrEmpty(text) ? "" :
            "<color=#F4C66A><b>NHIỆM VỤ</b></color>  <color=#EDF3F8>" + text + "</color>";
    }

    public void SetPrompt(string text)
    {
        if(prompt)
        {
            if(string.IsNullOrEmpty(text)) prompt.text = "";
            else if(text.StartsWith("[E]"))
                prompt.text = "<color=#F4C66A><b>E</b></color>  <color=#EDF3F8>" + text.Substring(3).Trim() + "</color>";
            else prompt.text = "<color=#FF9B7A>" + text + "</color>";
        }
        if(crosshair) crosshair.color = string.IsNullOrEmpty(text) ? Color.white : new Color(1,.8f,.45f);
    }
    public void SetPaused(bool value) { if(pausePanel) pausePanel.SetActive(value); }

    private void ConfigurePresentation()
    {
        if(subtitle)
        {
            subtitle.richText = true;
            subtitle.enableAutoSizing = true;
            subtitle.fontSizeMin = 20f;
            subtitle.fontSizeMax = 30f;
            subtitle.alignment = TextAlignmentOptions.Center;
            subtitle.lineSpacing = 7f;
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
        if(!subtitlePanel) return;
        RectTransform panelRect = subtitlePanel.GetComponent<RectTransform>();
        if(panelRect)
        {
            panelRect.anchorMin = new Vector2(.5f, 0f);
            panelRect.anchorMax = new Vector2(.5f, 0f);
            panelRect.anchoredPosition = new Vector2(0f, 142f);
            panelRect.sizeDelta = new Vector2(1180f, 154f);
        }
        Image panelImage = subtitlePanel.GetComponent<Image>();
        if(panelImage) panelImage.color = new Color(.018f, .028f, .052f, .92f);
    }
}
