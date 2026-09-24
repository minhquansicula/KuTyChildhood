using UnityEngine;

/// <summary>
/// ItemData — ScriptableObject chứa dữ liệu vật phẩm trong shop.
/// 
/// Tạo asset bằng: Right-click trong Project → Create → KuTy → Item Data
/// Mỗi vật phẩm là một asset riêng. Cốt truyện hiện dùng Kẹo mút và Hũ bi ve.
/// </summary>
[CreateAssetMenu(fileName = "NewItem", menuName = "KuTy/Item Data")]
public class ItemData : ScriptableObject
{
    [Header("Thông tin cơ bản")]
    public string itemName = "Vật phẩm mới";

    [TextArea(2, 4)]
    public string description = "Mô tả vật phẩm...";

    public Sprite icon;                 // Hình vẽ 2D hiển thị trong shop/inventory

    [Header("Giá cả")]
    public int price = 1;               // Giá mua (đơn vị: đồng)

    [Header("Phân loại")]
    public ItemType itemType = ItemType.Candy;

    [Header("Đặc biệt")]
    [Tooltip("Dành cho mở rộng nội dung; QuestManager hiện cấp ký ức sau khi mua đủ hai món cốt truyện.")]
    public bool isSpecialItem = false;
}

/// <summary>
/// Enum phân loại vật phẩm.
/// </summary>
public enum ItemType
{
    Candy,
    Toy,
    Special
}
