using TMPro;
using UnityEngine;
using UnityEngine.UI;

public partial class HUDController
{
    private GameObject objectiveCard;
    private CanvasGroup presentationGroup;
    private TMP_Text memorySummary;

    private void BuildPresentation()
    {
        var canvas = GetComponentInParent<Canvas>() ?? GetComponentInChildren<Canvas>(true);
        if (!canvas) return;
        GameUiPresentation.ConfigureCanvas(canvas);
        var safe = GameUiPresentation.SafeRoot(canvas.transform, "GameplaySafeArea");
        presentationGroup = safe.gameObject.AddComponent<CanvasGroup>();
        presentationGroup.interactable = presentationGroup.blocksRaycasts = false;
        foreach (var legacy in canvas.GetComponentsInChildren<TMP_Text>(true))
        {
            if (string.IsNullOrEmpty(legacy.text)) continue;
            if (legacy.text == "Mẹ" || legacy.text == "Niềm vui" || legacy.text == "Tự do")
                legacy.gameObject.SetActive(false);
            if (!legacy.text.StartsWith("WASD:")) continue;
            var controls = GameUiPresentation.Panel("ControlsCard", safe, new Vector2(.5f, 0), new Vector2(1100, 50), Vector2.zero);
            legacy.transform.SetParent(controls.transform, false);
            GameUiPresentation.Stretch(legacy.rectTransform, 18, 7);
            GameUiPresentation.Text(legacy, 21, GameUiPresentation.Muted, false);
            legacy.alignment = TextAlignmentOptions.Center;
            legacy.text = "<color=#F0C26E>WASD</color>  Di chuyển    ·    <color=#F0C26E>CHUỘT</color>  Quan sát    ·    <color=#F0C26E>E</color>  Tương tác    ·    <color=#F0C26E>ESC</color>  Tạm dừng";
        }

        if (gameObject.scene.name != SceneNames.Act1)
        {
            var memories = GameUiPresentation.Panel("MemoryCollectionCard", safe, new Vector2(0, 1),
                new Vector2(456, 116), Vector2.zero);
            var heading = OfficeUiStyle.Label("MemoryHeading", memories.transform, ObjectiveFont,
                "NHỮNG MẢNH KÝ ỨC", 19, new Vector2(350, 28), new Vector2(-28, 32), GameUiPresentation.Gold);
            GameUiPresentation.Text(heading, 19, GameUiPresentation.Gold, false);
            heading.alignment = TextAlignmentOptions.Left;
            memorySummary = OfficeUiStyle.Label("MemoryCount", memories.transform, ObjectiveFont,
                "0 / 3", 19, new Vector2(64, 28), new Vector2(177, 32), GameUiPresentation.Muted);
            GameUiPresentation.Text(memorySummary, 19, GameUiPresentation.Muted, false);
            string[] names = { "Mẹ", "Niềm vui", "Tự do" };
            for (int i = 0; memoryIcons != null && i < Mathf.Min(3, memoryIcons.Length); i++)
            {
                var chip = OfficeUiStyle.Panel("MemoryChip" + i, memories.transform, new Vector2(132, 44),
                    new Vector2(-144 + i * 144, -18), new Color(.15f, .22f, .19f));
                var icon = memoryIcons[i];
                if (icon)
                {
                    foreach (var old in icon.GetComponentsInChildren<TMP_Text>(true)) old.gameObject.SetActive(false);
                    icon.transform.SetParent(chip.transform, false);
                    OfficeUiStyle.Rect(icon.rectTransform, Vector2.one * .5f, new Vector2(12, 12), new Vector2(-50, 0));
                    icon.sprite = OfficeUiStyle.Rounded;
                    icon.type = Image.Type.Sliced;
                    icon.raycastTarget = false;
                }
                var label = OfficeUiStyle.Label("MemoryName", chip.transform, ObjectiveFont, names[i], 22,
                    new Vector2(98, 34), new Vector2(12, 0), GameUiPresentation.Paper);
                GameUiPresentation.Text(label, 22, GameUiPresentation.Paper, false);
            }
            memoryUncollectedColor = new Color(.40f, .50f, .44f, 1f);
            memoryCollectedColor = GameUiPresentation.Gold;

            if (moneyText)
            {
                var wallet = GameUiPresentation.Panel("WalletCard", safe, Vector2.one, new Vector2(270, 82), Vector2.zero);
                GameUiPresentation.Card(wallet, true);
                moneyText.transform.SetParent(wallet.transform, false);
                GameUiPresentation.Stretch(moneyText.rectTransform, 24, 13);
                GameUiPresentation.Text(moneyText, 29, GameUiTheme.Current ? GameUiTheme.Current.text : Color.black, false);
                moneyText.alignment = TextAlignmentOptions.Center;
                moneyText.enableAutoSizing = true;
                moneyText.fontSizeMin = 23;
            }
        }

        if (objectiveText)
        {
            var card = GameUiPresentation.Panel("SharedObjectiveCard", safe, Vector2.one, new Vector2(460, 190), new Vector2(0, -108));
            objectiveCard = card.gameObject;
            objectiveCard.SetActive(objectiveText.gameObject.activeSelf);
            objectiveText.transform.SetParent(card.transform, false);
            GameUiPresentation.Stretch(objectiveText.rectTransform, 24, 20);
            GameUiPresentation.Text(objectiveText, 25, GameUiPresentation.Paper);
            objectiveText.alignment = TextAlignmentOptions.TopLeft;
        }
        if (promptPanel)
        {
            promptPanel.transform.SetParent(safe, false);
            OfficeUiStyle.Rect((RectTransform)promptPanel.transform, new Vector2(.5f, 0), new Vector2(740, 64), new Vector2(0, 42));
            GameUiPresentation.Card(promptPanel.GetComponent<Image>());
            if (promptText)
            {
                GameUiPresentation.Stretch(promptText.rectTransform, 24, 10);
                GameUiPresentation.Text(promptText, 26, GameUiPresentation.Paper);
                promptText.alignment = TextAlignmentOptions.Center;
            }
        }
        if (crosshairImage) crosshairImage.rectTransform.sizeDelta = Vector2.one * 5f;
    }

    private void LateUpdate()
    {
        if (presentationGroup) presentationGroup.alpha = (GameManager.Instance != null && GameManager.Instance.InputBlocked) ||
            (MarbleAimUI.Instance != null && MarbleAimUI.Instance.IsVisible) ? 0f : 1f;
    }

    private void UpdateMemorySummary()
    {
        if (!memorySummary) return;
        int count = 0;
        for (int i = 0; i < 3; i++)
            if (MemoryCollectionManager.Instance && MemoryCollectionManager.Instance.HasCollected((MemoryType)i)) count++;
        memorySummary.text = count + " / 3";
    }
}
