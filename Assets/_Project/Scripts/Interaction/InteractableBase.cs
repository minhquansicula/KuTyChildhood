using UnityEngine;

/// <summary>
/// InteractableBase — Abstract base class cho các vật thể tương tác.
/// 
/// Cung cấp chức năng chung:
/// - Prompt text (hiển thị khi nhìn vào)
/// - One-time use option (chỉ tương tác 1 lần)
/// - Override OnInteract() ở class con để implement logic riêng
/// 
/// Tất cả vật tương tác nên kế thừa class này thay vì implement IInteractable trực tiếp.
/// </summary>
public abstract class InteractableBase : MonoBehaviour, IInteractable
{
    // ========== SETTINGS ==========
    [Header("Interaction Settings")]
    [SerializeField] protected string promptText = "[E] Tương tác";
    [SerializeField] protected bool isOneTimeUse = false;

    // ========== STATE ==========
    protected bool hasBeenUsed = false;

    // ========== IInteractable IMPLEMENTATION ==========

    public virtual string GetPromptText()
    {
        if (isOneTimeUse && hasBeenUsed)
            return ""; // Không hiện prompt nếu đã dùng rồi

        return promptText;
    }

    public void Interact()
    {
        if (isOneTimeUse && hasBeenUsed)
        {
            Debug.Log($"[{GetType().Name}] Đã tương tác rồi, bỏ qua.");
            return;
        }

        OnInteract();

        if (isOneTimeUse)
        {
            hasBeenUsed = true;
        }
    }

    // ========== ABSTRACT METHOD ==========

    /// <summary>
    /// Override ở class con để implement logic tương tác cụ thể.
    /// Được gọi khi player bấm E lên vật này.
    /// </summary>
    protected abstract void OnInteract();

    // ========== HELPER ==========

    /// <summary>
    /// Reset trạng thái "đã dùng" (nếu cần cho gameplay).
    /// </summary>
    public void ResetInteraction()
    {
        hasBeenUsed = false;
    }
}
