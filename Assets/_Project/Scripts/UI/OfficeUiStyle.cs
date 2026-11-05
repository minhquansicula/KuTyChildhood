using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Office text panels retain their own readable treatment; pause uses the Main Menu theme.</summary>
public static class OfficeUiStyle
{
    public static readonly Color Ink = new Color(.055f, .085f, .095f, .98f);
    public static readonly Color Paper = new Color(.96f, .94f, .87f);
    public static readonly Color Muted = new Color(.64f, .71f, .70f);
    public static readonly Color Gold = new Color(.87f, .70f, .42f);
    private static Sprite rounded;

    public static Sprite Rounded
    {
        get
        {
            if (rounded != null) return rounded;
            const int size = 64;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { name = "Office UI rounded panel", hideFlags = HideFlags.HideAndDontSave };
            texture.wrapMode = TextureWrapMode.Clamp;
            for (int y = 0; y < size; y++) for (int x = 0; x < size; x++)
            {
                float dx = Mathf.Max(12f - x, x - 51f, 0f);
                float dy = Mathf.Max(12f - y, y - 51f, 0f);
                float alpha = Mathf.Clamp01(12f - Mathf.Sqrt(dx * dx + dy * dy));
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
            texture.Apply(false, true);
            rounded = Sprite.Create(texture, new Rect(0, 0, size, size), Vector2.one * .5f, 100f, 0, SpriteMeshType.FullRect, Vector4.one * 14f);
            rounded.name = "Office UI rounded panel";
            rounded.hideFlags = HideFlags.HideAndDontSave;
            return rounded;
        }
    }

    public static void Rect(RectTransform rect, Vector2 anchor, Vector2 size, Vector2 position)
    {
        rect.anchorMin = rect.anchorMax = anchor;
        rect.pivot = new Vector2(.5f, .5f);
        rect.sizeDelta = size; rect.anchoredPosition = position;
        rect.localScale = Vector3.one;
    }

    public static Image Panel(string name, Transform parent, Vector2 size, Vector2 position, Color color)
    {
        var obj = new GameObject(name, typeof(RectTransform), typeof(Image));
        obj.transform.SetParent(parent, false);
        Rect((RectTransform)obj.transform, Vector2.one * .5f, size, position);
        var image = obj.GetComponent<Image>();
        image.sprite = size.x > 10 && size.y > 10 ? Rounded : null; image.type = Image.Type.Sliced; image.pixelsPerUnitMultiplier = 1f; image.color = color; image.raycastTarget = false;
        return image;
    }

    public static TextMeshProUGUI Label(string name, Transform parent, TMP_FontAsset font, string text,
        float size, Vector2 bounds, Vector2 position, Color color)
    {
        var obj = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        obj.transform.SetParent(parent, false);
        Rect((RectTransform)obj.transform, Vector2.one * .5f, bounds, position);
        var label = obj.GetComponent<TextMeshProUGUI>();
        label.font = font; label.text = text; label.fontSize = size; label.color = color;
        label.alignment = TextAlignmentOptions.Center; label.raycastTarget = false;
        return label;
    }

    public static Button BuildPause(GameObject panel, Button resume, Button menu, TMP_FontAsset font)
    {
        if (!panel || !resume) return menu;
        var theme = GameUiTheme.Current;
        if (!theme) return menu;
        font = theme.font;
        GameUiPresentation.ConfigureCanvas(panel.GetComponentInParent<Canvas>(true));
        foreach (Transform old in panel.transform) old.gameObject.SetActive(false);
        var canvas = panel.GetComponent<Canvas>();
        if (canvas == null) canvas = panel.AddComponent<Canvas>();
        canvas.overrideSorting = true; canvas.sortingOrder = 1100;
        if (!panel.GetComponent<GraphicRaycaster>()) panel.AddComponent<GraphicRaycaster>();
        var overlay = panel.GetComponent<Image>();
        if (overlay == null) overlay = panel.AddComponent<Image>();
        overlay.sprite = null; overlay.color = new Color(.015f, .025f, .03f, .65f); overlay.raycastTarget = true;
        var overlayRect = panel.GetComponent<RectTransform>();
        overlayRect.anchorMin = Vector2.zero; overlayRect.anchorMax = Vector2.one;
        overlayRect.offsetMin = overlayRect.offsetMax = Vector2.zero;

        // Use the illustrated main menu artwork, not a stretched golden button as a panel.
        var card = Panel("OfficePauseCard", panel.transform, new Vector2(720f, 790f), Vector2.zero,
            new Color(.055f, .075f, .055f, .88f));
        if (theme.pauseLogo)
        {
            var logo = Panel("PauseLogo", card.transform, new Vector2(570f, 190f), new Vector2(0, 252f), Color.white);
            logo.sprite = theme.pauseLogo;
            logo.type = Image.Type.Simple;
            logo.preserveAspect = true;
        }
        else
            Label("Chapter", card.transform, font, "KU TÝ", 56f, new Vector2(570, 190), new Vector2(0, 252), GameUiPresentation.Gold);
        Label("PauseTitle", card.transform, font, "Nghỉ một chút", 42f, new Vector2(580, 66), new Vector2(0, 112), GameUiPresentation.Paper);
        var description = Label("PauseDescription", card.transform, font, "Hít thở chậm lại.\nTiếp tục khi bạn sẵn sàng.", 23f,
            new Vector2(580, 78), new Vector2(0, 40), GameUiPresentation.Muted);
        description.lineSpacing = 8f;
        StyleButton(resume, card.transform, font, "Tiếp tục", new Vector2(0, -75));
        if (!menu)
        {
            var image = Panel("OfficePauseMenu", card.transform, new Vector2(450, 66), new Vector2(0, -159), Ink);
            menu = image.gameObject.AddComponent<Button>();
        }
        StyleButton(menu, card.transform, font, "Về menu chính", new Vector2(0, -205));
        Label("PauseFooter", card.transform, font, "ESC  /  Tiếp tục", 18f, new Vector2(540, 30), new Vector2(0, -326), GameUiPresentation.Muted);
        FitPauseCard(panel);
        return menu;
    }

    public static void FitPauseCard(GameObject panel)
    {
        if (!panel) return;
        var card = panel.transform.Find("OfficePauseCard");
        var canvas = panel.GetComponent<Canvas>();
        if (card == null || canvas == null) return;
        var available = ((RectTransform)canvas.rootCanvas.transform).rect.size - Vector2.one * 64f;
        var size = ((RectTransform)card).sizeDelta;
        float scale = Mathf.Max(.01f, Mathf.Min(1f, available.x / size.x, available.y / size.y));
        card.localScale = Vector3.one * scale;
    }

    private static void StyleButton(Button button, Transform parent, TMP_FontAsset font, string caption, Vector2 position)
    {
        button.transform.SetParent(parent, false);
        button.gameObject.SetActive(true);
        Rect((RectTransform)button.transform, Vector2.one * .5f, new Vector2(590f, 192f), position);
        foreach (Transform old in button.transform) old.gameObject.SetActive(false);
        var image = button.GetComponent<Image>();
        if (image == null) image = button.gameObject.AddComponent<Image>();
        button.targetGraphic = image;
        var label = Label("Caption", button.transform, font, caption, 29f, new Vector2(420, 60), new Vector2(0, -3), Paper);
        label.enableAutoSizing = true;
        label.fontSizeMin = 23f;
        label.fontSizeMax = 29f;
        GameUiTheme.Current.StyleButton(button);
        if (GameUiTheme.Current.woodenButton) image.sprite = GameUiTheme.Current.woodenButton;
        // Ignore transparent padding in the artwork when clicking between the two signs.
        image.raycastPadding = new Vector4(18f, 42f, 18f, 42f);
    }
}
