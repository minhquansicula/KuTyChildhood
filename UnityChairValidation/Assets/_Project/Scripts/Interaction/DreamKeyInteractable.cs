using UnityEngine;

public class DreamKeyInteractable : InteractableBase
{
    private void Reset() { promptText = "[E] Cầm Chìa Khóa Ước Mơ"; isOneTimeUse = false; }
    public override string GetPromptText() =>
        QuestManager.Instance != null && QuestManager.Instance.CurrentStep == QuestStep.TakeDreamKey
            ? base.GetPromptText() : "";
    protected override void OnInteract()
    {
        if (QuestManager.Instance != null && QuestManager.Instance.TakeDreamKey())
            DialogueUI.Instance?.ShowText("Chìa khóa ấm trong lòng bàn tay. Mình đã sẵn sàng.", 2.5f);
    }
}
