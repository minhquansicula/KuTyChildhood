/// <summary>
/// IInteractable — Interface chung cho mọi vật thể có thể tương tác được.
/// 
/// Bất kỳ vật nào muốn player tương tác (bấm E) đều phải implement interface này.
/// PlayerInteraction dùng Raycast để tìm vật có IInteractable → gọi Interact().
/// 
/// Ví dụ các class implement:
/// - DishInteractable (bồn rửa chén)
/// - MarbleInteractable (chỗ chơi bi)
/// - ShopInteractable (quầy tạp hóa)
/// - MemoryTrigger (vật kỷ niệm ở Act1)
/// </summary>
public interface IInteractable
{
    /// <summary>
    /// Trả về text hiển thị khi player nhìn vào vật.
    /// VD: "[E] Rửa chén", "[E] Chơi bi", "[E] Mua hàng"
    /// </summary>
    string GetPromptText();

    /// <summary>
    /// Được gọi khi player bấm phím E khi đang nhìn vào vật này.
    /// Implement logic tương tác tại đây.
    /// </summary>
    void Interact();
}
