using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>A blocking result with explicit next actions; the mini-game still owns all state.</summary>
public sealed class MiniGameResultCard
{
    private readonly GameObject root;
    private readonly TMP_Text heading;
    private readonly TMP_Text body;
    private readonly TMP_Text kicker;
    private readonly Button primary;
    private readonly Button secondary;
    private readonly Button close;

    public MiniGameResultCard(Transform parent, TMP_FontAsset font)
    {
        root = new GameObject("MiniGameResult", typeof(RectTransform), typeof(Image));
        root.transform.SetParent(parent, false);
        GameUiPresentation.Stretch((RectTransform)root.transform, 0, 0);
        root.GetComponent<Image>().color = new Color(.025f, .06f, .05f, .94f);
        var card = OfficeUiStyle.Panel("ResultCard", root.transform, new Vector2(850, 490), Vector2.zero,
            new Color(.96f, .95f, .89f));
        card.gameObject.AddComponent<MiniGamePanelFit>();
        OfficeUiStyle.Panel("Accent", card.transform, new Vector2(72, 4), new Vector2(0, 195), GameUiPresentation.Gold);
        kicker = OfficeUiStyle.Label("ResultKicker", card.transform, font, "", 18, new Vector2(730, 32), new Vector2(0, 157), new Color(.31f, .40f, .33f));
        heading = OfficeUiStyle.Label("ResultTitle", card.transform, font, "", 38, new Vector2(750, 66), new Vector2(0, 96), new Color(.08f, .25f, .19f));
        heading.fontStyle = FontStyles.Bold;
        body = OfficeUiStyle.Label("ResultBody", card.transform, font, "", 25, new Vector2(730, 105), new Vector2(0, 0), new Color(.23f, .29f, .25f));
        body.enableWordWrapping = true;
        primary = CreateButton(card.transform, font, "Retry", new Vector2(-185, -115), new Vector2(345, 68), new Color(.10f, .35f, .25f));
        secondary = CreateButton(card.transform, font, "Relaxed", new Vector2(185, -115), new Vector2(345, 68), new Color(.22f, .32f, .31f));
        close = CreateButton(card.transform, font, "Close", new Vector2(0, -198), new Vector2(480, 46), new Color(.78f, .80f, .72f));
        close.GetComponentInChildren<TMP_Text>().color = new Color(.13f, .25f, .19f);
        root.SetActive(false);
    }

    public void Show(bool success, string title, string message, Action retry = null, Action relaxed = null, Action leave = null)
    {
        kicker.text = success ? "BẢNG TÍNH ĐÃ ĐƯỢC KIỂM TRA" : "CÓ THỂ THỬ LẠI · KHÔNG MẤT DỮ LIỆU";
        heading.text = title;
        body.text = message;
        Bind(primary, "THỬ LẠI · 60 GIÂY", retry);
        Bind(secondary, "CHƠI THƯ THẢ", relaxed);
        Bind(close, "ĐÓNG BẢNG TÍNH  [ESC]", leave);
        root.SetActive(true);
        root.transform.SetAsLastSibling();
        if (EventSystem.current) EventSystem.current.SetSelectedGameObject(retry != null ? primary.gameObject : null);
    }

    public void Hide() => root.SetActive(false);

    private static void Bind(Button button, string caption, Action action)
    {
        button.gameObject.SetActive(action != null);
        button.GetComponentInChildren<TMP_Text>().text = caption;
        button.onClick.RemoveAllListeners();
        if (action != null) button.onClick.AddListener(() => action());
    }

    private static Button CreateButton(Transform parent, TMP_FontAsset font, string name, Vector2 position, Vector2 size, Color color)
    {
        var image = OfficeUiStyle.Panel(name, parent, size, position, color);
        image.raycastTarget = true;
        var button = image.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        OfficeUiStyle.Label("Caption", image.transform, font, "", 23, size - new Vector2(24, 8), Vector2.zero, Color.white);
        MenuButtonSfx.Attach(button);
        return button;
    }
}
