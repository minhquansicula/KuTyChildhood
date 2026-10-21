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
        font = theme.font;
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

        var card = Panel("OfficePauseCard", panel.transform, new Vector2(590f, 570f), Vector2.zero, Color.white);
        card.sprite = theme.PanelFrame; card.pixelsPerUnitMultiplier = 5f;
        Panel("Accent", card.transform, new Vector2(64f, 3f), new Vector2(0, 235f), theme.accent);
        Label("Chapter", card.transform, font, "CHÌA KHÓA KÝ ỨC", 17f, new Vector2(480, 32), new Vector2(0, 194), theme.accent).characterSpacing = 3f;
        Label("PauseTitle", card.transform, font, "Nghỉ một chút", 46f, new Vector2(500, 72), new Vector2(0, 124), theme.text);
        var description = Label("PauseDescription", card.transform, font, "Hít thở chậm lại.\nTiếp tục khi bạn sẵn sàng.", 23f,
            new Vector2(480, 78), new Vector2(0, 43), theme.muted);
        description.lineSpacing = 8f;
        StyleButton(resume, card.transform, font, "Tiếp tục", new Vector2(0, -61), true);
        if (!menu)
        {
            var image = Panel("OfficePauseMenu", card.transform, new Vector2(450, 66), new Vector2(0, -159), Ink);
            menu = image.gameObject.AddComponent<Button>();
        }
        StyleButton(menu, card.transform, font, "Về menu chính", new Vector2(0, -159), false);
        Label("PauseFooter", card.transform, font, "ESC  /  Tiếp tục", 18f, new Vector2(450, 30), new Vector2(0, -235), theme.muted);
        FitPauseCard(panel);
        return menu;
    }

    public static void FitPauseCard(GameObject panel)
    {
        if (!panel) return;
        var card = panel.transform.Find("OfficePauseCard");
        var canvas = panel.GetComponent<Canvas>();
        if (card == null || canvas == null) return;
        float scale = Screen.height / 1080f / Mathf.Max(.01f, canvas.rootCanvas.scaleFactor);
        card.localScale = Vector3.one * scale;
    }

    private static void StyleButton(Button button, Transform parent, TMP_FontAsset font, string caption, Vector2 position, bool primary)
    {
        button.transform.SetParent(parent, false);
        button.gameObject.SetActive(true);
        Rect((RectTransform)button.transform, Vector2.one * .5f, new Vector2(450f, 86f), position);
        foreach (Transform old in button.transform) old.gameObject.SetActive(false);
        var image = button.GetComponent<Image>();
        if (image == null) image = button.gameObject.AddComponent<Image>();
        button.targetGraphic = image;
        Label("Caption", button.transform, font, caption, 25f, new Vector2(360, 54), Vector2.zero, Paper);
        GameUiTheme.Current.StyleButton(button);
    }
}
