using UnityEngine;
public class MarbleInteractable : InteractableBase
{
    [SerializeField] private MarbleShootingGame marbleGame;
    private void Reset() { promptText = "[E] Chơi bi"; isOneTimeUse = false; }
    public override string GetPromptText() => marbleGame == null || marbleGame.IsCompleted ||
        (QuestManager.Instance != null && !QuestManager.Instance.CanPlayMarbles) ? "" : base.GetPromptText();
    protected override void OnInteract() { if (marbleGame != null) marbleGame.StartGame(); }
}
