using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// EndingUI — the final card of the ending.
///
/// With an <see cref="EndingDirector"/> in the scene the film plays first and calls
/// <see cref="BeginFinalReveal"/>: the card fades in over the 3D sunrise, the title sweeps in with light, the
/// message is typed, the player's own keepsakes and results count up, then the buttons appear.
/// Without a director (tests, older scenes) the card shows immediately over the illustrated backdrop.
/// </summary>
public class EndingUI : MonoBehaviour
{
    // ========== UI REFERENCES ==========
    [Header("References")]
    [SerializeField] private TextMeshProUGUI messageText;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Message")]
    [SerializeField]
    [TextArea(3, 6)]
    private string endingMessage = "Cơn mưa đêm ngoài ô cửa văn phòng đã tạnh.\nNhững việc nhà nhỏ bé mẹ dạy năm nào vẫn ở đó —\ntrở thành hơi ấm nâng đỡ con giữa những ngày trưởng thành nhiều giông bão.";

    // ========== RUNTIME STATE ==========
    private struct MemoryMote
    {
        public RectTransform rect;
        public Image image;
        public Vector2 basePos;
        public float speedY;
        public float swayAmp;
        public float swayFreq;
        public float phase;
        public float baseAlpha;
    }

    private readonly List<MemoryMote> motes = new List<MemoryMote>();
    private readonly List<RectTransform> keepsakeIcons = new List<RectTransform>();
    private RectTransform backgroundRect;
    private Image sunriseGlow;
    private CanvasGroup stageGroup;
    private CanvasGroup headerGroup;
    private CanvasGroup coreGroup;
    private CanvasGroup quoteGroup;
    private CanvasGroup statsGroup;
    private CanvasGroup buttonsGroup;
    private CanvasGroup skipHintGroup;
    private Image logoImage;
    private Image logoSweep;
    private TextMeshProUGUI coreLabel;
    private TextMeshProUGUI statsLabel;
    private TextMeshProUGUI honorificLabel;
    private TextMeshProUGUI playTimeLabel;
    private RectTransform cardRect;
    private bool cinematic;
    private bool revealStarted;
    private bool revealFinished;
    private Coroutine revealCoroutine;
    private static Sprite vignetteSprite;

    private static readonly Vector2 HeaderPos = new Vector2(0f, 236f);
    private static readonly Vector2 CorePos = new Vector2(0f, 72f);
    private static readonly Vector2 QuotePos = new Vector2(0f, -38f);
    private static readonly Vector2 StatsPos = new Vector2(0f, -156f);
    private static readonly Vector2 ButtonsPos = new Vector2(0f, -300f);

    // ========== LIFECYCLE ==========
    private void Start()
    {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        cinematic = FindObjectOfType<EndingDirector>() != null;

        var canvas = GetComponentInParent<Canvas>() ?? GetComponentInChildren<Canvas>(true);
        GameUiPresentation.ConfigureCanvas(canvas);
        if (GetComponent<MusicCreditsUI>() == null)
            gameObject.AddComponent<MusicCreditsUI>();

        BuildFinalCard(canvas);

        if (cinematic)
        {
            stageGroup.alpha = 0f;
            stageGroup.blocksRaycasts = false;
            stageGroup.interactable = false;
        }
        else
        {
            BeginFinalReveal();
        }
    }

    private void Update()
    {
        float t = Time.unscaledTime;

        if (backgroundRect != null)
        {
            float scale = 1.035f + Mathf.Sin(t * 0.18f) * 0.025f;
            backgroundRect.localScale = new Vector3(scale, scale, 1f);
            backgroundRect.anchoredPosition = new Vector2(Mathf.Sin(t * 0.14f) * 16f, Mathf.Cos(t * 0.11f) * 8f);
        }

        if (sunriseGlow != null)
        {
            float alpha = (cinematic ? 0.1f : 0.22f) + Mathf.Sin(t * 1.1f) * 0.04f;
            sunriseGlow.color = new Color(1f, 0.82f, 0.42f, alpha);
        }

        float dt = Time.unscaledDeltaTime;
        for (int i = 0; i < motes.Count; i++)
        {
            var m = motes[i];
            if (m.rect == null) continue;
            m.basePos.y += m.speedY * dt;
            if (m.basePos.y > 590f)
            {
                m.basePos.y = -590f;
                m.basePos.x = Random.Range(-940f, 940f);
            }
            float x = m.basePos.x + Mathf.Sin(t * m.swayFreq + m.phase) * m.swayAmp;
            m.rect.anchoredPosition = new Vector2(x, m.basePos.y);
            float twinkle = m.baseAlpha * (0.65f + 0.35f * Mathf.Sin(t * (m.swayFreq * 1.7f) + m.phase));
            Color c = m.image.color;
            c.a = twinkle;
            m.image.color = c;
            motes[i] = m;
        }

        // Keepsake icons breathe once they have popped in.
        if (revealFinished)
            for (int i = 0; i < keepsakeIcons.Count; i++)
            {
                if (keepsakeIcons[i] == null) continue;
                float s = 1f + Mathf.Sin(t * 1.6f + i * .7f) * .035f;
                keepsakeIcons[i].localScale = new Vector3(s, s, 1f);
            }

        if (revealStarted && !revealFinished && (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return) ||
            Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetMouseButtonDown(0)))
        {
            CompleteRevealImmediately();
        }

        FitCardToScreen(GetComponentInParent<Canvas>());
    }

    // ========== UI CONSTRUCTION ==========
    private void BuildFinalCard(Canvas canvas)
    {
        if (canvas == null) return;
        Transform root = canvas.transform;
        var theme = GameUiTheme.Current;
        TMP_FontAsset font = theme != null && theme.font != null ? theme.font : TMP_Settings.defaultFontAsset;

        // Reuse the scene's existing buttons so their wiring survives.
        Button playAgainBtn = null;
        Button quitBtn = null;
        foreach (var btn in GetComponentsInChildren<Button>(true))
        {
            if (btn.name.Contains("MusicCredits") || btn.name.Contains("CloseMusic")) continue;
            if (playAgainBtn == null) playAgainBtn = btn;
            else if (quitBtn == null) quitBtn = btn;
        }

        // Hide the loose text objects of the original scene.
        foreach (var oldText in GetComponentsInChildren<TMP_Text>(true))
        {
            if (oldText.transform.parent == root)
                oldText.gameObject.SetActive(false);
        }

        var stageObj = new GameObject("EndingStage", typeof(RectTransform), typeof(CanvasGroup));
        stageObj.transform.SetParent(root, false);
        stageObj.transform.SetAsFirstSibling();
        var stageRect = (RectTransform)stageObj.transform;
        GameUiPresentation.Stretch(stageRect, 0f, 0f);
        stageGroup = stageObj.GetComponent<CanvasGroup>();
        stageGroup.alpha = 1f;
        canvasGroup = stageGroup;

        if (!cinematic)
        {
            // Standalone: the illustrated dawn replaces the missing 3D set.
            var bgObj = new GameObject("BackgroundArt", typeof(RectTransform), typeof(Image));
            bgObj.transform.SetParent(stageRect, false);
            backgroundRect = (RectTransform)bgObj.transform;
            OfficeUiStyle.Rect(backgroundRect, Vector2.one * 0.5f, new Vector2(2040f, 1150f), Vector2.zero);
            var bgImage = bgObj.GetComponent<Image>();
            bgImage.raycastTarget = false;
            if (theme != null && theme.endingBackground != null)
            {
                bgImage.sprite = theme.endingBackground;
                bgImage.color = new Color(0.96f, 0.92f, 0.86f, 1f);
            }
            else bgImage.color = new Color(0.12f, 0.16f, 0.18f, 1f);
        }

        var vignetteObj = new GameObject("WarmVignette", typeof(RectTransform), typeof(Image));
        vignetteObj.transform.SetParent(stageRect, false);
        GameUiPresentation.Stretch((RectTransform)vignetteObj.transform, 0f, 0f);
        var vignetteImg = vignetteObj.GetComponent<Image>();
        vignetteImg.sprite = GetOrCreateVignetteSprite();
        vignetteImg.color = cinematic ? new Color(0.03f, 0.04f, 0.05f, 0.62f) : new Color(0.03f, 0.05f, 0.06f, 0.72f);
        vignetteImg.raycastTarget = false;

        sunriseGlow = GameUiPresentation.Disc("SunriseGlow", stageRect, new Vector2(0f, 60f), 1050f,
            new Color(1f, 0.82f, 0.42f, 0.14f));

        if (!cinematic)
        {
            var motesRoot = new GameObject("MemoryMotes", typeof(RectTransform)).transform;
            motesRoot.SetParent(stageRect, false);
            GameUiPresentation.Stretch((RectTransform)motesRoot, 0f, 0f);
            SpawnMemoryMotes(motesRoot, 28);
        }

        // Card
        var cardGlow = OfficeUiStyle.Panel("CardGlowBorder", stageRect, new Vector2(1156f, 836f),
            new Vector2(0f, 8f), new Color(0.94f, 0.76f, 0.43f, 0.20f));
        cardRect = cardGlow.rectTransform;
        var card = OfficeUiStyle.Panel("EpilogueCard", cardRect, new Vector2(1140f, 820f),
            Vector2.zero, new Color(0.04f, 0.06f, 0.055f, cinematic ? 0.8f : 0.88f));
        var innerTrim = OfficeUiStyle.Panel("InnerGoldTrim", card.transform, new Vector2(1110f, 790f),
            Vector2.zero, new Color(0.94f, 0.76f, 0.43f, 0.28f));
        OfficeUiStyle.Panel("InnerBody", innerTrim.transform, new Vector2(1106f, 786f),
            Vector2.zero, new Color(0.045f, 0.07f, 0.064f, cinematic ? 0.82f : 0.94f));

        CreateCornerOrnament(card.transform, font, new Vector2(-528f, 368f));
        CreateCornerOrnament(card.transform, font, new Vector2(528f, 368f));
        CreateCornerOrnament(card.transform, font, new Vector2(-528f, -368f));
        CreateCornerOrnament(card.transform, font, new Vector2(528f, -368f));

        // Header: kicker + logo + divider
        headerGroup = CreateSectionGroup(card.transform, "HeaderSection", HeaderPos, new Vector2(1040f, 240f));
        var kicker = OfficeUiStyle.Label("ChapterKicker", headerGroup.transform, font,
            "CHƯƠNG KẾT   ·   NGÀY MAI TRỜI LẠI SÁNG", 18f, new Vector2(900f, 32f),
            new Vector2(0f, 104f), new Color(0.94f, 0.78f, 0.48f, 0.92f));
        kicker.characterSpacing = 6f;
        CreateDiamond(headerGroup.transform, "KickerDiamondL", new Vector2(-335f, 104f), 11f);
        CreateDiamond(headerGroup.transform, "KickerDiamondR", new Vector2(335f, 104f), 11f);

        if (theme != null && theme.goldBrushBanner != null)
        {
            var bannerGlow = OfficeUiStyle.Panel("LogoBrushGlow", headerGroup.transform,
                new Vector2(620f, 120f), new Vector2(0f, 16f), new Color(1f, 0.88f, 0.55f, 0.16f));
            bannerGlow.sprite = theme.goldBrushBanner;
            bannerGlow.type = Image.Type.Simple;
            bannerGlow.preserveAspect = true;
        }

        Sprite logoSprite = theme != null ? (theme.endingTitleLogo != null ? theme.endingTitleLogo : theme.pauseLogo) : null;
        if (logoSprite != null)
        {
            logoImage = OfficeUiStyle.Panel("GameLogo", headerGroup.transform,
                new Vector2(460f, 148f), new Vector2(0f, 18f), Color.white);
            logoImage.sprite = logoSprite;
            logoImage.type = Image.Type.Filled;
            logoImage.fillMethod = Image.FillMethod.Horizontal;
            logoImage.fillOrigin = 0;
            logoImage.fillAmount = 1f;
            logoImage.preserveAspect = true;

            // A soft light that rides the leading edge of the logo while it is revealed.
            logoSweep = GameUiPresentation.Disc("LogoSweep", headerGroup.transform, new Vector2(-230f, 18f), 150f,
                new Color(1f, .9f, .6f, 0f));
            logoSweep.sprite = InteractionVfx.SpriteFor(InteractionVfxKind.Dust);
        }
        else
        {
            var fallbackTitle = OfficeUiStyle.Label("FallbackTitle", headerGroup.transform, font,
                "TRỞ VỀ TUỔI THƠ", 48f, new Vector2(880f, 70f), new Vector2(0f, 22f), GameUiPresentation.Gold);
            fallbackTitle.fontStyle = FontStyles.Bold;
        }

        OfficeUiStyle.Panel("DividerLeft", headerGroup.transform, new Vector2(240f, 2f),
            new Vector2(-240f, -72f), new Color(0.94f, 0.76f, 0.43f, 0.45f));
        OfficeUiStyle.Panel("DividerRight", headerGroup.transform, new Vector2(240f, 2f),
            new Vector2(240f, -72f), new Color(0.94f, 0.76f, 0.43f, 0.45f));
        var dividerLabel = OfficeUiStyle.Label("DividerTitle", headerGroup.transform, font,
            "CHÌA KHÓA KÝ ỨC", 18f, new Vector2(260f, 28f),
            new Vector2(0f, -72f), GameUiPresentation.Gold);
        dividerLabel.characterSpacing = 3f;
        CreateDiamond(headerGroup.transform, "DividerDiamondL", new Vector2(-150f, -72f), 9f);
        CreateDiamond(headerGroup.transform, "DividerDiamondR", new Vector2(150f, -72f), 9f);

        // Core message (typed)
        coreGroup = CreateSectionGroup(card.transform, "Stanza_CoreMessage", CorePos, new Vector2(1020f, 130f));
        coreLabel = OfficeUiStyle.Label("CoreMessageText", coreGroup.transform, font,
            "Cơn mưa đêm ngoài ô cửa văn phòng đã tạnh.\nNhững việc nhà nhỏ bé mẹ dạy năm nào vẫn ở đó —\ntrở thành hơi ấm nâng đỡ con giữa những ngày trưởng thành nhiều giông bão.",
            26f, new Vector2(1000f, 124f), Vector2.zero, GameUiPresentation.Paper);
        coreLabel.lineSpacing = 12f;
        messageText = coreLabel;

        // Dawn quote
        quoteGroup = CreateSectionGroup(card.transform, "Stanza_DawnQuote", QuotePos, new Vector2(980f, 76f));
        if (theme != null && theme.goldBrushBanner != null)
        {
            var quoteBrush = OfficeUiStyle.Panel("QuoteBrush", quoteGroup.transform,
                new Vector2(820f, 64f), Vector2.zero, new Color(0.95f, 0.76f, 0.38f, 0.22f));
            quoteBrush.sprite = theme.goldBrushBanner;
            quoteBrush.type = Image.Type.Simple;
            quoteBrush.preserveAspect = true;
        }
        var dawnQuote = OfficeUiStyle.Label("DawnQuoteText", quoteGroup.transform, font,
            "“ Sau này đi đâu làm gì, nhớ ăn cơm đúng bữa nhé.\nNhà mình luôn có một chỗ chờ con. ”", 25f, new Vector2(960f, 68f),
            Vector2.zero, new Color(0.98f, 0.84f, 0.48f, 1f));
        dawnQuote.fontStyle = FontStyles.Bold | FontStyles.Italic;

        // Personal results
        BuildStats(card.transform, font);

        // Buttons
        buttonsGroup = CreateSectionGroup(card.transform, "ButtonsSection", ButtonsPos, new Vector2(960f, 130f));
        if (playAgainBtn == null)
        {
            var img = OfficeUiStyle.Panel("PlayAgainButton", buttonsGroup.transform, new Vector2(390f, 116f), new Vector2(-235f, 0f), Color.white);
            playAgainBtn = img.gameObject.AddComponent<Button>();
            playAgainBtn.onClick.AddListener(OnPlayAgainClicked);
        }
        if (quitBtn == null)
        {
            var img = OfficeUiStyle.Panel("QuitButton", buttonsGroup.transform, new Vector2(390f, 116f), new Vector2(235f, 0f), Color.white);
            quitBtn = img.gameObject.AddComponent<Button>();
            quitBtn.onClick.AddListener(OnQuitClicked);
        }
        StyleWoodenActionButton(playAgainBtn, buttonsGroup.transform, font, "Chơi lại", new Vector2(-235f, 0f));
        StyleWoodenActionButton(quitBtn, buttonsGroup.transform, font, "Thoát", new Vector2(235f, 0f));

        // Hint
        skipHintGroup = CreateSectionGroup(stageRect, "SkipHint", new Vector2(0f, -500f), new Vector2(600f, 36f));
        OfficeUiStyle.Label("SkipText", skipHintGroup.transform, font,
            "Nhấn Space hoặc Chuột trái để hiện toàn bộ", 17f, new Vector2(580f, 30f),
            Vector2.zero, new Color(0.85f, 0.82f, 0.72f, 0.65f));
        skipHintGroup.gameObject.SetActive(false);
    }

    private void BuildStats(Transform card, TMP_FontAsset font)
    {
        statsGroup = CreateSectionGroup(card, "Stats_YourJourney", StatsPos, new Vector2(1040f, 150f));

        var names = new List<string>(EndingMemoryFrames.EarnedSpriteNames());
        float spacing = 66f;
        float startX = -(names.Count - 1) * spacing * .5f;
        for (int i = 0; i < names.Count; i++)
        {
            var sprite = Resources.Load<Sprite>("Scene2/MiniGames/" + names[i]);
            if (sprite == null) continue;
            var tile = OfficeUiStyle.Panel("Keepsake_" + names[i], statsGroup.transform, new Vector2(56f, 56f),
                new Vector2(startX + i * spacing, 46f), new Color(0.94f, 0.76f, 0.43f, 0.2f));
            var icon = OfficeUiStyle.Panel("Icon", tile.transform, new Vector2(44f, 44f), Vector2.zero, Color.white);
            icon.sprite = sprite;
            icon.type = Image.Type.Simple;
            icon.preserveAspect = true;
            keepsakeIcons.Add(tile.rectTransform);
            tile.rectTransform.localScale = Vector3.zero;
        }

        statsLabel = OfficeUiStyle.Label("PiecesText", statsGroup.transform, font, "", 26f, new Vector2(1000f, 40f),
            new Vector2(0f, -14f), GameUiPresentation.Paper);
        statsLabel.richText = true;
        honorificLabel = OfficeUiStyle.Label("Honorific", statsGroup.transform, font, "", 24f, new Vector2(1000f, 36f),
            new Vector2(0f, -52f), new Color(0.98f, 0.84f, 0.48f, 1f));
        honorificLabel.fontStyle = FontStyles.Bold | FontStyles.Italic;
        playTimeLabel = OfficeUiStyle.Label("PlayTime", statsGroup.transform, font, "", 17f, new Vector2(1000f, 28f),
            new Vector2(0f, -84f), new Color(0.78f, 0.78f, 0.7f, 0.8f));

        statsLabel.text = PiecesText(EndingSummary.Pieces);
        honorificLabel.text = "— " + EndingSummary.Honorific + " —";
        string time = EndingSummary.PlayTimeText;
        playTimeLabel.text = string.IsNullOrEmpty(time) ? "" : "Hành trình kéo dài " + time;
    }

    private static string PiecesText(int pieces)
    {
        return "Con đã gom được <b><color=#F8D27A>" + pieces + "</color></b> / " + EndingSummary.TotalPieces + " mảnh ký ức";
    }

    private static string ExtractCoreMessage(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return "Thế giới của người lớn luôn đầy những cơn bão.\nNhưng đứa trẻ bên trong bạn, cùng những ký ức tươi đẹp này,\nsẽ luôn là nơi trú ẩn an toàn nhất.";
        }
        string cleaned = raw.Trim();
        string suffix = "Ngày mai trời lại sáng.";
        if (cleaned.EndsWith(suffix))
            cleaned = cleaned.Substring(0, cleaned.Length - suffix.Length).Trim();
        return cleaned;
    }

    private static CanvasGroup CreateSectionGroup(Transform parent, string name, Vector2 anchoredPos, Vector2 size)
    {
        var obj = new GameObject(name, typeof(RectTransform), typeof(CanvasGroup));
        obj.transform.SetParent(parent, false);
        OfficeUiStyle.Rect((RectTransform)obj.transform, Vector2.one * 0.5f, size, anchoredPos);
        var cg = obj.GetComponent<CanvasGroup>();
        cg.alpha = 0f;
        return cg;
    }

    private static void CreateCornerOrnament(Transform parent, TMP_FontAsset font, Vector2 pos)
    {
        CreateDiamond(parent, "CornerDiamond", pos, 16f);
    }

    /// <summary>Ornament drawn as a rotated square: the Vietnamese UI font has no dingbat glyphs.</summary>
    private static void CreateDiamond(Transform parent, string title, Vector2 pos, float size)
    {
        var diamond = OfficeUiStyle.Panel(title, parent, new Vector2(size, size), pos, new Color(0.94f, 0.76f, 0.43f, 0.7f));
        diamond.sprite = null;
        diamond.type = Image.Type.Simple;
        diamond.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 45f);
    }

    private static void StyleWoodenActionButton(Button button, Transform parent, TMP_FontAsset font, string caption, Vector2 pos)
    {
        var theme = GameUiTheme.Current;
        button.transform.SetParent(parent, false);
        button.gameObject.SetActive(true);
        OfficeUiStyle.Rect((RectTransform)button.transform, Vector2.one * 0.5f, new Vector2(410f, 128f), pos);

        foreach (Transform child in button.transform)
            child.gameObject.SetActive(false);

        var image = button.GetComponent<Image>();
        if (image == null) image = button.gameObject.AddComponent<Image>();
        button.targetGraphic = image;

        var label = OfficeUiStyle.Label("Caption", button.transform, font, caption, 26f,
            new Vector2(310f, 52f), new Vector2(0f, -2f),
            theme != null ? theme.text : new Color(0.23f, 0.12f, 0.055f));
        label.fontStyle = FontStyles.Bold;
        label.enableAutoSizing = true;
        label.fontSizeMin = 20f;
        label.fontSizeMax = 26f;

        if (theme != null)
        {
            theme.StyleButton(button);
            if (theme.woodenButton != null)
            {
                image.sprite = theme.woodenButton;
                image.type = Image.Type.Simple;
                image.preserveAspect = true;
                image.raycastPadding = new Vector4(14f, 26f, 14f, 26f);
                label.color = new Color(0.23f, 0.12f, 0.055f);
            }
        }

        if (button.GetComponent<EndingButtonHover>() == null)
            button.gameObject.AddComponent<EndingButtonHover>();

        MenuButtonSfx.Attach(button);
    }

    private void SpawnMemoryMotes(Transform parent, int count)
    {
        motes.Clear();
        for (int i = 0; i < count; i++)
        {
            float size = Random.Range(10f, 34f);
            Vector2 startPos = new Vector2(Random.Range(-940f, 940f), Random.Range(-540f, 540f));
            float baseAlpha = Random.Range(0.10f, 0.36f);
            Color color = Color.Lerp(new Color(1f, 0.86f, 0.48f, baseAlpha), new Color(1f, 0.96f, 0.82f, baseAlpha), Random.value);
            var disc = GameUiPresentation.Disc("Mote_" + i, parent, startPos, size, color);
            motes.Add(new MemoryMote
            {
                rect = disc.rectTransform,
                image = disc,
                basePos = startPos,
                speedY = Random.Range(14f, 34f),
                swayAmp = Random.Range(12f, 38f),
                swayFreq = Random.Range(0.6f, 1.4f),
                phase = Random.Range(0f, Mathf.PI * 2f),
                baseAlpha = baseAlpha
            });
        }
    }

    private static Sprite GetOrCreateVignetteSprite()
    {
        if (vignetteSprite != null) return vignetteSprite;
        const int size = 128;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            name = "EndingWarmVignette",
            wrapMode = TextureWrapMode.Clamp,
            hideFlags = HideFlags.HideAndDontSave
        };
        Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
        float maxRadius = center.magnitude;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center) / maxRadius;
                float alpha = Mathf.SmoothStep(0.18f, 0.96f, dist);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }
        tex.Apply(false, true);
        vignetteSprite = Sprite.Create(tex, new Rect(0, 0, size, size), Vector2.one * 0.5f, 100f);
        vignetteSprite.hideFlags = HideFlags.HideAndDontSave;
        return vignetteSprite;
    }

    private void FitCardToScreen(Canvas canvas)
    {
        if (cardRect == null || canvas == null) return;
        var rootRect = (RectTransform)canvas.rootCanvas.transform;
        Vector2 available = rootRect.rect.size - new Vector2(64f, 56f);
        if (available.x <= 10f || available.y <= 10f) return;
        Vector2 size = cardRect.sizeDelta;
        float scale = Mathf.Clamp(Mathf.Min(available.x / size.x, available.y / size.y), 0.5f, 1f);
        cardRect.localScale = Vector3.one * scale;
    }

    // ========== FINAL REVEAL ==========
    /// <summary>Starts the card sequence. Called by the director, or immediately when no film exists.</summary>
    public void BeginFinalReveal()
    {
        if (revealStarted) return;
        revealStarted = true;
        if (stageGroup != null)
        {
            stageGroup.blocksRaycasts = true;
            stageGroup.interactable = true;
        }
        revealCoroutine = StartCoroutine(PlayRevealSequence());
    }

    private IEnumerator PlayRevealSequence()
    {
        if (skipHintGroup != null) { skipHintGroup.gameObject.SetActive(true); skipHintGroup.alpha = 1f; }

        // The card rises out of the sunrise.
        yield return FadeCanvasGroup(stageGroup, stageGroup.alpha, 1f, cinematic ? 1.1f : .7f);

        yield return RevealTitle();
        yield return new WaitForSecondsRealtime(.2f);

        yield return RevealSection(coreGroup, .75f, 12f);
        yield return new WaitForSecondsRealtime(.25f);

        yield return RevealSection(quoteGroup, .65f, 10f);
        yield return new WaitForSecondsRealtime(.25f);

        yield return RevealStats();
        yield return RevealSection(buttonsGroup, .55f, 10f);

        CompleteRevealImmediately();
    }

    private IEnumerator RevealTitle()
    {
        var rect = (RectTransform)headerGroup.transform;
        Vector2 target = HeaderPos;
        headerGroup.alpha = 1f;
        rect.anchoredPosition = target + Vector2.down * 14f;
        if (logoImage != null) logoImage.fillAmount = 0f;
        float duration = 1.5f, t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float e = Mathf.SmoothStep(0f, 1f, t / duration);
            rect.anchoredPosition = Vector2.Lerp(target + Vector2.down * 14f, target, e);
            if (logoImage != null)
            {
                logoImage.fillAmount = e;
                if (logoSweep != null)
                {
                    float x = Mathf.Lerp(-230f, 230f, e);
                    logoSweep.rectTransform.anchoredPosition = new Vector2(x, 18f);
                    var c = logoSweep.color;
                    c.a = Mathf.Sin(Mathf.Clamp01(t / duration) * Mathf.PI) * .85f;
                    logoSweep.color = c;
                }
            }
            yield return null;
        }
        rect.anchoredPosition = target;
        if (logoImage != null) logoImage.fillAmount = 1f;
        if (logoSweep != null) { var c = logoSweep.color; c.a = 0f; logoSweep.color = c; }
    }

    private IEnumerator TypeMessage(TextMeshProUGUI label, float charsPerSecond)
    {
        if (label == null) yield break;
        label.ForceMeshUpdate();
        int total = label.textInfo.characterCount;
        label.maxVisibleCharacters = 0;
        float shown = 0f;
        while (shown < total)
        {
            shown += Time.unscaledDeltaTime * charsPerSecond;
            label.maxVisibleCharacters = Mathf.Min(total, Mathf.FloorToInt(shown));
            yield return null;
        }
        label.maxVisibleCharacters = int.MaxValue;
    }

    private IEnumerator RevealStats()
    {
        statsGroup.alpha = 1f;
        statsLabel.alpha = 0f;
        honorificLabel.alpha = 0f;
        playTimeLabel.alpha = 0f;

        for (int i = 0; i < keepsakeIcons.Count; i++)
        {
            StartCoroutine(PopIcon(keepsakeIcons[i], i * .06f));
        }
        yield return new WaitForSecondsRealtime(keepsakeIcons.Count * .06f * .6f + .25f);

        statsLabel.text = PiecesText(0);
        yield return FadeText(statsLabel, 0f, 1f, .35f);
        float duration = 1.3f, t = 0f;
        int target = EndingSummary.Pieces;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float e = 1f - Mathf.Pow(1f - Mathf.Clamp01(t / duration), 3f);
            statsLabel.text = PiecesText(Mathf.RoundToInt(target * e));
            yield return null;
        }
        statsLabel.text = PiecesText(target);
        yield return FadeText(honorificLabel, 0f, 1f, .55f);
        yield return FadeText(playTimeLabel, 0f, 1f, .4f);
    }

    private static IEnumerator PopIcon(RectTransform rect, float delay)
    {
        yield return new WaitForSecondsRealtime(delay);
        if (rect == null) yield break;
        float t = 0f, duration = .45f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / duration);
            float overshoot = 1f + Mathf.Sin(k * Mathf.PI) * .28f;
            float s = Mathf.SmoothStep(0f, 1f, k) * overshoot;
            rect.localScale = new Vector3(s, s, 1f);
            yield return null;
        }
        rect.localScale = Vector3.one;
    }

    private static IEnumerator FadeText(TMP_Text text, float from, float to, float seconds)
    {
        float t = 0f;
        while (t < seconds)
        {
            t += Time.unscaledDeltaTime;
            text.alpha = Mathf.Lerp(from, to, Mathf.SmoothStep(0f, 1f, t / seconds));
            yield return null;
        }
        text.alpha = to;
    }

    private static IEnumerator FadeCanvasGroup(CanvasGroup cg, float from, float to, float duration)
    {
        if (cg == null) yield break;
        float elapsed = 0f;
        cg.alpha = from;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
            cg.alpha = Mathf.Lerp(from, to, t);
            yield return null;
        }
        cg.alpha = to;
    }

    private static IEnumerator RevealSection(CanvasGroup cg, float duration, float slideOffset)
    {
        if (cg == null) yield break;
        var rect = (RectTransform)cg.transform;
        Vector2 targetPos = rect.anchoredPosition;
        Vector2 startPos = targetPos + Vector2.down * slideOffset;
        float elapsed = 0f;
        cg.alpha = 0f;
        rect.anchoredPosition = startPos;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
            cg.alpha = t;
            rect.anchoredPosition = Vector2.Lerp(startPos, targetPos, t);
            yield return null;
        }

        cg.alpha = 1f;
        rect.anchoredPosition = targetPos;
    }

    private void CompleteRevealImmediately()
    {
        if (revealFinished) return;
        revealFinished = true;
        if (revealCoroutine != null)
        {
            StopCoroutine(revealCoroutine);
            revealCoroutine = null;
        }
        StopAllCoroutines();

        if (stageGroup != null) stageGroup.alpha = 1f;
        Snap(headerGroup, HeaderPos);
        if (logoImage != null) logoImage.fillAmount = 1f;
        if (logoSweep != null) { var c = logoSweep.color; c.a = 0f; logoSweep.color = c; }
        Snap(coreGroup, CorePos);
        if (coreLabel != null) coreLabel.maxVisibleCharacters = int.MaxValue;
        Snap(quoteGroup, QuotePos);
        Snap(statsGroup, StatsPos);
        foreach (var icon in keepsakeIcons) if (icon != null) icon.localScale = Vector3.one;
        if (statsLabel != null) { statsLabel.alpha = 1f; statsLabel.text = PiecesText(EndingSummary.Pieces); }
        if (honorificLabel != null) honorificLabel.alpha = 1f;
        if (playTimeLabel != null) playTimeLabel.alpha = 1f;
        Snap(buttonsGroup, ButtonsPos);

        if (skipHintGroup != null)
        {
            skipHintGroup.alpha = 0f;
            skipHintGroup.gameObject.SetActive(false);
        }
    }

    private static void Snap(CanvasGroup cg, Vector2 targetPos)
    {
        if (cg == null) return;
        cg.alpha = 1f;
        ((RectTransform)cg.transform).anchoredPosition = targetPos;
    }

    // ========== BUTTON HANDLERS ==========

    /// <summary>Chơi lại — quay về Main Menu.</summary>
    public void OnPlayAgainClicked()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.ReturnToMainMenu();
        else
            UnityEngine.SceneManagement.SceneManager.LoadScene(SceneNames.MainMenu);
    }

    /// <summary>Thoát game.</summary>
    public void OnQuitClicked()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.QuitGame();
        else
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}

/// <summary>Hiệu ứng phóng nhẹ khi rê chuột vào nút bảng gỗ trên màn hình kết thúc.</summary>
public sealed class EndingButtonHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private Vector3 baseScale = Vector3.one;
    private bool hovered;

    private void Awake()
    {
        baseScale = transform.localScale;
    }

    public void OnPointerEnter(PointerEventData eventData) => hovered = true;
    public void OnPointerExit(PointerEventData eventData) => hovered = false;

    private void OnDisable()
    {
        hovered = false;
        transform.localScale = baseScale;
    }

    private void Update()
    {
        Vector3 target = hovered ? baseScale * 1.055f : baseScale;
        transform.localScale = Vector3.Lerp(transform.localScale, target, Time.unscaledDeltaTime * 12f);
    }
}
