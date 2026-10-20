using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Music attribution remains accessible in every build from the main menu.</summary>
public sealed class MusicCreditsUI : MonoBehaviour
{
    private GameObject panel;
    private TMP_FontAsset font;

    private void Start()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null) canvas = GetComponentInChildren<Canvas>();
        if (canvas == null) return;
        var existing = canvas.GetComponentInChildren<TMP_Text>(true);
        font = existing != null ? existing.font : TMP_Settings.defaultFontAsset;
        var launch = Button(canvas.transform, "MusicCreditsButton", "Nhạc / Ghi công", new Vector2(240, 44));
        var launchRect = (RectTransform)launch.transform;
        launchRect.anchorMin = launchRect.anchorMax = Vector2.zero;
        launchRect.pivot = Vector2.zero;
        launchRect.anchoredPosition = new Vector2(20, 20);

        panel = new GameObject("MusicCreditsPanel", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(canvas.transform, false);
        var rect = (RectTransform)panel.transform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        panel.GetComponent<Image>().color = new Color(.06f, .08f, .08f, .98f);
        var text = Label(panel.transform, "Attribution", new Vector2(860, 510));
        text.enableAutoSizing = true;
        text.fontSizeMin = 16;
        text.fontSizeMax = 26;
        text.text = "<b>ÂM NHẠC</b>\n\n" +
            "Childhood — Scott Buckley\nMenu chính\n\n" +
            "Reverie — Scott Buckley\nThư mẹ và đoạn kết\n\n" +
            "Released under CC BY 4.0\nwww.scottbuckley.com.au\ncreativecommons.org/licenses/by/4.0/\n\n" +
            "Countryside Summer (Inaka no Natsuyasumi)\nYoshinori Tanaka — Nhà và đường làng\n" +
            "OpenTracks / DOVA-SYNDROME\nopentracks.com/bgm/detail/19313\n\n" +
            "Nhạc được chuyển mã và chuyển âm lượng khi phát trong game.";
        var back = Button(panel.transform, "CloseMusicCredits", "Quay lại", new Vector2(240, 48));
        ((RectTransform)back.transform).anchoredPosition = new Vector2(0, -310);
        back.onClick.AddListener(() => panel.SetActive(false));
        launch.onClick.AddListener(() => { panel.SetActive(true); panel.transform.SetAsLastSibling(); back.Select(); });
        MenuButtonSfx.Attach(launch);
        MenuButtonSfx.Attach(back);
        panel.SetActive(false);
    }

    private void Update()
    {
        if (panel != null && panel.activeSelf && Input.GetKeyDown(KeyCode.Escape)) panel.SetActive(false);
    }

    private TextMeshProUGUI Label(Transform parent, string name, Vector2 size)
    {
        var obj = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        obj.transform.SetParent(parent, false);
        ((RectTransform)obj.transform).sizeDelta = size;
        var text = obj.GetComponent<TextMeshProUGUI>();
        text.font = font;
        text.fontSize = 24;
        text.alignment = TextAlignmentOptions.Center;
        text.color = new Color(.95f, .91f, .8f);
        text.raycastTarget = false;
        return text;
    }

    private Button Button(Transform parent, string name, string caption, Vector2 size)
    {
        var obj = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        obj.transform.SetParent(parent, false);
        ((RectTransform)obj.transform).sizeDelta = size;
        obj.GetComponent<Image>().color = new Color(.19f, .24f, .22f);
        Label(obj.transform, "Text", size - new Vector2(12, 4)).text = caption;
        return obj.GetComponent<Button>();
    }
}
