using UnityEngine;

public class OfficeInteractable : InteractableBase
{
    [SerializeField] private OfficeSceneController office;
    [SerializeField] private OfficeInteractionKind kind;
    private void Reset() { isOneTimeUse = false; }
    public override string GetPromptText() => office != null ? office.PromptFor(kind) : "";
    protected override void OnInteract() { office?.HandleInteraction(kind); }
}
