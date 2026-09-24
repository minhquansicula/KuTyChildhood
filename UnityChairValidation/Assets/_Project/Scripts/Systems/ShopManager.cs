using System.Collections.Generic;
using UnityEngine;

public enum PurchaseFailure { None, InvalidItem, MissingSystems, AlreadyOwned, InsufficientMoney, Busy, QuestLocked }

public class ShopManager : MonoBehaviour
{
    public static ShopManager Instance { get; private set; }
    [SerializeField] private List<ItemData> shopItems = new List<ItemData>();
    public IReadOnlyList<ItemData> ShopItems => shopItems;
    public PurchaseFailure LastFailure { get; private set; }
    public event System.Action<ItemData> OnPurchaseSuccess;
    public event System.Action<ItemData> OnPurchaseFailed;
    private bool purchasing;
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }
    private void OnDestroy() { if (Instance == this) Instance = null; }
    public PurchaseFailure CheckPurchase(ItemData item)
    {
        if (purchasing) return PurchaseFailure.Busy;
        if (QuestManager.Instance != null && !QuestManager.Instance.CanShop) return PurchaseFailure.QuestLocked;
        if (item == null || item.price <= 0 || !shopItems.Contains(item)) return PurchaseFailure.InvalidItem;
        if (CurrencyManager.Instance == null || InventoryManager.Instance == null) return PurchaseFailure.MissingSystems;
        if (InventoryManager.Instance.HasItem(item)) return PurchaseFailure.AlreadyOwned;
        if (!CurrencyManager.Instance.HasEnoughMoney(item.price)) return PurchaseFailure.InsufficientMoney;
        return PurchaseFailure.None;
    }
    public bool CanPurchase(ItemData item) => CheckPurchase(item) == PurchaseFailure.None;
    public bool PurchaseItem(ItemData item)
    {
        LastFailure = CheckPurchase(item);
        if (LastFailure != PurchaseFailure.None)
        {
            OnPurchaseFailed?.Invoke(item);
            AudioManager.Instance?.PlaySFX("purchase_fail");
            return false;
        }
        purchasing = true;
        try
        {
            if (!CurrencyManager.Instance.SpendMoney(item.price)) return false;
            InventoryManager.Instance.AddItem(item);
            AudioManager.Instance?.PlaySFX("purchase");
            OnPurchaseSuccess?.Invoke(item);
            return true;
        }
        finally { purchasing = false; }
    }
}
