using UnityEngine;

/// <summary>
/// Uses the intro seating transition once, then hands later sit/rest interactions to
/// OfficeSceneController without adding a second input reader.
/// </summary>
public sealed class OfficeBlockoutChairInteractable : InteractableBase
{
    [SerializeField] private OfficeIntroController intro;
    [SerializeField] private OfficeSceneController office;

    public override string GetPromptText()
    {
        if (intro != null && !intro.GameplayHandoffComplete)
            return intro.GetChairPrompt();
        return office != null ? office.PromptFor(OfficeInteractionKind.Chair) : string.Empty;
    }

    protected override void OnInteract()
    {
        if (intro != null && !intro.GameplayHandoffComplete) intro.TrySitAtDesk();
        else office?.HandleInteraction(OfficeInteractionKind.Chair);
    }
}
