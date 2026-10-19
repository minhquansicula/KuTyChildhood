using UnityEngine;

public enum OfficeMicroInteractionKind
{
    Clock,
    WaterCooler,
    PrinterPaper,
    ColdCoffee,
    ChildhoodMarble
}

/// <summary>
/// A short, optional office interaction. These never gate the main story flow.
/// </summary>
public sealed class OfficeMicroInteractable : InteractableBase
{
    [SerializeField] private OfficeSceneController office;
    [SerializeField] private OfficeMicroInteractionKind kind;
    [SerializeField, Min(0f)] private float cooldownSeconds = 1.25f;
    private float nextUseTime;

    private OfficeSceneController Office => office != null ? office : OfficeSceneController.Instance;

    public override string GetPromptText()
    {
        if ((isOneTimeUse && hasBeenUsed) || Time.time < nextUseTime) return string.Empty;
        return Office != null ? Office.PromptForMicro(kind) : string.Empty;
    }

    protected override void OnInteract()
    {
        Office?.HandleMicroInteraction(kind);
        nextUseTime = Time.time + cooldownSeconds;
    }
}
