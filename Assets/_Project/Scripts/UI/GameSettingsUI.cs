using TMPro;
using UnityEngine;
using UnityEngine.UI;

public static class GameUiPreferences
{
    public static float MasterVolume => PlayerPrefs.GetFloat("KuTy.Settings.MasterVolume", 1f);
    public static float LookSensitivity => PlayerPrefs.GetFloat("KuTy.Settings.LookSensitivity", 1f);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void ApplyAudio() { AudioListener.volume = MasterVolume; }

    public static void SetMasterVolume(float value)
    {
        value = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat("KuTy.Settings.MasterVolume", value);
        AudioListener.volume = value;
    }
    public static void SetLookSensitivity(float value)
        => PlayerPrefs.SetFloat("KuTy.Settings.LookSensitivity", Mathf.Clamp(value, .5f, 2f));
}

/// <summary>Complete the existing menu's settings panel without replacing its button bindings.</summary>
public sealed class GameSettingsUI : MonoBehaviour
{
    private Slider volume;
    private Slider sensitivity;
    private TMP_Text volumeValue;
    private TMP_Text sensitivityValue;

    public static void Build(MainMenuUI menu)
    {
        Transform settings = null;
        foreach (var candidate in menu.GetComponentsInChildren<Transform>(true))
            if (candidate.name == "Panel_Settings") { settings = candidate; break; }
        if (!settings || settings.GetComponent<GameSettingsUI>()) return;
        settings.gameObject.AddComponent<GameSettingsUI>().BuildView();
    }

    private void BuildView()
    {
        var canvas = GetComponentInParent<Canvas>(true);
        if (canvas) transform.SetParent(canvas.rootCanvas.transform, false);
        Button back = GetComponentInChildren<Button>(true);
        foreach (Transform child in transform) child.gameObject.SetActive(false);
        var rect = transform as RectTransform;
        if (!rect) return;
        GameUiPresentation.Stretch(rect, 0, 0);
        var dimmer = GetComponent<Image>();
        if (!dimmer) dimmer = gameObject.AddComponent<Image>();
        dimmer.sprite = null;
        dimmer.color = new Color(.025f, .04f, .035f, .82f);
        dimmer.raycastTarget = true;
        var card = GameUiPresentation.Panel("SettingsCard", transform, Vector2.one * .5f,
            new Vector2(940, 700), Vector2.zero);
        GameUiPresentation.Card(card, true);
        var theme = GameUiTheme.Current;
        OfficeUiStyle.Label("SettingsHeading", card.transform, theme.font, "Thiết lập", 44,
            new Vector2(780, 70), new Vector2(0, 252), theme.text);
        OfficeUiStyle.Label("SettingsDescription", card.transform, theme.font, "Chọn cảm giác thoải mái cho hành trình của bạn.", 24,
            new Vector2(780, 45), new Vector2(0, 188), theme.muted);
        volume = BuildSlider(card.transform, "Âm lượng chung", 68, 0, 1, out volumeValue);
        sensitivity = BuildSlider(card.transform, "Độ nhạy chuột", -94, .5f, 2, out sensitivityValue);
        volume.onValueChanged.AddListener(value => { GameUiPreferences.SetMasterVolume(value); RefreshValues(); });
        sensitivity.onValueChanged.AddListener(value => { GameUiPreferences.SetLookSensitivity(value); RefreshValues(); });
        if (back)
        {
            back.transform.SetParent(card.transform, false);
            back.gameObject.SetActive(true);
            OfficeUiStyle.Rect((RectTransform)back.transform, Vector2.one * .5f,
                new Vector2(410, 86), new Vector2(0, -247));
            var caption = back.GetComponentInChildren<TMP_Text>(true);
            if (caption) caption.text = "Quay lại · Esc";
            GameUiPresentation.Button(back);
        }
        RefreshValues();
    }

    private void OnEnable() { RefreshValues(); }
    private void OnDisable() { PlayerPrefs.Save(); }
    private void Update() { if (Input.GetKeyDown(KeyCode.Escape)) gameObject.SetActive(false); }
    private void RefreshValues()
    {
        if (!volume || !sensitivity) return;
        volume.SetValueWithoutNotify(GameUiPreferences.MasterVolume);
        sensitivity.SetValueWithoutNotify(GameUiPreferences.LookSensitivity);
        volumeValue.text = Mathf.RoundToInt(GameUiPreferences.MasterVolume * 100) + " %";
        sensitivityValue.text = GameUiPreferences.LookSensitivity.ToString("0.0", System.Globalization.CultureInfo.GetCultureInfo("vi-VN")) + "×";
    }

    private static Slider BuildSlider(Transform parent, string caption, float y, float min, float max, out TMP_Text value)
    {
        var theme = GameUiTheme.Current;
        var label = OfficeUiStyle.Label(caption, parent, theme.font, caption, 28, new Vector2(720, 40), new Vector2(0, y + 49), theme.text);
        label.alignment = TextAlignmentOptions.Left;
        var obj = new GameObject(caption + "Slider", typeof(RectTransform), typeof(Slider));
        obj.transform.SetParent(parent, false);
        OfficeUiStyle.Rect((RectTransform)obj.transform, Vector2.one * .5f, new Vector2(560, 48), new Vector2(-80, y));
        var track = OfficeUiStyle.Panel("Track", obj.transform, new Vector2(560, 12), Vector2.zero, new Color(.59f, .53f, .38f));
        track.raycastTarget = true;
        var fill = OfficeUiStyle.Panel("Fill", track.transform, Vector2.zero, Vector2.zero, new Color(.28f, .43f, .34f));
        var handle = GameUiPresentation.Disc("Handle", obj.transform, Vector2.zero, 32, theme.accent);
        handle.raycastTarget = true;
        var slider = obj.GetComponent<Slider>();
        slider.fillRect = fill.rectTransform;
        slider.handleRect = handle.rectTransform;
        slider.targetGraphic = handle;
        slider.minValue = min; slider.maxValue = max;
        value = OfficeUiStyle.Label("Value", parent, theme.font, "", 28, new Vector2(130, 44), new Vector2(304, y), theme.text);
        return slider;
    }
}
