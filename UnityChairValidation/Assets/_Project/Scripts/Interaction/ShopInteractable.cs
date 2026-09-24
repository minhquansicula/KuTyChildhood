using UnityEngine;

/// <summary>
/// ShopInteractable — Vật thể tương tác quầy tạp hóa.
/// Khi player bấm E → mở Shop UI.
/// 
/// Setup: Gán lên quầy hàng, set Layer = "Interactable", cần Collider.
/// </summary>
public class ShopInteractable : InteractableBase
{
    private void Reset()
    {
        promptText = "[E] Mua hàng";
        isOneTimeUse = false; // Mua nhiều lần
    }

    protected override void OnInteract()
    {
        if (QuestManager.Instance != null && !QuestManager.Instance.CanShop) return;
        Debug.Log("[ShopInteractable] Mở tiệm tạp hóa!");

        if (ShopUI.Instance != null)
        {
            ShopUI.Instance.OpenShop();
        }
        else
        {
            Debug.LogError("[ShopInteractable] Không tìm thấy ShopUI!");
        }
    }
    public override string GetPromptText() => QuestManager.Instance != null && !QuestManager.Instance.CanShop
        ? "" : base.GetPromptText();
}
