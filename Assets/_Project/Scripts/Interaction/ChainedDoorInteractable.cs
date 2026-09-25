using UnityEngine;

public class ChainedDoorInteractable : InteractableBase
{
    [SerializeField] private GameObject chainsVisual;
    // [SerializeField] private Color transitionColor = Color.white;
    // [SerializeField] private float transitionSeconds = 1.8f;
    private void Reset() { promptText = "[E] Dùng chìa khóa mở cửa"; isOneTimeUse = false; }
    public override string GetPromptText()
    {
        var quests = QuestManager.Instance;
        if (quests == null || quests.CurrentStep == QuestStep.Ending) return "";
        return quests.CurrentStep == QuestStep.UnlockFrontDoor
            ? "[E] Dùng Chìa Khóa Ước Mơ" : "[E] Kiểm tra cánh cửa bị xích";
    }
    protected override void OnInteract()
    {
        var quests = QuestManager.Instance;
        if (quests == null) return;
        if (!quests.UnlockFrontDoor())
        {
            DialogueUI.Instance?.ShowText("Những sợi xích lạnh ngắt. Mình cần một chìa khóa.", 2.5f);
            return;
        }
        GameManager.Instance?.AcquireInput(this);
        if (chainsVisual != null) chainsVisual.SetActive(false);
        DialogueUI.Instance?.ShowText("Chiếc chìa khóa xoay trong ổ. Những sợi xích vỡ ra...", 3f);
        StartCoroutine(Finish());
    }
    private System.Collections.IEnumerator Finish()
    {
        yield return new WaitForSecondsRealtime(3f);
        DialogueUI.Instance?.ShowText("Tạm dừng tại đây (Chưa chuyển qua Scene 3).", 3f);
        // SceneLoader.Instance?.LoadSceneWithColor(SceneNames.Act3_OutSide, transitionColor, transitionSeconds);
        GameManager.Instance?.ReleaseInput(this);
    }
}
