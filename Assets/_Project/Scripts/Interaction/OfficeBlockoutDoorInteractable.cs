using UnityEngine;

/// <summary>Routes the existing Blockout door through the shared PlayerInteraction raycast.</summary>
public sealed class OfficeBlockoutDoorInteractable : InteractableBase
{
    [SerializeField] private OfficeIntroController intro;

    public override string GetPromptText() => intro != null ? intro.GetDoorPrompt() : string.Empty;

    protected override void OnInteract()
    {
        intro?.TryOpenDoor();
    }
}
