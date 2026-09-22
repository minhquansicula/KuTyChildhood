using UnityEngine;

/// <summary>
/// DishInteractable — Vật thể tương tác bồn rửa chén.
/// Khi player bấm E → bắt đầu mini-game rửa chén.
/// 
/// Setup: Gán lên bồn rửa, set Layer = "Interactable", cần Collider.
/// </summary>
public class DishInteractable : InteractableBase
{
    [Header("Mini-game Reference")]
    [SerializeField] private DishWashingGame dishWashingGame;

    private void Reset()
    {
        // Giá trị mặc định khi thêm component
        promptText = "[E] Rửa chén";
        isOneTimeUse = true;
    }

    protected override void OnInteract()
    {
        Debug.Log("[DishInteractable] Bắt đầu rửa chén!");

        if (dishWashingGame != null)
        {
            dishWashingGame.StartGame();
        }
        else
        {
            Debug.LogError("[DishInteractable] Chưa gán DishWashingGame reference!");
        }
    }
}
