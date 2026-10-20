using UnityEngine;

/// <summary>
/// Opens the Excel report task from the real desk mesh instead of a separate demo prop.
/// </summary>
public sealed class OfficeWorkstationInteractable : InteractableBase
{
    [SerializeField] private OfficeSceneController office;

    private OfficeSceneController Office => office != null ? office : OfficeSceneController.Instance;

    // The real desk starts Scene 1's Excel step before the physical print-and-deliver tasks.
    private OfficeInteractionKind CurrentKind => OfficeInteractionKind.Laptop;

    private void Reset() => isOneTimeUse = false;

    public override string GetPromptText() => Office != null ? Office.PromptFor(CurrentKind) : "";

    protected override void OnInteract() => Office?.HandleInteraction(CurrentKind);
}
