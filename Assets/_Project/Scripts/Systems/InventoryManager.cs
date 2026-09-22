using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// InventoryManager — Quản lý túi đồ của người chơi.
/// 
/// Lưu trữ các vật phẩm đã mua từ tiệm tạp hóa.
/// </summary>
public class InventoryManager : MonoBehaviour
{
    // ========== SINGLETON ==========
    public static InventoryManager Instance { get; private set; }

    // ========== STATE ==========
    private List<ItemData> ownedItems = new List<ItemData>();

    /// <summary>Danh sách vật phẩm đã mua (read-only).</summary>
    public IReadOnlyList<ItemData> OwnedItems => ownedItems.AsReadOnly();

    // ========== EVENTS ==========
    /// <summary>Gọi khi thêm vật phẩm mới. Param: item vừa thêm</summary>
    public System.Action<ItemData> OnItemAdded;

    /// <summary>Gọi khi xóa vật phẩm. Param: item vừa xóa</summary>
    public System.Action<ItemData> OnItemRemoved;

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
    /// Thêm vật phẩm vào túi đồ.
    /// </summary>
    public void AddItem(ItemData item)
    {
        if (item == null)
        {
            Debug.LogWarning("[InventoryManager] Thêm item null, bỏ qua.");
            return;
        }

        ownedItems.Add(item);
        Debug.Log($"[InventoryManager] Thêm vào túi: {item.itemName}");
        OnItemAdded?.Invoke(item);
    }

    /// <summary>
    /// Xóa vật phẩm khỏi túi đồ.
    /// </summary>
    public bool RemoveItem(ItemData item)
    {
        if (ownedItems.Remove(item))
        {
            Debug.Log($"[InventoryManager] Xóa khỏi túi: {item.itemName}");
            OnItemRemoved?.Invoke(item);
            return true;
        }
        return false;
    }

    /// <summary>
    /// Kiểm tra có vật phẩm này trong túi không.
    /// </summary>
    public bool HasItem(ItemData item)
    {
        return ownedItems.Contains(item);
    }

    /// <summary>
    /// Kiểm tra có bất kỳ vật phẩm đặc biệt nào không (trigger ký ức).
    /// </summary>
    public bool HasSpecialItem()
    {
        foreach (var item in ownedItems)
        {
            if (item.isSpecialItem) return true;
        }
        return false;
    }

    /// <summary>
    /// Xóa toàn bộ túi đồ (dùng khi chơi lại).
    /// </summary>
    public void ClearInventory()
    {
        ownedItems.Clear();
        Debug.Log("[InventoryManager] Đã xóa toàn bộ túi đồ.");
    }
}
