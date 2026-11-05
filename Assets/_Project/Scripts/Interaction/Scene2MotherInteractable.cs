using UnityEngine;

/// <summary>Uses normal raycast focus, not proximity through walls.</summary>
public sealed class Scene2MotherInteractable : InteractableBase
{
    [SerializeField] private Scene2MotherCompanion companion;
    public void Configure(Scene2MotherCompanion value)
    {
        companion = value;
        isOneTimeUse = false;
    }
    public override string GetPromptText() => companion && companion.CanTalk ? "[E] Nói chuyện với mẹ" : "";
    protected override void OnInteract()
    {
        if (companion) companion.BeginConversation();
    }
}
