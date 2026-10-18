using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class OfficeIntroHUD : MonoBehaviour
{
    public TMP_Text subtitle, objective, prompt, crosshair;
    public GameObject subtitlePanel, pausePanel;
    public Button resumeButton;
    public void SetSubtitle(string speaker, string text)
    {
        if(subtitlePanel) subtitlePanel.SetActive(!string.IsNullOrEmpty(text));
        if(subtitle) subtitle.text = string.IsNullOrEmpty(text) ? "" : "<b>" + speaker + "</b>\n" + text;
    }
    public void SetPrompt(string text)
    {
        if(prompt) prompt.text = text;
        if(crosshair) crosshair.color = string.IsNullOrEmpty(text) ? Color.white : new Color(1,.8f,.45f);
    }
    public void SetPaused(bool value) { if(pausePanel) pausePanel.SetActive(value); }
}
