using UnityEngine;

public class JournalInteractable : InteractableBase
{
    private void Reset() { promptText = "[E] Đọc nhật ký"; isOneTimeUse = false; }
    public override string GetPromptText()
    {
        if (QuestManager.Instance != null && QuestManager.Instance.CurrentStep == QuestStep.ReturnToJournal)
            return "[E] Ghép ba mảnh ký ức";
        return base.GetPromptText();
    }
    protected override void OnInteract()
    {
        var quests = QuestManager.Instance;
        if (quests == null) return;
        if (quests.CurrentStep == QuestStep.ReadJournal)
        {
            quests.ReadJournal();
            JournalUI.Instance?.Open();
            DialogueUI.Instance?.ShowText("Mẹ: Quân đi học về rồi đấy à, cất cặp rồi phụ mẹ rửa chén nhé.", 4f);
        }
        else if (quests.CurrentStep == QuestStep.ReturnToJournal)
        {
            quests.CraftDreamKey();
            JournalUI.Instance?.Open();
            DialogueUI.Instance?.ShowText("Ba mảnh ký ức đã hợp thành Chìa Khóa Ước Mơ.", 3f);
        }
        else JournalUI.Instance?.Open();
    }
}
