using UnityEngine;

public class SimpleInteractable : InteractableBase
{
    private string customPrompt = "[E] Tương tác";

    public void SetPrompt(string prompt)
    {
        customPrompt = prompt;
        promptText = prompt;
    }

    private void Start()
    {
        promptText = customPrompt;
        isOneTimeUse = false;
    }

    protected override void OnInteract()
    {
        Debug.Log("Interacted with " + gameObject.name);
        // Có thể thêm logic mở cửa tủ ở đây sau
    }
}
