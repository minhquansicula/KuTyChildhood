using UnityEngine;
public class DishInteractable : InteractableBase
{
    [SerializeField] private DishWashingGame dishWashingGame;
    private void Reset() { promptText = "[E] Rửa chén"; isOneTimeUse = false; }
    public override string GetPromptText() => dishWashingGame == null || dishWashingGame.IsCompleted ||
        (QuestManager.Instance != null && !QuestManager.Instance.CanWashDishes) ? "" : base.GetPromptText();
    protected override void OnInteract() { if (dishWashingGame != null) dishWashingGame.StartGame(); }
}
