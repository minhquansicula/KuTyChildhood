using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum ExcelAssistMode { Timed, Relaxed }

/// <summary>First-run explanation and difficulty choice for the office spreadsheet.</summary>
public sealed class ExcelTutorialController : MonoBehaviour
{
    private GameObject overlay;
    private Action<ExcelAssistMode> onSelected;

    public bool IsVisible => overlay != null && overlay.activeSelf;

    public void Show(RectTransform host, TMP_FontAsset font, Action<ExcelAssistMode> selected)
    {
        if (host == null) return;
        EnsureUi(host, font);
        onSelected = selected;
        overlay.SetActive(true);
        overlay.transform.SetAsLastSibling();
    }

    public void HideWithoutSelection()
    {
        onSelected = null;
        if (overlay != null) overlay.SetActive(false);
    }

    public void Choose(ExcelAssistMode mode)
    {
        if (!IsVisible) return;
        overlay.SetActive(false);
        Action<ExcelAssistMode> callback = onSelected;
        onSelected = null;
        callback?.Invoke(mode);
    }

    private void EnsureUi(RectTransform host, TMP_FontAsset font)
    {
        if (overlay != null) return;
        overlay = new GameObject("ExcelFirstRunTutorial", typeof(RectTransform), typeof(Image));
        overlay.transform.SetParent(host, false);
        RectTransform root = (RectTransform)overlay.transform;
        root.anchorMin = Vector2.zero;
        root.anchorMax = Vector2.one;
        root.offsetMin = root.offsetMax = Vector2.zero;
        Image shield = overlay.GetComponent<Image>();
        shield.color = new Color(.025f, .045f, .055f, .97f);
        shield.raycastTarget = true;

        RectTransform card = CreateRect("TutorialCard", root, new Vector2(.5f, .5f), new Vector2(.5f, .5f),
            new Vector2(880f, 580f), Vector2.zero);
        Image cardImage = card.gameObject.AddComponent<Image>();
        cardImage.sprite = OfficeUiStyle.Rounded;
        cardImage.type = Image.Type.Sliced;
        cardImage.color = new Color(.93f, .96f, .94f, 1f);
        card.gameObject.AddComponent<MiniGamePanelFit>();

        CreateText("Title", card, font, "KIỂM TRA BÁO CÁO", 35f, FontStyles.Bold,
            new Vector2(.08f, .78f), new Vector2(.92f, .94f), new Color(.06f, .34f, .19f), TextAlignmentOptions.Center);
        CreateText("Body", card, font,
            "Có hai ô số liệu cần sửa. Ô màu xanh là ô được nhập.\n\n" +
            "Sheet Q3: Lợi nhuận = Doanh thu − Chi phí.\n" +
            "Ví dụ: 3.600 − 2.300 = 1.300.\n\n" +
            "Sheet Q4: Tổng thực tế = Marketing + Phát triển.\n" +
            "Các giá trị đúng sẽ được giữ lại khi thử lại.",
            26f, FontStyles.Normal, new Vector2(.09f, .31f), new Vector2(.91f, .77f),
            new Color(.12f, .17f, .15f), TextAlignmentOptions.TopLeft);

        Button timed = CreateButton("TimedMode", card, font, "BẮT ĐẦU · 60 GIÂY",
            new Vector2(.08f, .08f), new Vector2(.48f, .25f), new Color(.08f, .48f, .25f));
        Button relaxed = CreateButton("RelaxedMode", card, font, "THƯ THẢ · KHÔNG GIỚI HẠN",
            new Vector2(.52f, .08f), new Vector2(.92f, .25f), new Color(.18f, .32f, .42f));
        timed.onClick.AddListener(() => Choose(ExcelAssistMode.Timed));
        relaxed.onClick.AddListener(() => Choose(ExcelAssistMode.Relaxed));
    }

    private static RectTransform CreateRect(string name, Transform parent, Vector2 anchorMin,
        Vector2 anchorMax, Vector2 size, Vector2 position)
    {
        RectTransform rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = new Vector2(.5f, .5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
        return rect;
    }

    private static TextMeshProUGUI CreateText(string name, Transform parent, TMP_FontAsset font,
        string value, float size, FontStyles style, Vector2 min, Vector2 max, Color color,
        TextAlignmentOptions alignment)
    {
        RectTransform rect = CreateRect(name, parent, min, max, Vector2.zero, Vector2.zero);
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        TextMeshProUGUI text = rect.gameObject.AddComponent<TextMeshProUGUI>();
        if (font != null) text.font = font;
        if (GameUiTheme.Current && GameUiTheme.Current.font) text.font = GameUiTheme.Current.font;
        text.text = value;
        text.fontSize = size;
        text.fontStyle = style;
        text.color = color;
        text.alignment = alignment;
        text.enableWordWrapping = true;
        text.enableAutoSizing = true;
        text.fontSizeMin = size * .85f;
        text.fontSizeMax = size;
        text.raycastTarget = false;
        return text;
    }

    private static Button CreateButton(string name, Transform parent, TMP_FontAsset font, string label,
        Vector2 min, Vector2 max, Color color)
    {
        RectTransform rect = CreateRect(name, parent, min, max, Vector2.zero, Vector2.zero);
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        Image image = rect.gameObject.AddComponent<Image>();
        image.color = color;
        Button button = rect.gameObject.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1.12f, 1.12f, 1.12f, 1f);
        colors.pressedColor = new Color(.78f, .82f, .8f, 1f);
        button.colors = colors;
        MenuButtonSfx.Attach(button);
        CreateText("Label", rect, font, label, 20f, FontStyles.Bold, Vector2.zero, Vector2.one,
            Color.white, TextAlignmentOptions.Center);
        return button;
    }
}
