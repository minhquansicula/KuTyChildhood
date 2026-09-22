using UnityEngine;

/// <summary>
/// ItemData — ScriptableObject chứa dữ liệu vật phẩm trong shop.
/// 
/// Tạo asset bằng: Right-click trong Project → Create → KuTy → Item Data
/// Mỗi vật phẩm (kẹo dừa, con diều, v.v.) là 1 asset riêng, sửa trong Inspector.
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
    [Tooltip("Nếu true, mua vật phẩm này sẽ trigger mảnh ký ức (Niềm vui giản dị)")]
    public bool isSpecialItem = false;
}

/// <summary>
/// Enum phân loại vật phẩm.
/// </summary>
public enum ItemType
{
    Candy,      // Kẹo dừa, kẹo kéo
    Toy,        // Bi ve, đồ chơi nhỏ
    Special     // Vật phẩm đặc biệt (con diều giấy) — trigger ký ức
}
