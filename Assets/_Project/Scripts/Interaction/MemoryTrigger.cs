using UnityEngine;

public class MemoryTrigger : InteractableBase
{
    [SerializeField] private StoryPrologueController prologue;
    [SerializeField] private Color fadeColor = new Color(1f, 0.85f, 0.4f);
    [SerializeField] private float fadeDuration = 1.2f;
    private void Reset() { promptText = "[E] Mở cánh cửa tuổi thơ"; isOneTimeUse = true; }
    public override string GetPromptText() => prologue != null && !prologue.IsComplete ? "" : base.GetPromptText();
    protected override void OnInteract()
    {
        if (prologue != null && !prologue.IsComplete) return;
        GameManager.Instance?.AcquireInput(this);
        if (DialogueUI.Instance != null)
            DialogueUI.Instance.ShowDialogue("Căn nhà cũ", "Chỉ cần mở cửa... những buổi chiều ấy vẫn đang chờ mình.", 2f, Transition);
        else Transition();
    }
    private void Transition()
    {
        SceneLoader.Instance?.LoadSceneWithColor(SceneNames.Act2_Home, fadeColor, fadeDuration);
        GameManager.Instance?.ReleaseInput(this);
    }
}
