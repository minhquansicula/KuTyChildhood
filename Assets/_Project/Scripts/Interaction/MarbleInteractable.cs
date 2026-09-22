using UnityEngine;

/// <summary>
/// MarbleInteractable — Vật thể tương tác chỗ chơi bi.
/// Khi player bấm E → bắt đầu mini-game bắn bi.
/// 
/// Setup: Gán lên chỗ chơi bi, set Layer = "Interactable", cần Collider.
/// </summary>
public class MarbleInteractable : InteractableBase
{
    [Header("Mini-game Reference")]
    [SerializeField] private MarbleShootingGame marbleGame;

    private void Reset()
    {
        promptText = "[E] Chơi bi";
        isOneTimeUse = true;
    }

    protected override void OnInteract()
    {
        Debug.Log("[MarbleInteractable] Bắt đầu chơi bi!");

        if (marbleGame != null)
        {
            marbleGame.StartGame();
        }
        else
        {
            Debug.LogError("[MarbleInteractable] Chưa gán MarbleShootingGame reference!");
        }
    }
}
