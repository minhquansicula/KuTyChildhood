using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShopItemSlotUI : MonoBehaviour
{
    [SerializeField] private Image icon;
    [SerializeField] private TextMeshProUGUI itemNameText;
    [SerializeField] private TextMeshProUGUI priceText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private TextMeshProUGUI buttonText;
    [SerializeField] private Button buyButton;
    public void Setup(ItemData item, System.Action<ItemData> onBuy)
    {
        if (itemNameText != null) itemNameText.text = item.itemName;
        if (priceText != null) priceText.text = GameUiPresentation.Money(item.price);
        if (descriptionText != null) descriptionText.text = item.description;
        if (icon != null) { icon.sprite = item.icon; icon.enabled = item.icon != null; }
        bool owned = InventoryManager.Instance != null && InventoryManager.Instance.HasItem(item);
        if (buttonText != null) buttonText.text = owned ? "Đã mua" : "Mua";
        if (buyButton != null)
        {
            buyButton.onClick.RemoveAllListeners();
            buyButton.interactable = !owned;
            buyButton.onClick.AddListener(() => onBuy?.Invoke(item));
        }
        StylePresentation(item);
    }

    private void StylePresentation(ItemData item)
    {
        foreach (Transform child in transform)
            if (child.name == "SlotBorder") child.gameObject.SetActive(false);
        if (buyButton)
            foreach (Transform child in buyButton.transform)
                if (child.name == "Border") child.gameObject.SetActive(false);
        GameUiPresentation.Card(GetComponent<Image>());
        if (icon)
        {
            OfficeUiStyle.Rect(icon.rectTransform, new Vector2(.5f, 1), new Vector2(64, 64), new Vector2(0, -48));
            icon.preserveAspect = true; icon.raycastTarget = false;
        }
        if (!item.icon) BuildItemIllustration(item.itemName);
        Place(itemNameText, 27, new Vector2(0, -103), new Vector2(390, 40), GameUiPresentation.Paper);
        Place(priceText, 25, new Vector2(0, -144), new Vector2(390, 34), GameUiPresentation.Gold);
        Place(descriptionText, 21, new Vector2(0, -193), new Vector2(380, 60), GameUiPresentation.Muted);
        if (buyButton)
        {
            OfficeUiStyle.Rect((RectTransform)buyButton.transform, new Vector2(.5f, 0), new Vector2(280, 64), new Vector2(0, 44));
            GameUiPresentation.Button(buyButton);
        }
    }

    private void BuildItemIllustration(string itemName)
    {
        var visual = new GameObject("ItemIllustration", typeof(RectTransform));
        visual.transform.SetParent(transform, false);
        OfficeUiStyle.Rect((RectTransform)visual.transform, new Vector2(.5f, 1), new Vector2(70, 70), new Vector2(0, -47));
        if (itemName.ToLowerInvariant().Contains("kẹo"))
        {
            var stick = OfficeUiStyle.Panel("LollipopStick", visual.transform, new Vector2(5, 36), new Vector2(0, -21), GameUiPresentation.Paper);
            stick.transform.localRotation = Quaternion.Euler(0, 0, -18);
            GameUiPresentation.Disc("Candy", visual.transform, new Vector2(0, 9), 46, new Color(.87f, .43f, .35f));
            GameUiPresentation.Disc("CandySwirl", visual.transform, new Vector2(0, 9), 29, GameUiPresentation.Paper);
            GameUiPresentation.Disc("CandyCenter", visual.transform, new Vector2(0, 9), 15, new Color(.87f, .43f, .35f));
        }
        else
        {
            var colors = new[] { new Color(.35f, .72f, .68f), new Color(.39f, .60f, .82f), GameUiPresentation.Gold };
            var positions = new[] { new Vector2(-18, -10), new Vector2(18, -10), new Vector2(0, 18) };
            for (int i = 0; i < 3; i++)
            {
                GameUiPresentation.Disc("Marble", visual.transform, positions[i], 31, colors[i]);
                GameUiPresentation.Disc("Glint", visual.transform, positions[i] + new Vector2(-6, 6), 8, GameUiPresentation.Paper);
            }
        }
    }

    private static void Place(TMP_Text label, float size, Vector2 position, Vector2 bounds, Color color)
    {
        if (!label) return;
        OfficeUiStyle.Rect(label.rectTransform, new Vector2(.5f, 1), bounds, position);
        GameUiPresentation.Text(label, size, color);
        label.alignment = TextAlignmentOptions.Center;
    }
}
