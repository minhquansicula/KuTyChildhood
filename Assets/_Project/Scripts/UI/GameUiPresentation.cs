using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Shared visual vocabulary for readable gameplay UI and the existing parchment menu.</summary>
public static class GameUiPresentation
{
    public static readonly Color Ink = new Color(.055f, .105f, .095f, .96f);
    public static readonly Color Paper = new Color(.97f, .94f, .84f);
    public static readonly Color Muted = new Color(.75f, .79f, .72f);
    public static readonly Color Gold = new Color(.94f, .76f, .43f);
    public static readonly Color Success = new Color(.66f, .85f, .65f);
    private static readonly CultureInfo Vietnamese = CultureInfo.GetCultureInfo("vi-VN");
    private static Sprite disc;
    public static string Money(int amount) => amount.ToString("N0", Vietnamese) + " đồng";

    public static void ConfigureCanvas(Canvas canvas)
    {
        if (!canvas || canvas.renderMode == RenderMode.WorldSpace) return;
        var root = canvas.rootCanvas;
        var scaler = root.GetComponent<CanvasScaler>();
        if (!scaler) scaler = root.gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = .5f;
    }

    public static RectTransform SafeRoot(Transform parent, string name)
    {
        var obj = new GameObject(name, typeof(RectTransform), typeof(GameUiSafeArea));
        obj.transform.SetParent(parent, false);
        return (RectTransform)obj.transform;
    }

    public static void Text(TMP_Text label, float size, Color color, bool wrapping = true)
    {
        if (!label) return;
        if (GameUiTheme.Current && GameUiTheme.Current.font) label.font = GameUiTheme.Current.font;
        label.fontSize = size;
        label.fontSizeMin = Mathf.Min(size, 20f);
        label.fontSizeMax = size;
        label.enableAutoSizing = false;
        label.enableWordWrapping = wrapping;
        label.color = color;
        label.raycastTarget = false;
        label.richText = true;
        label.lineSpacing = 3f;
    }

    public static void Card(Image image, bool parchment = false)
    {
        if (!image) return;
        image.sprite = parchment && GameUiTheme.Current ? GameUiTheme.Current.PanelFrame : OfficeUiStyle.Rounded;
        image.type = Image.Type.Sliced;
        image.pixelsPerUnitMultiplier = parchment ? 5f : 1f;
        image.color = parchment ? Color.white : Ink;
        image.raycastTarget = false;
    }

    public static Image Panel(string name, Transform parent, Vector2 anchor, Vector2 size, Vector2 position)
    {
        var image = OfficeUiStyle.Panel(name, parent, size, position, Ink);
        var rect = image.rectTransform;
        rect.anchorMin = rect.anchorMax = rect.pivot = anchor;
        rect.anchoredPosition = position;
        return image;
    }

    public static void Stretch(RectTransform rect, float horizontal, float vertical)
    {
        rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(horizontal, vertical);
        rect.offsetMax = new Vector2(-horizontal, -vertical);
    }

    public static void Button(Button button)
    {
        if (!button) return;
        if (GameUiTheme.Current) GameUiTheme.Current.StyleButton(button);
        foreach (var label in button.GetComponentsInChildren<TMP_Text>(true))
        {
            Text(label, 25f, GameUiTheme.Current ? GameUiTheme.Current.text : Color.black);
            label.enableAutoSizing = true;
            label.fontSizeMin = 21f;
            label.alignment = TextAlignmentOptions.Center;
            Stretch(label.rectTransform, 24f, 10f);
        }
        MenuButtonSfx.Attach(button);
    }

    public static GameObject ModalRoot(GameObject panel, string name, int order)
    {
        var canvas = panel.GetComponentInParent<Canvas>(true);
        if (!canvas) return null;
        ConfigureCanvas(canvas);
        var obj = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster));
        obj.transform.SetParent(canvas.rootCanvas.transform, false);
        Stretch((RectTransform)obj.transform, 0, 0);
        var modalCanvas = obj.GetComponent<Canvas>();
        modalCanvas.overrideSorting = true;
        modalCanvas.sortingOrder = order;
        var dim = new GameObject("Dimmer", typeof(RectTransform), typeof(Image));
        dim.transform.SetParent(obj.transform, false);
        Stretch((RectTransform)dim.transform, 0, 0);
        dim.GetComponent<Image>().color = new Color(.025f, .04f, .035f, .76f);
        panel.transform.SetParent(obj.transform, false);
        return obj;
    }

    public static Image Disc(string name, Transform parent, Vector2 position, float size, Color color)
    {
        if (!disc)
        {
            var texture = new Texture2D(32, 32, TextureFormat.RGBA32, false) { hideFlags = HideFlags.HideAndDontSave };
            for (int y = 0; y < 32; y++) for (int x = 0; x < 32; x++)
                texture.SetPixel(x, y, new Color(1, 1, 1, Mathf.Clamp01(15.5f - Vector2.Distance(new Vector2(x, y), Vector2.one * 15.5f))));
            texture.Apply(false, true);
            disc = Sprite.Create(texture, new Rect(0, 0, 32, 32), Vector2.one * .5f, 100);
            disc.hideFlags = HideFlags.HideAndDontSave;
        }
        var image = OfficeUiStyle.Panel(name, parent, Vector2.one * size, position, color);
        image.sprite = disc; image.type = Image.Type.Simple;
        return image;
    }
}
