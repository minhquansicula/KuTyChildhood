using UnityEngine;

/// <summary>
/// Keeps the office report flow on a real desk mesh instead of separate demo props.
/// The same desk handles screen, documents and rewriting according to the current phase.
/// </summary>
public sealed class OfficeWorkstationInteractable : InteractableBase
{
    [SerializeField] private OfficeSceneController office;

    private OfficeSceneController Office => office != null ? office : OfficeSceneController.Instance;

    // The real desk is the single main workstation. The controller selects the
    // relevant task from the current narrative phase.
    private OfficeInteractionKind CurrentKind => OfficeInteractionKind.Laptop;

    private void Reset() => isOneTimeUse = false;

    public override string GetPromptText() => Office != null ? Office.PromptFor(CurrentKind) : "";

    protected override void OnInteract() => Office?.HandleInteraction(CurrentKind);
}
