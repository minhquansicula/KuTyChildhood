using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>The menu and in-game screens share the same authored artwork and typography.</summary>
[CreateAssetMenu(menuName = "KuTy/UI Theme")]
public sealed class GameUiTheme : ScriptableObject
{
    public Sprite menuFrame;
    public TMP_FontAsset font;
    public Color text = new Color(.23f, .12f, .055f);
    public Color muted = new Color(.40f, .26f, .12f);
    public Color accent = new Color(.55f, .29f, .075f);
    private static GameUiTheme current;
    private Sprite panelFrame;
    public static GameUiTheme Current
    {
        get
        {
            if (!current) current = Resources.Load<GameUiTheme>("GameUiTheme");
            return current;
        }
    }

    // A sliced view of the very same menu texture; no duplicate texture or changed importer.
    public Sprite PanelFrame
    {
        get
        {
            if (!panelFrame && menuFrame)
            {
                var r = menuFrame.rect;
                panelFrame = Sprite.Create(menuFrame.texture, r, Vector2.one * .5f, 100f,
                    0, SpriteMeshType.FullRect, new Vector4(r.width * .13f, r.height * .31f, r.width * .13f, r.height * .31f));
                panelFrame.name = "Shared menu parchment frame";
                panelFrame.hideFlags = HideFlags.HideAndDontSave;
            }
            return panelFrame;
        }
    }

    public void StyleButton(Button button)
    {
        var image = button.GetComponent<Image>();
        if (image)
        {
            image.sprite = menuFrame;
            image.type = Image.Type.Simple;
            image.color = Color.white;
            image.raycastTarget = true;
            button.targetGraphic = image;
        }
        button.transition = Selectable.Transition.ColorTint;
        var colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1f, .92f, .74f);
        colors.selectedColor = colors.highlightedColor;
        colors.pressedColor = new Color(.79f, .66f, .43f);
        colors.disabledColor = new Color(.67f, .62f, .52f, .65f);
        colors.fadeDuration = .12f;
        button.colors = colors;
        foreach (var label in button.GetComponentsInChildren<TMP_Text>(true))
        {
            label.font = font;
            label.color = text;
        }
    }
}
