using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Screen-space layer of the ending film: letterbox, captions, sun rays, feathered eyelids (<see cref="SleepEyelidOverlay"/>),
/// veil / flash / dim, and the hold-to-skip indicator. It owns its own canvas so the final card
/// (<see cref="EndingUI"/>) stays independent.
/// </summary>
public sealed class EndingCinematicUI : MonoBehaviour
{
    private const float BarHeight = 132f;

    private Canvas canvas;
    private RectTransform root;
    private Camera cam;
    private Light sun;
    private TMP_FontAsset font;

    private RectTransform sunRoot, sunRays, sunGlow;
    private Image sunRaysImage, sunGlowImage;
    private SleepEyelidOverlay eyelids;
    private Image dim, veil, flash;
    private RectTransform barTop, barBottom;
    private TextMeshProUGUI caption;
    private Image skipRing;
    private TextMeshProUGUI skipLabel;
    private CanvasGroup skipGroup;
    private float barAmount;
    private Coroutine captionRoutine;
    private Texture2D rayTexture;
    private Sprite raySprite;

    public RectTransform Root => root;

    public void Build(Camera camera, Light sunLight, Sprite unusedPlate = null)
    {
        cam = camera;
        sun = sunLight;
        var theme = GameUiTheme.Current;
        font = theme != null && theme.font != null ? theme.font : TMP_Settings.defaultFontAsset;

        var canvasObject = new GameObject("EndingCinematicCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        canvasObject.transform.SetParent(transform, false);
        canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.overrideSorting = true;
        canvas.sortingOrder = 40;
        GameUiPresentation.ConfigureCanvas(canvas);
        root = (RectTransform)canvasObject.transform;

        BuildSun();
        dim = Fill("Dim", new Color(0.01f, .008f, .006f, 0f));
        eyelids = SleepEyelidOverlay.Create(root);
        eyelids.color = new Color(.03f, .02f, .015f, 1f);
        eyelids.SetClosure(0f);
        BuildBars();
        BuildCaption();
        flash = Fill("Flash", new Color(1f, .95f, .82f, 0f));
        veil = Fill("Veil", new Color(.99f, .97f, .92f, 1f));
        BuildSkip();
    }

    // ---------- construction ----------
    private Image Fill(string title, Color color)
    {
        var go = new GameObject(title, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(root, false);
        GameUiPresentation.Stretch((RectTransform)go.transform, 0f, 0f);
        var image = go.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private void BuildSun()
    {
        rayTexture = MakeRayTexture(384);
        raySprite = Sprite.Create(rayTexture, new Rect(0, 0, rayTexture.width, rayTexture.height), Vector2.one * .5f, 100f);
        var sunGo = new GameObject("SunAnchor", typeof(RectTransform));
        sunGo.transform.SetParent(root, false);
        sunRoot = (RectTransform)sunGo.transform;
        sunRoot.anchorMin = sunRoot.anchorMax = new Vector2(.5f, .5f);
        sunRoot.sizeDelta = Vector2.zero;

        var glow = new GameObject("SunGlow", typeof(RectTransform), typeof(Image));
        glow.transform.SetParent(sunRoot, false);
        sunGlow = (RectTransform)glow.transform;
        sunGlow.sizeDelta = new Vector2(1500f, 1500f);
        sunGlowImage = glow.GetComponent<Image>();
        sunGlowImage.sprite = InteractionVfx.SpriteFor(InteractionVfxKind.Dust);
        sunGlowImage.color = new Color(1f, .78f, .4f, 0f);
        sunGlowImage.raycastTarget = false;

        var rays = new GameObject("SunRays", typeof(RectTransform), typeof(Image));
        rays.transform.SetParent(sunRoot, false);
        sunRays = (RectTransform)rays.transform;
        sunRays.sizeDelta = new Vector2(3200f, 3200f);
        sunRaysImage = rays.GetComponent<Image>();
        sunRaysImage.sprite = raySprite;
        sunRaysImage.color = new Color(1f, .86f, .55f, 0f);
        sunRaysImage.raycastTarget = false;
    }

    private void BuildBars()
    {
        barTop = Bar("BarTop", true);
        barBottom = Bar("BarBottom", false);
    }

    private RectTransform Bar(string title, bool top)
    {
        var go = new GameObject(title, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(root, false);
        var rect = (RectTransform)go.transform;
        rect.anchorMin = new Vector2(0f, top ? 1f : 0f);
        rect.anchorMax = new Vector2(1f, top ? 1f : 0f);
        rect.pivot = new Vector2(.5f, top ? 1f : 0f);
        rect.sizeDelta = new Vector2(0f, 0f);
        rect.anchoredPosition = Vector2.zero;
        var image = go.GetComponent<Image>();
        image.color = Color.black;
        image.raycastTarget = false;
        return rect;
    }

    private void BuildCaption()
    {
        caption = OfficeUiStyle.Label("Caption", root, font, "", 36f, new Vector2(1500f, 90f), Vector2.zero,
            new Color(.98f, .94f, .82f, 0f));
        var rect = caption.rectTransform;
        rect.anchorMin = rect.anchorMax = new Vector2(.5f, 0f);
        rect.pivot = new Vector2(.5f, .5f);
        rect.anchoredPosition = new Vector2(0f, BarHeight * .5f);
        caption.fontStyle = FontStyles.Italic;
        caption.enableWordWrapping = true;
        caption.characterSpacing = 2f;
        caption.lineSpacing = 6f;
    }

    private void BuildSkip()
    {
        var go = new GameObject("Skip", typeof(RectTransform), typeof(CanvasGroup));
        go.transform.SetParent(root, false);
        var rect = (RectTransform)go.transform;
        rect.anchorMin = rect.anchorMax = new Vector2(1f, 0f);
        rect.pivot = new Vector2(1f, .5f);
        rect.anchoredPosition = new Vector2(-48f, BarHeight * .5f);
        rect.sizeDelta = new Vector2(380f, 60f);
        skipGroup = go.GetComponent<CanvasGroup>();
        skipGroup.alpha = 0f;

        var ringGo = new GameObject("Ring", typeof(RectTransform), typeof(Image));
        ringGo.transform.SetParent(rect, false);
        var ringRect = (RectTransform)ringGo.transform;
        ringRect.anchorMin = ringRect.anchorMax = new Vector2(1f, .5f);
        ringRect.pivot = new Vector2(1f, .5f);
        ringRect.sizeDelta = new Vector2(34f, 34f);
        ringRect.anchoredPosition = Vector2.zero;
        skipRing = ringGo.GetComponent<Image>();
        skipRing.sprite = GameUiPresentationDisc();
        skipRing.type = Image.Type.Filled;
        skipRing.fillMethod = Image.FillMethod.Radial360;
        skipRing.fillAmount = 0f;
        skipRing.color = new Color(.97f, .8f, .45f, .95f);
        skipRing.raycastTarget = false;

        skipLabel = OfficeUiStyle.Label("Text", rect, font, "Giữ Space để bỏ qua", 20f, new Vector2(320f, 40f),
            new Vector2(-52f, 0f), new Color(.9f, .86f, .74f, .8f));
        skipLabel.rectTransform.anchorMin = skipLabel.rectTransform.anchorMax = new Vector2(1f, .5f);
        skipLabel.rectTransform.pivot = new Vector2(1f, .5f);
        skipLabel.rectTransform.anchoredPosition = new Vector2(-48f, 0f);
        skipLabel.alignment = TextAlignmentOptions.Right;
    }

    private static Sprite discSprite;
    private static Sprite GameUiPresentationDisc()
    {
        if (discSprite != null) return discSprite;
        const int size = 64;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { hideFlags = HideFlags.HideAndDontSave };
        texture.wrapMode = TextureWrapMode.Clamp;
        var pixels = new Color32[size * size];
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), Vector2.one * (size - 1) * .5f);
                float ring = Mathf.Clamp01(3f - Mathf.Abs(d - 26f) * 1.1f);
                pixels[y * size + x] = new Color32(255, 255, 255, (byte)(ring * 255f));
            }
        texture.SetPixels32(pixels);
        texture.Apply(false, true);
        discSprite = Sprite.Create(texture, new Rect(0, 0, size, size), Vector2.one * .5f, 100f);
        discSprite.hideFlags = HideFlags.HideAndDontSave;
        return discSprite;
    }

    // ---------- textures ----------
    private static Texture2D MakeRayTexture(int size)
    {
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false, true)
        { name = "EndingSunRays", wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.DontSave };
        var pixels = new Color32[size * size];
        float half = size * .5f;
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = (x - half) / half, dy = (y - half) / half;
                float r = Mathf.Sqrt(dx * dx + dy * dy);
                float a = Mathf.Atan2(dy, dx);
                float ray = Mathf.PerlinNoise(a * 5.1f + 10f, .31f) * .62f + Mathf.PerlinNoise(a * 13.7f + 4f, 1.7f) * .38f;
                ray = Mathf.Pow(Mathf.Clamp01((ray - .28f) * 2.1f), 1.5f);
                float falloff = Mathf.Exp(-r * 2.6f) * (1f - Mathf.SmoothStep(.82f, 1f, r));
                float alpha = Mathf.Clamp01(ray * falloff * 1.8f + Mathf.Exp(-r * 9f) * .55f);
                pixels[y * size + x] = new Color32(255, 255, 255, (byte)(alpha * 255f));
            }
        texture.SetPixels32(pixels);
        texture.Apply(false, true);
        return texture;
    }

    // ---------- public control ----------
    public void SetVeil(float alpha) { SetAlpha(veil, alpha); }
    public void SetDim(float alpha) { SetAlpha(dim, alpha); }
    public void SetEyelids(float closure) { if (eyelids != null) eyelids.SetClosure(closure); }

    public void SetFlash(float alpha, Color? color = null)
    {
        if (color.HasValue) flash.color = new Color(color.Value.r, color.Value.g, color.Value.b, alpha);
        else SetAlpha(flash, alpha);
    }

    public void SetBars(float amount)
    {
        barAmount = Mathf.Clamp01(amount);
        float h = BarHeight * barAmount * (root.rect.height > 0f ? root.rect.height / 1080f : 1f);
        barTop.sizeDelta = new Vector2(0f, h);
        barBottom.sizeDelta = new Vector2(0f, h);
        float captionAlpha = caption.color.a;
        caption.rectTransform.anchoredPosition = new Vector2(0f, Mathf.Max(46f, h * .5f));
        skipGroup.GetComponent<RectTransform>().anchoredPosition = new Vector2(-48f, Mathf.Max(46f, h * .5f));
        caption.alpha = captionAlpha;
    }

    public void SetSunRays(float alpha)
    {
        SetAlpha(sunRaysImage, alpha * .45f);
        SetAlpha(sunGlowImage, alpha * .5f);
    }

    public void ShowSkip(bool visible, float progress)
    {
        skipGroup.alpha = Mathf.MoveTowards(skipGroup.alpha, visible ? 1f : 0f, Time.unscaledDeltaTime * 4f);
        skipRing.fillAmount = Mathf.Clamp01(progress);
    }

    public void HideSkip() { skipGroup.alpha = 0f; }

    public void ShowCaption(string text, float fadeIn = .7f, float hold = 2.6f, float fadeOut = .65f)
    {
        if (captionRoutine != null) StopCoroutine(captionRoutine);
        captionRoutine = StartCoroutine(CaptionRoutine(text, fadeIn, hold, fadeOut));
    }

    public void HideCaption()
    {
        if (captionRoutine != null) StopCoroutine(captionRoutine);
        captionRoutine = null;
        caption.alpha = 0f;
    }

    private IEnumerator CaptionRoutine(string text, float fadeIn, float hold, float fadeOut)
    {
        caption.text = text;
        float t = 0f;
        while (t < fadeIn) { t += Time.unscaledDeltaTime; caption.alpha = Mathf.SmoothStep(0f, 1f, t / fadeIn); yield return null; }
        caption.alpha = 1f;
        yield return new WaitForSecondsRealtime(hold);
        t = 0f;
        while (t < fadeOut) { t += Time.unscaledDeltaTime; caption.alpha = 1f - Mathf.SmoothStep(0f, 1f, t / fadeOut); yield return null; }
        caption.alpha = 0f;
        captionRoutine = null;
    }

    /// <summary>Per-frame sun projection: rays and glow follow the sun through the lens.</summary>
    public void UpdateSun(float visibility)
    {
        if (cam == null || sun == null) return;
        Vector3 world = cam.transform.position - sun.transform.forward * 600f;
        Vector3 vp = cam.WorldToViewportPoint(world);
        float facing = vp.z > 0f ? 1f : 0f;
        Vector2 size = root.rect.size;
        sunRoot.anchoredPosition = new Vector2((vp.x - .5f) * size.x, (vp.y - .5f) * size.y);
        float edge = Mathf.Clamp01(1.4f - Mathf.Max(Mathf.Abs(vp.x - .5f), Mathf.Abs(vp.y - .5f)) * 1.5f);
        SetSunRays(visibility * facing * edge);
        sunRays.localRotation = Quaternion.Euler(0f, 0f, Time.unscaledTime * 1.6f);
    }

    public void HideEverything()
    {
        HideCaption();
        SetEyelids(0f);
        SetSunRays(0f);
        SetVeil(0f);
        SetDim(0f);
        SetFlash(0f);
        HideSkip();
    }

    private static void SetAlpha(Graphic graphic, float alpha)
    {
        var c = graphic.color;
        c.a = Mathf.Clamp01(alpha);
        graphic.color = c;
    }

    private void OnDestroy()
    {
        if (raySprite != null) Destroy(raySprite);
        if (rayTexture != null) Destroy(rayTexture);
    }
}
