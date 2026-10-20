using UnityEngine;
using UnityEngine.SceneManagement;

public class ChainedDoorInteractable : InteractableBase
{
    [SerializeField] private GameObject chainsVisual;
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
        GameManager.Instance?.ReleaseInput(this);
        if (SceneLoader.Instance != null)
            SceneLoader.Instance.LoadSceneWithColor(SceneNames.Act_Ending, Color.white, 1.8f);
        else SceneManager.LoadScene(SceneNames.Act_Ending);
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        GameManager.Instance?.ReleaseInput(this);
    }
}
