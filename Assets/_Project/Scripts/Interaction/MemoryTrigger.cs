using UnityEngine;

/// <summary>
/// MemoryTrigger — Vật kỷ niệm ở Act1 (căn nhà cũ).
/// Khi player bấm E → xuyên không vào thế giới ký ức (Act2).
/// 
/// Hiệu ứng: Fade vàng ấm (giống ánh nắng chiều).
/// 
/// Setup: Gán lên vật kỷ niệm (con lật đật, cuốn tập cũ...),
///        set Layer = "Interactable", cần Collider.
/// </summary>
public class MemoryTrigger : InteractableBase
{
    [Header("Transition Settings")]
    [SerializeField] private Color fadeColor = new Color(1f, 0.85f, 0.4f); // Vàng ấm
    [SerializeField] private float fadeDuration = 2f;

    [Header("Optional")]
    [SerializeField] private string dialogueBeforeTransition = "Cái này... sao mình nhớ nó quá...";
    [SerializeField] private float dialogueDelay = 1.5f;

    private void Reset()
    {
        promptText = "[E] Chạm vào";
        isOneTimeUse = true;
    }

    protected override void OnInteract()
    {
        Debug.Log("[MemoryTrigger] Chạm vật kỷ niệm → xuyên không!");

        // Lock player
        var player = FindObjectOfType<FirstPersonController>();
        if (player != null) player.LockMovement(true);

        var interaction = FindObjectOfType<PlayerInteraction>();
        if (interaction != null) interaction.LockInteraction(true);

        // Hiện dialogue (nếu có)
        if (!string.IsNullOrEmpty(dialogueBeforeTransition) && DialogueUI.Instance != null)
        {
            DialogueUI.Instance.ShowText(dialogueBeforeTransition, dialogueDelay);
        }

        // Chờ rồi chuyển scene
        StartCoroutine(TransitionCoroutine());
    }

    private System.Collections.IEnumerator TransitionCoroutine()
    {
        // Chờ dialogue hiện xong
        if (!string.IsNullOrEmpty(dialogueBeforeTransition))
        {
            yield return new WaitForSeconds(dialogueDelay + 0.5f);
        }

        // Fade vàng ấm → load Act2
        if (SceneLoader.Instance != null)
        {
            SceneLoader.Instance.LoadSceneWithColor("Act2_MemoryWorld", fadeColor, fadeDuration);
        }

        // Update game state
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetGameState(GameState.Act2_MemoryWorld);
        }
    }
}
