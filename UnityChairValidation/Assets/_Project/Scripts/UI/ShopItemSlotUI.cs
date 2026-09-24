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
        if (priceText != null) priceText.text = item.price + " đồng";
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
    }
}
