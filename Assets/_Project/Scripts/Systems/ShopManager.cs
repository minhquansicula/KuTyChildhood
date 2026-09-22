using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ShopManager — Quản lý logic mua bán ở tiệm tạp hóa.
/// 
/// Kiểm tra tiền → trừ tiền → thêm vào inventory → check special item.
/// </summary>
public class ShopManager : MonoBehaviour
{
    // ========== SINGLETON ==========
    public static ShopManager Instance { get; private set; }

    // ========== SETTINGS ==========
    [Header("Shop Items")]
    [SerializeField] private List<ItemData> shopItems = new List<ItemData>();

    /// <summary>Danh sách vật phẩm bán trong shop (read-only).</summary>
    public IReadOnlyList<ItemData> ShopItems => shopItems.AsReadOnly();

    // ========== EVENTS ==========
    /// <summary>Gọi khi mua thành công. Param: item đã mua</summary>
    public System.Action<ItemData> OnPurchaseSuccess;

    /// <summary>Gọi khi mua thất bại (không đủ tiền). Param: item muốn mua</summary>
    public System.Action<ItemData> OnPurchaseFailed;

    // ========== LIFECYCLE ==========
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    // ========== PUBLIC METHODS ==========

    /// <summary>
    /// Mua vật phẩm. Kiểm tra tiền → trừ tiền → thêm vào inventory.
    /// Trả về true nếu mua thành công.
    /// </summary>
    public bool PurchaseItem(ItemData item)
    {
        if (item == null)
        {
            Debug.LogWarning("[ShopManager] Item null, bỏ qua.");
            return false;
        }

        // Kiểm tra đã mua chưa (mỗi item mua 1 lần)
        if (InventoryManager.Instance != null && InventoryManager.Instance.HasItem(item))
        {
            Debug.Log($"[ShopManager] Đã mua {item.itemName} rồi!");
            OnPurchaseFailed?.Invoke(item);
            return false;
        }

        // Kiểm tra đủ tiền
        if (CurrencyManager.Instance == null || !CurrencyManager.Instance.SpendMoney(item.price))
        {
            Debug.Log($"[ShopManager] Không đủ tiền mua {item.itemName} (giá: {item.price})");
            OnPurchaseFailed?.Invoke(item);
            return false;
        }

        // Thêm vào inventory
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.AddItem(item);
        }

        Debug.Log($"[ShopManager] Đã mua: {item.itemName}");
        OnPurchaseSuccess?.Invoke(item);

        // Nếu là vật phẩm đặc biệt → trigger mảnh ký ức
        if (item.isSpecialItem)
        {
            Debug.Log($"[ShopManager] {item.itemName} là vật phẩm đặc biệt! Trigger ký ức...");
            if (MemoryCollectionManager.Instance != null)
            {
                MemoryCollectionManager.Instance.CollectMemory(MemoryType.SimpleJoy);
            }
        }

        return true;
    }

    /// <summary>
    /// Kiểm tra có thể mua vật phẩm không (đủ tiền + chưa mua).
    /// </summary>
    public bool CanPurchase(ItemData item)
    {
        if (item == null) return false;

        // Đã mua rồi?
        if (InventoryManager.Instance != null && InventoryManager.Instance.HasItem(item))
            return false;

        // Đủ tiền?
        if (CurrencyManager.Instance == null || !CurrencyManager.Instance.HasEnoughMoney(item.price))
            return false;

        return true;
    }
}
